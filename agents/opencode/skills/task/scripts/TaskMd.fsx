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
let numberedSubtaskIdRegex = Regex(@"^\d+(?:\.\d+)?$")
let numberedHeadingRegex = Regex(@"^###\s+(?<id>\d+(?:\.\d+)?)\.\s+(?<title>.+)$")
let taskIdRegex = Regex(@"^[A-Za-z]+-\d+$")
let requiredCodeGateLabels =
    [ "Engineer-owned implementation completed"
      "Engineer-owned build verdict recorded"
      "Tester inspected existing coverage" ]

let requiredDesignGateLabels =
    [ "Architecture routed per `references/agent-gates.md`; independent proposals used only when that gate requires them, then coordinator solution frozen"
      "Conditional specialists run per `references/agent-gates.md` or explicitly N/A"
      "Set `Implementation plan` to `non-complex` or `complex`; approve the final task-specific structure with no generic planning placeholders" ]

let implementationGateLabels =
    [ requiredCodeGateLabels.[0]
      requiredCodeGateLabels.[1] ]

let validationGateLabels = [ requiredCodeGateLabels.[2] ]
let implementationPlanPrefix = "- Implementation plan: "
let behavioralSpecReferencePrefix = "- Behavioral specification: "
let validImplementationPlans = Set.ofList [ "TBD"; "non-complex"; "complex" ]
let genericImplementationHeading = "### 5. Implement and validate"
let genericImplementationPlaceholder = "<!-- Add task-specific implementation and validation steps here for a non-complex task. -->"
let solutionContractHeading = "## Solution Contract"
let reviewHeading = "## Review"
let solutionStatePrefix = "- State: "
let requirementsPrefix = "- Requirements: "
let acceptanceCriteriaPrefix = "- Acceptance criteria: "
let acceptedAssumptionsPrefix = "- Accepted assumptions: "
let nonGoalsPrefix = "- Non-goals: "
let chosenSolutionPrefix = "- Chosen solution: "
let importantContractsPrefix = "- Important boundaries/contracts: "
let implementationConstraintsPrefix = "- Implementation constraints: "
let reviewProfilePrefix = "- Review profile: "
let rejectedAlternativesPrefix = "- Rejected alternatives: "
let reviewStatePrefix = "- State: "
let implementationBaselinePrefix = "- Implementation baseline: "
let remediationPassPrefix = "- Remediation pass: "
let buildEvidencePrefix = "- Build evidence: "
let testEvidencePrefix = "- Test evidence: "
let acceptedFindingsHeading = "### Accepted findings"
let verificationReceiptsHeading = "### Verification receipts"
let validSolutionStates = Set.ofList [ "DRAFT"; "FROZEN" ]
let validReviewStates = Set.ofList [ "NEW"; "DISCOVERY"; "REMEDIATION"; "VERIFICATION"; "FROZEN" ]
let discoveryReviewProfiles = [ "routine"; "contract"; "architecture"; "combined" ]
let validReviewProfileValues = "TBD" :: discoveryReviewProfiles
let validReviewProfiles = Set.ofList validReviewProfileValues

let reviewProfileConstraint =
    let leading = validReviewProfileValues |> List.take (validReviewProfileValues.Length - 1) |> String.concat ", "
    $"{leading}, or {List.last validReviewProfileValues}"

let validFindingStatuses = Set.ofList [ "PENDING"; "FIXED"; "NOT FIXED"; "REGRESSION INTRODUCED" ]
let validVerificationResults = Set.ofList [ "APPROVE"; "FIXED"; "NOT FIXED"; "REGRESSION INTRODUCED" ]
let validFindingVerificationResults = Set.ofList [ "FIXED"; "NOT FIXED"; "REGRESSION INTRODUCED" ]

let openingFenceRegex = Regex(@"^ {0,3}(?<fence>`{3,}|~{3,})")
let checkboxRegex = Regex(@"^\s*-\s+\[[ xX]\]")
let checkedCheckboxRegex = Regex(@"^\s*-\s+\[[xX]\]")

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

/// Resolve the optional `.tasks/<TASK-ID>/SPEC.md` that a TASK.md references.
/// Mirrors tryResolveTaskPath's safety checks: the spec must sit strictly under
/// `.tasks/<TASK-ID>/SPEC.md` relative to root, must not be or traverse a reparse
/// point, and must not escape the root or mismatch the task folder. The file itself
/// may be absent — callers decide how to report a missing spec.
let tryResolveSpecPath (root: string) (taskFilePath: string) =
    try
        let fullRoot = DirectoryInfo(Path.GetFullPath root).FullName
        let fullTaskPath = Path.GetFullPath taskFilePath
        let specPath = Path.Combine(Path.GetDirectoryName fullTaskPath, "SPEC.md")
        let relative = Path.GetRelativePath(fullRoot, specPath).Replace("\\", "/")
        let comparison = if OperatingSystem.IsWindows() then StringComparison.OrdinalIgnoreCase else StringComparison.Ordinal

        if not (Directory.Exists fullRoot) then
            Error $"project root does not exist: {fullRoot}"
        elif not (File.Exists fullTaskPath) then
            Error $"task file does not exist: {fullTaskPath}"
        elif not (Regex.IsMatch(Path.GetRelativePath(fullRoot, fullTaskPath).Replace("\\", "/"), @"^\.tasks/[^/]+/TASK\.md$", RegexOptions.IgnoreCase)) then
            Error "task file must match .tasks/<TASK-ID>/TASK.md under the active project root"
        elif Path.IsPathRooted relative || relative = ".." || relative.StartsWith("../", StringComparison.Ordinal) then
            Error "SPEC.md must be contained by the active project root"
        elif not (Regex.IsMatch(relative, @"^\.tasks/[^/]+/SPEC\.md$", RegexOptions.IgnoreCase)) then
            Error "SPEC.md must match .tasks/<TASK-ID>/SPEC.md under the active project root"
        else
            let mutable directory = FileInfo(specPath).Directory
            let mutable reparsePoint = false

            while not (isNull directory) && not (directory.FullName.Equals(fullRoot, comparison)) do
                if directory.Attributes.HasFlag FileAttributes.ReparsePoint then
                    reparsePoint <- true
                directory <- directory.Parent

            if isNull directory then Error "SPEC.md escaped the active project root"
            elif reparsePoint then Error "SPEC.md path must not traverse a reparse point"
            elif File.Exists specPath && FileInfo(specPath).Attributes.HasFlag FileAttributes.ReparsePoint then
                Error "SPEC.md must not be a reparse point"
            else Ok specPath
    with ex ->
        Error $"invalid SPEC.md path: {ex.Message}"

/// Line indexes outside fenced Markdown code blocks.
let contentLineIndexes (lines: ResizeArray<string>) =
    let mutable openingFence: string option = None

    let closesFence (opening: string) (line: string) =
        let leadingSpaces = line |> Seq.takeWhile ((=) ' ') |> Seq.length

        if leadingSpaces > 3 || line.Length <= leadingSpaces || line.[leadingSpaces] <> opening.[0] then
            false
        else
            let candidateLength =
                line.Substring(leadingSpaces)
                |> Seq.takeWhile ((=) opening.[0])
                |> Seq.length

            candidateLength >= opening.Length
            && line.Substring(leadingSpaces + candidateLength).Trim().Length = 0

    lines
    |> Seq.mapi (fun i line ->
        match openingFence with
        | None ->
            let matched = openingFenceRegex.Match line
            if matched.Success then
                openingFence <- Some matched.Groups.["fence"].Value
                None
            else
                Some i
        | Some opening when closesFence opening line ->
            openingFence <- None
            None
        | Some _ -> None)
    |> Seq.choose id
    |> Set.ofSeq

let tryParseSubtaskNumber (value: string) =
    match Int32.TryParse value with
    | true, number -> Some number
    | false, _ -> None

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

let sectionLines (heading: string) (lines: ResizeArray<string>) =
    sectionContentLineIndexes heading lines
    |> Seq.map (fun index -> index, lines.[index])
    |> Seq.toList

let markerValues (prefix: string) (section: (int * string) list) =
    section
    |> List.choose (fun (index, line) ->
        if line.StartsWith(prefix, StringComparison.Ordinal) then
            Some(index, line.Substring(prefix.Length).Trim())
        else
            None)

let private tryMarkdownHeadingLevel (line: string) =
    let matched = Regex.Match(line, @"^ {0,3}(?<marks>#{1,6})(?:\s|$)")
    if matched.Success then Some matched.Groups.["marks"].Length else None

let tableRows (heading: string) (section: (int * string) list) =
    let headingIndex = section |> List.tryFind (fun (_, line) -> line.Trim() = heading) |> Option.map fst
    let headingLevel = tryMarkdownHeadingLevel heading

    match headingIndex, headingLevel with
    | None, _
    | _, None -> []
    | Some start, Some level ->
        section
        |> List.filter (fun (index, _) -> index > start)
        |> List.takeWhile (fun (_, line) ->
            match tryMarkdownHeadingLevel line with
            | Some nextLevel -> nextLevel > level
            | None -> true)
        |> List.filter (fun (_, line) -> line.StartsWith("|", StringComparison.Ordinal))
        |> List.choose (fun (index, line) ->
            let cells = line.Split('|') |> Array.map (fun value -> value.Trim())
            if cells.Length < 3 then
                Some(index, [])
            else
                let values = cells.[1 .. cells.Length - 2]
                let isSeparator = values |> Array.forall (fun value -> Regex.IsMatch(value, @"^:?-+:?$") )
                let isHeader = values |> Array.exists (fun value -> value.Equals("ID", StringComparison.OrdinalIgnoreCase) || value.Equals("Finding ID", StringComparison.OrdinalIgnoreCase))
                if isSeparator || isHeader then None else Some(index, values |> Array.toList))

/// The <id> of a subtask heading line (e.g. "3", "2a", "3.1", "C0"), or None.
let tryHeadingId (line: string) : string option =
    let m = headingRegex.Match(line)
    if m.Success then Some m.Groups.["id"].Value else None

/// A numbered subtask ID and its safely parsed top-level number, if representable.
let tryNumberedSubtaskId (line: string) =
    match tryHeadingId line with
    | Some id when numberedSubtaskIdRegex.IsMatch id ->
        let root = id.Split('.').[0]
        Some(id, tryParseSubtaskNumber root)
    | _ -> None

/// A numbered subtask heading with its title and safely parsed top-level number.
let tryNumberedHeading (line: string) =
    let matched = numberedHeadingRegex.Match line
    if matched.Success then
        let id = matched.Groups.["id"].Value
        let root = id.Split('.').[0]
        Some(id, matched.Groups.["title"].Value, tryParseSubtaskNumber root)
    else
        None

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

let hasGateCheckbox (gateLabel: string) (block: string list) =
    block
    |> List.exists (fun line ->
        Regex.IsMatch(line, @"^\s*-\s+\[[ xX]\]\s+" + Regex.Escape gateLabel))

let hasExactCheckbox (label: string) (block: string list) =
    block
    |> List.exists (fun line ->
        Regex.IsMatch(line, @"^\s*-\s+\[[ xX]\]\s+" + Regex.Escape label + @"\s*$"))

let exactCheckboxCount (label: string) (block: string list) =
    block
    |> List.filter (fun line ->
        Regex.IsMatch(line, @"^\s*-\s+\[[ xX]\]\s+" + Regex.Escape label + @"\s*$"))
    |> List.length

let hasExactCheckedCheckbox (label: string) (block: string list) =
    block
    |> List.exists (fun line ->
        Regex.IsMatch(line, @"^\s*-\s+\[[xX]\]\s+" + Regex.Escape label + @"\s*$"))

/// A block is "complete" iff it has at least one checkbox and all are ticked.
let allChecked (block: string list) =
    let checks = block |> List.filter checkboxRegex.IsMatch
    not checks.IsEmpty && checks |> List.forall checkedCheckboxRegex.IsMatch

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
