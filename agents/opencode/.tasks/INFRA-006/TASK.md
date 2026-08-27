# INFRA-006 - Finalize agent ownership and routing

**Progress: 7/7 subtasks complete** | **Status: Complete** | **Created: 2026-08-25** | **Completed: 2026-08-26**

## Brief Summary

Resolve the remaining normalized-agent ownership and review-routing ambiguity. Preserve the agreed inventory, provider assignments, names, and overall workflow while making test ownership, frozen-solution boundaries, and review selection deterministic.

## Continuity

Work locally on `master`; do not create, switch, commit, or push branches without the applicable explicit confirmation.

## Context

- Target repo(s):
    - `./.` (branch: TBD after research) - global OpenCode infrastructure and task workflow
- Task kind: code
- Implementation plan: non-complex
- Problem / opportunity: shared test ownership and discovery-review selection are internally inconsistent.
- Constraints: retain the final agent vocabulary, models/providers, names, routing strategy, and no automatic paid fallback; do not modify pre-existing unrelated edits to `agents/executor.md`, `agents/explorer.md`, `opencode.json`, or `skills/audit/scripts/ValidateInfrastructure.fsx`.
- Related links/issues: INFRA-006

## Key Files

<!-- Map important files, configs, docs, and scripts as they are discovered. -->

| Purpose               | Path                             |
| --------------------- | -------------------------------- |
| Shared ownership contract | `rules/software/agent-handoff.md` |
| Test ownership rule | `rules/software/testing.md` |
| Database and DevOps agents | `agents/software/{database,devops}/engineer.md` |
| Task routing and templates | `skills/task/{SKILL.md,references/{agent-gates.md,template.md,validation.md}}` |
| Deterministic checks | `skills/audit/scripts/ValidateInfrastructure.fsx` |

## Requirements / Acceptance Criteria

- Make the tester the normal owner of all applicable test design, implementation, and execution; reserve the implementation-owner fallback for absent language testers.
- Make review profiles map exactly to `routine`, `contract`, `architecture`, and `combined` reviewer sets, with specialist reviewers selected only by touched surface.
- Centralize engineer frozen-solution authority and validate ownership/routing without changing agent inventory or model/provider assignments.

## Solution Contract

- State: FROZEN
- Requirements: Normalize test ownership, frozen-solution boundaries, and deterministic review routing across the live infrastructure.
- Acceptance criteria: Shared and specialist rules agree; task documents agree; static routing checks encode all requested ownership, review, specialist, and invalidation cases; inventory/providers/models remain unchanged.
- Accepted assumptions: This is non-complex infrastructure work; no behavioral specification or architecture proposal is needed because the user supplied the canonical contract.
- Non-goals: Rename/add agents or skills; alter provider strategy, model assignments, naming, workflow architecture, historical completed tasks, or unrelated working-tree changes.
- Chosen solution: Update centralized contracts and their references, clarify task documentation, and extend the deterministic infrastructure validator self-test routing matrix.
- Important boundaries/contracts: `agent-handoff.md` is the central engineer contract; tester owns software tests where applicable; engineers own builds and implementation-native validation; specialist reviews are surface-driven.
- Implementation constraints: Work on current `master` without branch mutation; do not alter unrelated pre-existing edits; no commit or push.
- Review profile: contract
- Rejected alternatives: None recorded.

## Non-Goals

- Agent inventory, file names, provider routing, and model assignments.
- Rewriting historical completed task records that are not current normative guidance.

## References

<!-- Keep links to external docs, analysis notes, tickets, and generated artifacts here. -->

- Ticket/spec: INFRA-006 user-provided contract
- Analysis: read-only auditor report from this session

## Subtasks

### 1. Research and define approach

Steps:

- [x] Investigate relevant code paths and document findings
  - Summary: Audited the shared contracts, specialist and language engineers, task references, and validator routing matrix; found database-engineer test ownership and non-deterministic profiles.
- [x] Define expected behavior and constraints
  - Summary: User supplied the canonical ownership, profile, specialist composition, architecture gate, and provider/inventory constraints.
- [x] Draft task-specific delivery and validation work; for code tasks, classify implementation as complex or non-complex
  - Summary: One integrated documentation-and-static-validation slice is sufficient; classified non-complex.

### 2. Clarify gaps before implementation

Classify gaps under `references/clarification.md`: ask only `BLOCKING`
questions, record meaningful assumptions, resolve `NON-BLOCKING` gaps without
interrupting, and block only unresolved `BLOCKING` gaps.

Steps:

- [x] Classify research gaps as BLOCKING, ASSUMPTION, or NON-BLOCKING
  - Summary: No blocking gaps; the supplied canonical contract resolves the only material routing decisions.
- [x] Ask only BLOCKING questions and record answers or meaningful assumptions in `## Decisions` or `## Open Questions`
  - Summary: No blocking question required; recorded the non-complex assumption in the Solution Contract.
- [x] Resolve NON-BLOCKING gaps in the task plan without interruption
  - Summary: Chose centralized rule updates and validator self-tests rather than duplicating contracts in language agents.
- [x] Mark only unresolved BLOCKING gaps with `[blocked]` notation and set `Status: Blocked` while waiting
  - Summary: Not applicable; no unresolved blocking gaps exist.

### 3. Design gate

Blocks implementation subtasks until the gate is clean or explicitly waived
(see `references/agent-gates.md`). Architect routing follows that gate:
complex or architecture-sensitive tasks use two isolated read-only architect
proposals; non-complex tasks default to coordinator design, with at most one
appropriate architect for concrete unresolved uncertainty.

Steps:

- [x] Architecture routed per `references/agent-gates.md`; independent proposals used only when that gate requires them, then coordinator solution frozen
  - Summary: Non-complex work with a user-provided canonical solution; coordinator froze the minimal solution and did not invoke architect or challenger.
- [x] Conditional specialists run per `references/agent-gates.md` or explicitly N/A
  - Summary: Database and DevOps scope was audited; security/performance review is N/A because no security or performance surface changes are planned.
- [x] Set `Implementation plan` to `non-complex` or `complex`; approve the final task-specific structure with no generic planning placeholders
  - Summary: Set non-complex and replaced the generic implementation placeholder with concrete implementation and validation work.

### 4. Branch setup across touched repos

- Branch format: `INFRA-006-description` when the repo does not define another format.
- Create or switch branches only after research identifies the repos that will actually be touched.
  Steps:
- [x] Create or switch to a working branch in each touched repo
  - Summary: Not applicable: user explicitly required local work on the existing `master` branch; no branch mutation was performed.
- [x] Update `## Context` target repo branch entries with the selected branch names
  - Summary: Retained the validator-compatible TBD branch marker while Continuity records the explicit `master` constraint.

### 5. Implement and validate

Steps:

- [x] Engineer-owned implementation completed
  - Summary: Centralized frozen-solution and test ownership in shared rules; aligned database/DevOps agents, deterministic review routing, behavioral scenario S12, and F# validator self-tests without changing agent inventory or assignments.
- [x] Engineer-owned build verdict recorded, or build explicitly not applicable
  - Summary: Not applicable: no compiled project surface changed. Engineer ran `dotnet fsi skills/audit/scripts/ValidateInfrastructure.fsx --self-test` successfully as implementation-native static validation.
- [x] Tester inspected existing coverage, designed and implemented required tests, and recorded the test verdict; if no tester exists, implementation-agent test ownership recorded
  - Summary: Tester found required coverage present and ran the F# task and validator suites successfully; no new test implementation was needed. Live infrastructure validation has 23 pre-existing out-of-scope model-routing errors; all INFRA-006 scoped checks pass.

## Review

- State: FROZEN
- Implementation baseline: INFRA-006 scoped ownership/routing diff; unrelated pre-existing model-routing WIP excluded.
- Remediation pass: 0
- Build evidence: Not applicable: no compiled project surface changed; `dotnet fsi skills/audit/scripts/ValidateInfrastructure.fsx --self-test` passed as implementation-native static validation.
- Test evidence: Passed: `dotnet fsi skills/task/scripts/TaskMdTests.fsx`, `dotnet fsi skills/task/scripts/TaskWorkflowTests.fsx`, and `dotnet fsi skills/audit/scripts/ValidateInfrastructure.fsx --self-test` passed.

After work, use `Passed: <command/result>`, `Not applicable: <reason>`, or `Waived: <Decision reference>`.

### Accepted findings

| ID | Contract | Status |
| -- | -------- | ------ |

### Verification receipts

| Finding ID | Result | Evidence |
| ---------- | ------ | -------- |
| None | APPROVE | Independent reviewer and validator both returned PASS; focused F# suites passed. |

## Closing Steps

<!-- Re-check from C0 onward whenever new work lands after a C-step was checked
      — treat the earlier check as stale. -->

### C0. Pre-commit review board

- Run the one Discovery selected by `references/agent-gates.md` independently and in parallel on the full diff only after build and test evidence is recorded. An explicit not-applicable rationale or recorded waiver may replace either evidence item. A reviewer missing required evidence returns `BLOCKED`; do not transition from `NEW` or proceed with review.
  Steps:
- [x] Discovery reviewers' verdicts recorded after the evidence precondition
  - Summary: Isolated F# reviewer and validator both returned PASS against the frozen scoped diff; validator recorded only two non-blocking documentation observations.
- [x] Critical/Error findings fixed or waived in `## Decisions`
  - Summary: No Critical/Error findings were reported or accepted; no remediation was required.
- [x] Targeted Verification receipts recorded for the frozen accepted finding set
  - Summary: Recorded the `None` approval receipt because the accepted finding set is empty.

### C1. Clean up temporary artifacts

Before committing, remove only task-created scratchpad or temp files. See `references/closing-steps.md` for the keep/remove list. Every removal requires explicit confirmation.

Steps:

- [x] Scratchpad / temp working files created for this task removed
  - Summary: No task-created scratchpad or temporary files exist; no removal was needed.

## Decisions

| Date | Decision | Rationale |
| ---- | -------- | --------- |
| 2026-08-25 | Frozen minimal ownership/routing solution | The user supplied the governing contract; no architecture uncertainty remains. |
| 2026-08-25 | No remediation after contract Discovery | Independent reviewer and validator returned PASS; their two observations are non-blocking and require no scope change. |
| 2026-08-26 | complete status confirmed | User explicitly confirmed task completion after all lifecycle steps and verification evidence were recorded. |

<!-- Before setting Status: Complete, record a dated "complete status confirmed"
     decision. If incomplete lifecycle items are intentionally waived, record
     "complete status waiver" and its rationale in the same or a separate row. -->

## Open Questions

- None.

## Notes

- State: Complete.
- Evidence: Focused F# suites and task validation pass; full live infrastructure validation has 23 pre-existing out-of-scope provider-model errors, while all INFRA-006 scoped checks pass. `git diff --check` passes.
- Next: No task work remains. No commit or push was requested.

<!-- Durable portable record: keep State/Evidence/Next current and leave out
     volatile snapshots and large tables. On completion they must be
     terminal-current: Next states explicitly that no task work remains, or
     lists only real manual/optional follow-up (e.g., a known dependent record
     to refresh — never mutate it). Optional Origin:/Unblocks:/Unblock condition:
     markers live in `## References`; completing this task does not establish the dependent condition. -->
