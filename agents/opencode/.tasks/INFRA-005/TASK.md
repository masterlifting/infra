# INFRA-005 - Normalize OpenCode agent and skill names

**Progress: 8/9 subtasks complete** | **Status: In Progress** | **Created: 2026-08-25**

## Brief Summary

Normalize OpenCode agent and skill identifiers to concise semantic names, complete the planned routing/model refactor, and migrate all authored references without compatibility aliases. Preserve domains, permissions, validation, and role ownership.

## Context

- Target repo(s):
    - `./opencode` - local mirror of `masterlifting/infra`; user directed the current `master` branch for this task.
- Task kind: code
- Implementation plan: complex
- Constraints: no compatibility aliases or old identifiers; do not broaden permissions; model IDs must be verified before assignment; no commit, push, or remote backup without explicit confirmation.

## Key Files

| Purpose | Path |
| ------- | ---- |
| Agent definitions | `agents/**/*.md` |
| Skill procedures | `skills/**` |
| Routing configuration | `opencode.json`, `AGENTS.md` |
| Task gates | `skills/task/references/agent-gates.md` |
| Infrastructure validation | `skills/audit/scripts/ValidateInfrastructure.fsx` |

## Requirements / Acceptance Criteria

- Rename the specified global, language, database, and skill identifiers; retain only the new names.
- Preserve agent roles, permissions, domain paths, workflow ownership, and C#/F#/Rust symmetry.
- Apply the stated model-routing assignments using verified installed model IDs.
- Migrate every authored old-identifier reference and update naming documentation.
- Record successful validator/static/behavioral checks, clean diff whitespace, new-name discoverability, absence of old references, unchanged credential/auth files, and no model/permission drift.

## Solution Contract

- State: FROZEN
- Requirements: Naming and routing requirements supplied by the user in this task request.
- Acceptance criteria: All specified old identifiers are removed from authored infrastructure; new semantic identifiers resolve; validation passes.
- Accepted assumptions: Existing uncommitted refactor files are user work and must be preserved while the task is moved to `dev`.
- Non-goals: Commit, push, remote backup, direct xAI production routing, or any unrelated infra redesign.
- Chosen solution: Retain the committed semantic language/database/global-agent baseline; atomically rename the remaining global agents/skills and their authored references, change the C# challenger route to `opencode-go/grok-4.5` as an uncommitted configuration change, do not reload OpenCode during the task, then validate.
- Important boundaries/contracts: `auditor` is the read-only infra auditor using `audit`; `guardian` is frozen-design conformity only; `validator` does not rerun tests; no permission broadening.
- Implementation constraints: Update agent frontmatter rather than inline config where an agent file owns assignment; preserve confirmation gates.
- Review profile: combined
- Rejected alternatives: Legacy aliases, forwarding wrappers, flattened domain paths, and direct xAI API routing.

### Frozen Resulting Vocabulary

The resulting global vocabulary is `auditor`, `vision`, `audit`, and `documents`; language teams use `architect`, `challenger`, `engineer`, `tester`, `reviewer`, `guardian`, and `validator`; database uses `engineer` and `reviewer`. Do not restart/reload OpenCode during this task; the uncommitted route change leaves the running session unchanged.

### Frozen Model Routing

| Agent role | Model / variant | Channel |
| --- | --- | --- |
| `build`, `vision` | GPT-5.6 Terra / medium | ChatGPT Plus |
| `auditor` | GPT-5.6 Luna / medium | ChatGPT Plus |
| `explorer` | DeepSeek V4 Flash / medium | DeepSeek API |
| `executor`, language `tester`, language `validator` | DeepSeek V4 Flash / high | DeepSeek API |
| language `architect` | GPT-5.6 Sol / high | ChatGPT Plus |
| language `challenger`, language `guardian` | Grok 4.5 / high | OpenCode Go |
| language `engineer`, database/devops `engineer` | DeepSeek V4 Pro / high | DeepSeek API |
| language/database/devops `reviewer` | GPT-5.6 Luna / high | ChatGPT Plus |
| security/performance `reviewer` | GPT-5.6 Terra / high | ChatGPT Plus |

## Non-Goals

- Commit, publish, or modify the remote mirror.
- Read credentials, auth, token, or session files.

## References

- User request: session request dated 2026-08-25.
- Audit procedure: `skills/audit/SKILL.md`.
- Canonical task gates: `skills/task/references/agent-gates.md`.

## Subtasks

### 1. Research and define approach

- [x] Inventory current agent/skill definitions, references, validation surfaces, model IDs, and pre-existing changes.
  - Summary: Baseline `aa2ba93` completed language/database/global vision renames; audit identified remaining global agent/skill paths and C# challenger route.
- [x] Freeze the exact old-to-new migration map and validation matrix after audit findings.
  - Summary: Migration map and model-routing table are frozen in the Solution Contract.

### 2. Clarify gaps before implementation

- [x] Classify research gaps; record assumptions and ask only blocking questions.
  - Summary: User confirmed `auditor` is the agent, `audit` is the skill, and target model routing is authoritative; no open blocking gap remains.

### 3. Design gate

- [x] Architecture routed per `references/agent-gates.md`; independent proposals used only when that gate requires them, then coordinator solution frozen
  - Summary: N/A for non-language OpenCode infrastructure; read-only `audit` completed instead.
- [x] Conditional specialists run per `references/agent-gates.md` or explicitly N/A
  - Summary: N/A; no database, DevOps, or application security surface is changed.
- [x] Set `Implementation plan` to `non-complex` or `complex`; approve the final task-specific structure with no generic planning placeholders
  - Summary: Complex atomic infrastructure migration; map and batches frozen above.

### 4. Branch setup

- [x] Continue on `master` as explicitly directed by the user after the baseline commit.
  - Summary: `dev` was not switched to; `master` is the approved task branch.

### 5. Implement: semantic migration

- [x] Rename agent and skill paths, update semantic role/routing prose, model assignments, and naming documentation.
  - Summary: Completed by executor; `auditor`, `audit`, and `documents` replace the remaining long names; language tester/validator routes corrected after Discovery.
- [x] Migrate all authored old-name references atomically; remove old paths and compatibility forms.
  - Summary: Completed by executor; old paths are removed and validators retain only intentional rejection fixtures.
- [x] Engineer-owned implementation completed.
  - Summary: Executor completed the frozen semantic migration and P1 model-routing remediation.
- [x] Engineer-owned build verdict recorded.
  - Summary: Executor recorded passing validator self-test and live validator after implementation and remediation.

### 6. Validate: migration invariants

- [x] Tester inspected existing coverage, designed and implemented required tests, and recorded the test verdict; if no tester exists, implementation-agent test ownership recorded.
  - Summary: Executor ran the 11-step infrastructure test suite successfully; behavioral scenarios were not required by the audit procedure.
- [x] Verify validators, new agent/skill discovery, no old authored identifiers, structural symmetry, permission invariance, model routing, credential/auth file invariance, and `git diff --check`.
  - Summary: Validator/self-test and 11-step suite passed after final model alignment; JSON parsed; diff is clean; targeted scans found only intentional validator fixtures; all agent groups conform to the frozen routes.

## Review

- State: FROZEN
- Implementation baseline: Uncommitted `master` diff after `aa2ba93`.
- Remediation pass: 2
- Build evidence: Passed: `dotnet fsi "skills/audit/scripts/ValidateInfrastructure.fsx" --self-test`; `dotnet fsi "skills/audit/scripts/ValidateInfrastructure.fsx"`.
- Test evidence: Passed: `dotnet fsi "skills/audit/scripts/TestInfrastructure.fsx"` (11 steps).

### Accepted findings

| ID | Contract | Status |
| -- | -------- | ------ |
| P1 | Language tester/validator routes must match the frozen model-routing table. | FIXED |
| P2 | No stale identifiers may remain in authored task metadata. | FIXED |
| P3 | Task review evidence and state must be recorded. | FIXED |
| P4 | All agent frontmatter routes must match the frozen model-routing table. | FIXED |

### Verification receipts

| Finding ID | Result | Evidence |
| ---------- | ------ | -------- |
| P1 | FIXED | Targeted audit verification confirmed all six tester/validator frontmatters and validator enforcement. |
| P2 | FIXED | Targeted audit verification confirmed task metadata contains only resulting vocabulary. |
| P3 | FIXED | Targeted audit verification confirmed review state and build/test evidence are recorded. |
| P4 | FIXED | Targeted audit verification confirmed all 32 agent frontmatters and global defaults match the frozen routes. |

## Closing Steps

### C0. Pre-commit review board

- [x] Discovery reviewers' verdicts recorded after implementation evidence.
  - Summary: Independent audit Discovery accepted P1–P3; P1/P2/P3 were remediated without a new Discovery.
- [x] Critical/Error findings fixed or explicitly waived.
  - Summary: All accepted findings are FIXED; no waiver is used.
- [x] Targeted verification receipts recorded for accepted findings.
  - Summary: Independent targeted Verification recorded FIXED for P1–P3.

### C1. Clean up temporary artifacts

- [x] No task-created scratch artifacts exist in the repository; no removal was needed.
  - Summary: Agent/tool output is system-managed outside the repository; no in-repo scratch file was created.

### C2. Commit and publish

- [x] Commit the completed migration after the user explicitly approved the exact commit message.
  - Summary: Committed `1cd1a9f` on `master`.
- [ ] Push or back up to the remote mirror after separate explicit user approval.
  - Summary: Blocked: this local repository has no configured push destination; no retry was attempted.

## Decisions

| Date | Decision | Rationale |
| ---- | -------- | --------- |
| 2026-08-25 | Create INFRA-005 before changing infrastructure. | Required task workflow and durable migration/validation record. |
| 2026-08-25 | Continue INFRA-005 on `master`, not `dev`. | User directed that work occur on the current `master` branch after the baseline commit. |
| 2026-08-25 | Correct global explorer/executor models after task review froze. | The user explicitly requested alignment with the frozen routing table; targeted Verification passed without reopening Discovery. |

## Open Questions

- Whether the existing uncommitted migration is cleanly applicable to `dev`; resolve before switching.

## Notes

- State: All implementation, validation, Discovery, and targeted Verification work is frozen.
- Evidence: Infrastructure validator/self-test and 11-step suite pass; audit verified all 32 agent routes against the frozen table.
- Next: Obtain the configured remote name/URL for `masterlifting/infra`, then explicitly confirm the exact push destination.
