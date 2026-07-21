#load "TaskMd.fsx"

open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open TaskMd

type RunResult = { ExitCode: int; Output: string }

let runFsi workingDirectory script arguments =
    let start = ProcessStartInfo("dotnet", WorkingDirectory = workingDirectory)
    start.ArgumentList.Add "fsi"
    start.ArgumentList.Add "--nologo"
    start.ArgumentList.Add script
    arguments |> List.iter start.ArgumentList.Add
    start.RedirectStandardOutput <- true
    start.RedirectStandardError <- true
    start.UseShellExecute <- false
    start.CreateNoWindow <- true

    use childProcess = Process.Start start
    let stdout = childProcess.StandardOutput.ReadToEndAsync()
    let stderr = childProcess.StandardError.ReadToEndAsync()
    childProcess.WaitForExit()
    { ExitCode = childProcess.ExitCode
      Output = stdout.Result + Environment.NewLine + stderr.Result }

let assertTrue name condition =
    if not condition then failwith name

let assertContains name (expected: string) (actual: string) =
    if not (actual.Contains(expected, StringComparison.OrdinalIgnoreCase)) then
        failwithf "%s: expected output containing %A, got %A" name expected actual

let tempParent = Path.Combine(Path.GetTempPath(), "opencode")
let fixtureRoot = Path.Combine(tempParent, $"task-workflow-tests-{Guid.NewGuid():N}")
let createScript = Path.Combine(__SOURCE_DIRECTORY__, "CreateTask.fsx")
let validateScript = Path.Combine(__SOURCE_DIRECTORY__, "ValidateTask.fsx")

Directory.CreateDirectory fixtureRoot |> ignore

try
    let nonCode = runFsi fixtureRoot createScript [ "TASK-101"; "Non-code task"; "--non-code"; "--no-commit" ]
    assertTrue "non-code task creation failed" (nonCode.ExitCode = 0)

    let nonCodePath = Path.Combine(fixtureRoot, ".tasks", "TASK-101", "TASK.md")
    let nonCodeText = File.ReadAllText nonCodePath
    assertTrue "non-code task kind marker missing" (nonCodeText.Contains "- Task kind: non-code")
    assertTrue "non-code task retained design gate" (not (nonCodeText.Contains "### 3. Design gate"))
    assertTrue "non-code task retained C0" (not (nonCodeText.Contains "### C0."))
    assertTrue "no-commit task retained C2" (not (nonCodeText.Contains "### C2."))
    assertTrue "non-code task retained engineer gate" (not (nonCodeText.Contains "Engineer-owned implementation"))
    assertTrue "non-code task retained tester gate" (not (nonCodeText.Contains "Tester inspected existing coverage"))
    assertTrue "non-code task retained reviewer gate" (not (nonCodeText.Contains "Substantive reviewer verdict"))
    assertTrue "generated progress denominator is not numeric" (Regex.IsMatch(nonCodeText, @"Progress: 0/\d+"))

    let nonCodeValidation = runFsi fixtureRoot validateScript [ nonCodePath ]
    assertTrue ("generated non-code task is invalid: " + nonCodeValidation.Output) (nonCodeValidation.ExitCode = 0)

    let nonCodeWithGateProse =
        nonCodeText.Replace(
            "## Notes",
            "## Notes" + Environment.NewLine + Environment.NewLine + "Example text: Engineer-owned implementation completed")
    File.WriteAllText(nonCodePath, nonCodeWithGateProse)
    let nonCodeProseValidation = runFsi fixtureRoot validateScript [ nonCodePath ]
    assertTrue ("non-code prose caused a false gate violation: " + nonCodeProseValidation.Output) (nonCodeProseValidation.ExitCode = 0)

    let overwrite = runFsi fixtureRoot createScript [ "TASK-101"; "Replacement title"; "--non-code" ]
    assertTrue "existing task was overwritten" (overwrite.ExitCode <> 0)
    assertContains "no-overwrite diagnostic" "task already exists" overwrite.Output

    let codeTask = runFsi fixtureRoot createScript [ "TASK-102"; "Code task" ]
    assertTrue "code task creation failed" (codeTask.ExitCode = 0)

    let codePath = Path.Combine(fixtureRoot, ".tasks", "TASK-102", "TASK.md")
    let codeText = File.ReadAllText codePath
    assertTrue "code task kind marker missing" (codeText.Contains "- Task kind: code")
    assertTrue "code task dropped C0" (codeText.Contains "### C0.")
    assertTrue "code task dropped C2" (codeText.Contains "### C2.")

    let codeValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue ("generated code task is invalid: " + codeValidation.Output) (codeValidation.ExitCode = 0)

    let misplacedTaskKind =
        codeText.Replace("- Task kind: code", "- Task kind: missing")
            .Replace("## Notes", "## Notes" + Environment.NewLine + Environment.NewLine + "- Task kind: code")
    File.WriteAllText(codePath, misplacedTaskKind)
    let misplacedKindValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "task-kind marker outside Context was accepted" (misplacedKindValidation.ExitCode <> 0)
    assertContains "task-kind Context diagnostic" "## Context must contain exactly one" misplacedKindValidation.Output

    let codeWithoutC0 =
        Regex.Replace(
            codeText,
            @"^### C0\. Pre-commit review board\s*\r?\n.*?(?=^### C1\.)",
            "",
            RegexOptions.Multiline ||| RegexOptions.Singleline)
    File.WriteAllText(codePath, codeWithoutC0)
    let missingC0Validation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "code task without C0 was accepted" (missingC0Validation.ExitCode <> 0)
    assertContains "missing C0 diagnostic" "missing required closing step C0" missingC0Validation.Output

    let fencedCheckedExample =
        Regex.Replace(
            codeText,
            @"(?m)^## Decisions$",
            "```markdown" + Environment.NewLine + "- [x] Example checkbox without task evidence" + Environment.NewLine + "```" + Environment.NewLine + Environment.NewLine + "## Decisions")
    File.WriteAllText(codePath, fencedCheckedExample)
    let fencedValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue ("fenced checkbox caused a false violation: " + fencedValidation.Output) (fencedValidation.ExitCode = 0)

    let proseOnlyGate =
        codeText.Replace(
            "- [ ] Engineer-owned build verdict recorded, or build explicitly not applicable",
            "Engineer-owned build verdict recorded, or build explicitly not applicable")
    File.WriteAllText(codePath, proseOnlyGate)
    let gateValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "gate prose unexpectedly satisfied checkbox validation" (gateValidation.ExitCode <> 0)
    assertContains "canonical gate diagnostic" "Engineer-owned build verdict recorded" gateValidation.Output

    let checkedWithoutEvidence =
        codeText.Replace("- [ ] Investigate relevant code paths", "- [x] Investigate relevant code paths")
    File.WriteAllText(codePath, checkedWithoutEvidence)
    let summaryValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "empty checked-item summary was accepted" (summaryValidation.ExitCode <> 0)
    assertContains "summary diagnostic" "checked item requires" summaryValidation.Output

    let today = DateTime.UtcNow.ToString("yyyy-MM-dd")
    let completeWithoutDecision =
        checkedWithoutEvidence.Replace(
            "**Status: In Progress**",
            "**Status: Complete**")
            .Replace(
                $"**Created: {today}**",
                $"**Created: {today}** | **Completed: {today}**")
    File.WriteAllText(codePath, completeWithoutDecision)
    let completionValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertContains "completion confirmation diagnostic" "complete status confirmed" completionValidation.Output
    assertContains "completion waiver diagnostic" "completion waiver" completionValidation.Output

    let completionHeader (text: string) =
        text.Replace("**Status: In Progress**", "**Status: Complete**")
            .Replace($"**Created: {today}**", $"**Created: {today}** | **Completed: {today}**")

    let completedNonCodeText = completionHeader nonCodeText

    let separateDecisionRows =
        completedNonCodeText
            .Replace(
                "|      |          |           |",
                $"| {today} | complete status confirmed | user confirmed closure |{Environment.NewLine}| {today} | complete status waiver | intentionally incomplete |")
    File.WriteAllText(nonCodePath, separateDecisionRows)
    let separateDecisionValidation = runFsi fixtureRoot validateScript [ nonCodePath ]
    assertTrue ("separate completion rows were rejected: " + separateDecisionValidation.Output) (separateDecisionValidation.ExitCode = 0)

    let combinedDecisionRow =
        completedNonCodeText
            .Replace(
                "|      |          |           |",
                $"| {today} | complete status confirmed; complete status waiver | intentionally incomplete |")
    File.WriteAllText(nonCodePath, combinedDecisionRow)
    let combinedDecisionValidation = runFsi fixtureRoot validateScript [ nonCodePath ]
    assertTrue ("combined completion row was rejected: " + combinedDecisionValidation.Output) (combinedDecisionValidation.ExitCode = 0)

    let stalePath = Path.Combine(fixtureRoot, ".tasks", "TASK-102", "stale-write.txt")
    File.WriteAllLines(stalePath, [| "original" |])
    let original = File.ReadAllLines stalePath
    File.WriteAllLines(stalePath, [| "manual edit" |])
    match tryWriteAllLinesIfUnchanged stalePath original (ResizeArray [ "automated edit" ]) with
    | Ok () -> failwith "stale write unexpectedly replaced a newer edit"
    | Error _ -> assertTrue "newer edit was not preserved" (File.ReadAllText(stalePath).Contains "manual edit")

    printfn "OK task creation, validation, and stale-write safety"
finally
    if Directory.Exists fixtureRoot then Directory.Delete(fixtureRoot, true)
