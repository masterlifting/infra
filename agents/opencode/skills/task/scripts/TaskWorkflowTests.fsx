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
        @"^### 5\. Implement and validate\s*\r?\n.*?(?=^## Review)",
        replacement + Environment.NewLine + Environment.NewLine,
        RegexOptions.Multiline ||| RegexOptions.Singleline)
    |> synchronizeProgress

let replaceRequired (oldValue: string) (newValue: string) (text: string) =
    if not (text.Contains(oldValue, StringComparison.Ordinal)) then
        failwithf "fixture marker missing: %s" oldValue
    text.Replace(oldValue, newValue, StringComparison.Ordinal)

let frozenReview (text: string) =
    text
    |> replaceRequired "- State: DRAFT" "- State: FROZEN"
    |> replaceRequired "- Accepted assumptions: None recorded." "- Accepted assumptions: No material assumptions remain."
    |> replaceRequired "- Chosen solution: TBD" "- Chosen solution: Durable task workflow"
    |> replaceRequired "- Important boundaries/contracts: TBD" "- Important boundaries/contracts: TASK.md durable review contract"
    |> replaceRequired "- Implementation constraints: TBD" "- Implementation constraints: Preserve resumability and safe writes"
    |> replaceRequired "- Review profile: TBD" "- Review profile: Standard"
    |> replaceRequired "- State: NEW" "- State: FROZEN"
    |> replaceRequired "- Implementation baseline: TBD" "- Implementation baseline: baseline-1"
    |> replaceRequired "- Build evidence: Not run." "- Build evidence: Not applicable: script-only contract"
    |> replaceRequired "- Test evidence: Not run." "- Test evidence: Passed: npm run test:task"
    |> replaceRequired "| ---------- | ------ | -------- |" "| ---------- | ------ | -------- |\n| None | APPROVE | No accepted findings |"

let addAcceptedFinding findingId contract status (text: string) =
    replaceRequired
        "| -- | -------- | ------ |"
        $"| -- | -------- | ------ |{Environment.NewLine}| {findingId} | {contract} | {status} |"
        text

let addVerificationReceipt findingId result evidence (text: string) =
    replaceRequired
        "| ---------- | ------ | -------- |"
        $"| ---------- | ------ | -------- |{Environment.NewLine}| {findingId} | {result} | {evidence} |"
        text

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
    assertTrue "generated code task retained an obsolete routine reviewer gate" (not (codeText.Contains "Substantive reviewer verdict recorded"))

    for field in
        [ "## Solution Contract"
          "- State: DRAFT"
          "- Accepted assumptions: None recorded."
          "- Chosen solution: TBD"
          "- Important boundaries/contracts: TBD"
          "- Implementation constraints: TBD"
          "- Review profile: TBD"
          "## Non-Goals"
          "## Review"
          "- State: NEW"
          "- Implementation baseline: TBD"
          "- Remediation pass: 0"
          "- Build evidence: Not run."
          "- Test evidence: Not run."
          "### Accepted findings"
          "### Verification receipts" ] do
        assertTrue ($"generated task is missing durable contract field: {field}") (codeText.Contains(field, StringComparison.Ordinal))

    let codeValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue ("canonical initial task is invalid: " + codeValidation.Output) (codeValidation.ExitCode = 0)

    let invalidSolutionState = replaceRequired "- State: DRAFT" "- State: UNKNOWN" codeText
    File.WriteAllText(codePath, invalidSolutionState)
    let invalidSolutionStateValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "invalid solution state was accepted" (invalidSolutionStateValidation.ExitCode <> 0)
    assertContains "invalid solution state diagnostic" "Solution Contract State must be DRAFT or FROZEN" invalidSolutionStateValidation.Output

    let invalidReviewProfile = replaceRequired "- Review profile: TBD" "- Review profile: Unreviewed" codeText
    File.WriteAllText(codePath, invalidReviewProfile)
    let invalidReviewProfileValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "invalid review profile was accepted" (invalidReviewProfileValidation.ExitCode <> 0)
    assertContains "invalid review profile diagnostic" "Solution Contract Review profile must be TBD, Standard, or Full / architecture-sensitive" invalidReviewProfileValidation.Output

    let invalidRemediationPass = replaceRequired "- Remediation pass: 0" "- Remediation pass: 3" codeText
    File.WriteAllText(codePath, invalidRemediationPass)
    let invalidRemediationPassValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "invalid remediation pass was accepted" (invalidRemediationPassValidation.ExitCode <> 0)
    assertContains "invalid remediation pass diagnostic" "Review Remediation pass must be an integer from 0 through 2" invalidRemediationPassValidation.Output

    let invalidReviewState = replaceRequired "- State: NEW" "- State: INVALID" codeText
    File.WriteAllText(codePath, invalidReviewState)
    let invalidReviewStateValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "invalid review state was accepted" (invalidReviewStateValidation.ExitCode <> 0)
    assertContains "invalid review state diagnostic" "Review State must be NEW, DISCOVERY, REMEDIATION, VERIFICATION, or FROZEN" invalidReviewStateValidation.Output

    for state, pass, diagnostic in
        [ "NEW", 1, "Review State NEW requires remediation pass 0"
          "DISCOVERY", 1, "Review State DISCOVERY requires remediation pass 0"
          "REMEDIATION", 0, "Review State REMEDIATION requires remediation pass 1 or 2" ] do
        let invalidStatePass =
            codeText
            |> replaceRequired "- State: NEW" $"- State: {state}"
            |> replaceRequired "- Remediation pass: 0" $"- Remediation pass: {pass}"
        File.WriteAllText(codePath, invalidStatePass)
        let invalidStatePassValidation = runFsi fixtureRoot validateScript [ codePath ]
        assertTrue ($"invalid {state} remediation-pass combination was accepted") (invalidStatePassValidation.ExitCode <> 0)
        assertContains ($"invalid {state} remediation-pass diagnostic") diagnostic invalidStatePassValidation.Output

    let reviewWithoutFrozenPrerequisites = replaceRequired "- State: NEW" "- State: DISCOVERY" codeText
    File.WriteAllText(codePath, reviewWithoutFrozenPrerequisites)
    let frozenPrerequisitesValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "review without frozen prerequisites was accepted" (frozenPrerequisitesValidation.ExitCode <> 0)
    assertContains "missing frozen state diagnostic" "Review beyond NEW requires a FROZEN solution contract" frozenPrerequisitesValidation.Output
    assertContains "missing frozen details diagnostic" "Review beyond NEW requires frozen solution details" frozenPrerequisitesValidation.Output
    assertContains "missing frozen evidence diagnostic" "Review beyond NEW requires exact 'Passed: <command/result>'" frozenPrerequisitesValidation.Output

    let frozenWithoutEvidence = frozenReview codeText |> replaceRequired "- Test evidence: Passed: npm run test:task" "- Test evidence: Not run."
    File.WriteAllText(codePath, frozenWithoutEvidence)
    let missingEvidenceValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "frozen review without required evidence was accepted" (missingEvidenceValidation.ExitCode <> 0)
    assertContains "missing evidence diagnostic" "Review beyond NEW requires exact 'Passed: <command/result>'" missingEvidenceValidation.Output

    let canonicalFrozenReview = frozenReview codeText
    File.WriteAllText(codePath, canonicalFrozenReview)
    let canonicalFrozenValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue ("canonical frozen review is invalid: " + canonicalFrozenValidation.Output) (canonicalFrozenValidation.ExitCode = 0)

    let frozenWithConcreteEvidence =
        frozenReview codeText
        |> replaceRequired "- Build evidence: Not applicable: script-only contract" "- Build evidence: Passed: dotnet fsi TaskWorkflowTests.fsx"
    File.WriteAllText(codePath, frozenWithConcreteEvidence)
    let concreteEvidenceValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue ("concrete build and test evidence is invalid: " + concreteEvidenceValidation.Output) (concreteEvidenceValidation.ExitCode = 0)

    let frozenWithNotApplicableRationale =
        frozenReview codeText
        |> replaceRequired "- Test evidence: Passed: npm run test:task" "- Test evidence: Not applicable: external test environment is unavailable"
    File.WriteAllText(codePath, frozenWithNotApplicableRationale)
    let notApplicableRationaleValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue ("explicit not-applicable rationale is invalid: " + notApplicableRationaleValidation.Output) (notApplicableRationaleValidation.ExitCode = 0)

    let frozenWithPlaceholderEvidence = frozenReview codeText |> replaceRequired "- Build evidence: Not applicable: script-only contract" "- Build evidence: TBD"
    File.WriteAllText(codePath, frozenWithPlaceholderEvidence)
    let placeholderEvidenceValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "placeholder evidence was accepted" (placeholderEvidenceValidation.ExitCode <> 0)
    assertContains "placeholder evidence diagnostic" "Review beyond NEW requires exact 'Passed: <command/result>'" placeholderEvidenceValidation.Output

    let frozenWithMalformedNotApplicableEvidence = frozenReview codeText |> replaceRequired "- Build evidence: Not applicable: script-only contract" "- Build evidence: Not applicable"
    File.WriteAllText(codePath, frozenWithMalformedNotApplicableEvidence)
    let malformedEvidenceValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "malformed not-applicable evidence was accepted" (malformedEvidenceValidation.ExitCode <> 0)
    assertContains "malformed not-applicable evidence diagnostic" "Review beyond NEW requires exact 'Passed: <command/result>'" malformedEvidenceValidation.Output

    let today = DateTime.UtcNow.ToString("yyyy-MM-dd")
    let frozenWithWaivedBuild =
        frozenReview codeText
        |> replaceRequired "- Build evidence: Not applicable: script-only contract" "- Build evidence: Waived: BUILD-001"
        |> replaceRequired "|      |          |           |" $"| {today} | waived BUILD-001 | build environment unavailable |"
    File.WriteAllText(codePath, frozenWithWaivedBuild)
    let waivedBuildValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue ("dated matching waiver is invalid: " + waivedBuildValidation.Output) (waivedBuildValidation.ExitCode = 0)

    let frozenWithUnlinkedWaiver = frozenReview codeText |> replaceRequired "- Build evidence: Not applicable: script-only contract" "- Build evidence: Waived: BUILD-001"
    File.WriteAllText(codePath, frozenWithUnlinkedWaiver)
    let unlinkedWaiverValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "unlinked waiver was accepted" (unlinkedWaiverValidation.ExitCode <> 0)
    assertContains "unlinked waiver diagnostic" "Review beyond NEW requires exact 'Passed: <command/result>'" unlinkedWaiverValidation.Output

    let frozenWithUndatedWaiver =
        frozenReview codeText
        |> replaceRequired "- Build evidence: Not applicable: script-only contract" "- Build evidence: Waived: BUILD-001"
        |> replaceRequired "|      |          |           |" "| undated | waived BUILD-001 | build environment unavailable |"
    File.WriteAllText(codePath, frozenWithUndatedWaiver)
    let undatedWaiverValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "undated waiver was accepted" (undatedWaiverValidation.ExitCode <> 0)
    assertContains "undated waiver evidence diagnostic" "Review beyond NEW requires exact 'Passed: <command/result>'" undatedWaiverValidation.Output

    let frozenWithMalformedWaiver = frozenReview codeText |> replaceRequired "- Build evidence: Not applicable: script-only contract" "- Build evidence: Waived BUILD-001"
    File.WriteAllText(codePath, frozenWithMalformedWaiver)
    let malformedWaiverValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "malformed waiver was accepted" (malformedWaiverValidation.ExitCode <> 0)
    assertContains "malformed waiver diagnostic" "Review beyond NEW requires exact 'Passed: <command/result>'" malformedWaiverValidation.Output

    let frozenWithArbitraryEvidence = frozenReview codeText |> replaceRequired "- Build evidence: Not applicable: script-only contract" "- Build evidence: arbitrary"
    File.WriteAllText(codePath, frozenWithArbitraryEvidence)
    let arbitraryEvidenceValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "arbitrary evidence was accepted" (arbitraryEvidenceValidation.ExitCode <> 0)
    assertContains "arbitrary evidence diagnostic" "Review beyond NEW requires exact 'Passed: <command/result>'" arbitraryEvidenceValidation.Output

    let frozenWithFailedEvidence = frozenReview codeText |> replaceRequired "- Build evidence: Not applicable: script-only contract" "- Build evidence: Failed: dotnet fsi TaskWorkflowTests.fsx"
    File.WriteAllText(codePath, frozenWithFailedEvidence)
    let failedEvidenceValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "failed evidence was accepted" (failedEvidenceValidation.ExitCode <> 0)
    assertContains "failed evidence diagnostic" "Review beyond NEW requires exact 'Passed: <command/result>'" failedEvidenceValidation.Output

    let frozenWithPendingEvidence = frozenReview codeText |> replaceRequired "- Build evidence: Not applicable: script-only contract" "- Build evidence: Pending: dotnet fsi TaskWorkflowTests.fsx"
    File.WriteAllText(codePath, frozenWithPendingEvidence)
    let pendingEvidenceValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "pending evidence was accepted" (pendingEvidenceValidation.ExitCode <> 0)
    assertContains "pending evidence diagnostic" "Review beyond NEW requires exact 'Passed: <command/result>'" pendingEvidenceValidation.Output

    let frozenWithDuplicateFindingId =
        frozenReview codeText
        |> addAcceptedFinding "B2-F1" "Durable contract" "FIXED"
        |> addAcceptedFinding "B2-F1" "Duplicate durable contract" "FIXED"
    File.WriteAllText(codePath, frozenWithDuplicateFindingId)
    let duplicateFindingIdValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "duplicate accepted finding ID was accepted" (duplicateFindingIdValidation.ExitCode <> 0)
    assertContains "duplicate accepted finding ID diagnostic" "accepted finding ID 'B2-F1' is duplicated" duplicateFindingIdValidation.Output

    let frozenWithInvalidFindingStatus =
        frozenReview codeText
        |> addAcceptedFinding "B2-F2" "Durable contract" "APPROVED"
    File.WriteAllText(codePath, frozenWithInvalidFindingStatus)
    let invalidFindingStatusValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "invalid accepted finding status was accepted" (invalidFindingStatusValidation.ExitCode <> 0)
    assertContains "invalid accepted finding status diagnostic" "accepted finding status must be PENDING, FIXED, NOT FIXED, or REGRESSION INTRODUCED" invalidFindingStatusValidation.Output

    let frozenWithDuplicateNoneReceipt =
        frozenReview codeText
        |> addVerificationReceipt "None" "APPROVE" "Duplicate approval"
    File.WriteAllText(codePath, frozenWithDuplicateNoneReceipt)
    let duplicateNoneReceiptValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "duplicate None receipt was accepted" (duplicateNoneReceiptValidation.ExitCode <> 0)
    assertContains "duplicate None receipt diagnostic" "verification receipt ID 'None' is duplicated" duplicateNoneReceiptValidation.Output

    let frozenWithDuplicateFindingReceipt =
        frozenReview codeText
        |> addAcceptedFinding "B2-F3" "Durable contract" "FIXED"
        |> replaceRequired "| None | APPROVE | No accepted findings |" "| B2-F3 | FIXED | First receipt |"
        |> addVerificationReceipt "B2-F3" "FIXED" "Duplicate receipt"
    File.WriteAllText(codePath, frozenWithDuplicateFindingReceipt)
    let duplicateFindingReceiptValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "duplicate real-finding receipt was accepted" (duplicateFindingReceiptValidation.ExitCode <> 0)
    assertContains "duplicate real-finding receipt diagnostic" "verification receipt ID 'B2-F3' is duplicated" duplicateFindingReceiptValidation.Output

    let frozenWithApproveForFinding =
        frozenReview codeText
        |> addAcceptedFinding "B2-F4" "Durable contract" "FIXED"
        |> replaceRequired "| None | APPROVE | No accepted findings |" "| B2-F4 | APPROVE | Invalid real-finding approval |"
    File.WriteAllText(codePath, frozenWithApproveForFinding)
    let approveForFindingValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "APPROVE result for a real finding was accepted" (approveForFindingValidation.ExitCode <> 0)
    assertContains "real-finding APPROVE diagnostic" "APPROVE is valid only for Finding ID None" approveForFindingValidation.Output

    let frozenWithInvalidNoneResult = frozenReview codeText |> replaceRequired "| None | APPROVE | No accepted findings |" "| None | FIXED | Invalid None result |"
    File.WriteAllText(codePath, frozenWithInvalidNoneResult)
    let invalidNoneResultValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "invalid None receipt result was accepted" (invalidNoneResultValidation.ExitCode <> 0)
    assertContains "invalid None receipt result diagnostic" "a None verification receipt must have result APPROVE" invalidNoneResultValidation.Output

    let frozenWithOutOfDomainVerificationResult = frozenReview codeText |> replaceRequired "| None | APPROVE | No accepted findings |" "| None | BOGUS | Invalid result |"
    File.WriteAllText(codePath, frozenWithOutOfDomainVerificationResult)
    let outOfDomainVerificationResultValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "out-of-domain verification result was accepted" (outOfDomainVerificationResultValidation.ExitCode <> 0)
    assertContains "out-of-domain verification result diagnostic" "verification result must be APPROVE, FIXED, NOT FIXED, or REGRESSION INTRODUCED" outOfDomainVerificationResultValidation.Output

    let frozenWithUnlinkedReceipt = frozenReview codeText |> replaceRequired "| None | APPROVE | No accepted findings |" "| FIND-404 | FIXED | Evidence |"
    File.WriteAllText(codePath, frozenWithUnlinkedReceipt)
    let unlinkedReceiptValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "unlinked verification receipt was accepted" (unlinkedReceiptValidation.ExitCode <> 0)
    assertContains "unlinked receipt diagnostic" "verification receipt references unknown accepted finding 'FIND-404'" unlinkedReceiptValidation.Output

    let frozenWithUnlinkedFinding =
        frozenReview codeText
        |> addAcceptedFinding "FIND-1" "Durable contract" "FIXED"
        |> fun text -> text.Replace("| None | APPROVE | No accepted findings |", "", StringComparison.Ordinal)
    File.WriteAllText(codePath, frozenWithUnlinkedFinding)
    let unlinkedFindingValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "accepted finding without a receipt was accepted" (unlinkedFindingValidation.ExitCode <> 0)
    assertContains "unlinked finding diagnostic" "Review State FROZEN requires a verification receipt for accepted finding 'FIND-1'" unlinkedFindingValidation.Output

    let incompleteFrozenReview =
        frozenReview codeText
        |> addAcceptedFinding "FIND-1" "Durable contract" "NOT FIXED"
        |> replaceRequired "| None | APPROVE | No accepted findings |" "| FIND-1 | NOT FIXED | Evidence |"
    File.WriteAllText(codePath, incompleteFrozenReview)
    let incompleteFrozenValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "incomplete frozen review was accepted" (incompleteFrozenValidation.ExitCode <> 0)
    assertContains "incomplete frozen review diagnostic" "Review State FROZEN requires accepted finding 'FIND-1' to verify as FIXED, not NOT FIXED" incompleteFrozenValidation.Output

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
    assertTrue "complex Implement subtask retained an obsolete routine reviewer gate" (not (complexPlan.Contains "Substantive reviewer verdict recorded"))

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

### 7. Validate: adaptive task workflow""")
        |> synchronizeProgress
    File.WriteAllText(codePath, complexWithIncompleteSecondImplementation)
    let incompleteSecondImplementationValidation = runFsi fixtureRoot validateScript [ codePath ]
    assertTrue "complex plan accepted an implementation subtask without a build gate" (incompleteSecondImplementationValidation.ExitCode <> 0)
    assertContains "scoped second implementation gate diagnostic" "complex Implement subtask is missing required gate checkbox: Engineer-owned build verdict recorded" incompleteSecondImplementationValidation.Output

    let complexWithMissingValidationTester =
        complexPlan
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
