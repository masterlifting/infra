(*
    Extensions for Seq<'T>.
    Ported from: fsharp-infrastructure/src/prelude/Seq.fs

    Usage:
      #load "Seq.fsx"
      [(1,"a"); (2,"b")] |> Prelude.Seq.unzip   // -> [1;2], ["a";"b"]
*)

namespace Prelude

module Seq =
    let unzip tuples =
        let map (acc1, acc2) (item1, item2) = (item1 :: acc1, item2 :: acc2)

        tuples
        |> Seq.fold map ([], [])
        |> fun (acc1, acc2) -> List.rev acc1, List.rev acc2
