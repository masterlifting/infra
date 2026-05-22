namespace Common

module Async =
    let bind next workflow =
        async {
            let! result = workflow
            return! next result
        }

    let map next workflow =
        async {
            let! result = workflow
            return next result
        }
