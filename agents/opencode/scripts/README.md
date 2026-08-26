# F# Scripting

Name every F# script using `PascalCase.fsx`, including test scripts (for example, `TaskWorkflowTests.fsx`). Keep shared `#load` modules here and workflow-specific entry scripts under `{skills,commands}/{name}/scripts/`.

```text
kind: helper-index
scope: scripts/*.fsx
namespace: Common
conventions: rules/software/dotnet/fsharp/engineering.md
load_style: absolute #load from C:/Users/andre/.config/opencode/scripts/
preferred_call_style: Shell.run (after open Common)
update_policy: keep file/module/export metadata synchronized with scripts/*.fsx
```

## Change Checklist

```text
1) If a helper file is added, removed, or renamed, update one index row per file.
2) If module names change, update the `modules` column and the Usage snippet.
3) If exported helpers change, update `key_exports` and adjust `purpose`.
```

## Included Helpers

```text
format: file | modules | key_exports | purpose
scripts/ActivePatterns.fsx     | AP          | IsString, IsInt, IsFloat, IsGuid, IsTimeSpan, IsDateOnly, IsTimeOnly, IsDateTime, IsEmail, IsLettersOrNumbers | active patterns for parsing and validation
scripts/Async.fsx              | Async       | bind, map | async combinators
scripts/Cli.fsx                | Args, Shell | Args.ofFsi, Args.get, Args.getOrDefault, Args.getList, Args.has; Shell.run, Shell.runWithTimeout, Shell.runInDir, Shell.runInDirWithTimeout | CLI argument parsing and OS-aware shell execution
scripts/ComputationExpressions.fsx | CE      | result, asyncResult | result and async-result computation expression builders
scripts/Exception.fsx          | Exception   | toMessage | exception-to-message normalization
scripts/Map.fsx                | Map         | combine, removeKeys, reverse | map helpers
scripts/Option.fsx             | Option      | toResult, min, max | option helpers
scripts/Result.fsx             | Result, ResultAsync | Result.choose, Result.unzip; ResultAsync.wrap, bind, bindAsync, map, mapAsync, mapError, mapErrorAsync, defaultWith, validate, validateAsync | result and async-result helpers
scripts/Seq.fsx                | Seq         | unzip | sequence helpers
scripts/String.fsx             | String      | fromTimeSpan, fromDateTime, addLines, toDefault, has, hasSeq | string/date-time helpers
scripts/Threading.fsx          | Threading   | canceled, notCanceled | cancellation token helpers
scripts/TimeSpan.fsx           | TimeSpan    | print | compact duration formatting ("2h 30m")
```

## Usage

```fsharp
#load "../../../scripts/ComputationExpressions.fsx"
#load "../../../scripts/Cli.fsx"

open Common
open Common.CE

result {
    let! output = Shell.run "dotnet --info"
    return output
}
```

`#load` path from skill / command entry scripts: `#load "C:/Users/andre/.config/opencode/scripts/X.fsx"`.

## Related Skill Scripts

These scripts are intentionally outside the `scripts/*.fsx` helper-index scope, but are useful entry points for OpenCode workflows:

```text
skills/task/scripts/RecomputeProgress.fsx | recompute the TASK.md progress counter from checkbox state
skills/task/scripts/ValidateTask.fsx      | validate TASK.md invariants (--fix to auto-repair; --sync to recompute then validate in one pass)
skills/task/scripts/CreateTask.fsx        | create a canonical task safely without overwriting an existing TASK.md
skills/task/scripts/TaskMd.fsx             | shared TASK.md parsing and progress helpers
skills/task/scripts/TaskMdTests.fsx        | test TASK.md parsing and progress helpers
skills/task/scripts/TaskWorkflowTests.fsx  | integration-test task creation, validation, and stale-write safety
skills/youtrack/scripts/YouTrackRest.fsx   | YouTrack REST helper used by the youtrack skill
skills/audit/scripts/ValidateInfrastructure.fsx | validate global OpenCode infrastructure structure and DRY invariants
skills/documents/scripts/OfficeDocuments.fsx | convert simple Markdown/DOCX and CSV/JSON/XLSX files for ONLYOFFICE
```
