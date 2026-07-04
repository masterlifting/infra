// Recompute Progress: X/N from checkbox state. Idempotent; safe to run on every edit.
// Usage:
//   dotnet fsi recompute-progress.fsx <path-to-TASK.md>

open System
open System.IO
open System.Text.RegularExpressions

let args = fsi.CommandLineArgs |> Array.skip 1

if args.Length < 1 then
    eprintfn "usage: recompute-progress.fsx <TASK.md>"
    exit 2

let path = args.[0]
let lines = File.ReadAllLines path |> ResizeArray

let headings =
    lines
    |> Seq.mapi (fun i l -> i, l)
    |> Seq.filter (fun (_, l) -> Regex.IsMatch(l, @"^###\s+[0-9C]\w*[\.\d]*\."))
    |> Seq.toList

let rangeOf (i: int) =
    let next =
        headings
        |> List.tryFind (fun (j, _) -> j > i)
        |> Option.map fst
        |> Option.defaultValue lines.Count

    [ for k in i + 1 .. next - 1 -> lines.[k] ]

let allChecked (block: string list) =
    let checks = block |> List.filter (fun l -> Regex.IsMatch(l, @"^\s*-\s+\[[ x]\]"))
    not checks.IsEmpty && checks |> List.forall (fun l -> Regex.IsMatch(l, @"^\s*-\s+\[x\]"))

let n = headings.Length
let x = headings |> List.filter (fun (i, _) -> allChecked (rangeOf i)) |> List.length

let mutable updated = false

for i in 0 .. lines.Count - 1 do
    let l = lines.[i]

    if l.StartsWith "**Progress:" then
        let replaced = Regex.Replace(l, @"Progress:\s*\d+/\d+", sprintf "Progress: %d/%d" x n)

        if replaced <> l then
            lines.[i] <- replaced
            updated <- true

if updated then
    File.WriteAllLines(path, lines)
    printfn "updated %s -> %d/%d" path x n
else
    printfn "no change (%d/%d)" x n
