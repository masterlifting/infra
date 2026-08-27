# INFRA-008 - Align infrastructure model routing validation

**Progress: 7/7 subtasks complete** | **Status: Complete** | **Created: 2026-08-26** | **Completed: 2026-08-26**

## Brief Summary

Make infrastructure validation express the checked-in experimental mixed provider routing as capability plus approved serving channel, without altering agent routing, quality tiers, or workflow gates.

## Continuity

Work in the local configuration repository on `master`; do not commit or push. Preserve the pre-existing user modification to `opencode.json`.

## Context

- Target repo(s):
    - `./opencode` (branch: TBD; user-directed existing `master`) - OpenCode validation scripts, tests, documentation, and task record
- Task kind: code
- Implementation plan: non-complex
- Problem / opportunity: validator assumes direct DeepSeek IDs for worker roles although current frontmatter intentionally mixes direct DeepSeek and OpenCode Go.
- Constraints: preserve all current frontmatter, agent architecture, review/concurrency/latency gates, economic strategy, and explicit-only provider reassignment. Do not add automatic fallback.
- Related links/issues: user-provided INFRA-008 requirements.

## Key Files

<!-- Map important files, configs, docs, and scripts as they are discovered. -->

| Purpose               | Path                             |
| --------------------- | -------------------------------- |
| Validator and self-tests | `skills/audit/scripts/ValidateInfrastructure.fsx` |
| Canonical suite | `skills/audit/scripts/TestInfrastructure.fsx` |
| Infrastructure reference | `README.md` |
| Current configuration (read-only for this task) | `opencode.json` |

## Requirements / Acceptance Criteria

- Model capability and provider channel are validated separately; approved direct and OpenCode Go DeepSeek V4 Flash/Pro IDs are recognized.
- Current agent frontmatter, including `small_model = opencode-go/deepseek-v4-flash`, passes live validation without modification.
- Self-tests cover both approved DeepSeek channels, tier rejection, Grok/DeepSeek role rejection, no paid fallback, and live matrix validity.
- Preserve exact OpenAI identities, direct xAI Grok 4.5 diversity roles, all workflow/latency gates, and no-automatic-fallback invariant.

## Solution Contract

- State: FROZEN
- Requirements: validate model quality capability independently from explicitly approved serving provider channel.
- Acceptance criteria: required deterministic self-tests and live/canonical validation pass; no agent frontmatter, routing, latency, or credential changes.
- Accepted assumptions: the user-provided requirements are the canonical specification because no prior INFRA-008 task record existed.
- Non-goals: model/provider migrations, model profiles, automatic fallback, economic/billing documentation, and workflow architecture changes.
- Chosen solution: define canonical capabilities and their allowed concrete IDs; express role assignments and language consistency by capability plus variant, preserving exact provider expectations only where intentionally architectural; document the invariant concisely.
- Important boundaries/contracts: OpenAI identities remain exact; Grok 4.5 is currently direct xAI only; DeepSeek V4 Flash/Pro permit only listed direct or OpenCode Go channels; provider exhaustion returns control.
- Implementation constraints: retain `small_model`; do not alter `opencode.json`; retain all routing and parallel-wave definitions; do not introduce additional LLM calls.
- Review profile: contract
- Rejected alternatives: provider-prefix validation, literal provider equality for language teams, and automatic paid-provider fallback.

## Non-Goals

- Changing any checked-in agent model/provider frontmatter.
- Changing `opencode.json`, automatic fallback policy, task orchestration, latency architecture, or provider-spend strategy.

## References

<!-- Keep links to external docs, analysis notes, tickets, and generated artifacts here. -->

- Ticket/spec: user-provided INFRA-008 request.
- Analysis: auditor and explorer findings held in the session; no behavioral specification is required for this infrastructure-only contract.

## Subtasks

### 1. Research and define approach

Steps:

- [x] Investigate relevant code paths and document findings
  - Summary: `ValidateInfrastructure.fsx` has stale direct-DeepSeek prefix and literal-model team checks; current frontmatter mixes direct DeepSeek and OpenCode Go.
- [x] Define expected behavior and constraints
  - Summary: capability/tier is the quality contract; channel is an explicitly approved operational choice; no automatic fallback or routing changes.
- [x] Draft task-specific delivery and validation work; classify implementation
  - Summary: non-complex, one F# validator/documentation slice with deterministic self-tests.

### 2. Clarify gaps before implementation

Classify gaps under `references/clarification.md`: ask only `BLOCKING`
questions, record meaningful assumptions, resolve `NON-BLOCKING` gaps without
interrupting, and block only unresolved `BLOCKING` gaps.

Steps:

- [x] Classify research gaps as BLOCKING, ASSUMPTION, or NON-BLOCKING
  - Summary: no blocking gaps; absence of the initial task record is resolved by this user-provided specification.
- [x] Ask only BLOCKING questions and record answers or meaningful assumptions
  - Summary: no questions required.
- [x] Resolve NON-BLOCKING gaps in the task plan without interruption
  - Summary: actual validator and test paths are under `skills/audit/scripts/`.
- [x] Mark only unresolved BLOCKING gaps
  - Summary: not applicable.

### 3. Design gate

Blocks implementation subtasks until the gate is clean or explicitly waived
(see `references/agent-gates.md`). Architect routing follows that gate:
complex or architecture-sensitive tasks use two isolated read-only architect
proposals; non-complex tasks default to coordinator design, with at most one
appropriate architect for concrete unresolved uncertainty.

Steps:

- [x] Architecture routed per `references/agent-gates.md`; independent proposals used only when that gate requires them, then coordinator solution frozen
  - Summary: non-complex prescriptive validator refactor; no unresolved architecture decision.
- [x] Conditional specialists run per `references/agent-gates.md` or explicitly N/A
  - Summary: DevOps/database/security/performance specialists are not applicable; audit was completed read-only.
- [x] Set `Implementation plan` to `non-complex` or `complex`; approve the final task-specific structure with no generic planning placeholders
  - Summary: implement capabilities and tests, document invariant, run required validation, then contract Discovery.

### 4. Branch setup across touched repos

Steps:
- [x] Confirm approved local branch state
  - Summary: user explicitly directed work on existing `master`; no branch creation or switching performed.

### 5. Implement and validate

Steps:

- [x] Engineer-owned implementation completed
  - Summary: capability/channel assignments, role contracts, team consistency, and deterministic self-tests are coherent; no frontmatter or config changes were made.
- [x] Document provider/channel invariant
  - Summary: README distinguishes agent-quality capability from explicit operational serving channel and prohibits automatic provider fallback.
- [x] Engineer-owned build verdict recorded, or build explicitly not applicable
  - Summary: Passed: `dotnet fsi skills/audit/scripts/ValidateInfrastructure.fsx --self-test` → `OK infrastructure validator self-test`.
- [x] Tester inspected existing coverage, designed and implemented required tests, and recorded the test verdict; if no tester exists, implementation-agent test ownership recorded
  - Summary: Passed: self-test, live validation, canonical `TestInfrastructure.fsx` (11 steps), and `git diff --check`; no test-file changes required.

## Review

- State: FROZEN
- Implementation baseline: `HEAD` `b92ddab` plus INFRA-008 changes to `README.md` and `skills/audit/scripts/ValidateInfrastructure.fsx`; excluded pre-existing `opencode.json` modification.
- Remediation pass: 0
- Build evidence: Passed: `dotnet fsi skills/audit/scripts/ValidateInfrastructure.fsx --self-test` → `OK infrastructure validator self-test`.
- Test evidence: Passed: `dotnet fsi skills/audit/scripts/ValidateInfrastructure.fsx`; `dotnet fsi skills/audit/scripts/TestInfrastructure.fsx` → 11 steps; `git diff --check`.

After work, use `Passed: <command/result>`, `Not applicable: <reason>`, or `Waived: <Decision reference>`.

### Accepted findings

| ID | Contract | Status |
| -- | -------- | ------ |

### Verification receipts

| Finding ID | Result | Evidence |
| ---------- | ------ | -------- |
| None | APPROVE | Both independent Discovery reviewers returned PASS with no Critical/Error findings or accepted remediation set. |

## Closing Steps

<!-- Re-check from C0 onward whenever new work lands after a C-step was checked
      — treat the earlier check as stale. -->

### C0. Pre-commit review board

- Run the one Discovery selected by `references/agent-gates.md` independently and in parallel on the full diff only after build and test evidence is recorded. An explicit not-applicable rationale or recorded waiver may replace either evidence item. A reviewer missing required evidence returns `BLOCKED`; do not transition from `NEW` or proceed with review.
  Steps:
- [x] Discovery reviewers' verdicts recorded after the evidence precondition
  - Summary: F# reviewer PASS; F# validator PASS. Both reviewed the frozen contract and implementation baseline independently after required evidence.
- [x] Critical/Error findings fixed or waived in `## Decisions`
  - Summary: no Critical/Error findings; no remediation required.
- [x] Targeted Verification receipts recorded for the frozen accepted finding set
  - Summary: not applicable; Discovery accepted no remediation findings. Info-only defensive notes are deferred outside the frozen scope.

### C1. Clean up temporary artifacts

Before committing, remove only task-created scratchpad or temp files. See `references/closing-steps.md` for the keep/remove list. Every removal requires explicit confirmation.

Steps:

- [x] Scratchpad / temp working files created for this task removed
  - Summary: no task-created scratchpad or temporary files exist; no deletion was performed.

## Decisions

| Date | Decision | Rationale |
| ---- | -------- | --------- |
| 2026-08-26 | Freeze capability/channel validation contract | User explicitly prescribed the current experimental matrix; no provider migration or routing redesign is authorized. |
| 2026-08-26 | Defer Info-level defensive test additions | Independent contract Discovery found no acceptance blocker; additional negative enforcement-loop tests and README marker pinning are outside the frozen bounded scope. |
| 2026-08-26 | Complete status confirmed | User explicitly confirmed task completion after all frozen implementation, validation, and Discovery work passed. |

<!-- Before setting Status: Complete, record a dated "complete status confirmed"
     decision. If incomplete lifecycle items are intentionally waived, record
     "complete status waiver" and its rationale in the same or a separate row. -->

## Open Questions

- None.

## Notes

- State: Complete.
- Evidence: self-test and live validator passed; canonical `TestInfrastructure.fsx` passed all 11 steps; `git diff --check` passed; F# reviewer and validator independently returned PASS.
- Next: no task work remains; separately await explicit confirmation of the proposed commit message before committing.

<!-- Durable portable record: keep State/Evidence/Next current and leave out
     volatile snapshots and large tables. On completion they must be
     terminal-current: Next states explicitly that no task work remains, or
     lists only real manual/optional follow-up (e.g., a known dependent record
     to refresh — never mutate it). Optional Origin:/Unblocks:/Unblock condition:
     markers live in `## References`; completing this task does not establish the dependent condition. -->
