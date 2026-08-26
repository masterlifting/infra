#load "TaskMd.fsx"

open System
open System.IO
open TaskMd

let assertEqual name expected actual =
    if actual <> expected then failwithf "%s: expected %A, got %A" name expected actual

let lines =
    ResizeArray [
        "# TEST-1 - Parser fixture"
        "## Context"
        "### 99. Numeric context heading"
        "- [ ] Must not affect progress"
        "## Subtasks"
        "### 1. Complete task"
        "- [x] Complete"
        "```markdown"
        "### 3. Fenced Markdown code example"
        "- [ ] Not a task checkbox"
        "```"
        "### 2. Incomplete task"
        "- [ ] Pending"
        "## Closing Steps"
        "### C1. Closing step"
        "- [X] Complete with uppercase marker"
        "~~~text"
        "### 4. Hidden by tilde fence"
        "~~~"
        "## Decisions"
        "### 5. Numeric decision heading"
        "- [ ] Must not extend the final closing-step range"
    ]

let headings = parseHeadings lines
let completed, total = computeProgress lines
let ids = headings |> List.choose (snd >> tryHeadingId)

assertEqual "lifecycle heading IDs" [ "1"; "2"; "C1" ] ids
assertEqual "completed subtasks" 2 completed
assertEqual "total subtasks" 3 total
assertEqual "valid task ID" (Ok "BACK-123") (tryTaskId "BACK-123")
assertEqual "discovery review profiles" [ "routine"; "contract"; "architecture"; "combined" ] discoveryReviewProfiles
assertEqual "valid review profiles" (Set.ofList [ "TBD"; "routine"; "contract"; "architecture"; "combined" ]) validReviewProfiles
assertEqual "review profile constraint" "TBD, routine, contract, architecture, or combined" reviewProfileConstraint

match tryTaskId "../BACK-123" with
| Error _ -> ()
| Ok value -> failwithf "unsafe task ID unexpectedly accepted: %s" value

let fenceLines =
    ResizeArray [
        "## Subtasks"
        "### 1. Complete before fence"
        "- [x] Complete"
        "````markdown"
        "### 2. Hidden by opening fence"
        "- [x] Hidden"
        "```` trailing content does not close"
        "### 3. Still hidden after trailing fence content"
        "- [x] Hidden"
        "`````"
        "### 4. Visible after longer closing fence"
        "- [ ] Pending"
        "    ```"
        "### 5. Visible after indented backticks"
        "- [x] Complete"
        "## Closing Steps"
        "### C1. Cleanup"
        "- [x] Complete"
        "## Decisions"
    ]

let fenceHeadings = parseHeadings fenceLines |> List.choose (snd >> tryHeadingId)
let fenceCompleted, fenceTotal = computeProgress fenceLines

assertEqual "fence parsing excludes only properly closed fenced content" [ "1"; "4"; "5"; "C1" ] fenceHeadings
assertEqual "fence parsing progress" (3, 4) (fenceCompleted, fenceTotal)

let contractLines =
    ResizeArray [
        "## Solution Contract"
        "- State: DRAFT"
        "- Accepted assumptions: None recorded."
        "- Chosen solution: TBD"
        "- Important boundaries/contracts: TBD"
        "- Implementation constraints: TBD"
        "- Review profile: TBD"
        "## Review"
        "- State: NEW"
        "- Implementation baseline: TBD"
        "- Remediation pass: 0"
        "- Build evidence: Not run."
        "- Test evidence: Not run."
        "### Accepted findings"
        "| ID | Contract | Status |"
        "| -- | -------- | ------ |"
        "| FIND-1 | Durable contract | FIXED |"
        "### Verification receipts"
        "| Finding ID | Result | Evidence |"
        "| ---------- | ------ | -------- |"
        "| FIND-1 | FIXED | TaskWorkflowTests |"
        "## Notes"
        "- State: ignored outside contract sections"
    ]

let solutionMarkers =
    sectionLines solutionContractHeading contractLines
    |> markerValues solutionStatePrefix

let reviewSection = sectionLines reviewHeading contractLines
let findingRows = tableRows acceptedFindingsHeading reviewSection
let receiptRows = tableRows verificationReceiptsHeading reviewSection

assertEqual "solution contract marker is isolated from review state" [ (1, "DRAFT") ] solutionMarkers
assertEqual "accepted finding rows exclude markdown table metadata" [ (16, [ "FIND-1"; "Durable contract"; "FIXED" ]) ] findingRows
assertEqual "verification receipt rows exclude markdown table metadata" [ (20, [ "FIND-1"; "FIXED"; "TaskWorkflowTests" ]) ] receiptRows

// Optional behavioral specification: SPEC.md path resolution must stay strictly
// under .tasks/<TASK-ID>/SPEC.md relative to root, reject task files outside the
// canonical pattern, and reject missing task files.
let specFixtureRoot = Path.Combine(Path.GetTempPath(), "opencode", $"taskmd-spec-tests-{Guid.NewGuid():N}")
try
    let specTaskDirectory = Path.Combine(specFixtureRoot, ".tasks", "TASK-201")
    Directory.CreateDirectory specTaskDirectory |> ignore
    let specTaskFile = Path.Combine(specTaskDirectory, "TASK.md")
    File.WriteAllText(specTaskFile, "# TASK-201 - Fixture")

    let expectedSpecPath = Path.Combine(specTaskDirectory, "SPEC.md")
    match tryResolveSpecPath specFixtureRoot specTaskFile with
    | Ok resolved when String.Equals(resolved, expectedSpecPath, StringComparison.OrdinalIgnoreCase) -> ()
    | Ok resolved -> failwithf "spec path resolution returned %s, expected %s" resolved expectedSpecPath
    | Error message -> failwithf "spec path resolution failed: %s" message

    let strayTaskFile = Path.Combine(specFixtureRoot, "TASK.md")
    File.WriteAllText(strayTaskFile, "# TASK-201 - Fixture")
    match tryResolveSpecPath specFixtureRoot strayTaskFile with
    | Error _ -> ()
    | Ok _ -> failwith "spec path resolution accepted a task file outside .tasks/<TASK-ID>/TASK.md"

    File.Delete specTaskFile
    match tryResolveSpecPath specFixtureRoot specTaskFile with
    | Error _ -> ()
    | Ok _ -> failwith "spec path resolution accepted a missing task file"
finally
    if Directory.Exists specFixtureRoot then Directory.Delete(specFixtureRoot, true)

// syncProgressCounters is the single shared recompute helper used by both
// RecomputeProgress.fsx and ValidateTask.fsx --sync. Every `**Progress:` line is
// rewritten to the freshly computed X/N; the change flag reports whether any line
// changed so callers can skip durable writes when nothing is stale.
let syncFixture =
    ResizeArray [
        "# TEST-1 - Parser fixture"
        "**Progress: 0/5 subtasks complete** | **Status: In Progress** | **Created: 2026-01-01**"
        "## Subtasks"
        "### 1. Complete task"
        "- [x] Complete"
        "### 2. Incomplete task"
        "- [ ] Pending"
        "## Closing Steps"
        "### C1. Cleanup"
        "- [x] Done"
        "## Decisions"
    ]

let syncCompleted, syncTotal, syncChanged = syncProgressCounters syncFixture
assertEqual "sync recompute counts" (2, 3, true) (syncCompleted, syncTotal, syncChanged)
assertEqual
    "sync rewrote the progress line"
    "**Progress: 2/3 subtasks complete** | **Status: In Progress** | **Created: 2026-01-01**"
    syncFixture.[1]

let stableFixture = ResizeArray(syncFixture)
let stableCompleted, stableTotal, stableChanged = syncProgressCounters stableFixture
assertEqual "sync idempotent counts" (2, 3, false) (stableCompleted, stableTotal, stableChanged)
assertEqual "sync idempotent keeps the header" syncFixture.[1] stableFixture.[1]

let multiProgressFixture =
    ResizeArray [
        "**Progress: 0/1 subtasks complete** | **Status: In Progress** | **Created: 2026-01-01**"
        "**Progress: 0/1 subtasks complete**"
        "## Subtasks"
        "### 1. Complete task"
        "- [x] Complete"
        "## Closing Steps"
        "### C1. Cleanup"
        "- [x] Done"
        "## Decisions"
    ]

let _, _, multiChanged = syncProgressCounters multiProgressFixture
assertEqual "sync rewrites every progress line" true multiChanged
assertEqual "second progress line rewritten too" "**Progress: 2/2 subtasks complete**" multiProgressFixture.[1]

printfn "OK task markdown lifecycle parsing, progress sync, and ID validation"
