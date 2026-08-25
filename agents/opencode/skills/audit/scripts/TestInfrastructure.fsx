// Canonical deterministic test entry point for the OpenCode infrastructure.
// Runs the frozen suite in fixed order: infrastructure validation (self-test,
// then live), F# task tests, Node safety/progress tests and plugin syntax
// checks, Cargo Firefox tests, and uv/pytest Telegram tests.
// Fails fast: the first failing child stops the suite and its exit code is
// propagated as the script exit code. Diagnostics are bounded and never
// include environment or secret values.
//
// Each step Command is a triple-quoted literal by contract:
// ValidateInfrastructure.fsx extracts these literals to verify the fixed
// targets, so the list below is not an arbitrary command runner surface.

#load "C:/Users/andre/.config/opencode/scripts/Cli.fsx"

open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open Common

let scriptArgs = Args.ofFsi fsi.CommandLineArgs

match scriptArgs with
| []
| [ "--root"; _ ] -> ()
| _ ->
    eprintfn "usage: TestInfrastructure.fsx [--root <dir>]"
    exit 2

let defaultRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../../.."))

let root =
    Args.getOrDefault "--root" defaultRoot scriptArgs
    |> Path.GetFullPath

type Step =
    { Label: string
      Command: string
      TimeoutMs: int }

let defaultTimeoutMs = 600000

// Task workflow integration tests spawn dozens of fsi children and dominate
// the suite runtime, so they get an explicit budget.
let taskWorkflowTimeoutMs = 1200000

let steps: Step list =
    [ { Label = "infrastructure validation self-test"
        Command = """dotnet fsi skills/audit/scripts/ValidateInfrastructure.fsx --self-test"""
        TimeoutMs = defaultTimeoutMs }
      { Label = "infrastructure validation live"
        Command = """dotnet fsi skills/audit/scripts/ValidateInfrastructure.fsx"""
        TimeoutMs = defaultTimeoutMs }
      { Label = "task markdown lifecycle tests"
        Command = """dotnet fsi skills/task/scripts/TaskMdTests.fsx"""
        TimeoutMs = defaultTimeoutMs }
      { Label = "task workflow integration tests"
        Command = """dotnet fsi skills/task/scripts/TaskWorkflowTests.fsx"""
        TimeoutMs = taskWorkflowTimeoutMs }
      { Label = "task progress core tests"
        Command = """node lib/task-progress-core.test.mjs"""
        TimeoutMs = defaultTimeoutMs }
      { Label = "destructive pattern safety tests"
        Command = """node lib/destructive-patterns.test.mjs"""
        TimeoutMs = defaultTimeoutMs }
      { Label = "plugin syntax: block-destructive"
        Command = """node --check plugins/block-destructive.js"""
        TimeoutMs = defaultTimeoutMs }
      { Label = "plugin syntax: compaction-context"
        Command = """node --check plugins/compaction-context.js"""
        TimeoutMs = defaultTimeoutMs }
      { Label = "plugin syntax: task-progress"
        Command = """node --check plugins/task-progress.js"""
        TimeoutMs = defaultTimeoutMs }
      { Label = "firefox MCP cargo tests"
        Command = """cargo test -q --manifest-path mcp/firefox/Cargo.toml"""
        TimeoutMs = defaultTimeoutMs }
      { Label = "telegram MCP pytest tests"
        Command = """uv --directory mcp/telegram run python -m pytest -q"""
        TimeoutMs = defaultTimeoutMs } ]

// Splits a fixed command string into exact arguments without a shell, so
// quoting, exit codes, and failure semantics stay deterministic.
let commandArguments (command: string) =
    Regex.Matches(command, "\"(?<quoted>[^\"]+)\"|(?<plain>[^\\s]+)")
    |> Seq.cast<Match>
    |> Seq.map (fun matched ->
        if matched.Groups.["quoted"].Success then matched.Groups.["quoted"].Value
        else matched.Groups.["plain"].Value)
    |> Seq.toList

type RunResult =
    { ExitCode: int
      Stdout: string
      Stderr: string }

let runStep (step: Step) =
    match commandArguments step.Command with
    | [] -> { ExitCode = 1; Stdout = ""; Stderr = "empty command" }
    | executable :: arguments ->
        let start = ProcessStartInfo(executable, WorkingDirectory = root)
        arguments |> List.iter start.ArgumentList.Add
        start.RedirectStandardOutput <- true
        start.RedirectStandardError <- true
        start.UseShellExecute <- false
        start.CreateNoWindow <- true

        try
            use child = Process.Start start
            let stdout = child.StandardOutput.ReadToEndAsync()
            let stderr = child.StandardError.ReadToEndAsync()

            if not (child.WaitForExit step.TimeoutMs) then
                try
                    child.Kill(entireProcessTree = true)
                with _ ->
                    ()

                { ExitCode = 124
                  Stdout = ""
                  Stderr = sprintf "timed out after %dms" step.TimeoutMs }
            else
                { ExitCode = child.ExitCode
                  Stdout = stdout.GetAwaiter().GetResult()
                  Stderr = stderr.GetAwaiter().GetResult() }
        with ex ->
            { ExitCode = 127
              Stdout = ""
              Stderr = ex.Message }

let boundedTail (maxLength: int) (text: string) =
    if text.Length <= maxLength then
        text
    else
        sprintf "...[truncated %d chars]%s" (text.Length - maxLength) (text.Substring(text.Length - maxLength))

let execute () =
    let total = steps.Length

    steps
    |> List.iteri (fun index step ->
        printfn "[test %d/%d] %s" (index + 1) total step.Label
        let result = runStep step

        if result.ExitCode = 0 then
            printfn "  ok"
        else
            eprintfn "FAILED %s (exit %d)" step.Label result.ExitCode
            let diagnostic =
                if String.IsNullOrWhiteSpace result.Stderr then result.Stdout
                else result.Stderr

            if not (String.IsNullOrWhiteSpace diagnostic) then
                eprintfn "%s" (boundedTail 4000 diagnostic)

            exit result.ExitCode)

execute ()

printfn "OK infrastructure test suite (%d steps)" (List.length steps)
exit 0
