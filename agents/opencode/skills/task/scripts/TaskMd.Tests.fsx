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

printfn "OK task markdown lifecycle parsing and ID validation"
