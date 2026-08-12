// Validate a TASK.md against the invariants in references/validation.md.
// Usage:
//   dotnet fsi "C:/Users/andre/.config/opencode/skills/task/scripts/ValidateTask.fsx" <path-to-TASK.md> [--fix]
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

let requireSingleMarker sectionName prefix values =
    match values with
    | [ (_, value) ] when not (String.IsNullOrWhiteSpace value) -> Some value
    | _ ->
        report $"{sectionName} must contain exactly one non-empty '{prefix}...' marker"
        None

let solutionContractLines = sectionLines solutionContractHeading lines
let reviewLines = sectionLines reviewHeading lines
let solutionState = requireSingleMarker solutionContractHeading solutionStatePrefix (markerValues solutionStatePrefix solutionContractLines)
let requirements = requireSingleMarker solutionContractHeading requirementsPrefix (markerValues requirementsPrefix solutionContractLines)
let acceptanceCriteria = requireSingleMarker solutionContractHeading acceptanceCriteriaPrefix (markerValues acceptanceCriteriaPrefix solutionContractLines)
let acceptedAssumptions = requireSingleMarker solutionContractHeading acceptedAssumptionsPrefix (markerValues acceptedAssumptionsPrefix solutionContractLines)
let nonGoals = requireSingleMarker solutionContractHeading nonGoalsPrefix (markerValues nonGoalsPrefix solutionContractLines)
let chosenSolution = requireSingleMarker solutionContractHeading chosenSolutionPrefix (markerValues chosenSolutionPrefix solutionContractLines)
let importantContracts = requireSingleMarker solutionContractHeading importantContractsPrefix (markerValues importantContractsPrefix solutionContractLines)
let implementationConstraints = requireSingleMarker solutionContractHeading implementationConstraintsPrefix (markerValues implementationConstraintsPrefix solutionContractLines)
let reviewProfile = requireSingleMarker solutionContractHeading reviewProfilePrefix (markerValues reviewProfilePrefix solutionContractLines)
let rejectedAlternatives = requireSingleMarker solutionContractHeading rejectedAlternativesPrefix (markerValues rejectedAlternativesPrefix solutionContractLines)
let reviewState = requireSingleMarker reviewHeading reviewStatePrefix (markerValues reviewStatePrefix reviewLines)
let implementationBaseline = requireSingleMarker reviewHeading implementationBaselinePrefix (markerValues implementationBaselinePrefix reviewLines)
let remediationPass = requireSingleMarker reviewHeading remediationPassPrefix (markerValues remediationPassPrefix reviewLines)
let buildEvidence = requireSingleMarker reviewHeading buildEvidencePrefix (markerValues buildEvidencePrefix reviewLines)
let testEvidence = requireSingleMarker reviewHeading testEvidencePrefix (markerValues testEvidencePrefix reviewLines)

match solutionState with
| Some state when not (validSolutionStates.Contains state) -> report "Solution Contract State must be DRAFT or FROZEN"
| _ -> ()

match reviewProfile with
| Some profile when not (validReviewProfiles.Contains profile) -> report "Solution Contract Review profile must be TBD, Standard, or Full / architecture-sensitive"
| _ -> ()

let reviewPass =
    match remediationPass with
    | Some value ->
        match Int32.TryParse value with
        | true, pass when pass >= 0 && pass <= 2 -> Some pass
        | _ ->
            report "Review Remediation pass must be an integer from 0 through 2"
            None
    | None -> None

match reviewState with
| Some state when not (validReviewStates.Contains state) -> report "Review State must be NEW, DISCOVERY, REMEDIATION, VERIFICATION, or FROZEN"
| _ -> ()

let acceptedFindingRows = tableRows acceptedFindingsHeading reviewLines
let verificationReceiptRows = tableRows verificationReceiptsHeading reviewLines
let acceptedFindingIds = ResizeArray<string>()
let acceptedFindingStatuses = Collections.Generic.Dictionary<string, string>()
let verificationReceiptIds = ResizeArray<string>()

for (index, cells) in acceptedFindingRows do
    match cells with
    | [ id; contract; status ] when not (String.IsNullOrWhiteSpace id) && not (String.IsNullOrWhiteSpace contract) ->
        if acceptedFindingIds.Contains id then report $"line {index + 1}: accepted finding ID '{id}' is duplicated"
        else
            acceptedFindingIds.Add id
            acceptedFindingStatuses.[id] <- status
        if not (validFindingStatuses.Contains status) then
            report $"line {index + 1}: accepted finding status must be PENDING, FIXED, NOT FIXED, or REGRESSION INTRODUCED"
    | _ -> report $"line {index + 1}: accepted finding requires ID, contract, and status"

for (index, cells) in verificationReceiptRows do
    match cells with
    | [ findingId; result; evidence ] when not (String.IsNullOrWhiteSpace findingId) && not (String.IsNullOrWhiteSpace evidence) ->
        if verificationReceiptIds.Contains findingId then report $"line {index + 1}: verification receipt ID '{findingId}' is duplicated"
        else verificationReceiptIds.Add findingId
        if not (validVerificationResults.Contains result) then
            report $"line {index + 1}: verification result must be APPROVE, FIXED, NOT FIXED, or REGRESSION INTRODUCED"
        elif findingId = "None" && result <> "APPROVE" then
            report $"line {index + 1}: a None verification receipt must have result APPROVE"
        elif findingId <> "None" && not (validFindingVerificationResults.Contains result) then
            report $"line {index + 1}: APPROVE is valid only for Finding ID None"
        elif findingId <> "None" && not (acceptedFindingIds.Contains findingId) then
            report $"line {index + 1}: verification receipt references unknown accepted finding '{findingId}'"
    | _ -> report $"line {index + 1}: verification receipt requires finding ID, result, and evidence"

let requiresFrozenSolution =
    reviewState
    |> Option.exists (fun state -> state <> "NEW")

let evidencePlaceholders =
    Set.ofList
        [ "TBD"
          "TBD."
          "NOT RUN"
          "NOT RUN."
          "NOT APPLICABLE"
          "NOT APPLICABLE."
          "N/A"
          "NA"
          "NONE"
          "NONE."
          "UNKNOWN"
          "PENDING"
          "PENDING."
          "SKIPPED"
          "SKIPPED."
          "LATER"
          "LATER." ]

let isEvidencePlaceholder (value: string) =
    String.IsNullOrWhiteSpace value || evidencePlaceholders.Contains(value.Trim().ToUpperInvariant())

let hasRecordedWaiver (reference: string) =
    sectionLines "## Decisions" lines
    |> List.exists (fun (_, line) ->
        let cells = line.Split('|') |> Array.map (fun value -> value.Trim()) |> Array.filter (fun value -> value <> "")
        cells.Length >= 2
        && Regex.IsMatch(cells.[0], @"^\d{4}-\d{2}-\d{2}$")
        && cells.[1].Contains("waiv", StringComparison.OrdinalIgnoreCase)
        && (cells |> Array.skip 1 |> Array.exists (fun value -> value.Contains(reference, StringComparison.OrdinalIgnoreCase))))

let hasValidEvidence (evidence: string) =
    let value = evidence.Trim()
    let passed = Regex.Match(value, @"^Passed:\s+(?<result>.+)$")
    let notApplicable = Regex.Match(value, @"^Not applicable:\s+(?<rationale>.+)$")
    let waiver = Regex.Match(value, @"^Waived:\s+(?<reference>.+)$")

    if isEvidencePlaceholder value then
        false
    elif passed.Success then
        let result = passed.Groups.["result"].Value
        not (isEvidencePlaceholder result)
        && not (Regex.IsMatch(result, @"\b(?:fail(?:ed|ure)?|pending)\b", RegexOptions.IgnoreCase))
    elif notApplicable.Success then
        not (isEvidencePlaceholder notApplicable.Groups.["rationale"].Value)
    elif waiver.Success then
        let reference = waiver.Groups.["reference"].Value
        not (isEvidencePlaceholder reference) && hasRecordedWaiver reference
    elif value.StartsWith("Not applicable", StringComparison.OrdinalIgnoreCase)
         || value.StartsWith("Waived", StringComparison.OrdinalIgnoreCase) then
        false
    else
        false

let isPopulatedContractValue (value: string) =
    not (String.IsNullOrWhiteSpace value)
    && not (value.Trim().Equals("TBD", StringComparison.OrdinalIgnoreCase))

let isPopulatedAcceptedAssumptions (value: string) =
    isPopulatedContractValue value
    && not (value.Trim().Equals("None recorded.", StringComparison.OrdinalIgnoreCase))

let solutionContractFrozenAndComplete =
    match solutionState, requirements, acceptanceCriteria, acceptedAssumptions, nonGoals, chosenSolution, importantContracts, implementationConstraints, reviewProfile, rejectedAlternatives with
    | Some "FROZEN", Some requirements, Some acceptanceCriteria, Some assumptions, Some nonGoals, Some solution, Some contracts, Some constraints, Some profile, Some alternatives
        when isPopulatedContractValue requirements
             && isPopulatedContractValue acceptanceCriteria
             && isPopulatedAcceptedAssumptions assumptions
             && isPopulatedContractValue nonGoals
             && isPopulatedContractValue solution
             && isPopulatedContractValue contracts
             && isPopulatedContractValue constraints
             && isPopulatedContractValue profile
             && isPopulatedContractValue alternatives -> true
    | _ -> false

if requiresFrozenSolution then
    match solutionState with
    | Some "FROZEN" -> ()
    | _ -> report "Review beyond NEW requires a FROZEN solution contract"
    match solutionContractFrozenAndComplete, implementationBaseline with
    | true, Some baseline when baseline <> "TBD" -> ()
    | _ -> report "Review beyond NEW requires frozen solution details, a selected review profile, and an implementation baseline"
    match buildEvidence, testEvidence with
    | Some build, Some test when hasValidEvidence build && hasValidEvidence test -> ()
    | _ -> report "Review beyond NEW requires exact 'Passed: <command/result>', exact 'Not applicable: <reason>', or a linked 'Waived: <reference>'"

match reviewState, reviewPass with
| Some "NEW", Some 0
| Some "DISCOVERY", Some 0
| Some "REMEDIATION", Some 1
| Some "REMEDIATION", Some 2
| Some "VERIFICATION", Some _
| Some "FROZEN", Some _ -> ()
| Some "NEW", Some _ -> report "Review State NEW requires remediation pass 0"
| Some "DISCOVERY", Some _ -> report "Review State DISCOVERY requires remediation pass 0"
| Some "REMEDIATION", Some _ -> report "Review State REMEDIATION requires remediation pass 1 or 2"
| _ -> ()

match reviewState, reviewPass with
| Some ("VERIFICATION" | "FROZEN"), Some 0 when acceptedFindingIds.Count > 0 ->
    report "Review with accepted findings requires remediation pass 1 or 2 before VERIFICATION or FROZEN"
| _ -> ()

match reviewState with
| Some "FROZEN" ->
    if acceptedFindingIds.Count = 0 then
        let hasApproval = verificationReceiptRows |> List.exists (fun (_, cells) -> cells = [ "None"; "APPROVE" ] || (cells.Length = 3 && cells.[0] = "None" && cells.[1] = "APPROVE"))
        if not hasApproval then report "Review State FROZEN with no accepted findings requires a None/APPROVE verification receipt"
    else
        for findingId in acceptedFindingIds do
            let receipts = verificationReceiptRows |> List.filter (fun (_, cells) -> cells.Length = 3 && cells.[0] = findingId)
            match receipts with
            | [ (_, [ _; "FIXED"; _ ]) ] when acceptedFindingStatuses.[findingId] = "FIXED" -> ()
            | [ (_, [ _; "FIXED"; _ ]) ] -> report $"Review State FROZEN requires accepted finding '{findingId}' status FIXED"
            | [ (_, [ _; result; _ ]) ] -> report $"Review State FROZEN requires accepted finding '{findingId}' to verify as FIXED, not {result}"
            | [] -> report $"Review State FROZEN requires a verification receipt for accepted finding '{findingId}'"
            | _ -> report $"Review State FROZEN requires exactly one verification receipt for accepted finding '{findingId}'"
| _ -> ()

let implementationPlans =
    lines
    |> Seq.mapi (fun i line -> i, line)
    |> Seq.choose (fun (i, line) ->
        if not (contextLines.Contains i) || not (line.StartsWith implementationPlanPrefix) then None
        else Some(line.Substring(implementationPlanPrefix.Length)))
    |> Seq.toList

let designGate =
    subtaskHeadings
    |> List.tryFind (fun (_, line) -> line.Trim() = "### 3. Design gate")

let designGateBlock =
    designGate
    |> Option.map (fun (index, _) -> subtaskRange lifecycleLines subtaskHeadings lines index)

let designGateComplete =
    designGateBlock
    |> Option.map (fun block ->
        requiredDesignGateLabels |> List.forall (fun label -> hasExactCheckedCheckbox label block)
        && (block |> List.filter checkboxRegex.IsMatch |> List.length) = requiredDesignGateLabels.Length
        && allChecked block
        && solutionContractFrozenAndComplete)
    |> Option.defaultValue false

if codeTask && not hasC0 then report "code task is missing required closing step C0"
if codeTask && designGate.IsNone then report "code task is missing required subtask '### 3. Design gate'"
if taskKinds = [ "non-code" ] && hasC0 then report "non-code task must not contain closing step C0"

if codeTask then
    match designGateBlock with
    | Some block ->
        if (block |> List.filter checkboxRegex.IsMatch |> List.length) <> requiredDesignGateLabels.Length then
            report "design gate must contain exactly the canonical required checkboxes"
        for requiredLabel in requiredDesignGateLabels do
            match exactCheckboxCount requiredLabel block with
            | 0 -> report $"design gate is missing required checkbox: {requiredLabel}"
            | 1 -> ()
            | _ -> report $"design gate must contain exactly one required checkbox: {requiredLabel}"
    | None -> ()

    match designGateBlock with
    | Some block when allChecked block && not solutionContractFrozenAndComplete ->
        report "completed design gate requires a FROZEN Solution Contract with populated requirements, acceptance criteria, accepted assumptions, non-goals, chosen solution, boundaries/contracts, constraints, review profile, and rejected alternatives"
    | _ -> ()

    if not designGateComplete then
        for (index, line) in subtaskHeadings do
            match tryNumberedSubtaskId line with
            | Some (_, Some root) when root >= 5 ->
                let block = subtaskRange lifecycleLines subtaskHeadings lines index
                if block |> List.exists checkedCheckboxRegex.IsMatch then
                    report "implementation and validation work rooted at subtask 5 or later cannot be checked before the design gate completes"
            | _ -> ()

    let decimalAdaptiveHeadings =
        subtaskHeadings
        |> List.choose (fun (index, line) ->
            match tryNumberedHeading line with
            | Some (id, title, _) when id.Contains "." && (title.StartsWith("Implement: ") || title.StartsWith("Validate: ")) ->
                Some index
            | _ -> None)

    if designGateComplete then
        for index in decimalAdaptiveHeadings do
            report $"line {index + 1}: adaptive Implement/Validate subtasks must use top-level integer IDs"

    match implementationPlans with
    | [ plan ] when validImplementationPlans.Contains plan ->
        if designGateComplete then
            let genericPlaceholderPresent =
                lifecycleLines
                |> Seq.exists (fun i -> lines.[i].Trim() = genericImplementationPlaceholder)

            match plan with
            | "TBD" -> report "completed design gate requires an implementation plan of non-complex or complex"
            | "non-complex" ->
                let nonComplexSubtask =
                    subtaskHeadings
                    |> List.tryFind (fun (_, line) -> line.Trim() = genericImplementationHeading)

                match nonComplexSubtask with
                | None -> report "non-complex implementation plan requires '### 5. Implement and validate'"
                | Some (index, _) ->
                    let block = subtaskRange lifecycleLines subtaskHeadings lines index
                    for gateLabel in requiredCodeGateLabels do
                        if not (hasGateCheckbox gateLabel block) then
                            report $"non-complex implementation subtask is missing required gate checkbox: {gateLabel}"

                if genericPlaceholderPresent then
                    report "non-complex implementation plan must remove the generic implementation placeholder"
            | "complex" ->
                let taskSpecificSubtasks =
                    subtaskHeadings
                    |> List.choose (fun (index, line) ->
                        match tryNumberedHeading line with
                        | Some (id, title, Some root) when root >= 5 && not (id.Contains ".") ->
                            Some(index, id, title)
                        | _ -> None)

                let implementationSubtasks =
                    taskSpecificSubtasks
                    |> List.filter (fun (_, _, title) -> title.StartsWith("Implement: "))
                let validationSubtasks =
                    taskSpecificSubtasks
                    |> List.filter (fun (_, _, title) -> title.StartsWith("Validate: "))

                if not (implementationSubtasks |> List.exists (fun (_, id, _) -> id = "5")) then
                    report "complex implementation plan requires a task-specific '### 5. Implement: ...' subtask"
                if validationSubtasks.IsEmpty then
                    report "complex implementation plan requires a task-specific '### <n>. Validate: ...' subtask"
                if subtaskHeadings |> List.exists (fun (_, line) -> line.Trim() = genericImplementationHeading) then
                    report "complex implementation plan must remove '### 5. Implement and validate'"
                if genericPlaceholderPresent then
                    report "complex implementation plan must remove the generic implementation placeholder"

                for (index, _, _) in implementationSubtasks do
                    let block = subtaskRange lifecycleLines subtaskHeadings lines index
                    for gateLabel in implementationGateLabels do
                        if not (hasGateCheckbox gateLabel block) then
                            report $"complex Implement subtask is missing required gate checkbox: {gateLabel}"

                for (index, _, _) in validationSubtasks do
                    let block = subtaskRange lifecycleLines subtaskHeadings lines index
                    for gateLabel in validationGateLabels do
                        if not (hasGateCheckbox gateLabel block) then
                            report $"complex Validate subtask is missing required gate checkbox: {gateLabel}"
            | _ -> ()
    | _ ->
        report "code task ## Context must contain exactly one valid '- Implementation plan: TBD|non-complex|complex' marker"

    for requiredText in requiredCodeGateLabels do
        let found =
            lines
            |> Seq.mapi (fun i line -> i, line)
            |> Seq.exists (fun (i, line) ->
                lifecycleLines.Contains i
                && Regex.IsMatch(line, @"^\s*-\s+\[[ xX]\]\s+" + Regex.Escape requiredText))
        if not found then report $"code task is missing required gate checkbox: {requiredText}"
elif taskKinds = [ "non-code" ] then
    if not implementationPlans.IsEmpty then
        report "non-code task must not contain an implementation plan marker"
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
    match tryHeadingId line with
    | None -> None
    | Some id when id.StartsWith "C" ->
        tryParseSubtaskNumber (id.Substring 1)
        |> Option.map (Choice2Of2 >> Ok)
        |> Option.defaultValue (Error id)
        |> Some
    | Some id when id.Contains "." ->
        let parts = id.Split '.'
        match tryParseSubtaskNumber parts.[0], tryParseSubtaskNumber parts.[1] with
        | Some root, Some decimal -> Some(Ok(Choice1Of2(root, 0, decimal)))
        | _ -> Some(Error id)
    | Some id ->
        let matched = Regex.Match(id, @"^(?<n>\d+)(?<s>[a-z]?)$")
        match tryParseSubtaskNumber matched.Groups.["n"].Value with
        | Some number -> Some(Ok(Choice1Of2(number, suffixRank matched.Groups.["s"].Value, 0)))
        | None -> Some(Error id)
let mutable lastNumKey : (int * int * int) option = None
let mutable lastClosing : int option = None
for (i, l) in subtaskHeadings do
    match parseSubtaskId l with
    | None -> ()
    | Some (Error id) ->
        report (sprintf "line %d: subtask id '%s' exceeds the supported numeric range" (i + 1) id)
    | Some (Ok (Choice1Of2 key)) ->
        if lastClosing.IsSome then
            report (sprintf "line %d: numbered subtask after a C-step — closing steps must be last" (i+1))
        match lastNumKey with
        | Some prev when key <= prev ->
            report (sprintf "line %d: subtask numbering not ascending (numbers must never be reused)" (i+1))
        | _ -> ()
        lastNumKey <- Some key
    | Some (Ok (Choice2Of2 c)) ->
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
