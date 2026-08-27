# INFRA-009 - Add provenance-based task scratch cleanup

**Progress: 10/11 subtasks complete** | **Status: Complete** | **Created: 2026-08-26** | **Completed: 2026-08-26**

## Brief Summary

Add a canonical, machine-local task scratch lifecycle for transient OpenCode
agent artifacts. Only manifest-proven, registered disposable scratch can be
automatically cleaned; durable evidence requires explicit promotion.

## Continuity

<!-- Optional: instructions to run every time this task is loaded. Delete this section if not needed. -->

## Context

- Target repo(s):
    - `./opencode` (branch: master) - global OpenCode task workflow infrastructure
- Task kind: code
- Implementation plan: complex
- Problem / opportunity: current task cleanup is prose-only and cannot prove ownership.
- Constraints: OS-temp scratch only; no generic deletion; preserve agents, gates, confirmation policy, durable task artifacts, source, and project-native outputs; no commit/push.
- Related links/issues:

## Key Files

<!-- Map important files, configs, docs, and scripts as they are discovered. -->

| Purpose               | Path                             |
| --------------------- | -------------------------------- |
| Scratch lifecycle helper | `skills/task/scripts/TaskScratch.fsx` |
| Task workflow closeout | `skills/task/references/closing-steps.md` |
| Infrastructure tests/validator | `skills/audit/scripts/{Test,Validate}Infrastructure.fsx` |

## Requirements / Acceptance Criteria

- Implement the behaviors in `SPEC.md`, including fail-closed provenance checks.
- Keep durable `.tasks/<TASK-ID>` evidence, repository files, and project-native outputs outside automatic deletion.
- Add deterministic helper and infrastructure validation coverage, then run the canonical suite and `git diff --check`.

## Solution Contract

- State: FROZEN
- Requirements: `SPEC.md` is authoritative for behavior; user acceptance criteria are retained in the request.
- Acceptance criteria: all required provenance, promotion, cleanup, resume, worktree, documentation, and validation behaviors are evidenced.
- Accepted assumptions: Closing cleanup is the explicit authorization boundary: `seal` records that no active dependency remains; cleanup requires the seal rather than task status, which remains In Progress until closeout completes. Worktree cleanup is documented provenance-only; no worktree automation is added. User waived Unix support on 2026-08-26; all helper operations refuse outside Windows rather than use a weaker implementation.
- Non-goals: change agents, models, providers, review gates, confirmation policy, task lifecycle semantics beyond bounded owned scratch cleanup, or project-native artifact cleanup.
- Chosen solution: Add a task-local F# `TaskScratch.fsx` with `create`, `register`, `report`, `promote`, `seal`, and `clean` operations plus a versioned JSON manifest below `<temp>/opencode/tasks/<TASK-ID>/<RUN-ID>/`. Automatic clean and promotion use a private Windows-native `SafeFs` layer with opaque handle capabilities. All non-Windows platforms refuse every helper operation; no weaker compatibility path is retained. The manifest records stable root/file identity, digest, and promotion state. Add deterministic tests, narrow C1 documentation/template changes, and infrastructure validator assertions.
- Important boundaries/contracts: task scratch helper is the sole mutable deletion surface; manifest paths are root-relative; durable promotion targets current task docs/scripts only. No arbitrary roots, force flags, recursive cleanup requests, age sweeps, broad temp deletion, Git clean, or worktree pruning. Reparse points in any ancestor, target, or descendant fail closed. Windows native operations bind cleanup/promotion to verified handles rather than revalidated pathnames.
- Implementation constraints: F# with Windows platform interop; all non-Windows operations fail closed; no unsafe compatibility fallback, arbitrary deletion API, `git clean`, time/name heuristics, or user data collection. Bounded eligible cleanup is automatic at C1 without per-file confirmation; all non-scratch deletion retains existing explicit confirmation.
- Review profile: combined
- Rejected alternatives: heuristic cleanup and generic temp cleanup (unprovable); status-only cleanup gating (incompatible with closeout ordering); automatic worktree cleanup (requires separate provenance and integration evidence); pathname-based check-then-delete/copy (cannot close reparse races); report-only cleanup (does not meet approved automatic-cleanup goal).

## Non-Goals

- Automatically deleting unknown, user-authored, repository, durable task, or project-native files.
- Broad worktree pruning or branch deletion.

## References

<!-- Keep links to external docs, analysis notes, tickets, and generated artifacts here. -->

- Ticket/spec: (link, if applicable)
- Notion/Drive/GitHub: (link, if applicable)
- Analysis docs: `.tasks/INFRA-009/docs/...`
- Behavioral specification: `.tasks/INFRA-009/SPEC.md`
- Origin: (optional) path of the source record this task derives from
- Unblocks: (optional) path of a dependent record to refresh on completion; never mutate it
- Unblock condition: (optional) observable condition (e.g., environment variable, file presence, API response) the dependent workflow must revalidate before proceeding; never mutate the dependent record from this task

## Subtasks

### 1. Research and define approach

Steps:

- [x] Investigate relevant code paths and document findings
  - Summary: Audit found only prose cleanup and no ownership/provenance helper or tests.
- [x] Define expected behavior and constraints
  - Summary: Behavioral invariants recorded in `SPEC.md`; cleanup must fail closed.
- [x] Draft task-specific delivery and validation work; for code tasks, classify implementation as complex or non-complex
  - Summary: Complex infrastructure task: helper/manifest, deterministic tests, workflow/docs, and validator integration require ordered implementation and separate test ownership.

### 2. Clarify gaps before implementation

Classify gaps under `references/clarification.md`: ask only `BLOCKING`
questions, record meaningful assumptions, resolve `NON-BLOCKING` gaps without
interrupting, and block only unresolved `BLOCKING` gaps.

Steps:

- [x] Classify research gaps as BLOCKING, ASSUMPTION, or NON-BLOCKING
  - Summary: Seal timing and worktree treatment were ASSUMPTION-level; no blocking gap remains.
- [x] Ask only BLOCKING questions and record answers or meaningful assumptions in `## Decisions` or `## Open Questions`
  - Summary: No blocking question; accepted assumptions are frozen in the Solution Contract.
- [x] Resolve NON-BLOCKING gaps in the task plan without interruption
  - Summary: Use a manifest seal after explicit closeout dependency verification; document worktree requirements without adding automation.
- [x] Mark only unresolved BLOCKING gaps with `[blocked]` notation and set `Status: Blocked` while waiting
  - Summary: No unresolved blocking gap.

### 3. Design gate

Blocks implementation subtasks until the gate is clean or explicitly waived
(see `references/agent-gates.md`). Architect routing follows that gate:
complex or architecture-sensitive tasks use two isolated read-only architect
proposals; non-complex tasks default to coordinator design, with at most one
appropriate architect for concrete unresolved uncertainty.

Steps:

- [x] Architecture routed per `references/agent-gates.md`; independent proposals used only when that gate requires them, then coordinator solution frozen
  - Summary: Isolated F# architect and challenger proposals synthesized into the frozen fail-closed manifest/seal design.
- [x] Conditional specialists run per `references/agent-gates.md` or explicitly N/A
  - Summary: Security and DevOps specialists N/A: no credential, authentication, CI/CD, deployment, or data surface changes.
- [x] Set `Implementation plan` to `non-complex` or `complex`; approve the final task-specific structure with no generic planning placeholders
  - Summary: Complex: implement lifecycle surface, validate with task-specific tests, then workflow/validator integration.

### 4. Branch setup across touched repos

- Branch format: `INFRA-009-description` when the repo does not define another format.
- Create or switch branches only after research identifies the repos that will actually be touched.
  Steps:
- [x] Create or switch to a working branch in each touched repo
  - Summary: User explicitly directed local work on existing `master`; no branch was created or switched.
- [x] Update `## Context` target repo branch entries with the selected branch names
  - Summary: Context records `master` as explicitly directed.

### 5. Implement: task scratch lifecycle

- [x] Engineer-owned implementation completed
  - Summary: Added `TaskScratch.fsx` with create/register/report/promote/seal/clean and manifest-backed fail-closed ownership checks.
- [x] Engineer-owned build verdict recorded, or build explicitly not applicable
  - Summary: `dotnet fsi skills/task/scripts/TaskScratch.fsx` typechecked cleanly (usage exit 2).

### 6. Validate: task scratch lifecycle

- [x] Tester inspected existing coverage, designed and implemented required tests, and recorded the test verdict; if no tester exists, implementation-agent test ownership recorded
  - Summary: Added and passed `TaskScratchTests.fsx`; file-reparse fixture skipped only because this host cannot create file symlinks, while junction coverage ran.

### 7. Implement: closing integration and infrastructure validation

- [x] Engineer-owned implementation completed
  - Summary: Integrated closing documentation/template, canonical suite entry, validator assertions, and helper index.
- [x] Engineer-owned build verdict recorded, or build explicitly not applicable
  - Summary: `ValidateInfrastructure.fsx --self-test` passed; live validation passed after tester added the required test target.

### 8. Validate: integrated infrastructure
- [x] Tester inspected existing coverage, designed and implemented required tests, and recorded the test verdict; if no tester exists, implementation-agent test ownership recorded
  - Summary: `TaskScratchTests.fsx`, live infrastructure validation, canonical 12-step `TestInfrastructure.fsx`, and `git diff --check` all passed.

## Review

- State: FROZEN
- Implementation baseline: uncommitted INFRA-009 working tree after `TaskScratchTests.fsx` passed on 2026-08-26
- Remediation pass: 2
- Build evidence: Passed: `dotnet fsi skills/task/scripts/TaskScratch.fsx` typecheck; `dotnet fsi skills/audit/scripts/ValidateInfrastructure.fsx --self-test`; `dotnet fsi skills/audit/scripts/ValidateInfrastructure.fsx`.
- Test evidence: Passed: `dotnet fsi skills/task/scripts/TaskScratchTests.fsx`; `dotnet fsi skills/audit/scripts/TestInfrastructure.fsx` (12 steps); `git diff --check`.

After work, use `Passed: <command/result>`, `Not applicable: <reason>`, or `Waived: <Decision reference>`.

### Accepted findings

| ID | Contract | Status |
| -- | -------- | ------ |
| D001 | Physical containment below canonical OS-temp scratch base | FIXED |
| D002 | Reparse-safe automatic deletion without pathname TOCTOU | FIXED |
| D003 | Sealed roots reject post-seal mutation | FIXED |
| D004 | Ownership manifest cannot be cleanup target | FIXED |
| D005 | Manifest mutation is serialized | FIXED |
| D006 | Dot-segment run IDs are rejected | FIXED |
| D007 | Retained unknown directories and missing targets are reported | FIXED |
| S001 | Promotion is confined to a physically safe durable task destination | FIXED |

### Verification receipts

| Finding ID | Result | Evidence |
| ---------- | ------ | -------- |
| D001 | FIXED | F# targeted verification: handle-relative canonical OS-temp walk and reparse fixtures. |
| D002 | FIXED | F# targeted verification: capability-bound Windows deletion and Unix report-only clean. |
| D003 | FIXED | F# targeted verification: sealed roots reject post-seal register and promote operations. |
| D004 | FIXED | F# targeted verification: manifest registration is rejected and manifest scanning excludes it. |
| D005 | FIXED | F# targeted verification: Windows exclusive manifest access and Unix `flock` serialization. |
| D006 | FIXED | F# targeted verification: dot-segment run IDs are rejected. |
| D007 | FIXED | F# targeted verification: unknown directories and missing registered targets are retained and reported. |
| S001 | FIXED | Security verification: Windows-only helper refuses every non-Windows operation; Windows promotion uses trusted-root component-wise handle traversal. |

## Closing Steps

<!-- Re-check from C0 onward whenever new work lands after a C-step was checked
      — treat the earlier check as stale. -->

### C0. Pre-commit review board

- Run the one Discovery selected by `references/agent-gates.md` independently and in parallel on the full diff only after build and test evidence is recorded. An explicit not-applicable rationale or recorded waiver may replace either evidence item. A reviewer missing required evidence returns `BLOCKED`; do not transition from `NEW` or proceed with review.
  Steps:
- [x] Discovery reviewers' verdicts recorded after the evidence precondition
  - Summary: Combined F# Discovery plus security Discovery ran after build/test evidence; D001-D007 and S001 were accepted and frozen.
- [x] Critical/Error findings fixed or waived in `## Decisions`
  - Summary: D001-D007 fixed. User approved a Windows-only scope waiver; Unix implementation was removed and every non-Windows helper operation now refuses.
- [x] Targeted Verification receipts recorded for the frozen accepted finding set
  - Summary: F# targeted verification fixed D001, D002, and D005; final security verification fixed S001 after the Windows-only change.

### C1. Clean up temporary artifacts

Run the canonical `TaskScratch` report, explicit promotion, dependency check, seal, and clean sequence in `references/closing-steps.md`. Eligible registered disposable material in a valid sealed task scratch root is cleaned automatically; all other deletion retains explicit confirmation.

Steps:

- [x] Owned task scratch reported, explicit durable evidence promoted, and eligible disposable scratch cleaned or retained with reason
  - Summary: Agents cleaned only their owned `SMOKE-*`, `TESTS-*`, and task-scratch test fixtures; no task scratch remains required for verification.
- [x] Non-scratch temporary material handled under existing explicit-confirmation policy, or confirmed absent
  - Summary: No non-scratch temporary deletion was performed; unknown material was retained.

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
| 2026-08-26 | Windows-only support waiver | User stated current use is Windows; removing Unix support eliminates the unresolved Unix `getcwd` promotion boundary instead of retaining a weaker fallback. |
| 2026-08-26 | complete status waiver | User explicitly confirmed completion without a commit or publication; C2 remains intentionally unchecked. |
| 2026-08-26 | complete status confirmed | User explicitly selected “Mark INFRA-009 complete without a commit” after all implementation, validation, review, and cleanup evidence passed. |

<!-- Before setting Status: Complete, record a dated "complete status confirmed"
     decision. If incomplete lifecycle items are intentionally waived, record
     "complete status waiver" and its rationale in the same or a separate row. -->

## Open Questions

- None.

## Notes

- State: Complete without commit or publication by explicit user confirmation.
- Evidence: SafeFs v2 deterministic tests, infrastructure validator self/live, canonical 12-step suite, and `git diff --check` passed; final security verification approved S001.
- Next: No task work remains. Optional future work requires a new explicit commit/publish request.

<!-- Durable portable record: keep State/Evidence/Next current and leave out
     volatile snapshots and large tables. On completion they must be
     terminal-current: Next states explicitly that no task work remains, or
     lists only real manual/optional follow-up (e.g., a known dependent record
     to refresh — never mutate it). Optional Origin:/Unblocks:/Unblock condition:
     markers live in `## References`; completing this task does not establish the dependent condition. -->
