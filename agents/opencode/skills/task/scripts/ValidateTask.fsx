// Validate a TASK.md against the invariants in references/validation.md.
// Usage:
//   dotnet fsi C:/Users/andre/.config/opencode/skills/task/scripts/ValidateTask.fsx .tasks/<TASK-ID>/TASK.md
//   dotnet fsi C:/Users/andre/.config/opencode/skills/task/scripts/ValidateTask.fsx .tasks/<TASK-ID>/TASK.md --fix
// Exit 0 = clean; 1 = violations found; 2 = bad invocation.

#load "TaskProgress.fsx"

open System
open System.IO
open System.Text.RegularExpressions

let args = fsi.CommandLineArgs |> Array.skip 1
if args.Length < 1 then
    eprintfn "usage: ValidateTask.fsx <TASK.md> [--fix]"
    exit 2

let path = args.[0]
let fix = args |> Array.contains "--fix"
if not (File.Exists path) then
    eprintfn "file not found: %s" path
    exit 2

let folder = Path.GetFileName(Path.GetDirectoryName path)
let lines = File.ReadAllLines path |> ResizeArray
let violations = ResizeArray<string>()
let report msg = violations.Add msg

let h1 = lines |> Seq.tryFind (fun l -> l.StartsWith "# ")
match h1 with
| Some line ->
    if not (line.StartsWith(sprintf "# %s - " folder)) then
        report (sprintf "H1 must start with '# %s - '" folder)
| None -> report "missing H1"

if lines |> Seq.exists (fun l -> l.Trim() = "## Title") then
    report "found legacy '## Title' section; remove it because H1 is the title"

let statusLine = lines |> Seq.tryFind (fun l -> l.StartsWith "**Progress:")
let statusRe = Regex(@"^\*\*Progress:\s*(?<x>\d+)/(?<n>\d+)\s+subtasks complete\*\*\s+\|\s+\*\*Status:\s*(?<st>In Progress|Blocked|Paused|Complete)\*\*(\s+\|\s+\*\*Created:\s*(?<c>\d{4}-\d{2}-\d{2})\*\*)?(\s+\|\s+\*\*Completed:\s*(?<done>\d{4}-\d{2}-\d{2})\*\*)?$")

match statusLine with
| None -> report "missing status header line"
| Some sl ->
    let m = statusRe.Match sl
    if not m.Success then
        report "status header line malformed"
    else
        let status = m.Groups.["st"].Value
        let hasCompleted = m.Groups.["done"].Success

        if status = "Complete" && not hasCompleted then
            report "Status=Complete but Completed: field missing"
            if fix then
                let idx = lines.IndexOf sl
                lines.[idx] <- sl.TrimEnd() + sprintf " | **Completed: %s**" (DateTime.UtcNow.ToString("yyyy-MM-dd"))

        if status <> "Complete" && hasCompleted then
            report "Status != Complete but Completed: field present"

let progress = TaskProgress.count lines

match statusLine with
| Some sl ->
    let m = statusRe.Match sl
    if m.Success then
        let declX = int m.Groups.["x"].Value
        let declN = int m.Groups.["n"].Value
        if declX <> progress.Completed || declN <> progress.Total then
            report (sprintf "Progress drift: declared %d/%d, actual %d/%d" declX declN progress.Completed progress.Total)
            if fix then
                let idx = lines.IndexOf sl
                lines.[idx] <- Regex.Replace(sl, @"Progress:\s*\d+/\d+", sprintf "Progress: %d/%d" progress.Completed progress.Total)
| None -> ()

for i in 0 .. lines.Count - 1 do
    let l = lines.[i]
    if Regex.IsMatch(l, @"^\s*-\s+\[\s\]\s+\[blocked\]") then
        if not (Regex.IsMatch(l, @"\s+-\s+\S")) then
            report (sprintf "line %d: [blocked] item missing '- reason'" (i + 1))

let decisionsIdx = lines |> Seq.tryFindIndex (fun l -> l.Trim() = "## Decisions")
match decisionsIdx with
| Some di ->
    let mutable stopped = false
    for k in di + 1 .. lines.Count - 1 do
        if not stopped then
            let l = lines.[k]
            if Regex.IsMatch(l, @"^#{2,3}\s") then
                stopped <- true
            elif l.StartsWith "|" then
                let cells = l.Split('|') |> Array.map (fun s -> s.Trim()) |> Array.filter (fun s -> s <> "")
                let isSeparator = cells |> Array.forall (fun c -> Regex.IsMatch(c, @"^:?-+:?$"))
                let isHeader = cells |> Array.exists (fun c -> c.Equals("Date", StringComparison.OrdinalIgnoreCase))
                if not isSeparator && not isHeader && cells.Length > 0 && cells.[0] <> "" then
                    if not (Regex.IsMatch(cells.[0], @"^\d{4}-\d{2}-\d{2}$")) then
                        report (sprintf "line %d: Decisions row has invalid Date '%s' (need YYYY-MM-DD)" (k + 1) cells.[0])
| None -> ()

let contextIdx = lines |> Seq.tryFindIndex (fun l -> l.Trim() = "## Context")
let mutable repoFound = false
match contextIdx with
| Some ci ->
    let mutable stopped = false
    for k in ci + 1 .. lines.Count - 1 do
        if not stopped then
            let l = lines.[k]
            if Regex.IsMatch(l, @"^#{2,3}\s") then
                stopped <- true
            elif Regex.IsMatch(l, @"`\./[\w\-.]+`") then
                repoFound <- true
| None -> report "missing ## Context section"

if not repoFound then
    report "## Context > Target repo(s) lists no repos"

if fix && violations.Count > 0 then
    for i in 0 .. lines.Count - 1 do
        lines.[i] <- lines.[i].TrimEnd()
    File.WriteAllLines(path, lines)
    printfn "applied auto-fixes where possible"

if violations.Count = 0 then
    printfn "OK %s" path
    exit 0
else
    printfn "VIOLATIONS in %s" path
    for v in violations do
        printfn "  - %s" v
    exit 1
