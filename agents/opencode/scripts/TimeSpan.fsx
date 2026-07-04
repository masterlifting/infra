namespace Common

module TimeSpan =
    open System

    let print (value: TimeSpan) =
        [ value.Days, "d"; value.Hours, "h"; value.Minutes, "m"; value.Seconds, "s" ]
        |> List.choose (fun (count, unit) ->
            if count > 0 then Some $"%d{count}%s{unit}" else None)
        |> function
            | [] -> "0s"
            | parts -> String.concat " " parts
