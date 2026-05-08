(*
    Active patterns for common type parsing.
    Ported from: fsharp-infrastructure/src/prelude/ActivePattern.fs
    Note: IsUUID16/32 and Leaf/Node omitted (require Infrastructure.Domain types).

    Usage:
      #load "ActivePattern.fsx"
      open Prelude.ActivePattern

      match "42" with
      | IsInt n -> printfn $"integer: {n}"
      | _ -> printfn "not an integer"
*)

namespace Prelude

module ActivePattern =
    open System

    let (|IsString|_|) (input: string | null) =
        match input with
        | null -> None
        | value ->
            match String.IsNullOrWhiteSpace value with
            | false -> Some value
            | _ -> None

    let (|IsInt|_|) (input: string) =
        match Int32.TryParse input with
        | true, value -> Some value
        | _ -> None

    let (|IsFloat|_|) (input: string) =
        match Double.TryParse input with
        | true, value -> Some value
        | _ -> None

    let (|IsGuid|_|) (input: string) =
        match Guid.TryParse input with
        | true, value -> Some value
        | _ -> None

    let (|IsTimeSpan|_|) (input: string) =
        match TimeSpan.TryParse input with
        | true, value -> Some value
        | _ -> None

    let (|IsDateOnly|_|) (input: string) =
        match DateOnly.TryParse input with
        | true, value -> Some value
        | _ -> None

    let (|IsTimeOnly|_|) (input: string) =
        match TimeOnly.TryParse input with
        | true, value -> Some value
        | _ -> None

    let (|IsDateTime|_|) (input: string) =
        match DateTime.TryParse input with
        | true, value -> Some value
        | _ -> None

    let (|IsEmail|_|) (input: string) =
        match Text.RegularExpressions.Regex.IsMatch(input, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") with
        | true -> Some input
        | _ -> None

    let (|IsLettersOrNumbers|_|) (input: string) =
        match Text.RegularExpressions.Regex.IsMatch(input, "^[a-zA-Z0-9]+$") with
        | true -> Some input
        | _ -> None
