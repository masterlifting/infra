# Agent Gates

Canonical software-agent orchestration for code tasks. Gates are enforced through durable `TASK.md` state and checkboxes.

Coordinator-to-subagent handoffs follow `@C:/Users/andre/.config/opencode/rules/software/agent-handoff.md`; do not duplicate its contract here.

Language match: pick the team under `agents/software/` that matches the touched code - `dotnet/csharp`, `dotnet/fsharp`, or `rust`.
The engineer owns production implementation, builds, and implementation-native validation. The tester owns test design, implementation, and execution. The primary coordinator owns synthesis, triage, state transitions, and completion. Review is a single post-evidence Discovery activity, not a routine implementation gate.
Use database, DevOps, security, or performance specialists only when concrete task or diff evidence materially affects their owned surface.
For a language without a dedicated team, assign `executor` as the editable implementation/build/test owner and use `general` invocations for the architecture and review roles.

## Gate matrix

| Phase | Always | Conditional (by concrete touched surface) |
| --------------------------- | ---------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| Design gate (subtask 3)     | complex or architecture-sensitive: language-matching `architect` and `challenger` (isolated, read-only, equivalent evidence); non-complex: defaults to coordinator design, with at most one appropriate `architect` for concrete unresolved uncertainty and no general design review | `devops/engineer` for DevOps-only work; `database/reviewer` if schema/migration planned; `security/reviewer` for sensitive surfaces |
| Implementation and build | language-matching `engineer` | `database/engineer` for DB-heavy work; `devops/engineer` for CI/deploy work |
| Tests                       | language-matching `tester` designs, implements, and runs tests   | If no tester exists, assign test work to the implementation owner                                                                        |
| Discovery review | Review profile `routine`: only the language `reviewer` is mandatory. `contract`: `reviewer` and `validator`. `architecture`: `reviewer` and `guardian`. `combined`: `reviewer`, `guardian`, and `validator`. Reviewers in a profile are selected independently. | Applicable specialist reviewers only, additive to the profile set |

Complex or architecture-sensitive language tasks use language-matching `architect` and `challenger`: each receives the same evidence, works in isolation, and is read-only. The coordinator subtractively synthesizes the smallest sufficient design from the two proposals. Non-complex tasks default to the coordinator's own design; the coordinator invokes at most one appropriate `architect` only for concrete unresolved uncertainty, and never a general design review. The coordinator records requirements, acceptance criteria, assumptions, non-goals, boundaries, constraints, review profile, and relevant rejected alternatives, then freezes it.

Architecture reopens only when the design cannot satisfy an acceptance criterion, contains a blocking correctness/security/data-integrity defect, materially misunderstands a required external contract, or is technically impossible under an approved constraint. Another valid design, style, extensibility, cleanup, or speculative optimization does not reopen it.

Reviewers receive the same frozen solution, implementation baseline, and build/test evidence, but use distinct mandates: `reviewer` correctness/regressions, `guardian` architecture conformity/complexity, `validator` contracts/acceptance/test adequacy. Do not show reviewers each other's Discovery findings. Before Discovery, build and test evidence must be recorded; each may instead cite an explicit not-applicable rationale or a recorded waiver. A reviewer missing required evidence returns `BLOCKED` and does not review.

## Verdict policy

1. Record `NEW -> DISCOVERY -> REMEDIATION -> VERIFICATION -> FROZEN` in `TASK.md` with the baseline identity and selected review profile (`routine`, `contract`, `architecture`, or `combined`). Keep `NEW` when a reviewer returns `BLOCKED` for missing evidence.
2. Discovery runs once, only after the evidence precondition, per frozen solution and implementation baseline. The coordinator deduplicates and triages findings, then freezes a finite accepted remediation set.
3. Critical/Error findings block. Warning/Info findings are explicitly accepted, deferred, or rejected and never restart Discovery.
4. Verification receives accepted finding IDs and contracts, the remediation diff, applicable requirements, and relevant build/test evidence. Return `FIXED`, `NOT FIXED`, or `REGRESSION INTRODUCED` per finding; do not conduct a fresh review or report new non-blocking findings.
5. One targeted second remediation pass is allowed for an unresolved blocking accepted finding or a blocking regression directly introduced by pass 1. Stop and report any blocker remaining after pass 2.
6. A later generic request to review a frozen artifact means Verification. Return to Discovery only on explicit user redesign or a hard invalidation condition.

## Cost control

- Applying findings follows `references/confirmation-policy.md`.
- Non-code tasks (pure analysis, docs-only): drop software-agent gates at template generation.
- Dispatch only already-required independent waves in parallel; keep dependent chains ordered.

## Parallel waves and state batching

Parallelism is bounded to already-required independent waves:

- Design: `architect` and `challenger` (isolated, read-only, equivalent frozen evidence).
- Implementation: task-specific `Implement:` subtasks that are genuinely independent slices.
- Discovery: the independently selected reviewers in the chosen profile.
- Remediation: independent accepted findings within a bounded pass.
- Verification: targeted receipts for independent accepted findings.

Dependent chains stay strictly ordered: implementation → build → test → Discovery → remediation → Verification. Never parallelize dependent work.

Batch TASK.md state coherently. Each batch recomputes progress then validates before it is durable (the automatic sync plugin performs this on every edit). Durability boundary: validated TASK.md facts are durable; in-flight proposals, drafts, and working notes are transient and must not be recorded as durable task state.

## Worktree policy

Use separate worktrees only when multiple editable agents may modify the same
repository/tree concurrently or when isolation is explicitly required;
otherwise use one coordinator-owned tree. Worktree creation, switching,
cleanup, and branch publication remain confirmation-gated. Worktree automation
is out of scope; this is a documented conditional rule, not an automated flow.
