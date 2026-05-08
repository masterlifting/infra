(*
    Extensions for Async<'T>.
    Ported from: fsharp-infrastructure/src/prelude/Async.fs
    Note: retry logic omitted (requires Infrastructure.Domain types).

    Usage:
      #load "Async.fsx"
      async { return 42 } |> Prelude.Async.map (fun x -> x + 1)
*)

namespace Prelude

module Async =
    let bind next workflow =
        async {
            let! result = workflow
            return! next result
        }

    let map next workflow =
        async {
            let! result = workflow
            return next result
        }
