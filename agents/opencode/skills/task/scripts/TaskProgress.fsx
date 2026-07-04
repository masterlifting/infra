module TaskProgress

open System
open System.Text.RegularExpressions

type Count =
    { Completed: int
      Total: int }

let private subtaskHeadingRe = Regex(@"^###\s+([0-9]+(\.[0-9]+)*|C[0-9]+)\.")
let private checkboxRe = Regex(@"^\s*-\s+\[[ x]\]")
let private checkedRe = Regex(@"^\s*-\s+\[x\]")

let private isSubtasksHeading (line: string) =
    line.Trim().Equals("## Subtasks", StringComparison.Ordinal)

let private isH2Heading (line: string) =
    Regex.IsMatch(line, @"^##\s+")

let private sectionBounds (lines: string[]) =
    lines
    |> Array.tryFindIndex isSubtasksHeading
    |> Option.map (fun start ->
        let finish =
            lines
            |> Array.mapi (fun index line -> index, line)
            |> Array.tryFind (fun (index, line) -> index > start && isH2Heading line)
            |> Option.map fst
            |> Option.defaultValue lines.Length

        start + 1, finish)

let private allChecked (block: string list) =
    let checks = block |> List.filter checkboxRe.IsMatch
    not checks.IsEmpty && checks |> List.forall checkedRe.IsMatch

let subtaskRanges (lines: string seq) =
    let lines = lines |> Seq.toArray

    match sectionBounds lines with
    | None -> []
    | Some(start, finish) ->
        let headings =
            [ start .. finish - 1 ]
            |> List.filter (fun index -> subtaskHeadingRe.IsMatch lines.[index])

        headings
        |> List.mapi (fun headingIndex lineIndex ->
            let next =
                headings
                |> List.tryItem (headingIndex + 1)
                |> Option.defaultValue finish

            lineIndex, [ for index in lineIndex + 1 .. next - 1 -> lines.[index] ])

let count (lines: string seq) =
    let ranges = subtaskRanges lines

    { Completed = ranges |> List.filter (snd >> allChecked) |> List.length
      Total = ranges.Length }
