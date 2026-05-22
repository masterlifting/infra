namespace Common

module String =
    open System

    let fromTimeSpan (value: TimeSpan) =
        let format = "dd\\.hh\\:mm\\:ss"
        value.ToString format

    let fromDateTime (value: DateTime) =
        let format = "yyyy-MM-dd HH:mm:ss"
        value.ToString format

    let addLines count =
        Seq.init count (fun _ -> Environment.NewLine) |> String.concat ""

    let toDefault (value: string | null) =
        match value with
        | null -> String.Empty
        | v -> v

    let has (pattern: string) (value: string) =
        value.Contains(pattern, StringComparison.OrdinalIgnoreCase)

    let hasSeq (patterns: string seq) (value: string) =
        patterns |> Seq.exists (fun pattern -> has pattern value)
