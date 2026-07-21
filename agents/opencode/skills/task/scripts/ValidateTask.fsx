// Validate a TASK.md against the invariants in references/validation.md.
// Usage:
//   Run from the project root: dotnet fsi ValidateTask.fsx <path-to-TASK.md> [--fix]
// Exit 0 = clean; 1 = violations found; 2 = bad invocation.

open System
open System.IO
open System.Text.RegularExpressions

#load "TaskMd.fsx"
open TaskMd // shared heading detection + progress counting (single source of truth)

let args = fsi.CommandLineArgs |> Array.skip 1
if args.Length < 1 then
    eprintfn "usage: ValidateTask.fsx <TASK.md> [--fix]"
    exit 2

let fix = args |> Array.contains "--fix"
let path =
    match tryResolveTaskPath (Directory.GetCurrentDirectory()) args.[0] with
    | Ok resolved -> resolved
    | Error message ->
        eprintfn "%s" message
        exit 2

let folder = Path.GetFileName(Path.GetDirectoryName path)
let raw = File.ReadAllLines path
let mutable lines = ResizeArray(raw)
let mutable violations = ResizeArray<string>()
let report msg = violations.Add msg

// 1. H1 matches folder name; hyphen or em-dash separator
let h1 = lines |> Seq.tryFind (fun l -> l.StartsWith "# ")
match h1 with
| Some line ->
    let m = Regex.Match(line, @"^#\s+(?<id>[A-Za-z]+-\d+)\s+[—-]\s+.+$")
    if not m.Success then report "H1 must match '# <TASK-ID> — Title'"
    elif m.Groups.["id"].Value <> folder then
        report (sprintf "H1 task-ID '%s' does not match folder '%s'" m.Groups.["id"].Value folder)
| None -> report "missing H1"

// 2. No '## Title' section
let content = contentLineIndexes lines
if lines |> Seq.indexed |> Seq.exists (fun (i, l) -> content.Contains i && l.Trim() = "## Title") then
    report "found '## Title' section — remove it (H1 is the title)"

// 3+4+5. Status header line
let statusLine =
    lines
    |> Seq.tryFind (fun l -> l.StartsWith "**Progress:")
// Created is mandatory; Completed is additive and present iff Status: Complete.
let statusRe = Regex(@"^\*\*Progress:\s*(?<x>\d+)/(?<n>\d+)\s+subtasks complete\*\*\s+\|\s+\*\*Status:\s*(?<st>In Progress|Blocked|Paused|Complete)\*\*\s+\|\s+\*\*Created:\s*(?<c>\d{4}-\d{2}-\d{2})\*\*(\s+\|\s+\*\*Completed:\s*(?<done>\d{4}-\d{2}-\d{2})\*\*)?$")
match statusLine with
| None -> report "missing status header line"
| Some sl ->
    let m = statusRe.Match sl
    if not m.Success then report "status header line malformed"
    else
        let status = m.Groups.["st"].Value
        let hasCompleted = m.Groups.["done"].Success
        if status = "Complete" && not hasCompleted then
            report "Status=Complete but Completed: field missing"
            if fix then
                let today = DateTime.UtcNow.ToString("yyyy-MM-dd")
                let idx = lines.IndexOf sl
                lines.[idx] <- sl.TrimEnd() + sprintf " | **Completed: %s**" today
        if status <> "Complete" && hasCompleted then
            report "Status != Complete but Completed: field present"

// 5. Progress counter (headings / range / allChecked come from TaskMd)
let subtaskHeadings = parseHeadings lines
let x, n = computeProgress lines

for requiredClosing in [ "C1" ] do
    if not (subtaskHeadings |> List.exists (fun (_, line) -> tryHeadingId line = Some requiredClosing)) then
        report (sprintf "missing required closing step %s" requiredClosing)

match statusLine with
| Some sl ->
    let m = statusRe.Match sl
    if m.Success then
        let declX = int m.Groups.["x"].Value
        let declN = int m.Groups.["n"].Value
        if declX <> x || declN <> n then
            report (sprintf "Progress drift: declared %d/%d, actual %d/%d" declX declN x n)
            if fix then
                let idx = lines |> Seq.findIndex (fun line -> line.StartsWith "**Progress:")
                lines.[idx] <- Regex.Replace(lines.[idx], @"Progress:\s*\d+/\d+", sprintf "Progress: %d/%d" x n)
| None -> ()

let lifecycleLines = lifecycleContentLineIndexes lines
let contextLines = sectionContentLineIndexes "## Context" lines
let taskKinds =
    lines
    |> Seq.mapi (fun i line -> i, line)
    |> Seq.choose (fun (i, line) ->
        if not (contextLines.Contains i) then None
        else
            let matched = Regex.Match(line, @"^- Task kind: (?<kind>code|non-code)$")
            if matched.Success then Some matched.Groups.["kind"].Value else None)
    |> Seq.toList

if taskKinds.Length <> 1 then report "## Context must contain exactly one '- Task kind: code|non-code' marker"
let codeTask = taskKinds = [ "code" ]
let hasC0 = subtaskHeadings |> List.exists (fun (_, line) -> tryHeadingId line = Some "C0")

if codeTask && not hasC0 then report "code task is missing required closing step C0"
if taskKinds = [ "non-code" ] && hasC0 then report "non-code task must not contain closing step C0"

if codeTask then
    for requiredText in requiredCodeGateLabels do
        let found =
            lines
            |> Seq.mapi (fun i line -> i, line)
            |> Seq.exists (fun (i, line) ->
                lifecycleLines.Contains i
                && Regex.IsMatch(line, @"^\s*-\s+\[[ xX]\]\s+" + Regex.Escape requiredText))
        if not found then report $"code task is missing required gate checkbox: {requiredText}"
elif taskKinds = [ "non-code" ] then
    for codeOnlyText in requiredCodeGateLabels do
        let found =
            lines
            |> Seq.mapi (fun i line -> i, line)
            |> Seq.exists (fun (i, line) ->
                lifecycleLines.Contains i
                && Regex.IsMatch(line, @"^\s*-\s+\[[ xX]\]\s+" + Regex.Escape codeOnlyText))
        if found then report $"non-code task contains code-only gate: {codeOnlyText}"

// 10. Stable subtask numbering. Contract: references/validation.md, invariant 10.
// Classify a heading's id (from the shared parser) into an orderable key:
// Choice1Of2 (number, suffixRank, decimal) for numbered subtasks, Choice2Of2 n for C-steps.
let suffixRank (s: string) = if s = "" then 0 else int s.[0] - int 'a' + 1
let parseSubtaskId (line: string) =
    tryHeadingId line
    |> Option.map (fun id ->
        if id.StartsWith "C" then Choice2Of2(int (id.Substring 1))
        elif id.Contains "." then
            let p = id.Split '.'
            Choice1Of2(int p.[0], 0, int p.[1])
        else
            let mm = Regex.Match(id, @"^(?<n>\d+)(?<s>[a-z]?)$")
            Choice1Of2(int mm.Groups.["n"].Value, suffixRank mm.Groups.["s"].Value, 0))
let mutable lastNumKey : (int * int * int) option = None
let mutable lastClosing : int option = None
for (i, l) in subtaskHeadings do
    match parseSubtaskId l with
    | None -> ()
    | Some (Choice1Of2 key) ->
        if lastClosing.IsSome then
            report (sprintf "line %d: numbered subtask after a C-step — closing steps must be last" (i+1))
        match lastNumKey with
        | Some prev when key <= prev ->
            report (sprintf "line %d: subtask numbering not ascending (numbers must never be reused)" (i+1))
        | _ -> ()
        lastNumKey <- Some key
    | Some (Choice2Of2 c) ->
        match lastClosing with
        | Some prev when c <= prev ->
            report (sprintf "line %d: C-step numbering not ascending" (i+1))
        | _ -> ()
        lastClosing <- Some c

// Letter suffixes are not allowed — plain sequential only.
for (i, l) in subtaskHeadings do
    match tryHeadingId l with
    | Some id when Regex.IsMatch(id, @"^\d+[a-z]$") ->
        report (sprintf "line %d: subtask id '%s' uses a letter suffix — renumber to plain sequential" (i+1) id)
    | _ -> ()

// 11. Summary lines must be nested bullets (`  - Summary:`), never bare
//     indented continuation lines. Checked items require non-empty evidence.
for i in 0 .. lines.Count - 1 do
    let l = lines.[i]
    let m = Regex.Match(l, @"^(?<indent>\s+)Summary:(?<rest>.*)$")
    if lifecycleLines.Contains i && m.Success then
        report (sprintf "line %d: bare 'Summary:' continuation line — use a nested '  - Summary:' bullet" (i+1))
        if fix then
            lines.[i] <- "  - Summary:" + m.Groups.["rest"].Value

for i in 0 .. lines.Count - 1 do
    if lifecycleLines.Contains i && Regex.IsMatch(lines.[i], @"^\s*-\s+\[[xX]\]") then
        let hasSummary =
            i + 1 < lines.Count
            && Regex.IsMatch(lines.[i + 1], @"^\s{2,}-\s+Summary:\s+\S")
        if not hasSummary then
            report (sprintf "line %d: checked item requires a directly nested non-empty Summary" (i + 1))

// 8. Blocked notation
for i in 0 .. lines.Count - 1 do
    let l = lines.[i]
    if Regex.IsMatch(l, @"^\s*-\s+\[\s\]\s+\[blocked\]") then
        if not (Regex.IsMatch(l, @"\s+[—-]\s+\S")) then
            report (sprintf "line %d: [blocked] item missing '— reason'" (i+1))

// 9. Decisions table dates
let decisionsIdx = lines |> Seq.tryFindIndex (fun l -> l.Trim() = "## Decisions")
let decisionRows = ResizeArray<string * string * string>()
match decisionsIdx with
| Some di ->
    let mutable stopped = false
    for k in di+1 .. lines.Count - 1 do
        if not stopped then
            let l = lines.[k]
            // Terminate at any next ##/### heading — nested tables under
            // subheadings are not Decisions rows.
            if Regex.IsMatch(l, @"^#{2,3}\s") then stopped <- true
            elif l.StartsWith "|" then
                let cells = l.Split('|') |> Array.map (fun s -> s.Trim()) |> Array.filter (fun s -> s <> "")
                let isSeparator = cells |> Array.forall (fun c -> c = "" || Regex.IsMatch(c, @"^:?-+:?$"))
                let isHeader = cells |> Array.exists (fun c -> c.Equals("Date", StringComparison.OrdinalIgnoreCase))
                if not isSeparator && not isHeader && cells.Length > 0 && cells.[0] <> "" then
                    if not (Regex.IsMatch(cells.[0], @"^\d{4}-\d{2}-\d{2}$")) then
                        report (sprintf "line %d: Decisions row has invalid Date '%s' (need YYYY-MM-DD)" (k+1) cells.[0])
                    elif cells.Length >= 2 then
                        let rationale = if cells.Length >= 3 then cells.[2] else ""
                        decisionRows.Add(cells.[0], cells.[1], rationale)
| None -> ()

match statusLine with
| Some sl ->
    let matched = statusRe.Match sl
    if matched.Success && matched.Groups.["st"].Value = "Complete" then
        let hasConfirmation =
            decisionRows
            |> Seq.exists (fun (_, decision, _) -> decision.Contains("complete status confirmed", StringComparison.OrdinalIgnoreCase))
        let hasWaiver =
            decisionRows
            |> Seq.exists (fun (_, decision, rationale) ->
                decision.Contains("complete status waiver", StringComparison.OrdinalIgnoreCase)
                && not (String.IsNullOrWhiteSpace rationale))

        if not hasConfirmation then report "Status=Complete requires a dated 'complete status confirmed' decision"
        if x <> n && not hasWaiver then report "Status=Complete requires complete progress or a dated completion waiver with rationale"
| None -> ()

// 10/11. Target repo + branch format
//  - At least one `./repo-name` row required under Context.
//  - If a concrete `(branch: ...)` annotation is present (not TBD), the branch
//    must start with the task ID. Annotation is optional; backticks optional.
let contextIdx = lines |> Seq.tryFindIndex (fun l -> l.Trim() = "## Context")
let mutable repoFound = false
match contextIdx with
| Some ci ->
    let mutable stopped = false
    for k in ci+1 .. lines.Count - 1 do
        if not stopped then
            let l = lines.[k]
            if Regex.IsMatch(l, @"^#{2,3}\s") then stopped <- true
            else
                let repoM = Regex.Match(l, @"`\./[\w\-\.]+`")
                if repoM.Success then
                    repoFound <- true
                    let branchM = Regex.Match(l, @"\(branch:\s*(?<b>[^)]+)\)")
                    if branchM.Success then
                        let branch = branchM.Groups.["b"].Value.Trim([| '`'; ' ' |])
                        if not (branch.StartsWith("TBD", StringComparison.OrdinalIgnoreCase))
                           && not (branch.StartsWith folder) then
                            report (sprintf "branch '%s' must start with task ID '%s'" branch folder)
| None -> report "missing ## Context section"
if not repoFound then report "## Context > Target repo(s) lists no repos"

// Write fixes
if fix && violations.Count > 0 then
    match tryWriteAllLinesIfUnchanged path raw lines with
    | Ok () -> printfn "applied auto-fixes where possible"
    | Error message ->
        eprintfn "%s" message
        exit 1

// Report
if violations.Count = 0 then
    printfn "OK %s" path
    exit 0
else
    printfn "VIOLATIONS in %s" path
    for v in violations do printfn "  - %s" v
    exit 1
