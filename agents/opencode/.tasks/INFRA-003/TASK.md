# INFRA-003 - Trial Grok and Mistral model allocation

**Progress: 6/6 subtasks complete** | **Status: Complete** | **Created: 2026-08-24** | **Completed: 2026-08-24**

## Brief Summary

Replace active DeepSeek and Kimi role assignments with the specified xAI Grok and Mistral trial matrix. Keep all existing agent topology, workflow semantics, permissions, and role descriptions intact; enable portable quota notifications for the active providers.

## Continuity

Revalidate installed OpenCode model metadata before changing model identifiers. Work only on the user-approved existing `master` branch; no commit or publication is requested.

## Context

- Target repo(s):
    - `./opencode` - global OpenCode infrastructure source mirror; user approved the existing `master` branch.
- Task kind: code
- Implementation plan: non-complex
- Problem / opportunity: The completed DeepSeek/Kimi experiment must be replaced with an experimentally bounded Grok/Mistral allocation and supported quota configuration.
- Constraints: Preserve architecture, routing, topology, permissions, role text, confirmation gates, and portability; no credentials, aliases, fallbacks, or workflow redesign.
- Related links/issues: User objective, 2026-08-24.

## Key Files

| Purpose | Path |
| --- | --- |
| Global defaults and permissions | `opencode.json` |
| File-based agent assignments | `agents/**/*.md` |
| Quota notification configuration | `opencode-quota/quota-toast.json` |
| Infrastructure validation | `skills/audit-infra/scripts/ValidateInfrastructure.fsx` |

## Requirements / Acceptance Criteria

- Active C#, F#, and Rust roles consistently use the specified OpenAI, Grok, Mistral Medium, and Mistral Small matrix.
- No active DeepSeek or Kimi/Moonshot assignment remains; global `small_model` is Mistral Small. Quota tracking retains its pre-existing DeepSeek/Moonshot providers and adds xAI/Mistral.
- Provider settings and quota configuration are portable and secret-free; permissions remain unchanged.
- Confirmed installed model identifiers and provider-supported reasoning variants are used.
- Deterministic validation, targeted model smoke checks, and `git diff --check` pass.

## Solution Contract

- State: FROZEN
- Requirements: Satisfy the user-specified allocation and quota support using only confirmed local OpenCode identifiers.
- Acceptance criteria: All requirements above pass with no topology or orchestration-semantic change.
- Accepted assumptions: `xai/grok-4.6` supports `high`; `mistral/mistral-medium-2604` and `mistral/mistral-small-2603` support `none` and `high`, per `opencode models <provider> --verbose --pure` on OpenCode 1.18.22. Quota support retains `openai`, `deepseek`, and `moonshotai`, adds `xai` and `mistral`, and enables notifications.
- Non-goals: Credential storage, provider authentication, path-portability cleanup, role-text cleanup, workflow redesign, model routing, fallback mapping, and commits.
- Chosen solution: Set the global small model and named file-agent frontmatter to the confirmed allocation; retain provider authentication outside the repository; extend the existing quota provider list and enable toast notifications.
- Important boundaries/contracts: File-based agent definitions remain the sole source of per-agent configuration. Existing OpenAI high-leverage roles remain unchanged. Reasoning effort is role-specific.
- Implementation constraints: No secret reads or values; preserve `opencode.json` permissions verbatim; do not add provider configuration without an official portable need.
- Review profile: Standard
- Rejected alternatives: Substituting unverified identifiers, retaining legacy provider aliases, and changing task architecture.

## Non-Goals

- Any workflow, reviewer-mandate, or specialist-routing change.
- Credentials, provider-router configuration, or unrelated portability remediation.

## References

- Ticket/spec: User objective, 2026-08-24.
- Analysis docs: Read-only audit session `ses_fcb6e2ee0ffeFvJEmD1AgAniJk`; installed catalog evidence via `opencode models xai|mistral --verbose --pure`.
- Behavioral specification: (optional) .tasks/INFRA-003/SPEC.md

## Subtasks

### 1. Research and define approach

- [x] Investigate relevant code paths and document findings
  - Summary: Audited active frontmatter, global configuration, quota configuration, and deterministic validator.
- [x] Define expected behavior and constraints
  - Summary: Confirmed the exact requested allocation and that provider authentication remains external to this repository.
- [x] Draft task-specific delivery and validation work; for code tasks, classify implementation as complex or non-complex
  - Summary: Non-complex: one bounded configuration allocation with targeted validation.

### 2. Clarify gaps before implementation

- [x] Classify research gaps as BLOCKING, ASSUMPTION, or NON-BLOCKING
  - Summary: No blocking gap; verified local identifiers and user defined quota support.
- [x] Ask only BLOCKING questions and record answers or meaningful assumptions in `## Decisions` or `## Open Questions`
  - Summary: The user explicitly approved task ID INFRA-003 and the existing `master` branch.
- [x] Resolve NON-BLOCKING gaps in the task plan without interruption
  - Summary: Provider authentication is intentionally external; no repository provider block is needed.
- [x] Mark only unresolved BLOCKING gaps with `[blocked]` notation and set `Status: Blocked` while waiting
  - Summary: No unresolved blocking gaps.

### 3. Design gate

- [x] Architecture routed per `references/agent-gates.md`; independent proposals used only when that gate requires them, then coordinator solution frozen
  - Summary: Non-complex operational allocation; coordinator froze the direct user-specified matrix after read-only infrastructure audit.
- [x] Conditional specialists run per `references/agent-gates.md` or explicitly N/A
  - Summary: N/A: no database, deployment, security, or performance surface changes.
- [x] Set `Implementation plan` to `non-complex` or `complex`; approve the final task-specific structure with no generic planning placeholders
  - Summary: Non-complex; one implementation and validation cycle.

### 5. Implement and validate

- [x] Update active model allocation and quota configuration
  - Summary: Applied the confirmed role matrix. Quota retains the existing providers and adds `xai` and `mistral`; toasts are enabled.
- [x] Engineer-owned implementation completed
  - Summary: The executor served as the no-language-dedicated implementation owner and changed only active agent frontmatter, global `small_model`, and quota-toast configuration; all agent prose, topology, and permissions remained unchanged.
- [x] Engineer-owned build verdict recorded, or build explicitly not applicable
  - Summary: Configuration validation passed: `ValidateInfrastructure.fsx`, both JSON parses, active-agent stale-provider sweep, and `git diff --check`.
- [x] Tester inspected existing coverage, designed and implemented required tests, and recorded the test verdict; if no tester exists, implementation-agent test ownership recorded
  - Summary: No language-test surface applies, so the executor-owned existing canonical infrastructure test suite was run and passed all 11 steps: `dotnet fsi "skills/audit-infra/scripts/TestInfrastructure.fsx"`.

## Review

- State: FROZEN
- Implementation baseline: INFRA-003 allocation/configuration diff after retained-provider quota correction.
- Remediation pass: 1
- Build evidence: Passed: `dotnet fsi "skills/audit-infra/scripts/ValidateInfrastructure.fsx"` → `OK infrastructure validation (0 warning(s))`.
- Test evidence: Passed: `dotnet fsi "skills/audit-infra/scripts/TestInfrastructure.fsx"` → `OK infrastructure test suite (11 steps)`.

### Accepted findings

| ID | Contract | Status |
| -- | -------- | ------ |
| INFRA-003-V1 | Active agent allocations match the frozen matrix; DeepSeek/Kimi/Moonshot are absent from agent definitions. | FIXED |
| INFRA-003-V2 | Quota tracking retains legacy providers, adds xAI/Mistral, and enables toast notifications. | FIXED |

### Verification receipts

| Finding ID | Result | Evidence |
| ---------- | ------ | -------- |
| INFRA-003-V1 | FIXED | Targeted `audit-infra` verification: all agent definitions clean; validator and stale-provider sweep passed. |
| INFRA-003-V2 | FIXED | Targeted `audit-infra` verification after correction: quota JSON retains `openai`, `deepseek`, `moonshotai` and adds `xai`, `mistral`; JSON parse passed. |

## Closing Steps

### C0. Pre-commit review board

- [x] Discovery reviewers' verdicts recorded after the evidence precondition
  - Summary: Read-only `audit-infra` discovery found the allocation correct; final targeted verification initially identified stale task bookkeeping only.
- [x] Critical/Error findings fixed or waived in `## Decisions`
  - Summary: Reconciled the quota contract with the user's explicit retained-provider correction; no infrastructure defect remained.
- [x] Targeted Verification receipts recorded for the frozen accepted finding set
  - Summary: Final `audit-infra` verification confirmed both allocation and retained-provider quota contracts as fixed.

### C1. Clean up temporary artifacts

- [x] Scratchpad / temp working files created for this task removed
  - Summary: No task-created temporary artifacts were produced.

## Decisions

| Date | Decision | Rationale |
| ---- | -------- | --------- |
| 2026-08-24 | Use the existing `master` branch. | Explicit user confirmation. |
| 2026-08-24 | Freeze the confirmed local IDs and variants. | Installed OpenCode catalog verifies them; no substitution is needed. |
| 2026-08-24 | Retain legacy quota providers while adding xAI and Mistral. | Explicit user correction; quota tracking is not active role allocation. |
| 2026-08-24 | Complete status confirmed. | Implementation, deterministic validation, canonical infrastructure tests, and targeted verification passed. |

## Open Questions

- None.

## Notes

- State: Complete.
- Evidence: Local OpenCode 1.18.22 catalog confirms requested IDs and variants; infrastructure validator and 11-step test suite passed; targeted audit verification passed.
- Next: No task work remains. Optional manual follow-up: decide whether ignored local `opencode-quota/` configuration should be versioned for cross-machine portability.
