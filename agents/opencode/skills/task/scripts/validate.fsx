// Validate a TASK.md against the invariants in references/validation.md.
// Usage:
//   dotnet fsi validate.fsx <path-to-TASK.md>
//   dotnet fsi validate.fsx <path-to-TASK.md> --fix
// Exit 0 = clean; 1 = violations found; 2 = bad invocation.

open System
open System.IO
open System.Text.RegularExpressions

let args = fsi.CommandLineArgs |> Array.skip 1

if args.Length < 1 then
    eprintfn "usage: validate.fsx <TASK.md> [--fix]"
    exit 2

let path = args.[0]
let fix = args |> Array.contains "--fix"

if not (File.Exists path) then
    eprintfn "file not found: %s" path
    exit 2

let folder = Path.GetFileName(Path.GetDirectoryName path)
let raw = File.ReadAllLines path
let lines = ResizeArray(raw)
let violations = ResizeArray<string>()
let report msg = violations.Add msg

// 1. H1 matches folder name; hyphen or em-dash separator
let h1 = lines |> Seq.tryFind (fun l -> l.StartsWith "# ")

match h1 with
| Some line ->
    let m = Regex.Match(line, @"^#\s+(?<id>[A-Za-z]+-\d+)\s+[—-]\s+.+$")

    if not m.Success then
        report "H1 must match '# <TASK-ID> - Title'"
    elif m.Groups.["id"].Value <> folder then
        report (sprintf "H1 task-ID '%s' does not match folder '%s'" m.Groups.["id"].Value folder)
| None -> report "missing H1"

// 2+3. Status header line
let statusLine = lines |> Seq.tryFind (fun l -> l.StartsWith "**Progress:")
// Accept at least one date pillar (Created OR Completed). Created is canonical
// for new tasks; legacy completed tasks may carry only Completed.
let statusRe =
    Regex(
        @"^\*\*Progress:\s*(?<x>\d+)/(?<n>\d+)\s+subtasks complete\*\*\s+\|\s+\*\*Status:\s*(?<st>In Progress|Blocked|Paused|Complete)\*\*(\s+\|\s+\*\*Created:\s*(?<c>\d{4}-\d{2}-\d{2})\*\*)?(\s+\|\s+\*\*Completed:\s*(?<done>\d{4}-\d{2}-\d{2})\*\*)?$"
    )

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
                let today = DateTime.UtcNow.ToString("yyyy-MM-dd")
                let idx = lines.IndexOf sl
                lines.[idx] <- sl.TrimEnd() + sprintf " | **Completed: %s**" today

        if status <> "Complete" && hasCompleted then
            report "Status != Complete but Completed: field present"

// 4. Progress counter
let subtaskHeadings =
    lines
    |> Seq.mapi (fun i l -> i, l)
    |> Seq.filter (fun (_, l) -> Regex.IsMatch(l, @"^###\s+[0-9C]\w*[\.\d]*\."))
    |> Seq.toList

let subtaskRange (i: int) =
    let next =
        subtaskHeadings
        |> List.tryFind (fun (j, _) -> j > i)
        |> Option.map fst
        |> Option.defaultValue lines.Count

    [ for k in i + 1 .. next - 1 -> lines.[k] ]

let allChecked (block: string list) =
    let checks = block |> List.filter (fun l -> Regex.IsMatch(l, @"^\s*-\s+\[[ x]\]"))
    not checks.IsEmpty && checks |> List.forall (fun l -> Regex.IsMatch(l, @"^\s*-\s+\[x\]"))

let n = subtaskHeadings.Length

let x =
    subtaskHeadings |> List.filter (fun (i, _) -> allChecked (subtaskRange i)) |> List.length

match statusLine with
| Some sl ->
    let m = statusRe.Match sl

    if m.Success then
        let declX = int m.Groups.["x"].Value
        let declN = int m.Groups.["n"].Value

        if declX <> x || declN <> n then
            report (sprintf "Progress drift: declared %d/%d, actual %d/%d" declX declN x n)

            if fix then
                let idx = lines.IndexOf sl
                lines.[idx] <- Regex.Replace(sl, @"Progress:\s*\d+/\d+", sprintf "Progress: %d/%d" x n)
| None -> ()

// 5. Blocked notation
for i in 0 .. lines.Count - 1 do
    let l = lines.[i]

    if Regex.IsMatch(l, @"^\s*-\s+\[\s\]\s+\[blocked\]") then
        if not (Regex.IsMatch(l, @"\s+[—-]\s+\S")) then
            report (sprintf "line %d: [blocked] item missing '- reason'" (i + 1))

// 6. Decisions table dates
let decisionsIdx = lines |> Seq.tryFindIndex (fun l -> l.Trim() = "## Decisions")

match decisionsIdx with
| Some di ->
    let mutable stopped = false

    for k in di + 1 .. lines.Count - 1 do
        if not stopped then
            let l = lines.[k]
            // Terminate at any next ##/### heading — nested tables under
            // subheadings are not Decisions rows.
            if Regex.IsMatch(l, @"^#{2,3}\s") then
                stopped <- true
            elif l.StartsWith "|" then
                let cells =
                    l.Split('|') |> Array.map (fun s -> s.Trim()) |> Array.filter (fun s -> s <> "")

                let isSeparator =
                    cells |> Array.forall (fun c -> c = "" || Regex.IsMatch(c, @"^:?-+:?$"))

                let isHeader =
                    cells |> Array.exists (fun c -> c.Equals("Date", StringComparison.OrdinalIgnoreCase))

                if not isSeparator && not isHeader && cells.Length > 0 && cells.[0] <> "" then
                    if not (Regex.IsMatch(cells.[0], @"^\d{4}-\d{2}-\d{2}$")) then
                        report (sprintf "line %d: Decisions row has invalid Date '%s' (need YYYY-MM-DD)" (k + 1) cells.[0])
| None -> ()

// 7/8. Target repo + branch format
//  - At least one `./repo-name` row required under Context.
//  - If a concrete `(branch: ...)` annotation is present (not TBD), the branch
//    must start with the task ID. Annotation is optional.
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
            else
                let repoM = Regex.Match(l, @"`\./[\w\-\.]+`")

                if repoM.Success then
                    repoFound <- true
                    let branchM = Regex.Match(l, @"\(branch:\s*(?<b>[^)]+)\)")

                    if branchM.Success then
                        let branch = branchM.Groups.["b"].Value.Trim([| '`'; ' ' |])

                        if
                            not (branch.StartsWith("TBD", StringComparison.OrdinalIgnoreCase))
                            && not (branch.StartsWith folder)
                        then
                            report (sprintf "branch '%s' must start with task ID '%s'" branch folder)
| None -> report "missing ## Context section"

if not repoFound then
    report "## Context > Target repo(s) lists no repos"

// Write fixes
if fix && violations.Count > 0 then
    File.WriteAllLines(path, lines)
    printfn "applied auto-fixes where possible"

// Report
if violations.Count = 0 then
    printfn "OK %s" path
    exit 0
else
    printfn "VIOLATIONS in %s" path

    for v in violations do
        printfn "  - %s" v

    exit 1
