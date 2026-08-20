# Agent Gates

Canonical software-agent orchestration for code tasks. Gates are enforced through durable `TASK.md` state and checkboxes.

Coordinator-to-subagent handoffs follow `@C:/Users/andre/.config/opencode/rules/software/agent-handoff.md`; do not duplicate its contract here.

Language match: pick the team under `agents/software/` that matches the touched code - `dotnet/csharp`, `dotnet/fsharp`, or `rust`.
The engineer owns production implementation and builds. The tester owns test analysis, implementation, and execution. The primary coordinator owns synthesis, triage, state transitions, and completion. Review is a single post-evidence Discovery activity, not a routine implementation gate.
Use database, DevOps, security, or performance specialists only when concrete task or diff evidence materially affects their owned surface.
For a language without a dedicated team, assign `executor` as the editable implementation/build/test owner and separate `general` invocations for independent design and review.

## Gate matrix

| Phase | Always | Conditional (by concrete touched surface) |
| --------------------------- | ---------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| Design gate (subtask 3)     | complex or architecture-sensitive: all language-matching architect agents; otherwise an independent `general` design review. Non-complex: only the minimum architecture analysis necessary, and never multiple architects merely because they exist | `devops/engineer` for DevOps-only work; `database/sql-reviewer` if schema/migration planned; `security/reviewer` for sensitive surfaces |
| Implementation and build | language-matching engineer | database engineer for DB-heavy work; DevOps engineer for CI/deploy work |
| Tests                       | language-matching `tester` designs, implements, and runs tests   | If no tester exists, assign test work to the implementation owner                                                                        |
| Discovery review | Standard: reviewer 1 and reviewer 3. Full/architecture-sensitive: reviewers 1, 2, and 3 | Applicable specialist reviewers only |

Complex or architecture-sensitive language tasks retain both independent architect agents and coordinator synthesis. Non-complex tasks use only the minimum architecture analysis necessary; never invoke multiple architects merely because they exist. When architects are used, both receive the same evidence and work independently. The coordinator chooses or subtractively synthesizes the smallest sufficient design, records requirements, acceptance criteria, assumptions, non-goals, boundaries, constraints, review profile, and relevant rejected alternatives, then freezes it.

Architecture reopens only when the design cannot satisfy an acceptance criterion, contains a blocking correctness/security/data-integrity defect, materially misunderstands a required external contract, or is technically impossible under an approved constraint. Another valid design, style, extensibility, cleanup, or speculative optimization does not reopen it.

Reviewers receive the same frozen solution, implementation baseline, and build/test evidence, but use distinct mandates: reviewer 1 correctness/regressions, reviewer 2 architecture conformity/complexity, reviewer 3 contracts/acceptance/test adequacy. Do not show reviewers each other's Discovery findings. Before Discovery, build and test evidence must be recorded; each may instead cite an explicit not-applicable rationale or a recorded waiver. A reviewer missing required evidence returns `BLOCKED` and does not review.

## Verdict policy

1. Record `NEW -> DISCOVERY -> REMEDIATION -> VERIFICATION -> FROZEN` in `TASK.md` with the baseline identity and selected review profile. Keep `NEW` when a reviewer returns `BLOCKED` for missing evidence.
2. Discovery runs once, only after the evidence precondition, per frozen solution and implementation baseline. The coordinator deduplicates and triages findings, then freezes a finite accepted remediation set.
3. Critical/Error findings block. Warning/Info findings are explicitly accepted, deferred, or rejected and never restart Discovery.
4. Verification receives accepted finding IDs and contracts, the remediation diff, applicable requirements, and relevant build/test evidence. Return `FIXED`, `NOT FIXED`, or `REGRESSION INTRODUCED` per finding; do not conduct a fresh review or report new non-blocking findings.
5. One targeted second remediation pass is allowed for an unresolved blocking accepted finding or a blocking regression directly introduced by pass 1. Stop and report any blocker remaining after pass 2.
6. A later generic request to review a frozen artifact means Verification. Return to Discovery only on explicit user redesign or a hard invalidation condition.

## Cost control

- Applying findings follows `references/confirmation-policy.md`.
- Non-code tasks (pure analysis, docs-only): drop software-agent gates at template generation.
- Run independently approved gate agents in parallel when possible.

## Worktree policy

Use separate worktrees only when multiple editable agents may modify the same
repository/tree concurrently or when isolation is explicitly required;
otherwise use one coordinator-owned tree. Worktree creation, switching,
cleanup, and branch publication remain confirmation-gated. Worktree automation
is out of scope; this is a documented conditional rule, not an automated flow.
