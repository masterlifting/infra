#load "TaskMd.fsx"

open System
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

printfn "OK task markdown lifecycle parsing and ID validation"
