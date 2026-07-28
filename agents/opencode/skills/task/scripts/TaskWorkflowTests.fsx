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

let synchronizeProgress (text: string) =
    let completed, total = text.Split([| "\r\n"; "\n" |], StringSplitOptions.None) |> ResizeArray |> computeProgress
    Regex.Replace(text, @"\*\*Progress:\s*\d+/\d+", $"**Progress: {completed}/{total}")

let canonicalizeDesignGate (text: string) =
    text.Replace(
        "Set `Implementation plan` to `non-complex` or `complex`; approve the final task-specific structure with no generic planning placeholders",
        requiredDesignGateLabels.[2])

let completeDesignGate (text: string) =
    requiredDesignGateLabels
    |> List.fold (fun updated label ->
        Regex.Replace(
            updated,
            $"(?m)^- \[ \] {Regex.Escape label}\r?\n  - Summary:$",
            $"- [x] {label}{Environment.NewLine}  - Summary: Design evidence recorded")) (canonicalizeDesignGate text)
    |> synchronizeProgress

let replaceGenericImplementationSubtask replacement (text: string) =
    Regex.Replace(
        text,
        @"^### 5\. Implement and validate\s*\r?\n.*?(?=^## Closing Steps)",
        replacement + Environment.NewLine + Environment.NewLine,
        RegexOptions.Multiline ||| RegexOptions.Singleline)
    |> synchronizeProgress

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
    assertTrue "non-code task retained implementation-plan marker" (not (nonCodeText.Contains implementationPlanPrefix))
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
    assertTrue "code task implementation plan marker is not exactly TBD" (Regex.Matches(codeText, @"(?m)^- Implementation plan: TBD\r?$").Count = 1)
    assertTrue "code task contains more than one implementation plan marker" (Regex.Matches(codeText, "(?m)^- Implementation plan: ").Count = 1)
    assertTrue "code task dropped C0" (codeText.Contains "### C0.")
    assertTrue "code task dropped C2" (codeText.Contains "### C2.")

    let codeValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue ("generated code task is invalid: " + codeValidation.Output) (codeValidation.ExitCode = 0)

    let architectGate = requiredDesignGateLabels.Head
    let completedGateWithoutArchitect =
        completeDesignGate codeText
        |> fun text ->
            Regex.Replace(
                text,
                $"(?m)^- \[x\] {Regex.Escape architectGate}\r?\n  - Summary: Design evidence recorded\r?\n",
                "")
        |> synchronizeProgress
    File.WriteAllText(codePath, completedGateWithoutArchitect)
    let missingArchitectValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "design gate without architect checkbox was accepted" (missingArchitectValidation.ExitCode <> 0)
    assertContains "missing architect gate diagnostic" $"design gate is missing required checkbox: {architectGate}" missingArchitectValidation.Output

    let renamedArchitectGate =
        completeDesignGate (codeText.Replace(architectGate, "Design gate: alternate architect review recorded"))
    File.WriteAllText(codePath, renamedArchitectGate)
    let renamedArchitectValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "design gate with renamed architect checkbox was accepted" (renamedArchitectValidation.ExitCode <> 0)
    assertContains "renamed architect gate diagnostic" $"design gate is missing required checkbox: {architectGate}" renamedArchitectValidation.Output

    let suffixedArchitectGate =
        completeDesignGate codeText
        |> fun text ->
            text.Replace(
                "- [x] " + architectGate,
                "- [x] " + architectGate + " but not approved",
                StringComparison.Ordinal)
        |> fun text ->
            text.Replace(
                "- [ ] Engineer-owned implementation completed\n  - Summary:",
                "- [x] Engineer-owned implementation completed\n  - Summary: Work evidence recorded",
                StringComparison.Ordinal)
        |> synchronizeProgress
    File.WriteAllText(codePath, suffixedArchitectGate)
    let suffixedArchitectValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "design gate with suffixed architect checkbox was accepted" (suffixedArchitectValidation.ExitCode <> 0)
    assertContains "suffixed architect gate diagnostic" $"design gate is missing required checkbox: {architectGate}" suffixedArchitectValidation.Output
    assertContains "suffixed architect gate lock diagnostic" "implementation and validation work rooted at subtask 5 or later cannot be checked before the design gate completes" suffixedArchitectValidation.Output
    assertTrue "suffixed architect gate fixture produced a summary violation" (not (suffixedArchitectValidation.Output.Contains("checked item requires", StringComparison.OrdinalIgnoreCase)))

    let arbitraryCompletedDesignGate =
        Regex.Replace(
            codeText,
            @"^### 3\. Design gate\s*\r?\n.*?(?=^### 4\.)",
            """### 3. Design gate

- [x] Arbitrary approval recorded
  - Summary: Approval evidence recorded

""",
            RegexOptions.Multiline ||| RegexOptions.Singleline)
            .Replace(
                "- [ ] Engineer-owned implementation completed\n  - Summary:",
                "- [x] Engineer-owned implementation completed\n  - Summary: Work evidence recorded",
                StringComparison.Ordinal)
        |> synchronizeProgress
    File.WriteAllText(codePath, arbitraryCompletedDesignGate)
    let arbitraryGateValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "arbitrary design gate checkbox unlocked implementation" (arbitraryGateValidation.ExitCode <> 0)
    assertContains "arbitrary design gate canonical diagnostic" $"design gate is missing required checkbox: {architectGate}" arbitraryGateValidation.Output
    assertContains "arbitrary design gate implementation lock diagnostic" "implementation and validation work rooted at subtask 5 or later cannot be checked before the design gate completes" arbitraryGateValidation.Output
    assertTrue "arbitrary design gate fixture produced a summary violation" (not (arbitraryGateValidation.Output.Contains("checked item requires", StringComparison.OrdinalIgnoreCase)))

    let codeWithoutDesignGate =
        Regex.Replace(
            codeText,
            @"^### 3\. Design gate\s*\r?\n.*?(?=^### 4\.)",
            "",
            RegexOptions.Multiline ||| RegexOptions.Singleline)
        |> synchronizeProgress
    File.WriteAllText(codePath, codeWithoutDesignGate)
    let missingDesignGateValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "code task without the design gate was accepted" (missingDesignGateValidation.ExitCode <> 0)
    assertContains "missing design gate diagnostic" "code task is missing required subtask '### 3. Design gate'" missingDesignGateValidation.Output

    let checkedIntegerImplementationBeforeDesign =
        codeText.Replace(
            "- [ ] Engineer-owned implementation completed\n  - Summary:",
            "- [x] Engineer-owned implementation completed\n  - Summary: Work evidence recorded",
            StringComparison.Ordinal)
        |> synchronizeProgress
    File.WriteAllText(codePath, checkedIntegerImplementationBeforeDesign)
    let checkedIntegerImplementationValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "checked integer implementation work was accepted before the design gate" (checkedIntegerImplementationValidation.ExitCode <> 0)
    assertContains "checked integer implementation diagnostic" "implementation and validation work rooted at subtask 5 or later cannot be checked before the design gate completes" checkedIntegerImplementationValidation.Output

    let checkedDecimalImplementationBeforeDesign =
        codeText.Replace(
            "## Closing Steps",
            """### 5.1. Follow-up implementation work

- [x] Follow-up work completed
  - Summary: Work evidence recorded

## Closing Steps""",
            StringComparison.Ordinal)
        |> synchronizeProgress
    File.WriteAllText(codePath, checkedDecimalImplementationBeforeDesign)
    let checkedDecimalImplementationValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "checked decimal implementation work was accepted before the design gate" (checkedDecimalImplementationValidation.ExitCode <> 0)
    assertContains "checked decimal implementation diagnostic" "implementation and validation work rooted at subtask 5 or later cannot be checked before the design gate completes" checkedDecimalImplementationValidation.Output

    let designGateTbd = completeDesignGate codeText
    File.WriteAllText(codePath, designGateTbd)
    let designGateTbdValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "completed design gate accepted TBD implementation plan" (designGateTbdValidation.ExitCode <> 0)
    assertContains "completed design gate TBD diagnostic" "completed design gate requires an implementation plan" designGateTbdValidation.Output

    let nonComplexWithPlaceholder =
        designGateTbd.Replace("- Implementation plan: TBD", "- Implementation plan: non-complex")
        |> synchronizeProgress
    File.WriteAllText(codePath, nonComplexWithPlaceholder)
    let nonComplexPlaceholderValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "finalized non-complex plan accepted the generic placeholder" (nonComplexPlaceholderValidation.ExitCode <> 0)
    assertContains "non-complex placeholder diagnostic" "non-complex implementation plan must remove the generic implementation placeholder" nonComplexPlaceholderValidation.Output

    let nonComplexPlan = nonComplexWithPlaceholder.Replace(genericImplementationPlaceholder, "") |> synchronizeProgress
    File.WriteAllText(codePath, nonComplexPlan)
    let nonComplexValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue ("finalized non-complex plan is invalid: " + nonComplexValidation.Output) (nonComplexValidation.ExitCode = 0)

    let complexImplementation =
        """### 5. Implement: adaptive task workflow

- [ ] Engineer-owned implementation completed
  - Summary:
- [ ] Engineer-owned build verdict recorded, or build explicitly not applicable
  - Summary:
- [ ] Substantive reviewer verdict recorded, or change documented as non-substantive
  - Summary:

### 6. Validate: adaptive task workflow

- [ ] Tester inspected existing coverage, designed and implemented required tests, and recorded the test verdict; if no tester exists, implementation-agent test ownership recorded
  - Summary:"""

    let complexPlan =
        designGateTbd.Replace("- Implementation plan: TBD", "- Implementation plan: complex")
        |> replaceGenericImplementationSubtask complexImplementation
    File.WriteAllText(codePath, complexPlan)
    let complexValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue ("finalized complex plan is invalid: " + complexValidation.Output) (complexValidation.ExitCode = 0)
    assertTrue "complex plan retained generic implementation heading" (not (complexPlan.Contains genericImplementationHeading))
    assertTrue "complex plan retained generic implementation placeholder" (not (complexPlan.Contains genericImplementationPlaceholder))

    let complexPlanWithDecimalAdaptiveHeadings =
        complexPlan.Replace("### 5. Implement: adaptive task workflow", "### 5.1. Implement: adaptive task workflow")
            .Replace("### 6. Validate: adaptive task workflow", "### 6.1. Validate: adaptive task workflow")
        |> synchronizeProgress
    File.WriteAllText(codePath, complexPlanWithDecimalAdaptiveHeadings)
    let decimalAdaptiveHeadingsValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "completed complex plan accepted decimal adaptive headings" (decimalAdaptiveHeadingsValidation.ExitCode <> 0)
    assertTrue
        "both decimal adaptive headings were not rejected"
        (Regex.Matches(decimalAdaptiveHeadingsValidation.Output, "adaptive Implement/Validate subtasks must use top-level integer IDs").Count = 2)

    let complexWithIncompleteSecondImplementation =
        complexPlan.Replace(
            "### 6. Validate: adaptive task workflow",
            """### 6. Implement: follow-up slice

- [ ] Engineer-owned implementation completed
  - Summary:
- [ ] Substantive reviewer verdict recorded, or change documented as non-substantive
  - Summary:

### 7. Validate: adaptive task workflow""")
        |> synchronizeProgress
    File.WriteAllText(codePath, complexWithIncompleteSecondImplementation)
    let incompleteSecondImplementationValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "complex plan accepted an implementation subtask without a build gate" (incompleteSecondImplementationValidation.ExitCode <> 0)
    assertContains "scoped second implementation gate diagnostic" "complex Implement subtask is missing required gate checkbox: Engineer-owned build verdict recorded" incompleteSecondImplementationValidation.Output

    let complexWithMissingValidationTester =
        complexPlan.Replace(
            "- [ ] Substantive reviewer verdict recorded, or change documented as non-substantive\n  - Summary:",
            """- [ ] Substantive reviewer verdict recorded, or change documented as non-substantive
  - Summary:
- [ ] Tester inspected existing coverage, designed and implemented required tests, and recorded the test verdict; if no tester exists, implementation-agent test ownership recorded
  - Summary:""",
            StringComparison.Ordinal)
        |> fun text ->
            Regex.Replace(
                text,
                @"(?m)(^### 6\. Validate: adaptive task workflow\r?\n\r?\n)- \[ \] Tester inspected existing coverage, designed and implemented required tests, and recorded the test verdict; if no tester exists, implementation-agent test ownership recorded\r?\n  - Summary:",
                "$1- [ ] Validation evidence recorded" + Environment.NewLine + "  - Summary:")
        |> synchronizeProgress
    File.WriteAllText(codePath, complexWithMissingValidationTester)
    let missingValidationTesterValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "complex plan accepted a Validate subtask without a tester gate" (missingValidationTesterValidation.ExitCode <> 0)
    assertContains "scoped Validate gate diagnostic" "complex Validate subtask is missing required gate checkbox: Tester inspected existing coverage" missingValidationTesterValidation.Output

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

    let oversizedHeading =
        codeText.Replace(
            "## Closing Steps",
            "### 999999999999999999999. Oversized heading" + Environment.NewLine + Environment.NewLine + "## Closing Steps",
            StringComparison.Ordinal)
        |> synchronizeProgress
    File.WriteAllText(codePath, oversizedHeading)
    let oversizedHeadingValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "oversized numeric heading crashed or was accepted" (oversizedHeadingValidation.ExitCode <> 0)
    assertContains "oversized numeric heading diagnostic" "exceeds the supported numeric range" oversizedHeadingValidation.Output

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
