# F# Scripting

Local entrypoint for repo-style `.fsx` automation and shared prelude helpers.

- Load only the helpers a script needs.
- Helpers expose `Prelude.X` modules to avoid shadowing FSharp.Core modules.
- Use `open Prelude.CE` and `open Prelude.ActivePattern`; prefer qualified access for the rest, for example `Prelude.Shell.run`.
- Keep repo-specific workflow logic in the calling script instead of shared helpers.

## Included Helpers

- `ActivePattern.fsx` for common parsing patterns
- `Args.fsx` for CLI argument parsing
- `Async.fsx` for async combinators
- `CE.fsx` for `result` helpers
- `Exception.fsx` for exception message extraction
- `Map.fsx` for map helpers
- `Option.fsx` for option helpers
- `ResultAsync.fsx` for async-result combinators
- `Result.fsx` for result helpers
- `Seq.fsx` for sequence helpers
- `Shell.fsx` for OS-aware command execution
- `String.fsx` for string helpers
- `Threading.fsx` for cancellation helpers
- `TimeSpan.fsx` for duration formatting

## Usage

```fsharp
#load "CE.fsx"
#load "Shell.fsx"

open Prelude.CE

result {
    let! output = Prelude.Shell.run "dotnet --info"
    return output
}
```

## Conventions

- Prefer F# scripts (`.fsx`) for reusable local automation.
- Use `Result` and `Option` for expected failures and missing values.
- Avoid `.Result` and `.Wait()` on async/Task values; use `async {}` with `Async.AwaitTask` or `asyncResult {}`.
- Use `(* *)` file headers instead of XML doc comments in scripts.
- Add dry-run or report-only modes for scripts that make structural or destructive changes.
- Use shared helpers when script logic grows beyond a few inline commands.
