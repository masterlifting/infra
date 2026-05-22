namespace Common

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

module ResultAsync =
    let wrap f =
        function
        | Ok x -> f x
        | Error e -> async { return Error e }

    let bind f workflow =
        async {
            let! result = workflow
            return Result.bind f result
        }

    let bindAsync f workflow =
        async {
            match! workflow with
            | Ok result -> return! f result
            | Error err -> return Error err
        }

    let map f workflow =
        async {
            let! result = workflow
            return Result.map f result
        }

    let mapAsync f workflow =
        async {
            match! workflow with
            | Ok result -> return Ok <| f result
            | Error err -> return Error err
        }

    let mapError f workflow =
        async {
            let! result = workflow
            return Result.mapError f result
        }

    let mapErrorAsync f workflow =
        async {
            match! workflow with
            | Ok result -> return Ok result
            | Error err ->
                let! err = f err
                return Error err
        }

    let defaultWith f workflow =
        async {
            let! result = workflow
            return Result.defaultWith f result
        }

    let apply f workflow =
        async {
            match! workflow with
            | Ok w ->
                match f (Some w) with
                | Ok() -> return Ok w
                | Error errF -> return Error errF
            | Error error ->
                match f None with
                | Ok() -> return Error error
                | Error errF -> return Error errF
        }

    let applyAsync f workflow =
        async {
            match! workflow with
            | Ok w ->
                match! f (Some w) with
                | Ok() -> return Ok w
                | Error errF -> return Error errF
            | Error error ->
                match! f None with
                | Ok() -> return Error error
                | Error errF -> return Error errF
        }
