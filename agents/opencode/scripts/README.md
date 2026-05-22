# F# Scripting Index

```text
kind: helper-index
scope: scripts/*.fsx
namespace: Common
conventions: rules/software/dotnet/fsharp/engineering.md
load_style: relative #load from repo root
preferred_call_style: Shell.run
update_policy: keep file/module/export metadata synchronized with scripts/*.fsx
```

## Change Checklist

```text
1) If a helper file is added, removed, or renamed, update one index row per file.
2) If module names change, update the `modules` column and the `## Usage` snippet if needed.
3) If exported helpers change, update `key_exports` and adjust `purpose` wording.
```

## Included Helpers

```text
format: file | modules | key_exports | purpose
scripts/ActivePatterns.fsx | AP | IsString, IsInt, IsFloat, IsGuid, IsTimeSpan, IsDateOnly, IsTimeOnly, IsDateTime, IsEmail, IsLettersOrNumbers | active patterns for parsing and validation
scripts/Async.fsx | Async | bind, map | async combinators
scripts/Cli.fsx | Args, Shell | Args.ofFsi, Args.get, Args.getOrDefault, Args.getList, Args.has; Shell.run, Shell.runInDir | CLI argument parsing and OS-aware shell execution
scripts/ComputationExpressions.fsx | CE | result, asyncResult | result and async-result computation expression builders
scripts/Exception.fsx | Exception | toMessage | exception-to-message normalization
scripts/Map.fsx | Map | combine, removeKeys, reverse | map helpers
scripts/Option.fsx | Option | toResult, min, max | option helpers
scripts/Result.fsx | Result, ResultAsync | Result.choose, Result.unzip; ResultAsync.wrap, bind, bindAsync, map, mapAsync, mapError, mapErrorAsync, defaultWith, apply, applyAsync | result and async-result helpers
scripts/Seq.fsx | Seq | unzip | sequence helpers
scripts/String.fsx | String | fromTimeSpan, fromDateTime, addLines, toDefault, has, hasSeq | string/date-time helpers
scripts/Threading.fsx | Threading | canceled, notCanceled | cancellation token helpers
scripts/TimeSpan.fsx | TimeSpan | print | compact duration formatting
```

## Usage

```fsharp
#load "scripts/ComputationExpressions.fsx"
#load "scripts/Cli.fsx"

open Common
open Common.CE

result {
    let! output = Shell.run "dotnet --info"
    return output
}
```

## Related Command/Skill Scripts

These scripts are intentionally outside the `scripts/*.fsx` helper-index scope, but are useful entry points for OpenCode workflows:

```text
commands/opencode/scripts/AuditModels.fsx | model inventory and routing report for connected OpenCode providers
skills/youtrack/scripts/youtrack.fsx | YouTrack REST helper used by the youtrack skill
```
