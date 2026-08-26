// Recompute Progress: X/N from checkbox state. Idempotent; safe to run on every edit.
// Usage:
//   dotnet fsi "C:/Users/andre/.config/opencode/skills/task/scripts/RecomputeProgress.fsx" <path-to-TASK.md>

open System
open System.IO

#load "TaskMd.fsx"
open TaskMd // shared heading detection + progress counting (single source of truth)

let args = fsi.CommandLineArgs |> Array.skip 1
if args.Length < 1 then
    eprintfn "usage: RecomputeProgress.fsx <TASK.md>"
    exit 2

let path =
    match tryResolveTaskPath (Directory.GetCurrentDirectory()) args.[0] with
    | Ok resolved -> resolved
    | Error message ->
        eprintfn "%s" message
        exit 2

let original = File.ReadAllLines path
let lines = original |> ResizeArray

let x, n, updated = syncProgressCounters lines

if updated then
    match tryWriteAllLinesIfUnchanged path original lines with
    | Ok () -> printfn "updated %s -> %d/%d" path x n
    | Error message ->
        eprintfn "%s" message
        exit 1
else
    printfn "no change (%d/%d)" x n
