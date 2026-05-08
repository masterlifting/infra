(*
    CLI argument helpers for F# scripts.
    Provides --key value parsing and fsi.CommandLineArgs utilities.

    Usage:
      #load "Args.fsx"

      let args = Prelude.Args.ofFsi fsi.CommandLineArgs
      let severity = args |> Prelude.Args.getOrDefault "--severity" "error"
      let terms = args |> Prelude.Args.getList "--search"
      let verbose = args |> Prelude.Args.has "--verbose"
*)

namespace Prelude

module Args =
    let ofFsi (argv: string[]) =
        argv |> Array.skip 1 |> Array.toList

    let get (key: string) (args: string list) =
        args
        |> List.tryFindIndex (fun a -> a = key)
        |> Option.bind (fun i ->
            if i + 1 < args.Length then Some args.[i + 1]
            else None)

    let getOrDefault (key: string) (defaultValue: string) (args: string list) =
        get key args |> Option.defaultValue defaultValue

    let getList (key: string) (args: string list) =
        get key args
        |> Option.map (fun s -> s.Split ',' |> Array.map (fun t -> t.Trim()) |> Array.toList)
        |> Option.defaultValue []

    let has (key: string) (args: string list) =
        args |> List.exists (fun a -> a = key)
