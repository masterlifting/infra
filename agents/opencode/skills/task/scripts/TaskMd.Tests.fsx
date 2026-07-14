#load "TaskMd.fsx"

open TaskMd

let lines =
    ResizeArray [
        "### 1. Real task"
        "- [x] Complete"
        "    ### 2. Markdown code example"
        "```markdown"
        "### 3. Fenced Markdown code example"
        "- [ ] Not a task checkbox"
        "```"
        "### C0. Closing step"
        "- [X] Complete with uppercase marker"
        "~~~text"
        "### 4. Hidden by tilde fence"
        "```"
        "### 5. Still hidden because fence type differs"
        "~~~"
    ]

let headings = parseHeadings lines
let completed, total = computeProgress lines

if headings.Length <> 2 then
    failwithf "expected 2 headings, got %d" headings.Length

if completed <> 2 || total <> 2 then
    failwithf "expected progress 2/2, got %d/%d" completed total

printfn "OK — task markdown heading parsing"
