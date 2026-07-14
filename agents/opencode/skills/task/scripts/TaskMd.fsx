module TaskMd

// Shared TASK.md parsing helpers — SINGLE SOURCE OF TRUTH for "what is a subtask
// heading" and how progress (X/N) is counted. `#load`-ed by both ValidateTask.fsx and
// RecomputeProgress.fsx so the two scripts can never disagree on heading detection
// or the completion rule.

open System
open System.IO
open System.Text.RegularExpressions

/// Canonical subtask-heading pattern. A heading is `### <id>.` where <id> is a
/// closing step (C0, C1, …), a decimal sub-subtask (3.1), or a number with an
/// optional letter suffix (3, 2a). Suffixes are still MATCHED here so ValidateTask.fsx
/// can flag them — they are not allowed in tasks. This is the ONE heading definition.
let headingRegex = Regex(@"^###\s+(?<id>C\d+|\d+\.\d+|\d+[a-z]?)\.")

let fenceRegex = Regex(@"^\s*(?<fence>`{3,}|~{3,})")

let tryResolveTaskPath (root: string) (path: string) =
    try
        let fullRoot = DirectoryInfo(Path.GetFullPath root).FullName
        let fullPath = Path.GetFullPath path
        let relative = Path.GetRelativePath(fullRoot, fullPath).Replace("\\", "/")
        let comparison = if OperatingSystem.IsWindows() then StringComparison.OrdinalIgnoreCase else StringComparison.Ordinal

        if not (Directory.Exists fullRoot) then
            Error $"project root does not exist: {fullRoot}"
        elif not (File.Exists fullPath) then
            Error $"task file does not exist: {fullPath}"
        elif FileInfo(fullPath).Attributes.HasFlag FileAttributes.ReparsePoint then
            Error "task file must not be a reparse point"
        elif Path.IsPathRooted relative || relative = ".." || relative.StartsWith("../", StringComparison.Ordinal) then
            Error "task file must be contained by the active project root"
        elif not (Regex.IsMatch(relative, @"^\.tasks/[^/]+/TASK\.md$", RegexOptions.IgnoreCase)) then
            Error "task file must match .tasks/<TASK-ID>/TASK.md under the active project root"
        else
            let mutable directory = FileInfo(fullPath).Directory
            let mutable reparsePoint = false

            while not (isNull directory) && not (directory.FullName.Equals(fullRoot, comparison)) do
                if directory.Attributes.HasFlag FileAttributes.ReparsePoint then
                    reparsePoint <- true
                directory <- directory.Parent

            if isNull directory then Error "task file escaped the active project root"
            elif reparsePoint then Error "task file path must not traverse a reparse point"
            else Ok fullPath
    with ex ->
        Error $"invalid task file path: {ex.Message}"

/// Line indexes outside fenced Markdown code blocks.
let contentLineIndexes (lines: ResizeArray<string>) =
    let mutable openingFence: string option = None

    lines
    |> Seq.mapi (fun i line ->
        let matched = fenceRegex.Match line
        match openingFence with
        | None when matched.Success ->
            openingFence <- Some matched.Groups.["fence"].Value
            None
        | Some opening when matched.Success ->
            let candidate = matched.Groups.["fence"].Value
            if candidate.[0] = opening.[0] && candidate.Length >= opening.Length then
                openingFence <- None
            None
        | Some _ -> None
        | None -> Some i)
    |> Seq.choose id
    |> Set.ofSeq

/// The <id> of a subtask heading line (e.g. "3", "2a", "3.1", "C0"), or None.
let tryHeadingId (line: string) : string option =
    let m = headingRegex.Match(line)
    if m.Success then Some m.Groups.["id"].Value else None

let isHeading (line: string) = headingRegex.IsMatch(line)

/// All subtask headings as (line-index, line) pairs, in document order.
let parseHeadings (lines: ResizeArray<string>) =
    let content = contentLineIndexes lines

    lines
    |> Seq.mapi (fun i l -> i, l)
    |> Seq.filter (fun (i, l) -> content.Contains i && isHeading l)
    |> Seq.toList

/// The lines belonging to the heading at index `i` (up to the next heading / EOF).
let subtaskRange (content: Set<int>) (headings: (int * string) list) (lines: ResizeArray<string>) (i: int) =
    let next =
        headings
        |> List.tryFind (fun (j, _) -> j > i)
        |> Option.map fst
        |> Option.defaultValue lines.Count

    [ for k in i + 1 .. next - 1 do if content.Contains k then yield lines.[k] ]

/// A block is "complete" iff it has at least one checkbox and all are ticked.
let allChecked (block: string list) =
    let checks = block |> List.filter (fun l -> Regex.IsMatch(l, @"^\s*-\s+\[[ xX]\]"))
    not checks.IsEmpty && checks |> List.forall (fun l -> Regex.IsMatch(l, @"^\s*-\s+\[[xX]\]"))

/// (completed, total) subtask counts for a TASK.md.
let computeProgress (lines: ResizeArray<string>) =
    let content = contentLineIndexes lines
    let headings = parseHeadings lines
    let n = headings.Length

    let x =
        headings
        |> List.filter (fun (i, _) -> allChecked (subtaskRange content headings lines i))
        |> List.length

    x, n
