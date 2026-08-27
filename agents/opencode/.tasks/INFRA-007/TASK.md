# INFRA-007 - Reduce task workflow latency

**Progress: 7/7 subtasks complete** | **Status: Complete** | **Created: 2026-08-26** | **Completed: 2026-08-26**

## Brief Summary

Reduce normal `/task` wall-clock time by consolidating automatic TASK.md synchronization and explicitly dispatching already-required independent work in parallel. Preserve the agent architecture, validation quality, safety gates, and model spend.

## Continuity

Work locally on `master`; do not create, switch, commit, or push branches without the applicable explicit confirmation.

## Context

- Target repo(s):
    - `./.` (branch: TBD after research) - global OpenCode infrastructure and task workflow
- Task kind: code
- Implementation plan: non-complex
- Problem / opportunity: normal task edits launch separate F# recomputation and validation processes; routing guidance does not fully make independent parallel waves and batching expectations explicit.
- Constraints: preserve agent/skill inventory and names, model/provider assignments, review profiles, architecture/build/test/Discovery/Verification ownership and semantics, confirmation policy, bounded non-fatal plugin behavior, and existing validation surface. Do not add LLM calls solely for speed.
- Related links/issues: INFRA-007

## Key Files

| Purpose | Path |
| --- | --- |
| TASK synchronization hook | `plugins/task-progress.js` |
| Queue and plugin tests | `lib/task-progress-core.{mjs,test.mjs}` |
| Shared task parser | `skills/task/scripts/TaskMd.fsx` |
| Progress and validation helpers | `skills/task/scripts/{RecomputeProgress,ValidateTask}.fsx` |
| Task workflow and routing guidance | `skills/task/{SKILL.md,references/agent-gates.md}` |
| Handoff contract | `rules/software/agent-handoff.md` |
| Infrastructure static tests | `skills/audit/scripts/{ValidateInfrastructure,TestInfrastructure}.fsx` |

## Requirements / Acceptance Criteria

- One automatic F# synchronization process recomputes progress then validates the resulting TASK.md, preserving useful diagnostics and non-fatal bounded hook behavior.
- Canonical task and routing guidance requires coherent state batching and safe parallel waves for only independently selected/required agents.
- Deterministic tests cover synchronization, batching guidance, parallel routing, and implementation concurrency without expanding required agent invocations.
- Required validation passes or records only pre-existing unrelated failures; no agent/model/provider assignment or confirmation/safety gate changes.

## Solution Contract

- State: FROZEN
- Requirements: Implement the user-specified latency-only refactor across task synchronization, task guidance, routing guidance, and deterministic tests.
- Acceptance criteria: One FSI process path per automatic task synchronization; post-recompute validation and existing diagnostic/failure isolation survive; documented waves constrain parallelism to already-required independent roles; required deterministic coverage and validation are recorded.
- Accepted assumptions: This is non-complex infrastructure work with a user-provided canonical solution; no behavioral specification or architecture proposal is required.
- Non-goals: Change agent/skill inventory, agent/model/provider assignments, review profiles, architecture gates, build/test ownership, Discovery/Verification semantics, confirmation policy, or add LLM calls/validation commands solely for latency.
- Chosen solution: Add synchronization mode to the canonical validator using `TaskMd.fsx`; change the plugin to invoke it once; update centralized task/gate/handoff guidance and existing deterministic static tests.
- Important boundaries/contracts: `TaskMd.fsx` remains the sole parsing/progress source; the plugin queue, timeout, output cap, sanitization, and non-fatal edit isolation remain; parallel reviewers/proposals stay isolated on identical frozen evidence.
- Implementation constraints: Work on current `master` without branch mutation; preserve existing validation surface; no commit or push.
- Review profile: contract
- Rejected alternatives: Two plugin FSI launches; duplicated parser/progress logic; generic DAG orchestration; new agents or calls for speed.

## Non-Goals

- Parallelizing dependent implementation, build, test, review, or remediation work.
- Changing historical task records or unrelated pre-existing working-tree changes.

## References

- Ticket/spec: INFRA-007 user-provided contract
- Analysis: read-only auditor report from this session

## Subtasks

### 1. Research and define approach

Steps:

- [x] Investigate relevant code paths and document findings
  - Summary: Audited the task-progress hook, queue tests, shared TASK.md parser, progress and validation scripts, task guidance, routing gates, and infrastructure validation surface.
- [x] Define expected behavior and constraints
  - Summary: The user supplied the complete latency-only contract, including preserved isolation, validation, architecture, ownership, and provider constraints.
- [x] Draft task-specific delivery and validation work; for code tasks, classify implementation as complex or non-complex
  - Summary: One integrated plugin, documentation, and deterministic-test slice is sufficient; classified non-complex.

### 2. Clarify gaps before implementation

Steps:

- [x] Classify research gaps as BLOCKING, ASSUMPTION, or NON-BLOCKING
  - Summary: No blocking gaps; synchronization mode in the validator is the smallest canonical implementation choice.
- [x] Ask only BLOCKING questions and record answers or meaningful assumptions in `## Decisions` or `## Open Questions`
  - Summary: No blocking question is needed because the user froze the governing constraints and acceptance criteria.
- [x] Resolve NON-BLOCKING gaps in the task plan without interruption
  - Summary: Retain standalone recomputation helper while routing automatic synchronization through a single validator invocation.
- [x] Mark only unresolved BLOCKING gaps with `[blocked]` notation and set `Status: Blocked` while waiting
  - Summary: Not applicable; no unresolved blocking gaps exist.

### 3. Design gate

Steps:

- [x] Architecture routed per `references/agent-gates.md`; independent proposals used only when that gate requires them, then coordinator solution frozen
  - Summary: Non-complex work with a user-provided canonical solution; no architect or challenger call is required by the existing gate.
- [x] Conditional specialists run per `references/agent-gates.md` or explicitly N/A
  - Summary: Database, DevOps, security, and performance surfaces are not materially touched; conditional specialist routing is N/A.
- [x] Set `Implementation plan` to `non-complex` or `complex`; approve the final task-specific structure with no generic planning placeholders
  - Summary: Set non-complex and defined one concrete implementation/validation slice with mandatory ownership gates.

### 4. Branch setup across touched repos

Steps:

- [x] Create or switch to a working branch in each touched repo
  - Summary: Not applicable: user explicitly required local work on existing `master`; no branch mutation was performed.
- [x] Update `## Context` target repo branch entries with the selected branch names
  - Summary: Retained the validator-compatible TBD branch marker while Continuity records the explicit `master` constraint.

### 5. Implement and validate

Steps:

- [x] Engineer-owned implementation completed
  - Summary: Added `--sync` mode to `ValidateTask.fsx` (recompute then validate in one FSI process via the shared `TaskMd.syncProgressCounters` helper); switched `plugins/task-progress.js` to a single `--sync` invocation; added `createTaskSynchronizer`/sanitization helpers to `lib/task-progress-core.mjs`; refactored `RecomputeProgress.fsx` to reuse the shared helper; updated task/gate/handoff guidance for coherent state batching and explicitly parallel independent waves.
- [x] Engineer-owned build verdict recorded, or build explicitly not applicable
  - Summary: No compile surface (interpreted `.fsx` and Node `.mjs`/`.js`); static validation passed (`node --check` on changed JS and `dotnet fsi` load/smoke runs incl. a live `--sync` invocation against this task).
- [x] Tester inspected existing coverage, designed and implemented required tests, and recorded the test verdict; if no tester exists, implementation-agent test ownership recorded
  - Summary: Verdict PASS. Added deterministic `--sync` coverage (drift detection, recompute-then-validate, post-recompute violation survival, repeat-sync) to `TaskWorkflowTests.fsx`; full INFRA-007 deterministic surface green: `TaskWorkflowTests.fsx`, `TaskMdTests.fsx`, `lib/task-progress-core.test.mjs`, `ValidateInfrastructure.fsx --self-test`; live `ValidateTask.fsx` on this record passes.

## Review

- State: FROZEN
- Implementation baseline: Uncommitted INFRA-007 scoped working-tree diff after tester evidence; excludes pre-existing agent/model-provider drift.
- Remediation pass: 1
- Build evidence: Not applicable: interpreted `.fsx` and Node `.mjs`/`.js` surface has no compile step; `node --check` and `dotnet fsi` load/smoke validation passed.
- Test evidence: Passed: dotnet fsi skills/task/scripts/TaskWorkflowTests.fsx

### Accepted findings

| ID | Contract | Status |
| --- | --- | --- |
| F1 | Preserve a surfaced bounded synchronization-helper failure diagnostic while validation continues on the current TASK.md. | FIXED |

### Verification receipts

| Finding ID | Result | Evidence |
| --- | --- | --- |
| F1 | FIXED | Targeted F# reviewer verified `ValidateTask.fsx` failure capture and `TaskWorkflowTests.fsx` write-failure/retry coverage; both affected F# suites passed. |

## Closing Steps

### C0. Pre-commit review board

Steps:

- [x] Discovery reviewers' verdicts recorded after the evidence precondition
  - Summary: One parallel contract-profile Discovery wave used isolated F# reviewer and validator contexts against the identical frozen scoped baseline and recorded evidence. Validator returned PASS; reviewer reported accepted Warning F1: a best-effort `--sync` progress-write failure can be silent even though validation continues.
- [x] Critical/Error findings fixed or waived in `## Decisions`
  - Summary: No Critical/Error findings were reported. Accepted Warning F1 was remediated because it directly affected required synchronization diagnostics.
- [x] Targeted Verification receipts recorded for the frozen accepted finding set
  - Summary: Targeted reviewer returned FIXED for F1 using `ValidateTask.fsx` implementation evidence and passing `TaskWorkflowTests.fsx` write-failure/retry coverage; no fresh Discovery was run.

### C1. Clean up temporary artifacts

Steps:

- [x] Scratchpad / temp working files created for this task removed
  - Summary: No task-created temporary artifacts remain under the approved temp location; no source-tree or task-record files were removed.

## Decisions

| Date | Decision | Rationale |
| --- | --- | --- |
| 2026-08-26 | Frozen latency-only synchronization and parallel-wave solution | The user supplied the governing contract and audit confirmed a bounded canonical implementation. |
| 2026-08-26 | Accept Discovery Warning F1 for targeted remediation | The warning directly affects the required preservation of useful non-fatal synchronization diagnostics; fix it without reopening Discovery. |
| 2026-08-26 | Freeze accepted review finding set | F1 is verified FIXED; the isolated validator had no findings and no further Discovery is needed for this baseline. |
| 2026-08-26 | complete status confirmed | User explicitly confirmed the INFRA-007 commit and task completion after cleanup, validation, Discovery, and targeted Verification. |

## Open Questions

- None.

## Notes

- State: Complete.
- Evidence: Read-only audit identified two sequential plugin FSI launches and bounded existing test/routing guidance surfaces; implementation collapsed them to one `ValidateTask.fsx --sync` invocation sharing `TaskMd.syncProgressCounters`. Deterministic coverage added: `TaskWorkflowTests.fsx` sync-mode assertions including the write-failure diagnostic regression, `TaskMdTests.fsx` `syncProgressCounters` unit tests, `lib/task-progress-core.test.mjs` single-invocation/sanitization/non-fatal-hook tests, `ValidateInfrastructure.fsx` parallel-wave self-tests and batching/parallel-guidance markers. Final passes: `dotnet fsi skills/task/scripts/TaskWorkflowTests.fsx`, `dotnet fsi skills/task/scripts/TaskMdTests.fsx`, `node lib/task-progress-core.test.mjs`, `dotnet fsi skills/audit/scripts/ValidateInfrastructure.fsx --self-test`, and `dotnet fsi skills/task/scripts/ValidateTask.fsx .tasks/INFRA-007/TASK.md --sync`; `git diff --check` passes. Scoped diff confirms no `agents/`, `opencode.json`, confirmation-policy, or closing-steps changes.
- Pre-existing unrelated live failures (recorded per acceptance criteria, out of scope by contract): `ValidateInfrastructure.fsx` live run fails on 23 agent-model/agent-routing/obsolete-reference/team-consistency errors in `agents/*.md` (e.g., `opencode-go/deepseek-v4-pro` not a verified production ID vs expected `deepseek/deepseek-v4-pro`; `xai/` obsolete references in challenger/guardian files), originating from the HEAD `update models` commit; those files are untouched by INFRA-007.
- Next: No task work remains. Commit `b92ddab` is local only; no push was requested.
