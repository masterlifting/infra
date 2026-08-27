# INFRA-001 - Align task routing policies

**Progress: 6/7 subtasks complete** | **Status: Complete** | **Created: 2026-08-22** | **Completed: 2026-08-22**

## Brief Summary

Align task architecture wording, design routing, and broad-edit confirmation boundaries without changing the established harness architecture.

## Continuity

Keep changes within the frozen scope below. The `.tasks/` directory is ignored; do not commit task records.

## Context

- Target repo(s):
    - `./opencode` (branch: TBD; no branch operation requested) - OpenCode infrastructure source at this repository root, mirrored to `masterlifting/infra`
- Task kind: code
- Implementation plan: non-complex
- Problem / opportunity: Task wording requires independent architects too broadly, the non-complex design gate is conflicting, and confirmation uses an arbitrary file count.
- Constraints: Preserve the existing architecture; do not change models, reviewer topology, worktrees, safety gates, or unrelated infrastructure.
- Related links/issues: User objective, 2026-08-22.

## Key Files

<!-- Map important files, configs, docs, and scripts as they are discovered. -->

| Purpose               | Path                             |
| --------------------- | -------------------------------- |
| Task orchestration | `skills/task/SKILL.md` |
| Canonical gates | `skills/task/references/agent-gates.md` |
| Task template/parser/validation | `skills/task/references/template.md`, `skills/task/scripts/TaskMd.fsx`, `skills/audit-infra/scripts/ValidateInfrastructure.fsx` |
| Global safety policy | `AGENTS.md` |
| Task-record ignore rule | `.gitignore` |

## Requirements / Acceptance Criteria

- `/task` delegates proportional architecture routing to `references/agent-gates.md` and does not require independent architects for every task.
- Non-complex tasks default to coordinator design, with at most one appropriate architect for concrete unresolved uncertainty; complex or architecture-sensitive tasks retain two independent architects and coordinator synthesis.
- Broad edits use an accepted bounded scope and re-confirm only for material scope expansion; action-specific safety gates remain unchanged.
- Canonical task artifacts and deterministic validation remain aligned; applicable deterministic and targeted behavioral validation passes.

## Solution Contract

- State: FROZEN
- Requirements: Proportional architecture routing, scope-expansion confirmation, and canonical task invariants remain consistent.
- Acceptance criteria: All user-stated completion criteria are met and relevant validation passes.
- Accepted assumptions: The current repository is the source mirrored to `masterlifting/infra`; no local branch operation is requested.
- Non-goals: New agents, workflow layers, mandatory SPEC/worktrees/TDD, and changes to unrelated safety or engineering policies.
- Chosen solution: Update the three requested policy owners and their exact task-template/parser/validator duplicates; add `.tasks/` to `.gitignore`; validate only affected deterministic and behavioral surfaces.
- Important boundaries/contracts: `agent-gates.md` remains canonical; complex routing retains two isolated read-only architect proposals and subtractive coordinator synthesis; architecture reopening remains hard-invalidation-only.
- Implementation constraints: Do not alter existing user edits to `agents/build.md` or `opencode.json`; no commit, push, external write, or destructive action.
- Review profile: Standard
- Rejected alternatives: None recorded.

## Non-Goals

- Broader harness redesign or policy prose cleanup.
- Changes to model allocation, reviewers, explorer, debug workflow, SPEC traceability, retention, worktrees, MCP setup, permissions, or domain rules.

## References

<!-- Keep links to external docs, analysis notes, tickets, and generated artifacts here. -->

- Ticket/spec: User objective, 2026-08-22.
- Analysis docs: Not needed; targeted read-only audit completed in session.
- Behavioral specification: (optional) `.tasks/INFRA-001/SPEC.md`

## Subtasks

### 1. Research and define approach

Steps:

- [x] Investigate relevant code paths and document findings
  - Summary: Audited the requested policy files and exact template/parser/validator duplicates; no unrelated policy duplicate requires change.
- [x] Define expected behavior and constraints
  - Summary: Preserved existing architecture and isolated the three semantic corrections plus canonical invariant alignment.
- [x] Draft task-specific delivery and validation work; for code tasks, classify implementation as complex or non-complex
  - Summary: Classified non-complex because one bounded documentation/invariant update and validation cycle covers the change.

### 2. Clarify gaps before implementation

Classify gaps under `references/clarification.md`: ask only `BLOCKING`
questions, record meaningful assumptions, resolve `NON-BLOCKING` gaps without
interrupting, and block only unresolved `BLOCKING` gaps.

Steps:

- [x] Classify research gaps as BLOCKING, ASSUMPTION, or NON-BLOCKING
  - Summary: No blocking gaps; accepted the user's clarification that this repository is the source mirror.
- [x] Ask only BLOCKING questions and record answers or meaningful assumptions in `## Decisions` or `## Open Questions`
  - Summary: No blocking question remained after the source-repository clarification.
- [x] Resolve NON-BLOCKING gaps in the task plan without interruption
  - Summary: Included template/parser/validator changes only where their exact canonical labels duplicate the modified policy.
- [x] Mark only unresolved BLOCKING gaps with `[blocked]` notation and set `Status: Blocked` while waiting
  - Summary: No unresolved blocking gaps.

### 3. Design gate

Blocks implementation subtasks until the gate is clean or explicitly waived
(see `references/agent-gates.md`). Architect delegation is proportional:
complex or architecture-sensitive tasks use both independent architect agents;
non-complex tasks use only the minimum architecture analysis necessary.

Steps:

- [x] Architecture routed per `references/agent-gates.md`; independent proposals used only when that gate requires them, then coordinator solution frozen
  - Summary: Coordinator settled the non-complex design; no material design uncertainty requires an architect.
- [x] Conditional specialists run per `references/agent-gates.md` or explicitly N/A
  - Summary: N/A; the diff does not materially affect database, DevOps, security, or performance-owned surfaces.
- [x] Set `Implementation plan` to `non-complex` or `complex`; approve the final task-specific structure with no generic planning placeholders
  - Summary: Set to non-complex; implementation is one bounded policy/invariant update followed by validation.

### 4. Branch setup across touched repos

- Branch format: `INFRA-001-description` when the repo does not define another format.
- Create or switch branches only after research identifies the repos that will actually be touched.
  Steps:
- [x] Create or switch to a working branch in each touched repo
  - Summary: Not applicable; no branch operation was requested or authorized.
- [x] Update `## Context` target repo branch entries with the selected branch names
  - Summary: Recorded the source repository with branch TBD because no local branch is selected.

### 5. Implement and validate

Steps:

- [x] Update the bounded policy and canonical label artifacts
  - Summary: Updated task policy, agent-gate routing, scope confirmation, template/parser/validator labels, and the `.tasks/` ignore rule within the frozen scope.
- [x] Run deterministic validation and targeted behavioral checks with recorded evidence
  - Summary: Task/workflow/infrastructure validation and self-test passed; S1, S3, S5, S9, S10, and focused architecture routing all passed in fresh contexts.

- [x] Engineer-owned implementation completed
  - Summary: Executor completed the six frozen policy and canonical-label artifacts without touching unrelated user changes.
- [x] Engineer-owned build verdict recorded, or build explicitly not applicable
  - Summary: Infrastructure validation and its self-test completed successfully; no separate build applies to this configuration-only update.
- [x] Tester inspected existing coverage, designed and implemented required tests, and recorded the test verdict; if no tester exists, implementation-agent test ownership recorded
  - Summary: F# tester ran task validation, task workflow tests, infrastructure validation/self-test, and `git diff --check`; all passed.

## Review

- State: NEW
- Implementation baseline: INFRA-001 scoped working-tree artifacts after implementation
- Remediation pass: 0
- Build evidence: Not applicable: configuration-only update; infrastructure validator passed.
- Test evidence: Passed: ValidateTask, TaskWorkflowTests, ValidateInfrastructure/self-test, and git diff --check all exited 0.

After work, use `Passed: <command/result>`, `Not applicable: <reason>`, or `Waived: <Decision reference>`.

### Accepted findings

| ID | Contract | Status |
| -- | -------- | ------ |
| None | Targeted verification of frozen INFRA-001 acceptance criteria | FIXED |

### Verification receipts

| Finding ID | Result | Evidence |
| ---------- | ------ | -------- |
| None | APPROVE | Targeted audit-infra Verification passed all six scoped acceptance criteria with no blockers. |

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

- [x] Scratchpad / temp working files created for this task removed
  - Summary: No task-created scratchpad or temporary files require removal; the retained ignored task record is intentional.

## Decisions

| Date | Decision | Rationale |
| ---- | -------- | --------- |
| 2026-08-22 | Source repository and bounded scope accepted | The user confirmed this repository is mirrored to `masterlifting/infra` and requested `.tasks/` be ignored. |
| 2026-08-22 | Non-complex coordinator design frozen | The update has no material design uncertainty; `agent-gates.md` permits coordinator-only design. |
| 2026-08-22 | Deterministic and targeted behavioral validation passed | ValidateTask, TaskWorkflowTests, ValidateInfrastructure/self-test, git diff --check, S1, S3, S5, S9, S10, and focused routing verification passed. |
| 2026-08-22 | complete status waiver | The user explicitly required post-change review to be targeted Verification only, so the unneeded broad Discovery closing subtask remains incomplete. |
| 2026-08-22 | complete status confirmed | User explicitly confirmed completion after all scoped implementation and validation evidence passed. |

<!-- Before setting Status: Complete, record a dated "complete status confirmed"
     decision. If incomplete lifecycle items are intentionally waived, record
     "complete status waiver" and its rationale in the same or a separate row. -->

## Open Questions

- None.

## Notes

- State: Complete under the recorded targeted-Verification waiver.
- Evidence: No accepted blocking findings; all deterministic and targeted behavioral checks passed.
- Next: No further action; do not commit or push without separate confirmation.
