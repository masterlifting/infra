(*
    Threading helpers.
    Ported from: fsharp-infrastructure/src/prelude/Threading.fs

    Usage:
      #load "Threading.fsx"
      if Prelude.Threading.notCanceled token then ...
*)

namespace Prelude

module Threading =
    open System.Threading

    let canceled (cToken: CancellationToken) = cToken.IsCancellationRequested
    let notCanceled (cToken: CancellationToken) = not <| cToken.IsCancellationRequested
