# INFRA-002 - Strengthen task continuity semantics

**Progress: 8/8 subtasks complete** | **Status: Complete** | **Created: 2026-08-24** | **Completed: 2026-08-24**

## Brief Summary

Make the standalone global task skill safely resumable around volatile project state, avoid duplicate remote effects after ambiguous results, and preserve concise portable continuity through completion.

## Continuity

Before material work after a resume, revalidate the volatile project facts relevant to that work; retain only concise durable decisions and evidence in this record.

## Context

- Target repo(s):
    - `./opencode` (branch: TBD; user selected existing master branch) - global OpenCode infrastructure source mirror
- Task kind: code
- Implementation plan: non-complex
- Problem / opportunity: Existing task records are portable but do not distinguish durable context from volatile facts or define safe handling for ambiguous remote effects and terminal Notes.
- Constraints: Preserve the lifecycle and confirmation gates; remain standalone; do not reference or depend on happy-life/life-ops, ActionGuard, connector machinery, or a generic workflow abstraction.
- Related links/issues: User objective, 2026-08-24.

## Key Files

<!-- Map important files, configs, docs, and scripts as they are discovered. -->

| Purpose               | Path                             |
| --------------------- | -------------------------------- |
| Task workflow | `skills/task/SKILL.md` |
| Task references | `skills/task/references/{template,closing-steps,confirmation-policy,validation}.md` |
| Deterministic task tests | `skills/task/scripts/TaskWorkflowTests.fsx` |
| Existing completed record | `.tasks/INFRA-001/TASK.md` |

## Requirements / Acceptance Criteria

- On resume, durable task context may be reused but materially volatile facts are revalidated before they drive material work.
- Ambiguous external/project effects are observed at their target before retrying, without introducing a durable action journal.
- TASK.md remains concise portable durable continuity; completion keeps State/Evidence/Next semantically current; optional origin/unblock handoffs are preserved without mutating dependents.
- Existing lifecycle, gates, and valid records remain compatible; focused regression/self-tests pass.

## Solution Contract

- State: FROZEN
- Requirements: Add the five requested standalone continuity semantics while preserving the existing task lifecycle and gates.
- Acceptance criteria: Each requested invariant is concise, discoverable in its natural task-skill owner, represented in the template only where useful, and covered by focused workflow self-tests where behavior is mechanically inspectable.
- Accepted assumptions: `C:\Users\andre\.config\opencode` is the source mirror; master is the approved existing branch; no external ticket or dependent record needs mutation.
- Non-goals: No life-ops coupling, generic workflow abstraction, ActionGuard journal, connector machinery, automatic dependent-record updates, or broad lifecycle redesign.
- Chosen solution: Add procedural semantics to SKILL.md and focused references/template; add string-level workflow regressions for required guidance, but do not parse free-form Notes semantics in ValidateTask.fsx.
- Important boundaries/contracts: The task lifecycle, agent ownership, confirmation policy, and existing valid TASK.md format remain authoritative; ambiguous remote results require observation before retry, not a persisted effect journal.
- Implementation constraints: Keep prose repository-agnostic and compact; change only the named task-skill artifacts and focused tests; no commit, push, external write, or destructive action.
- Review profile: Standard
- Rejected alternatives: None recorded.

## Non-Goals

- Integrating or naming happy-life/life-ops or any life-ops-specific model.
- Validator heuristics for natural-language freshness or a durable dependency graph.

## References

<!-- Keep links to external docs, analysis notes, tickets, and generated artifacts here. -->

- Ticket/spec: User objective, 2026-08-24.
- Analysis docs: Read-only audit result from `audit-infra` session `ses_fcd116688ffetT1SaHZ5lxOLBq`.
- Behavioral specification: (optional) `.tasks/INFRA-002/SPEC.md`

## Subtasks

### 1. Research and define approach

Steps:

- [x] Investigate relevant code paths and document findings
  - Summary: Inspected SKILL.md, all relevant references, validator, workflow tests, task-progress plugin, INFRA-001 completion record, and the new resumable record; audit confirms the requested semantics are absent.
- [x] Define expected behavior and constraints
  - Summary: Preserved the existing lifecycle and confirmation gates; excluded all life-ops, ActionGuard, generic workflow, and connector coupling.
- [x] Draft task-specific delivery and validation work; for code tasks, classify implementation as complex or non-complex
  - Summary: Classified non-complex because concise policy/reference/template changes and focused self-tests form one bounded implementation-and-validation cycle.

### 2. Clarify gaps before implementation

Classify gaps under `references/clarification.md`: ask only `BLOCKING`
questions, record meaningful assumptions, resolve `NON-BLOCKING` gaps without
interrupting, and block only unresolved `BLOCKING` gaps.

Steps:

- [x] Classify research gaps as BLOCKING, ASSUMPTION, or NON-BLOCKING
  - Summary: No blocking gap; use of master and task ID INFRA-002 were explicitly resolved by the user.
- [x] Ask only BLOCKING questions and record answers or meaningful assumptions in `## Decisions` or `## Open Questions`
  - Summary: No further blocking question remains; source-mirror and branch assumptions are recorded in the Solution Contract.
- [x] Resolve NON-BLOCKING gaps in the task plan without interruption
  - Summary: Chose procedural enforcement for semantic Notes freshness and minimal string-level self-tests rather than brittle natural-language validation.
- [x] Mark only unresolved BLOCKING gaps with `[blocked]` notation and set `Status: Blocked` while waiting
  - Summary: No unresolved blocking gaps.

### 3. Design gate

Blocks implementation subtasks until the gate is clean or explicitly waived
(see `references/agent-gates.md`). Architect routing follows that gate:
complex or architecture-sensitive tasks use two isolated read-only architect
proposals; non-complex tasks default to coordinator design, with at most one
appropriate architect for concrete unresolved uncertainty.

Steps:

- [x] Architecture routed per `references/agent-gates.md`; independent proposals used only when that gate requires them, then coordinator solution frozen
  - Summary: Coordinator froze the non-complex design after a read-only audit; no material architecture uncertainty warrants an architect proposal.
- [x] Conditional specialists run per `references/agent-gates.md` or explicitly N/A
  - Summary: N/A; the bounded infrastructure prose/test change does not materially touch database, DevOps, security, or performance-owned surfaces.
- [x] Set `Implementation plan` to `non-complex` or `complex`; approve the final task-specific structure with no generic planning placeholders
  - Summary: Non-complex implementation is limited to the named task-skill references/template and focused workflow self-tests.

### 4. Branch setup across touched repos

- Branch format: `INFRA-002-description` when the repo does not define another format.
- Create or switch branches only after research identifies the repos that will actually be touched.
  Steps:
- [x] Create or switch to a working branch in each touched repo
  - Summary: User selected the existing master branch; no branch creation or switching was performed.
- [x] Update `## Context` target repo branch entries with the selected branch names
  - Summary: Recorded the sole source mirror and its approved master branch.

### 5. Implement and validate

During subtasks 1-3, refine this implementation area before the design gate completes:

- Complex task: set `Implementation plan: complex` and replace this generic
  subtask with numbered `### 5. Implement: <slice>` and
  `### <n>. Validate: <scope>` subtasks. Add further numbered task-specific
  subtasks where needed. Use this form when one combined cycle would hide
  independent slices, ordering, ownership, or materially different components
  or repositories.
- Non-complex task: retain subtask 5 and add concrete task-specific
  implementation and validation steps with observable completion criteria;
  set `Implementation plan: non-complex` and remove the placeholder comment.
- Preserve the applicable implementation, build, and test gate checkboxes
  below in the resulting structure. For a complex task, put the
  implementation/build gates in each applicable implementation subtask and
  the tester gate in the applicable validation subtask or subtasks. Keep
  Discovery and Verification in C0 after build and test evidence.
- If this structure changes after the design gate was approved, reopen subtask
  3 and re-run that gate. On resume, preserve an already-started subtask 5 and
  its evidence; rename/refine it and append new IDs instead of deleting history.

Steps:

- [x] Add bounded resume, remote-effect, durable-state, terminal-notes, and dependency-handoff semantics to their natural policy owners
  - Summary: Added standalone, material-relevance resume checks; observe-before-retry; portable durable continuity; terminal-current Notes; and optional Origin/Unblocks handoff guidance without changing the lifecycle or adding a journal/framework.
- [x] Add focused workflow self-tests for the new required guidance and preserve validator compatibility
  - Summary: Added TaskWorkflowTests marker and generated-template coverage; ValidateTask.fsx remains unchanged because free-form Notes semantics are intentionally procedural.

- [x] Engineer-owned implementation completed
  - Summary: Executor updated only SKILL.md, the focused task references/template, and TaskWorkflowTests.fsx; no lifecycle, validator, connector, or external-effect journal redesign was introduced.
- [x] Engineer-owned build verdict recorded, or build explicitly not applicable
  - Summary: Not applicable: configuration-only task-skill policy and F# self-test update have no separate build artifact; executor's focused workflow self-test passed.
- [x] Tester inspected existing coverage, designed and implemented required tests, and recorded the test verdict; if no tester exists, implementation-agent test ownership recorded
  - Summary: F# tester independently ran TaskMdTests, TaskWorkflowTests, and ValidateTask on INFRA-002; all passed and confirmed no natural-language parsing was added to ValidateTask.fsx.

## Review

- State: FROZEN
- Implementation baseline: INFRA-002 scoped working-tree diff after executor implementation
- Remediation pass: 0
- Build evidence: Not applicable: configuration-only task-skill policy and F# self-test update have no separate build artifact.
- Test evidence: Passed: TaskMdTests, TaskWorkflowTests, and ValidateTask on INFRA-002 exited 0 under independent F# tester verification.

After work, use `Passed: <command/result>`, `Not applicable: <reason>`, or `Waived: <Decision reference>`.

### Accepted findings

| ID | Contract | Status |
| -- | -------- | ------ |

### Verification receipts

| Finding ID | Result | Evidence |
| ---------- | ------ | -------- |
| None | APPROVE | Reviewer-1 targeted Verification confirmed the empty accepted finding set and the frozen contract. |

## Closing Steps

<!-- Re-check from C0 onward whenever new work lands after a C-step was checked
      — treat the earlier check as stale. -->

### C0. Pre-commit review board

- Run the one Discovery selected by `references/agent-gates.md` independently and in parallel on the full diff only after build and test evidence is recorded. An explicit not-applicable rationale or recorded waiver may replace either evidence item. A reviewer missing required evidence returns `BLOCKED`; do not transition from `NEW` or proceed with review.
  Steps:
- [x] Discovery reviewers' verdicts recorded after the evidence precondition
  - Summary: Independent reviewer-1 and reviewer-3 approved the scoped diff after recorded build/test evidence; no Critical, Error, or Warning finding was raised.
- [x] Critical/Error findings fixed or waived in `## Decisions`
  - Summary: No Critical/Error findings exist. Reviewer-3's two Info test-strength observations were not accepted into the remediation set because the unique new markers still pin the required guidance.
- [x] Targeted Verification receipts recorded for the frozen accepted finding set
  - Summary: Reviewer-1 targeted Verification approved the empty accepted finding set and justified the None/APPROVE receipt.

### C1. Clean up temporary artifacts

Before committing, remove only task-created scratchpad or temp files. See `references/closing-steps.md` for the keep/remove list. Every removal requires explicit confirmation.

Steps:

- [x] Scratchpad / temp working files created for this task removed
  - Summary: No retained task-created scratch or temporary files require removal; tester cleaned only its own temporary probe artifacts and left pre-existing artifacts untouched.

### C2. Commit and publish

- Commit implementation changes after user confirmation.
- Push and open the repo's normal review artifact after user confirmation.
  Steps:
- [x] Changes committed in target repo(s)
  - Summary: Committed the five scoped task-skill artifacts locally on master as `20775cb` (`INFRA-002:`); no push was performed.
- [x] Review artifact created and linked
  - Summary: Not applicable: the user requested a local commit and private filesystem backup, not a remote review artifact.

## Decisions

| Date | Decision | Rationale |
| ---- | -------- | --------- |
| 2026-08-24 | Non-complex standalone continuity design frozen | The requested semantics fit existing policy owners and focused test coverage; free-form completion meaning is procedural, not safely validator-parsable. |
| 2026-08-24 | Discovery Info observations not accepted for remediation | Unique new regression markers retain sufficient focused coverage; no Critical, Error, or Warning finding requires a remediation pass. |
| 2026-08-24 | complete status confirmed | User explicitly confirmed completion after scoped implementation, independent testing, Discovery, targeted Verification, task/infrastructure validation, and diff checks passed. |
| 2026-08-24 | Closing reopened for local commit | The user later explicitly requested a local commit and backup, so the terminal Notes and closing state were refreshed before that work. |
| 2026-08-24 | Local commit and private backup completed | Commit `20775cb` was created and its five scoped files were copied to the explicitly confirmed private filesystem destination. |
| 2026-08-24 | complete status confirmed | User explicitly confirmed completion after the reopened local commit and backup closing work finished. |

<!-- Before setting Status: Complete, record a dated "complete status confirmed"
     decision. If incomplete lifecycle items are intentionally waived, record
     "complete status waiver" and its rationale in the same or a separate row. -->

## Open Questions

- None.

## Notes

- State: Complete; all scoped lifecycle work, local commit, and private filesystem backup are complete.
- Evidence: Commit `20775cb` contains the five scoped artifacts; the same files were copied to `D:\private\infra\agents\opencode`; TaskMdTests, TaskWorkflowTests, ValidateTask, ValidateInfrastructure, and git diff --check passed; both Discovery reviewers approved; targeted Verification recorded None/APPROVE.
- Next: No task work remains. No push was requested or performed.
