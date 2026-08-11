// Create a TASK.md from the canonical template without overwriting existing work.
// Usage:
//   dotnet fsi "C:/Users/andre/.config/opencode/skills/task/scripts/CreateTask.fsx" <TASK-ID> <title>

open System
open System.IO
open System.Text.RegularExpressions

#load "TaskMd.fsx"
open TaskMd

let args = fsi.CommandLineArgs |> Array.skip 1
let supportedFlags = Set.ofList [ "--non-code"; "--no-commit" ]
let flags = args |> Array.filter (fun value -> value.StartsWith("--", StringComparison.Ordinal)) |> Set.ofArray
let unsupportedFlags = Set.difference flags supportedFlags
let positional = args |> Array.filter (fun value -> not (value.StartsWith("--", StringComparison.Ordinal)))

if not unsupportedFlags.IsEmpty then
    eprintfn "unsupported option(s): %s" (String.concat ", " unsupportedFlags)
    exit 2

if positional.Length < 2 then
    eprintfn "usage: CreateTask.fsx <TASK-ID> <title> [--non-code] [--no-commit]"
    exit 2

let taskId = positional.[0]
let title = positional |> Array.skip 1 |> String.concat " " |> fun value -> value.Trim()
if String.IsNullOrWhiteSpace title || title.Contains '\r' || title.Contains '\n' then
    eprintfn "task title must be a non-empty single line"
    exit 2

let taskPath =
    match tryResolveNewTaskPath (Directory.GetCurrentDirectory()) taskId with
    | Ok resolved -> resolved
    | Error message ->
        eprintfn "%s" message
        exit 2

let templatePath = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "references", "template.md"))
let template = File.ReadAllText templatePath
let fenced = Regex.Match(template, @"```markdown\s*\r?\n(?<body>.*?)\r?\n```", RegexOptions.Singleline)
if not fenced.Success then
    eprintfn "canonical task template has no markdown code block"
    exit 2

let validTaskId =
    match tryTaskId taskId with
    | Ok value -> value
    | Error message ->
        eprintfn "%s" message
        exit 2

let baseBody =
    fenced.Groups.["body"].Value
        .Replace("{TASK-ID}", validTaskId)
        .Replace("# TASK-ID - Task Title", $"# {validTaskId} - {title}")
        .Replace("Created: YYYY-MM-DD", "Created: " + DateTime.UtcNow.ToString("yyyy-MM-dd"))

let removeBlock pattern value =
    Regex.Replace(value, pattern, "", RegexOptions.Multiline ||| RegexOptions.Singleline)

let specializedBody =
    baseBody
    |> fun value ->
        if flags.Contains "--non-code" then
            value
            |> fun text -> text.Replace("- Task kind: code", "- Task kind: non-code")
            |> fun text -> Regex.Replace(text, @"^- Implementation plan: .*\r?\n", "", RegexOptions.Multiline)
            |> removeBlock @"^### 3\. Design gate\s*\r?\n.*?(?=^### 4\.)"
            |> removeBlock @"^### C0\. Pre-commit review board\s*\r?\n.*?(?=^### C1\.)"
            |> fun text ->
                Regex.Replace(
                    text,
                    @"^### 5\. Implement and validate\s*\r?\n.*?(?=^## Review)",
                    "### 5. Execute and validate\n\nSteps:\n\n- [ ] Complete planned non-code work\n  - Summary:\n- [ ] Verify acceptance criteria and record evidence\n  - Summary:\n\n",
                    RegexOptions.Multiline ||| RegexOptions.Singleline)
        else value
    |> fun value ->
        if flags.Contains "--no-commit" then
            removeBlock @"^### C2\. Commit and publish\s*\r?\n.*?(?=^## Decisions)" value
        else value

let bodyLines = Regex.Split(specializedBody, @"\r?\n") |> ResizeArray
let _, total = computeProgress bodyLines
let body = specializedBody.Replace("Progress: 0/N", $"Progress: 0/{total}")

let taskDirectory = Path.GetDirectoryName taskPath
Directory.CreateDirectory taskDirectory |> ignore

try
    use stream = new FileStream(taskPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)
    use writer = new StreamWriter(stream)
    writer.Write body
    Directory.CreateDirectory(Path.Combine(taskDirectory, "docs")) |> ignore
    printfn "created %s" taskPath
with :? IOException ->
    eprintfn "task already exists or could not be created safely: %s" taskPath
    exit 1
