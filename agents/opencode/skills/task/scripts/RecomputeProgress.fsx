// Recompute Progress: X/N from checkbox state. Idempotent; safe to run after checkbox edits.
// Usage:
//   dotnet fsi C:/Users/andre/.config/opencode/skills/task/scripts/RecomputeProgress.fsx .tasks/<TASK-ID>/TASK.md

#load "TaskProgress.fsx"

open System.IO
open System.Text.RegularExpressions

let args = fsi.CommandLineArgs |> Array.skip 1
if args.Length < 1 then
    eprintfn "usage: RecomputeProgress.fsx <TASK.md>"
    exit 2

let path = args.[0]
if not (File.Exists path) then
    eprintfn "file not found: %s" path
    exit 2

let lines = File.ReadAllLines path |> ResizeArray

let progress = TaskProgress.count lines

let mutable updated = false
for i in 0 .. lines.Count - 1 do
    let l = lines.[i]
    if l.StartsWith "**Progress:" then
        let replaced = Regex.Replace(l, @"Progress:\s*\d+/\d+", sprintf "Progress: %d/%d" progress.Completed progress.Total)
        if replaced <> l then
            lines.[i] <- replaced
            updated <- true

if updated then
    File.WriteAllLines(path, lines)
    printfn "updated %s -> %d/%d" path progress.Completed progress.Total
else
    printfn "no change (%d/%d)" progress.Completed progress.Total
