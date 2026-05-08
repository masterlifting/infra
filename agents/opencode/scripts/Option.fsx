(*
    Extensions for Option<'T>.
    Ported from: fsharp-infrastructure/src/prelude/Option.fs

    Usage:
      #load "Option.fsx"
      Prelude.Option.min (Some 3) (Some 5)   // -> Some 3
*)

namespace Prelude

module Option =
    let toResult error =
        function
        | Some value -> Ok value
        | None -> Error error

    let min<'a when 'a: comparison> (x: 'a option) (y: 'a option) =
        match x, y with
        | Some x, Some y -> Some(min x y)
        | Some x, None -> Some x
        | None, Some y -> Some y
        | None, None -> None

    let max<'a when 'a: comparison> (x: 'a option) (y: 'a option) =
        match x, y with
        | Some x, Some y -> Some(max x y)
        | Some x, None -> Some x
        | None, Some y -> Some y
        | None, None -> None
