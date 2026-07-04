namespace Common

module String =
    open System

    let fromTimeSpan (value: TimeSpan) =
        value.ToString("dd\\.hh\\:mm\\:ss")

    let fromDateTime (value: DateTime) =
        value.ToString("yyyy-MM-dd HH:mm:ss")

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
