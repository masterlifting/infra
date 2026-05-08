(*
    Extensions for exn (Exception).
    Ported from: fsharp-infrastructure/src/prelude/Exception.fs

    Usage:
      #load "Exception.fsx"
      try ... with ex -> Prelude.Exception.toMessage ex
*)

namespace Prelude

module Exception =
    let toMessage (ex: exn) =
        ex.InnerException
        |> Option.ofObj
        |> Option.map _.Message
        |> Option.defaultValue ex.Message
