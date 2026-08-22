# Task Template

Load this file only when creating a new task. Fill in required fields: Brief Summary, Context, Requirements / Acceptance Criteria, and Subtasks.

Drop rules at generation time:

- Non-code tasks (pure analysis, docs-only): drop subtask 3 (Design gate) and closing step C0.
- `C1` (cleanup) always applies.
- Drop `C2` when the task does not require a commit or review artifact.
- Every verification step must state an observable completion criterion and must not contradict `## Non-Goals`.
- A behavioral specification (`.tasks/{TASK-ID}/SPEC.md`) is optional and never required for ordinary tasks. Create it only when the task materially involves user-facing behavior, business rules, public/API contracts, state transitions, or important edge cases (see `references/behavioral-spec.md`).

```markdown
# TASK-ID - Task Title

**Progress: 0/N subtasks complete** | **Status: In Progress** | **Created: YYYY-MM-DD**

## Brief Summary

(1-3 sentences describing what should work when complete)

## Continuity

<!-- Optional: instructions to run every time this task is loaded. Delete this section if not needed. -->

## Context

- Target repo(s):
    - `./repo-name` (branch: TBD after research) - why this repo is involved
    - `./other-repo` (branch: TBD after research) - why this repo is involved
- Task kind: code
- Implementation plan: TBD
- Problem / opportunity:
- Constraints:
- Related links/issues:

## Key Files

<!-- Map important files, configs, docs, and scripts as they are discovered. -->

| Purpose               | Path                             |
| --------------------- | -------------------------------- |
| (Main implementation) | `service/path/to/File.cs`        |
| (Config)              | `service/path/to/Config.cs`      |
| (Tests)               | `service/tests/path/to/Tests.cs` |

## Requirements / Acceptance Criteria

- (Build/test requirement)
- (Functional behavior requirement)
- (Safety/security/regression requirement)

## Solution Contract

- State: DRAFT
- Requirements: TBD
- Acceptance criteria: TBD
- Accepted assumptions: None recorded.
- Non-goals: TBD
- Chosen solution: TBD
- Important boundaries/contracts: TBD
- Implementation constraints: TBD
- Review profile: TBD
- Rejected alternatives: None recorded.

## Non-Goals

- (Explicitly out of scope)
- (Explicitly out of scope)

## References

<!-- Keep links to external docs, analysis notes, tickets, and generated artifacts here. -->

- Ticket/spec: (link, if applicable)
- Notion/Drive/GitHub: (link, if applicable)
- Analysis docs: `.tasks/{TASK-ID}/docs/...`
- Behavioral specification: (optional) `.tasks/{TASK-ID}/SPEC.md`

## Subtasks

### 1. Research and define approach

Steps:

- [ ] Investigate relevant code paths and document findings
  - Summary:
- [ ] Define expected behavior and constraints
  - Summary:
- [ ] Draft task-specific delivery and validation work; for code tasks, classify implementation as complex or non-complex
  - Summary:

### 2. Clarify gaps before implementation

Classify gaps under `references/clarification.md`: ask only `BLOCKING`
questions, record meaningful assumptions, resolve `NON-BLOCKING` gaps without
interrupting, and block only unresolved `BLOCKING` gaps.

Steps:

- [ ] Classify research gaps as BLOCKING, ASSUMPTION, or NON-BLOCKING
  - Summary:
- [ ] Ask only BLOCKING questions and record answers or meaningful assumptions in `## Decisions` or `## Open Questions`
  - Summary:
- [ ] Resolve NON-BLOCKING gaps in the task plan without interruption
  - Summary:
- [ ] Mark only unresolved BLOCKING gaps with `[blocked]` notation and set `Status: Blocked` while waiting
  - Summary:

### 3. Design gate

Blocks implementation subtasks until the gate is clean or explicitly waived
(see `references/agent-gates.md`). Architect routing follows that gate:
complex or architecture-sensitive tasks use two isolated read-only architect
proposals; non-complex tasks default to coordinator design, with at most one
appropriate architect for concrete unresolved uncertainty.

Steps:

- [ ] Architecture routed per `references/agent-gates.md`; independent proposals used only when that gate requires them, then coordinator solution frozen
  - Summary:
- [ ] Conditional specialists run per `references/agent-gates.md` or explicitly N/A
  - Summary:
- [ ] Set `Implementation plan` to `non-complex` or `complex`; approve the final task-specific structure with no generic planning placeholders
  - Summary:

### 4. Branch setup across touched repos

- Branch format: `{TASK-ID}-description` when the repo does not define another format.
- Create or switch branches only after research identifies the repos that will actually be touched.
  Steps:
- [ ] Create or switch to a working branch in each touched repo
  - Summary:
- [ ] Update `## Context` target repo branch entries with the selected branch names
  - Summary:

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

<!-- Add task-specific implementation and validation steps here for a non-complex task. -->

- [ ] Engineer-owned implementation completed
  - Summary:
- [ ] Engineer-owned build verdict recorded, or build explicitly not applicable
  - Summary:
- [ ] Tester inspected existing coverage, designed and implemented required tests, and recorded the test verdict; if no tester exists, implementation-agent test ownership recorded
  - Summary:

## Review

- State: NEW
- Implementation baseline: TBD
- Remediation pass: 0
- Build evidence: Not run.
- Test evidence: Not run.

After work, use `Passed: <command/result>`, `Not applicable: <reason>`, or `Waived: <Decision reference>`.

### Accepted findings

| ID | Contract | Status |
| -- | -------- | ------ |

### Verification receipts

| Finding ID | Result | Evidence |
| ---------- | ------ | -------- |

## Closing Steps

<!-- Re-check from C0 onward whenever new work lands after a C-step was checked
      — treat the earlier check as stale. -->

### C0. Pre-commit review board

- Run the one Discovery selected by `references/agent-gates.md` independently and in parallel on the full diff only after build and test evidence is recorded. An explicit not-applicable rationale or recorded waiver may replace either evidence item. A reviewer missing required evidence returns `BLOCKED`; do not transition from `NEW` or proceed with review.
  Steps:
- [ ] Discovery reviewers' verdicts recorded after the evidence precondition
  - Summary:
- [ ] Critical/Error findings fixed or waived in `## Decisions`
  - Summary:
- [ ] Targeted Verification receipts recorded for the frozen accepted finding set
  - Summary:

### C1. Clean up temporary artifacts

Before committing, remove only task-created scratchpad or temp files. See `references/closing-steps.md` for the keep/remove list. Every removal requires explicit confirmation.

Steps:

- [ ] Scratchpad / temp working files created for this task removed
  - Summary:

### C2. Commit and publish

<!-- Include only when this task requires a commit or review artifact. -->

- Commit implementation changes after user confirmation.
- Push and open the repo's normal review artifact after user confirmation.
  Steps:
- [ ] Changes committed in target repo(s)
  - Summary:
- [ ] Review artifact created and linked
  - Summary:

## Decisions

| Date | Decision | Rationale |
| ---- | -------- | --------- |
|      |          |           |

<!-- Before setting Status: Complete, record a dated "complete status confirmed"
     decision. If incomplete lifecycle items are intentionally waived, record
     "complete status waiver" and its rationale in the same or a separate row. -->

## Open Questions

- (Any uncertainties or decisions that still need resolution)
- (Mark resolved questions using `- ~~[x] Question text~~`)

## Notes

- State:
- Evidence:
- Next:
```
