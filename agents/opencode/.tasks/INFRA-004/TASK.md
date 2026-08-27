# INFRA-004 - Normalize OpenCode agent architecture and routing

**Progress: 7/9 subtasks complete** | **Status: In Progress** | **Created: 2026-08-25**

## Brief Summary

Normalize the file-based OpenCode agent topology, rename role IDs, remove obsolete model experiments, and establish the requested three-channel production routing. Preserve specialist responsibilities and proportional, evidence-driven orchestration.

## Continuity

Preserve pre-existing uncommitted edits as baseline; do not overwrite them. Do not read credential/auth stores or commit/push without the applicable explicit confirmation. Work in the user-approved existing `master` branch.

## Context

- Target repo(s):
    - `./opencode` - global OpenCode infrastructure source mirror; user approved the existing `master` branch.
- Task kind: code
- Implementation plan: complex
- Problem / opportunity: Replace obsolete experimental model routing and numbered agent IDs with a simple, cost-controlled production architecture.
- Constraints: Preserve useful specializations, isolated architecture proposals, separate Discovery/Verification, smallest-sufficient routing, permissions, MCP, shell, secret protection, and confirmation behavior. Agent frontmatter is the assignment source of truth; no compatibility aliases or automatic paid fallback.
- Related links/issues: User objective, 2026-08-25.

## Key Files

| Purpose | Path |
| --- | --- |
| Global instructions and naming | `AGENTS.md` |
| Global defaults | `opencode.json` |
| File-based agents | `agents/**/*.md` |
| Durable workflow and gates | `skills/task/**` |
| Infrastructure audit and validation | `skills/audit-infra/**` |
| Software orchestration rules | `rules/software/**` |
| Infrastructure scripts and tests | `scripts/**`, relevant test fixtures |

## Requirements / Acceptance Criteria

- Global, language, database, and retained specialist agent IDs match the requested simple naming map with no aliases or stale authored references.
- Agent frontmatter and global defaults use only verified installed requested OpenAI, direct DeepSeek, and OpenCode Go/Grok model IDs and variants; no silent paid-provider fallback is configured.
- Obsolete China/West profile artifacts and stale production-routing references are removed without touching credentials or unrelated operational configuration.
- Canonical gates and role contracts implement proportional architecture/review routing, engineer/tester ownership, specialist composition, and bounded Discovery/Verification.
- Documentation, validation scripts, static/behavioral fixtures, and naming guidance agree with the final topology.
- Deterministic validator, relevant tests, stale-reference scans, model-registry checks, team-consistency checks, discovery checks, and `git diff --check` pass; no credential/auth files change.

## Solution Contract

- State: FROZEN
- Requirements: Implement the user-specified agent rename map, model matrix, cost controls, semantic gates, ownership contracts, documentation, and deterministic validation.
- Acceptance criteria: All `Requirements / Acceptance Criteria` pass; agent frontmatter is the sole model-assignment source; no old IDs/profiles/routes or credential/auth changes remain.
- Accepted assumptions: Existing uncommitted changes are intentional baseline material and may be incorporated but not overwritten. `master` is the explicitly approved working branch. Confirmed installed IDs are `openai/gpt-5.6-{terra,luna,sol}`, `deepseek/deepseek-v4-{flash,pro}`, and `opencode-go/grok-4.5`.
- Non-goals: Credential/auth changes, compatibility aliases, profile tables, automatic paid-provider fallback, unrelated operational configuration, commit, and push.
- Chosen solution: Rename files directly to the semantic IDs; assign requested models in frontmatter and `small_model`; remove both obsolete profiles; centralize engineer constraints in the shared handoff rule; replace numbered/fixed review routing with semantic, risk-based routing; extend the existing infrastructure validator and tests to enforce the resulting topology and routing rules.
- Important boundaries/contracts: `build` remains coordinator; `task` remains durable workflow; language teams remain separate; architecture proposals remain isolated; engineers/testers/reviewers retain their distinct ownership; Discovery and Verification remain bounded state transitions; specialist selection is evidence-based.
- Implementation constraints: Retain domain directories; do not read credential/auth stores; preserve unrelated permission/MCP/shell/secret-protection/confirmation settings; do not silently overwrite prior baseline edits; use direct DeepSeek for normal workers, Go only for Grok diversity, and return provider failures to the coordinator/user.
- Review profile: combined
- Rejected alternatives: Retaining numbered aliases, creating a synchronized model profile table, reducing role specialization, direct xAI routing, routing ordinary DeepSeek work through Go, automatic paid fallback, and reopening architecture for merely different valid designs.

## Non-Goals

- Reading or changing credential/auth stores.
- Commit, push, or other remote write without a separate explicit confirmation.
- Altering unrelated permissions, MCP, shell, secret-protection, or confirmation configuration.

## References

- Ticket/spec: User objective, 2026-08-25.
- Behavioral specification: `.tasks/INFRA-004/SPEC.md`
- Analysis docs: Audit report to be recorded after Discovery.

## Subtasks

### 1. Research and define approach

Steps:

- [x] Investigate relevant code paths and document findings
  - Summary: Read-only audit covered global configuration, agent definitions, gates, validator/tests, profile artifacts, and existing baseline edits; exact installed requested IDs were verified with `opencode models`.
- [x] Define expected behavior and constraints
  - Summary: User requirements define semantic IDs, three provider channels, direct-DeepSeek worker routing, no silent paid fallback, and bounded risk-based orchestration; credentials and unrelated controls remain out of scope.
- [x] Draft task-specific delivery and validation work; for code tasks, classify implementation as complex or non-complex
  - Summary: Complex, architecture-sensitive infrastructure change: agent topology/model slice, routing/contract documentation slice, and F# validator/test slice followed by risk-based Discovery.

### 2. Clarify gaps before implementation

Steps:

- [x] Classify research gaps as BLOCKING, ASSUMPTION, or NON-BLOCKING
  - Summary: No product-design gap remains: the user explicitly specified the complete naming map, model matrix, profile removal, routing semantics, validation scenarios, and working branch.
- [x] Ask only BLOCKING questions and record answers or meaningful assumptions in `## Decisions` or `## Open Questions`
  - Summary: User confirmed task ID `INFRA-004`, preservation of the current baseline, and the existing `master` branch as the working branch; no branch switch or creation is required.
- [x] Resolve NON-BLOCKING gaps in the task plan without interruption
  - Summary: Installed model registry verified all requested IDs; agent frontmatter is authoritative and obsolete profile artifacts will be deleted rather than replaced.
- [x] Mark only unresolved BLOCKING gaps with `[blocked]` notation and set `Status: Blocked` while waiting
  - Summary: No unresolved blocking gap.

### 3. Design gate

Blocks implementation subtasks until the gate is clean or explicitly waived (see `references/agent-gates.md`). Architect routing follows that gate: complex or architecture-sensitive tasks use two isolated read-only architect proposals; non-complex tasks default to coordinator design, with at most one appropriate architect for concrete unresolved uncertainty.

Steps:

- [x] Architecture routed per `references/agent-gates.md`; independent proposals used only when that gate requires them, then coordinator solution frozen
  - Summary: Two isolated read-only F# architecture proposals were obtained against the same task artifacts. Build synthesized only the user-required semantic topology and rejected proposal deviations that retained legacy names.
- [x] Conditional specialists run per `references/agent-gates.md` or explicitly N/A
  - Summary: Infrastructure audit completed read-only. Database, DevOps, security, and performance specialists are N/A for the planned configuration/routing surface; security/performance review remains available only if the diff provides concrete evidence.
- [x] Set `Implementation plan` to `non-complex` or `complex`; approve the final task-specific structure with no generic planning placeholders
  - Summary: Complex; approved topology/routing, contracts/docs, and validator/test slices. The Solution Contract is frozen and implementation may proceed only within it.

### 4. Branch setup across touched repos

- [x] Create or switch to a working branch in each touched repo
  - Summary: No branch action performed: user explicitly approved making the changes in the already-checked-out `master` branch.
- [x] Update `## Context` target repo branch entries with the selected branch names
  - Summary: Context records the approved existing `master` branch without a new branch-name declaration.

### 5. Implement: agent topology and production routing

Steps:

- [x] Rename agent files and update their frontmatter assignments and role contracts
  - Summary: Renamed global, database, and language-team agent files to the requested semantic IDs and assigned the verified final model/variant matrix in frontmatter.
- [x] Remove obsolete profile artifacts and stale routing references
  - Summary: Removed `model-profiles/china.md` and `model-profiles/west.md`; renamed all requested agents and updated authored infrastructure routes, documentation, contracts, validator, and behavioral references with no compatibility aliases.
- [x] Engineer-owned implementation completed
  - Summary: F# engineer completed the frozen topology/model, semantic gate/contract, and validator slices; no credentials/auth, commits, or pushes were touched.
- [x] Engineer-owned build verdict recorded, or build explicitly not applicable
  - Summary: Passed: `dotnet build` (engineer result after completing the validator work).

### 6. Validate: infrastructure behavior and static consistency

Steps:

- [x] Update and execute relevant static/behavioral tests and routing scenarios
  - Summary: Passed: validator self-test, live validation, and all 11 infrastructure-suite steps. Static scenarios cover routine, contract-sensitive, architecture-sensitive, combined-risk, database-only, application+database, DevOps-only, security-sensitive, and executor fallback routing.
- [x] Tester inspected existing coverage, designed and implemented required tests, and recorded the test verdict; if no tester exists, implementation-agent test ownership recorded
  - Summary: Passed: `dotnet fsi skills/audit-infra/scripts/ValidateInfrastructure.fsx --self-test`; `dotnet fsi skills/audit-infra/scripts/ValidateInfrastructure.fsx`; `dotnet fsi skills/audit-infra/scripts/TestInfrastructure.fsx` → `OK infrastructure test suite (11 steps)`. No additional test files were required.

## Review

- State: FROZEN
- Implementation baseline: `c10d5f8` + current uncommitted implementation working tree, frozen for Discovery on 2026-08-25.
- Remediation pass: 1
- Build evidence: Not applicable: this infrastructure repository contains no `.sln`, `.csproj`, or `.fsproj`; deterministic F# script validation is the applicable build-equivalent evidence.
- Test evidence: Passed: `dotnet fsi skills/audit-infra/scripts/ValidateInfrastructure.fsx --self-test`; `dotnet fsi skills/audit-infra/scripts/ValidateInfrastructure.fsx`; `dotnet fsi skills/audit-infra/scripts/TestInfrastructure.fsx` → `OK infrastructure test suite (11 steps)`.

After work, use `Passed: <command/result>`, `Not applicable: <reason>`, or `Waived: <Decision reference>`.

### Accepted findings

| ID | Contract | Status |
| -- | -------- | ------ |
| F-001 | Durable task review-profile validation must use the semantic risk-based Discovery sets rather than obsolete `Standard`/`Full` selectors. | FIXED |
| F-002 | Validator/tests must positively exercise every required specialist and fallback routing scenario, not merely marker strings. | FIXED |

### Verification receipts

| Finding ID | Result | Evidence |
| ---------- | ------ | -------- |
| F-001 | FIXED | Independent validator verification: semantic `routine`, `contract`, `architecture`, and `combined` profiles are enforced in task parsing, validation, docs, and tests; old selectors are rejected. |
| F-002 | FIXED | Independent validator and infrastructure-audit verification: explicit routing tables, self-tests, and live required-agent checks cover all required specialist/fallback scenarios. |

## Closing Steps

### C0. Pre-commit review board

- Run the one Discovery selected by `references/agent-gates.md` independently and in parallel on the full diff only after build and test evidence is recorded. An explicit not-applicable rationale or recorded waiver may replace either evidence item. A reviewer missing required evidence returns `BLOCKED`; do not transition from `NEW` or proceed with review.
  Steps:
- [x] Discovery reviewers' verdicts recorded after the evidence precondition
  - Summary: Architecture-conformity review passed; contract/acceptance Discovery produced F-001/F-002, both fixed and verified; correctness targeted Verification passed with no blocker or introduced regression. Targeted infrastructure audit passed.
- [ ] Critical/Error findings fixed or waived in `## Decisions`
- [x] Critical/Error findings fixed or waived in `## Decisions`
  - Summary: F-001/F-002 were remediated in pass 1 and independently verified FIXED; no waiver was used.
- [x] Targeted Verification receipts recorded for the frozen accepted finding set
  - Summary: Contract validator and targeted infrastructure audit both verified F-001/F-002 FIXED with no blocker or new non-blocking finding.

### C1. Clean up temporary artifacts

Steps:

- [x] Scratchpad / temp working files created for this task removed
  - Summary: No task-created scratchpad or temporary artifacts exist; no removal operation was required.

### C2. Commit and publish

- Commit implementation changes after user confirmation.
- Push and create the requested remote backup after user confirmation.
  Steps:
- [ ] Changes committed in target repo(s)
  - Summary:
- [ ] Remote backup completed
  - Summary:

## Decisions

| Date | Decision | Rationale |
| ---- | -------- | --------- |
| 2026-08-25 | Preserve existing uncommitted baseline | User directed that pre-existing edits be preserved rather than overwritten. |
| 2026-08-25 | Use existing `master` branch | User explicitly directed that all changes be made into `master`; no create/switch operation is needed. |
| 2026-08-25 | Freeze semantic architecture | Two isolated proposals and installed-model evidence confirm feasibility; user requirements remain authoritative where proposals differed. |
| 2026-08-25 | Freeze first Discovery findings | Validator Discovery identified F-001/F-002 as blocking contract gaps. They are finite remediation work; no architecture reopening is justified. |
| 2026-08-25 | Use combined semantic review profile | INFRA-004 is both architecture- and contract-sensitive; the updated durable profile vocabulary selects `reviewer + guardian + validator`. |
| 2026-08-25 | Build not applicable | No solution or project file exists; F# script validators and test suites provide applicable execution evidence. |
| 2026-08-25 | Complete remediation pass 1 | F-001/F-002 were verified FIXED by independent contract validation and targeted infrastructure audit. |
| 2026-08-25 | Freeze review state | Independent architecture, contract, correctness-targeted, and infrastructure verification produced no unresolved blocker after the finite remediation set was verified. |

## Open Questions

- None.

## Notes

- State: Implementation, testing, and bounded review are frozen; the explicit commit/publish gate remains pending.
- Evidence: Installed model registry, infrastructure/task validators, full infrastructure suite, targeted verification, static agent discovery, credential-path metadata, and `git diff --check` all passed.
- Next: Await explicit confirmation of a proposed commit message before committing; await separate explicit confirmation before remote backup/push.
