(*
    Extensions for Result<'T, 'E>.
    Ported from: fsharp-infrastructure/src/prelude/Result.fs

    Usage:
      #load "Result.fsx"
      Prelude.Result.choose [ Ok 1; Ok 2; Error "x" ]
*)

namespace Prelude

module Result =
    let choose data =
        let map state itemRes =
            state
            |> Result.bind (fun items -> itemRes |> Result.map (fun item -> item :: items))

        Seq.fold map (Ok []) data |> Result.map List.rev

    let unzip data =
        data
        |> Seq.fold
            (fun (oks, errs) item ->
                match item with
                | Ok v -> v :: oks, errs
                | Error e -> oks, e :: errs)
            ([], [])
        |> fun (oks, errs) -> List.rev oks, List.rev errs
