#load "../../../scripts/ComputationExpressions.fsx"
#load "../../../scripts/Exception.fsx"

open System
open System.IO
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Text.Json
open Common
open Common.CE

let usage =
    """
YouTrack REST helper

Usage:
  dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\youtrack.fsx" -- me
  dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\youtrack.fsx" -- get ISSUE-ID
  dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\youtrack.fsx" -- search "for: me #Unresolved" [--top 20] [--skip 0]
  dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\youtrack.fsx" -- create --project-id 0-0 --summary "Title" [--description "Details"]
  dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\youtrack.fsx" -- comment ISSUE-ID --text "Comment text"
  dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\youtrack.fsx" -- request GET "/users/me?fields=id,login,fullName,email"
  dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\youtrack.fsx" -- request POST "/issues/PROJ-1" --body-file payload.json

Defaults:
  Base URL: https://gizmopowered.myjetbrains.com/youtrack

Required environment:
  YOUTRACK_API        Permanent token with the required YouTrack permissions.
                      Read from process env first, then Windows user env.

Optional environment:
  YOUTRACK_BASE_URL   Override the default YouTrack base URL.
"""

let argv =
    fsi.CommandLineArgs
    |> Array.skip 1
    |> Array.toList
    |> function
        | "--" :: rest -> rest
        | rest -> rest

let defaultBaseUrl = "https://gizmopowered.myjetbrains.com/youtrack"
let httpClient = new HttpClient()

let readEnvOptional name =
    [ Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process)
      Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User) ]
    |> List.tryFind (String.IsNullOrWhiteSpace >> not)

let readToken () =
    match readEnvOptional "YOUTRACK_API" with
    | Some token -> Ok token
    | None -> Error "Missing required environment variable: YOUTRACK_API"

let readBaseUrl () =
    readEnvOptional "YOUTRACK_BASE_URL"
    |> Option.defaultValue defaultBaseUrl

let tryArg (name: string) (args: string list) =
    args
    |> List.tryFindIndex ((=) name)
    |> Option.bind (fun index -> args |> List.tryItem (index + 1))

let intArg (name: string) fallback (args: string list) =
    tryArg name args
    |> Option.bind (fun value ->
        match Int32.TryParse value with
        | true, parsed -> Some parsed
        | false, _ -> None)
    |> Option.defaultValue fallback

let requireArg (name: string) (args: string list) =
    tryArg name args
    |> function
        | Some value -> Ok value
        | None -> Error $"Missing required argument: {name}"

let json value =
    JsonSerializer.Serialize(value)

let prettyJson (text: string) =
    try
        use doc = JsonDocument.Parse text
        JsonSerializer.Serialize(doc.RootElement, JsonSerializerOptions(WriteIndented = true))
    with _ ->
        text

let apiPath (path: string) =
    let trimmed = path.Trim()

    if trimmed.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)
       || trimmed.Equals("/api", StringComparison.OrdinalIgnoreCase) then
        trimmed
    elif trimmed.StartsWith("api/", StringComparison.OrdinalIgnoreCase) then
        "/" + trimmed
    elif trimmed.StartsWith("/", StringComparison.Ordinal) then
        "/api" + trimmed
    else
        "/api/" + trimmed

let request (methodName: string) (path: string) (body: string option) =
    async {
        match readToken () with
        | Error message -> return Error message
        | Ok token ->
            try
                let uri = (readBaseUrl ()).TrimEnd('/') + apiPath path

                use req = new HttpRequestMessage(HttpMethod(methodName.ToUpperInvariant()), uri)
                req.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)
                req.Headers.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))

                match body with
                | Some payload -> req.Content <- new StringContent(payload, Encoding.UTF8, "application/json")
                | None -> ()

                use! response = httpClient.SendAsync req |> Async.AwaitTask
                let! text = response.Content.ReadAsStringAsync() |> Async.AwaitTask

                if response.IsSuccessStatusCode then
                    return Ok text
                else
                    return Error $"YouTrack request failed: {(int response.StatusCode)} {response.ReasonPhrase}{Environment.NewLine}{text}"
            with ex ->
                return Error (Exception.toMessage ex)
    }

let fields (value: string) =
    Uri.EscapeDataString value

let command =
    match argv with
    | [] -> Error usage
    | "help" :: _
    | "--help" :: _
    | "-h" :: _ -> Ok(async { return Ok usage })
    | "me" :: _ ->
        Ok(request "GET" "/users/me?fields=id,login,fullName,email" None)
    | "get" :: issueId :: _ ->
        let issueFields =
            "id,idReadable,summary,description,project(shortName,name),customFields(name,value(name,login,fullName,isResolved)),comments(text,author(login,fullName),created)"

        let path =
            $"/issues/{Uri.EscapeDataString issueId}?fields={fields issueFields}"

        Ok(request "GET" path None)
    | "search" :: query :: rest ->
        let top = intArg "--top" 20 rest
        let skip = intArg "--skip" 0 rest
        let issueFields =
            "id,idReadable,summary,description,project(shortName,name),updated,customFields(name,value(name,login,fullName,isResolved))"

        let path =
            $"/issues?query={Uri.EscapeDataString query}&fields={fields issueFields}&$top={top}&$skip={skip}"

        Ok(request "GET" path None)
    | "create" :: rest ->
        result {
            let! projectId = requireArg "--project-id" rest
            let! summary = requireArg "--summary" rest
            let description = tryArg "--description" rest |> Option.defaultValue ""
            let body = json {| summary = summary; description = description; project = {| id = projectId |} |}
            return request "POST" "/issues?fields=idReadable,summary" (Some body)
        }
    | "comment" :: issueId :: rest ->
        result {
            let! text = requireArg "--text" rest
            let body = json {| text = text |}
            let path =
                $"/issues/{Uri.EscapeDataString issueId}/comments?fields=id,text,author(login,fullName),created"

            return request "POST" path (Some body)
        }
    | "request" :: methodName :: path :: rest ->
        let body =
            tryArg "--body-file" rest
            |> Option.map File.ReadAllText

        Ok(request methodName path body)
    | _ ->
        Error usage

match command with
| Error message ->
    Console.Error.WriteLine message
    exit 2
| Ok work ->
    match work |> Async.RunSynchronously with
    | Ok text ->
        printfn "%s" (prettyJson text)
    | Error message ->
        Console.Error.WriteLine message
        exit 1
