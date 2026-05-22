open System
open System.Globalization
open System.IO
open System.Text
open System.Text.Json
open System.Text.RegularExpressions

#load "../../../scripts/Cli.fsx"

open Common

module Const =
    [<Literal>]
    let FlagRefresh = "--refresh"

    [<Literal>]
    let FlagIncludedProviders = "--included-providers"

    [<Literal>]
    let FlagAvoidProviders = "--avoid-providers"

    [<Literal>]
    let FlagPriorityModels = "--priority-models"

    [<Literal>]
    let CostBandIncluded = "included"

    [<Literal>]
    let CostBandFree = "free"

    [<Literal>]
    let CostBandLow = "low"

    [<Literal>]
    let CostBandMedium = "medium"

    [<Literal>]
    let CostBandHigh = "high"

    [<Literal>]
    let StatusActive = "active"

    [<Literal>]
    let ColProvider = "Provider"

    [<Literal>]
    let ColStatus = "Status"

    [<Literal>]
    let ColModel = "Model"

    [<Literal>]
    let ColRating = "Rating"

    [<Literal>]
    let ColBestFor = "Best For"

    [<Literal>]
    let ColContext = "Context"

    [<Literal>]
    let ColInputOutputPer1M = "Input/Output $ per 1M"

type Model =
    { Ref: string
      Provider: string
      Id: string
      Name: string
      Family: string
      Status: string
      InputCost: decimal
      OutputCost: decimal
      CacheReadCost: decimal
      Context: int64
      OutputLimit: int64
      Reasoning: bool
      ToolCall: bool
      Attachment: bool
      Image: bool
      Pdf: bool
      ReleaseDate: string }

let args = Args.ofFsi fsi.CommandLineArgs
let refresh = Args.has Const.FlagRefresh args
let configRoot = Directory.GetParent(__SOURCE_DIRECTORY__).FullName

let argValue (name: string) =
    match Args.get name args with
    | Some value -> Some value
    | None ->
        args
        |> List.tryPick (fun arg ->
            let prefix = name + "="
            if arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) then
                Some(arg.Substring(prefix.Length))
            else
                None)

let parseCsv (value: string option) =
    value
    |> Option.defaultValue ""
    |> fun s -> s.Split(',', StringSplitOptions.TrimEntries ||| StringSplitOptions.RemoveEmptyEntries)
    |> Array.toList

let includedProviders =
    match parseCsv (argValue Const.FlagIncludedProviders) with
    | [] -> [ "openai"; "github-copilot" ]
    | items -> items |> List.map (fun x -> x.ToLowerInvariant())

let avoidProviders =
    parseCsv (argValue Const.FlagAvoidProviders)
    |> List.map (fun x -> x.ToLowerInvariant())

let priorityModels =
    parseCsv (argValue Const.FlagPriorityModels)
    |> List.map (fun x -> x.ToLowerInvariant())

let run command arguments =
    let commandLine =
        match arguments with
        | [] -> command
        | _ -> String.concat " " (command :: arguments)

    Shell.run commandLine

let stripAnsi (value: string) =
    Regex.Replace(value, "\u001b\[[0-9;]*m", "")

let jsonBlocks (text: string) =
    let blocks = ResizeArray<string>()
    let mutable depth = 0
    let mutable inBlock = false
    let current = StringBuilder()

    for line in text.Replace("\r\n", "\n").Split('\n') do
        let trimmed = line.Trim()

        if not inBlock && trimmed.StartsWith("{") then
            inBlock <- true
            depth <- 0
            current.Clear() |> ignore

        if inBlock then
            current.AppendLine(line) |> ignore

            for ch in line do
                match ch with
                | '{' -> depth <- depth + 1
                | '}' -> depth <- depth - 1
                | _ -> ()

            if depth = 0 then
                blocks.Add(current.ToString())
                inBlock <- false

    blocks |> Seq.toList

let prop (name: string) (element: JsonElement) =
    let mutable value = Unchecked.defaultof<JsonElement>
    if element.TryGetProperty(name, &value) then Some value else None

let propPath (path: string list) (element: JsonElement) =
    path |> List.fold (fun state name -> state |> Option.bind (prop name)) (Some element)

let stringProp name fallback element =
    prop name element
    |> Option.map (fun p -> if p.ValueKind = JsonValueKind.String then p.GetString() else p.ToString())
    |> Option.defaultValue fallback

let boolPath path element =
    propPath path element
    |> Option.map (fun p -> p.ValueKind = JsonValueKind.True)
    |> Option.defaultValue false

let decimalPath path element =
    propPath path element
    |> Option.bind (fun p ->
        match p.ValueKind with
        | JsonValueKind.Number ->
            let mutable value = 0M
            if p.TryGetDecimal(&value) then Some value else None
        | _ -> None)
    |> Option.defaultValue 0M

let int64Path path element =
    propPath path element
    |> Option.bind (fun p ->
        match p.ValueKind with
        | JsonValueKind.Number ->
            let mutable value = 0L
            if p.TryGetInt64(&value) then Some value else None
        | _ -> None)
    |> Option.defaultValue 0L

let parseModel (json: string) =
    use doc = JsonDocument.Parse(json)
    let root = doc.RootElement
    let provider = stringProp "providerID" "unknown" root
    let id = stringProp "id" "unknown" root
    let name = stringProp "name" id root

    { Ref = $"{provider}/{id}"
      Provider = provider
      Id = id
      Name = name
      Family = stringProp "family" "" root
      Status = stringProp "status" "" root
      InputCost = decimalPath [ "cost"; "input" ] root
      OutputCost = decimalPath [ "cost"; "output" ] root
      CacheReadCost = decimalPath [ "cost"; "cache"; "read" ] root
      Context = int64Path [ "limit"; "context" ] root
      OutputLimit = int64Path [ "limit"; "output" ] root
      Reasoning = boolPath [ "capabilities"; "reasoning" ] root
      ToolCall = boolPath [ "capabilities"; "toolcall" ] root
      Attachment = boolPath [ "capabilities"; "attachment" ] root
      Image = boolPath [ "capabilities"; "input"; "image" ] root
      Pdf = boolPath [ "capabilities"; "input"; "pdf" ] root
      ReleaseDate = stringProp "release_date" "" root }

let loadModels () =
    let arguments = [ "models"; "--verbose" ] @ if refresh then [ "--refresh" ] else []

    match run "opencode" arguments with
    | Error error -> failwith $"Failed to list OpenCode models: {error}"
    | Ok output ->
        output
        |> jsonBlocks
        |> List.map parseModel

let lower (value: string) = value.ToLowerInvariant()

let haystack (m: Model) = lower $"{m.Id} {m.Name} {m.Family}"
let hasAny (tokens: string list) (hay: string) = tokens |> List.exists hay.Contains

let smallFastTokens = [ "mini"; "nano"; "flash"; "haiku"; "fast"; "spark" ]
let costAwareTokens = [ "qwen"; "kimi"; "glm"; "minimax" ]

let isIncludedProvider (provider: string) =
    includedProviders |> List.contains (provider.ToLowerInvariant())

let routingCostBand model =
    match isIncludedProvider model.Provider, model.InputCost, model.OutputCost with
    | true, _, _ -> Const.CostBandIncluded
    | _, 0M, 0M -> Const.CostBandFree
    | _, inputCost, outputCost when inputCost <= 0.25M && outputCost <= 1.5M -> Const.CostBandLow
    | _, inputCost, outputCost when inputCost <= 1M && outputCost <= 5M -> Const.CostBandMedium
    | _ -> Const.CostBandHigh

let budgetFit model =
    match avoidProviders |> List.contains (model.Provider.ToLowerInvariant()), routingCostBand model with
    | true, _ -> "avoid (policy)"
    | false, Const.CostBandIncluded -> "included plan"
    | false, Const.CostBandFree -> "free"
    | false, Const.CostBandLow -> "low-cost"
    | false, Const.CostBandMedium -> "medium-cost"
    | _ -> "high-cost"

let taskFit model =
    let hay = haystack model

    if hay.Contains "codex" then "repo edits, coding agents, refactors"
    elif hay.Contains "opus" || hay.Contains "pro" && not (hay.Contains "mini" || hay.Contains "flash") then "hard architecture, critical review"
    elif hay.Contains "sonnet" then "balanced coding and review"
    elif hasAny smallFastTokens hay then "routine edits, summaries, triage"
    elif hasAny costAwareTokens hay then "cost-aware reasoning and code"
    elif model.Reasoning && model.ToolCall then "general agent work"
    else "simple text or fallback"

let isSmallOrFast model =
    hasAny smallFastTokens (haystack model)

let shortDescription model =
    let parts = ResizeArray<string>()
    if model.Reasoning then parts.Add "reasoning"
    if model.ToolCall then parts.Add "tools"
    if model.Attachment then parts.Add "files"
    if model.Image then parts.Add "vision"
    if model.Pdf then parts.Add "pdf"

    let caps = if parts.Count = 0 then "text" else String.Join("/", parts)
    $"{taskFit model}; {caps}; {routingCostBand model} cost"

let rating model =
    let mutable score = 4.0
    if model.Reasoning then score <- score + 1.2
    if model.ToolCall then score <- score + 1.0
    if model.Attachment then score <- score + 0.4
    if model.Context >= 1000000L then score <- score + 0.8
    elif model.Context >= 200000L then score <- score + 0.5
    elif model.Context >= 128000L then score <- score + 0.2

    match routingCostBand model with
    | Const.CostBandIncluded -> score <- score + 1.2
    | Const.CostBandFree -> score <- score + 1.0
    | Const.CostBandLow -> score <- score + 0.7
    | Const.CostBandMedium -> score <- score + 0.2
    | _ -> score <- score - 0.4

    let hay = haystack model
    if hay.Contains "codex" then score <- score + 0.7
    if priorityModels |> List.exists hay.Contains then score <- score + 0.6
    if hasAny smallFastTokens hay then score <- score + 0.3

    match hasAny [ "preview"; "beta" ] hay, model.Status with
    | true, status when status <> Const.StatusActive -> score <- score - 1.1
    | true, _ -> score <- score - 0.3
    | false, status when status <> Const.StatusActive -> score <- score - 0.8
    | _ -> ()

    Math.Clamp(score, 0.0, 10.0)

let money value =
    if value = 0M then "0"
    else value.ToString("0.###", CultureInfo.InvariantCulture)

let thousands value =
    if value <= 0L then "?"
    elif value >= 1000000L then sprintf "%.1fM" (float value / 1000000.0)
    else $"{value / 1000L}k"

let mdEscape (value: string) =
    value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ")

let row cells =
    cells |> List.map mdEscape |> String.concat " | " |> fun value -> $"| {value} |"

let table headers rows =
    [ yield row headers
      yield headers |> List.map (fun _ -> "---") |> row
      yield! rows |> List.map row ]

let configTargetRows models =
    let best predicate =
        models
        |> List.filter predicate
        |> List.sortByDescending rating
        |> List.tryHead
        |> Option.map _.Ref
        |> Option.defaultValue "not found"

    [ [ "executor / dotnet-engineer"; best (fun m -> lower m.Family |> fun f -> f.Contains("codex")); "default implementation and repo edits" ]
      [ "architecture-reviewer / teamlead"; best (fun m -> m.Reasoning && not (isSmallOrFast m) && ((lower m.Id).Contains("5.5") || (lower m.Id).Contains("opus") || (lower m.Id).Contains("pro"))); "expensive reasoning only when design risk is high" ]
      [ "explore / command drafts"; best (fun m -> routingCostBand m = Const.CostBandIncluded && ((lower m.Id).Contains("mini") || (lower m.Id).Contains("haiku") || (lower m.Id).Contains("flash"))); "cheap discovery, summaries, small command/rule work" ]
      [ "security-auditor / review"; best (fun m -> m.Reasoning && m.ToolCall && (isIncludedProvider m.Provider)); "use subscription models before spending credits" ]
      [ "credit fallback"; best (fun m -> (m.Provider = "openrouter" || m.Provider = "opencode") && (routingCostBand m = Const.CostBandFree || routingCostBand m = Const.CostBandLow)); "only for overflow, experiments, or provider comparison" ] ]

let agentsDir = Path.Combine(configRoot, "agents")

let currentAgentRows () =
    if not (Directory.Exists agentsDir) then []
    else
        Directory.EnumerateFiles(agentsDir, "*.md")
        |> Seq.choose (fun file ->
            let text = File.ReadAllText(file)
            let found = Regex.Match(text, @"(?m)^model:\s*(\S+)\s*$")
            if found.Success then Some [ Path.GetFileNameWithoutExtension(file); found.Groups.[1].Value ] else None)
        |> Seq.sortBy List.head
        |> Seq.toList

let models = loadModels () |> List.distinctBy _.Ref
let providers =
    models
    |> List.map _.Provider
    |> List.filter (fun p -> not (String.IsNullOrWhiteSpace(p)))
    |> List.distinct
    |> List.sort

let failures : (string * string) list = []

if models.IsEmpty then failwith "No models were discovered from connected providers."

let now = DateTimeOffset.Now
let dateText = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
let generatedAt = now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)
let fileStamp = now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
let outputPath = Path.Combine(Path.GetTempPath(), $"opencode-model-advisor-{fileStamp}.md")
let refreshText = if refresh then "yes" else "no"

let providerRows =
    providers
    |> List.map (fun provider ->
        let count = models |> List.filter (fun m -> m.Provider = provider) |> List.length
        let status = if avoidProviders |> List.contains (provider.ToLowerInvariant()) then "connected (avoid policy)" else "connected"
        [ provider; string count; status ])

let includedProvidersText = String.Join(", ", includedProviders)
let avoidProvidersText = String.Join(", ", avoidProviders)
let priorityModelsText = String.Join(", ", priorityModels)

let recommendationRows =
    models
    |> List.sortByDescending rating
    |> List.truncate 30
    |> List.map (fun m ->
        [ m.Ref
          sprintf "%.1f" (rating m)
          taskFit m
          shortDescription m
          budgetFit m
          thousands m.Context
          $"{money m.InputCost}/{money m.OutputCost}" ])

let inventoryRows =
    models
    |> List.sortBy (fun m -> m.Provider, -(rating m), m.Id)
    |> List.map (fun m ->
        [ m.Ref
          sprintf "%.1f" (rating m)
          m.Status
          routingCostBand m
          taskFit m
          thousands m.Context
          thousands m.OutputLimit
          $"{money m.InputCost}/{money m.OutputCost}"
          shortDescription m ])

let current = currentAgentRows ()

let providerErrorsSection =
    if failures.IsEmpty then []
    else
        [ "## Provider Errors" ]
        @ (failures |> List.map (fun (provider, error) -> [ provider; error ]) |> table [ Const.ColProvider; "Error" ])
        @ [ "" ]

let currentAgentsSection =
    if current.IsEmpty then [ "No local `agents/*.md` model assignments found." ]
    else table [ "Agent"; "Current Model" ] current

let markdown =
    [ "# OpenCode Model Advisor - " + dateText
      ""
      "Generated: " + generatedAt
      "Refresh used: " + refreshText
      ""
      "Purpose: choose models for agents, skills, commands, and rules to reduce spend and token waste without losing task quality. Ratings are heuristic, based on OpenCode model metadata, tool/reasoning capability, context, price band, and your stated subscriptions."
      ""
      $"Policy used: included providers = [{includedProvidersText}]; avoid providers = [{avoidProvidersText}]; priority model tags = [{priorityModelsText}]."
      ""
      "## Connected Providers" ]
    @ table [ Const.ColProvider; "Models"; Const.ColStatus ] providerRows
    @ [ "" ]
    @ providerErrorsSection
    @ [ "## Recommended Config Targets" ]
    @ table [ "Target"; Const.ColModel; "Why" ] (configTargetRows models)
    @ [ ""
        "## Current Agent Models" ]
    @ currentAgentsSection
    @ [ ""
        "## Top Recommendations" ]
    @ table [ Const.ColModel; Const.ColRating; Const.ColBestFor; "Short Description"; "Budget Fit"; Const.ColContext; Const.ColInputOutputPer1M ] recommendationRows
    @ [ ""
        "## Full Connected-Provider Inventory" ]
    @ (table [ Const.ColModel; Const.ColRating; Const.ColStatus; "Price"; Const.ColBestFor; Const.ColContext; "Output"; Const.ColInputOutputPer1M; "Description" ] inventoryRows)
    @ [ ""
        "## Notes"
        "- OpenCode may report zero cost for some subscription-backed providers; this script treats configured included providers as 'included' rather than generic free APIs."
        "- Ratings are for routing work, not absolute benchmark truth; validate important agent changes with real tasks."
        "- Use `--included-providers=...`, `--avoid-providers=...`, and `--priority-models=...` to adapt routing without editing code."
        "- Use `--refresh` when you want OpenCode to refresh its model metadata cache." ]
    |> String.concat Environment.NewLine

File.WriteAllText(outputPath, markdown + Environment.NewLine, UTF8Encoding(false))
printfn "%s" outputPath
