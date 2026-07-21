module TaskMd

// Shared TASK.md parsing helpers — SINGLE SOURCE OF TRUTH for "what is a subtask
// heading" and how progress (X/N) is counted. `#load`-ed by both ValidateTask.fsx and
// RecomputeProgress.fsx so the two scripts can never disagree on heading detection
// or the completion rule.

open System
open System.IO
open System.Text
open System.Text.RegularExpressions

/// Canonical subtask-heading pattern. A heading is `### <id>.` where <id> is a
/// closing step (C0, C1, …), a decimal sub-subtask (3.1), or a number with an
/// optional letter suffix (3, 2a). Suffixes are still MATCHED here so ValidateTask.fsx
/// can flag them — they are not allowed in tasks. This is the ONE heading definition.
let headingRegex = Regex(@"^###\s+(?<id>C\d+|\d+\.\d+|\d+[a-z]?)\.")
let taskIdRegex = Regex(@"^[A-Za-z]+-\d+$")
let requiredCodeGateLabels =
    [ "Engineer-owned implementation completed"
      "Engineer-owned build verdict recorded"
      "Tester inspected existing coverage"
      "Substantive reviewer verdict recorded" ]

let fenceRegex = Regex(@"^\s*(?<fence>`{3,}|~{3,})")

let tryTaskId (taskId: string) =
    let candidate = taskId.Trim()
    if taskIdRegex.IsMatch candidate then Ok candidate
    else Error "task ID must match <LETTERS>-<DIGITS> (for example BACK-123)"

let private isReparsePoint (path: string) =
    File.GetAttributes(path).HasFlag FileAttributes.ReparsePoint

let tryResolveNewTaskPath (root: string) (taskId: string) =
    try
        let fullRoot = DirectoryInfo(Path.GetFullPath root).FullName

        match tryTaskId taskId with
        | Error message -> Error message
        | Ok validTaskId ->
            let tasksRoot = Path.Combine(fullRoot, ".tasks")
            let taskDirectory = Path.Combine(tasksRoot, validTaskId)
            let taskPath = Path.Combine(taskDirectory, "TASK.md")

            if not (Directory.Exists fullRoot) then
                Error $"project root does not exist: {fullRoot}"
            elif isReparsePoint fullRoot then
                Error "project root must not be a reparse point"
            elif Directory.Exists tasksRoot && isReparsePoint tasksRoot then
                Error ".tasks must not be a reparse point"
            elif Directory.Exists taskDirectory && isReparsePoint taskDirectory then
                Error "task directory must not be a reparse point"
            elif File.Exists taskPath then
                Error $"task already exists; resume it instead: {taskPath}"
            else
                Ok taskPath
    with ex ->
        Error $"invalid task creation path: {ex.Message}"

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

let lifecycleBounds (lines: ResizeArray<string>) =
    let content = contentLineIndexes lines
    let indexed = lines |> Seq.mapi (fun i line -> i, line) |> Seq.toList

    let subtasks =
        indexed
        |> List.tryFind (fun (i, line) -> content.Contains i && line.Trim() = "## Subtasks")

    subtasks
    |> Option.map (fun (subtasksIndex, _) ->
        let closing =
            indexed
            |> List.tryFind (fun (i, line) ->
                i > subtasksIndex && content.Contains i && line.Trim() = "## Closing Steps")

        let searchAfter = closing |> Option.map fst |> Option.defaultValue subtasksIndex
        let lifecycleEnd =
            indexed
            |> List.tryFind (fun (i, line) ->
                i > searchAfter && content.Contains i && Regex.IsMatch(line, @"^##\s+") && line.Trim() <> "## Closing Steps")
            |> Option.map fst
            |> Option.defaultValue lines.Count

        subtasksIndex + 1, lifecycleEnd)

let lifecycleContentLineIndexes (lines: ResizeArray<string>) =
    let content = contentLineIndexes lines
    match lifecycleBounds lines with
    | Some (first, afterLast) -> content |> Set.filter (fun i -> i >= first && i < afterLast)
    | None -> Set.empty

let sectionContentLineIndexes (heading: string) (lines: ResizeArray<string>) =
    let content = contentLineIndexes lines
    let indexed = lines |> Seq.mapi (fun i line -> i, line) |> Seq.toList
    match indexed |> List.tryFind (fun (i, line) -> content.Contains i && line.Trim() = heading) with
    | None -> Set.empty
    | Some (start, _) ->
        let afterLast =
            indexed
            |> List.tryFind (fun (i, line) -> i > start && content.Contains i && Regex.IsMatch(line, @"^##\s+"))
            |> Option.map fst
            |> Option.defaultValue lines.Count
        content |> Set.filter (fun i -> i > start && i < afterLast)

/// The <id> of a subtask heading line (e.g. "3", "2a", "3.1", "C0"), or None.
let tryHeadingId (line: string) : string option =
    let m = headingRegex.Match(line)
    if m.Success then Some m.Groups.["id"].Value else None

let isHeading (line: string) = headingRegex.IsMatch(line)

/// All subtask headings as (line-index, line) pairs, in document order.
let parseHeadings (lines: ResizeArray<string>) =
    let content = lifecycleContentLineIndexes lines

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
        |> Option.defaultValue (lifecycleBounds lines |> Option.map snd |> Option.defaultValue lines.Count)

    [ for k in i + 1 .. next - 1 do if content.Contains k then yield lines.[k] ]

/// A block is "complete" iff it has at least one checkbox and all are ticked.
let allChecked (block: string list) =
    let checks = block |> List.filter (fun l -> Regex.IsMatch(l, @"^\s*-\s+\[[ xX]\]"))
    not checks.IsEmpty && checks |> List.forall (fun l -> Regex.IsMatch(l, @"^\s*-\s+\[[xX]\]"))

/// (completed, total) subtask counts for a TASK.md.
let computeProgress (lines: ResizeArray<string>) =
    let content = lifecycleContentLineIndexes lines
    let headings = parseHeadings lines
    let n = headings.Length

    let x =
        headings
        |> List.filter (fun (i, _) -> allChecked (subtaskRange content headings lines i))
        |> List.length

    x, n

let tryWriteAllLinesIfUnchanged (path: string) (original: string array) (updated: ResizeArray<string>) =
    try
        use stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)
        use reader = new StreamReader(stream, Encoding.UTF8, true, 1024, true)
        let currentText = reader.ReadToEnd()
        let current = ResizeArray<string>()
        use lineReader = new StringReader(currentText)
        let mutable line = lineReader.ReadLine()
        while not (isNull line) do
            current.Add line
            line <- lineReader.ReadLine()

        if current.ToArray() <> original then
            Error "task file changed after it was read; refusing to overwrite newer edits"
        else
            stream.Position <- 0L
            stream.SetLength 0L
            use writer = new StreamWriter(stream, UTF8Encoding(false), 1024, true)
            for updatedLine in updated do writer.WriteLine updatedLine
            writer.Flush()
            stream.Flush true
            Ok ()
    with :? IOException as error ->
        Error $"task file could not be locked for a safe update: {error.Message}"
