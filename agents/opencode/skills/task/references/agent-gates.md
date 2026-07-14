# Agent Gates

Mandatory agent involvement per task phase for code tasks.
Gates are **enforced via checkboxes** in the task template — a subtask cannot be ticked complete while its gate checkbox is open.

Language match: pick the team under `agents/software/` that matches the touched code — `dotnet/csharp`, `dotnet/fsharp`, or `rust`.
Use `database/sql-engineer`/`database/sql-reviewer` for DB-heavy work and `devops/engineer`/`devops/reviewer` for CI/infra surfaces.

## Gate matrix

| Phase                       | Always                                                           | Conditional (by touched surface)                                                                                                        |
| --------------------------- | ---------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| Design gate (subtask 3)     | language-matching `architect` — approach review                  | `database/sql-reviewer` if schema/migration planned; load `rules/security.md` context for auth/secrets/untrusted-input surfaces         |
| Implementation (each)       | engineer build check; one reviewer if substantive                | `database/sql-reviewer` for migrations, repositories, raw SQL; `devops/reviewer` for CI/deploy files                                    |
| Tests                       | language-matching `tester` — runs suite, owns the verdict        |                                                                                                                                         |
| C0. Pre-commit review board | `reviewer-1` + `reviewer-2` in parallel — full `git diff` review | `architect` if boundaries changed; `database/sql-reviewer` if migrations present; `devops/reviewer` if pipelines/deploy configs changed |

"Substantive" = more than ~50 changed lines or new files in core source folders — skip the per-subtask review for trivial edits, never skip C0.

When unsure whether a surface is touched, run the agent — a clean verdict is cheap; a missed Critical is not.

## Verdict policy

1. Each gate agent returns findings. Record the one-line verdict in the gate's checkbox line, e.g. `- [x] Design gate: architect - approved (2 suggestions adopted)`.
2. **Critical / Error findings block the gate.** Fix and re-run the agent, or get an explicit user waiver.
3. Waivers are recorded in `## Decisions`: date, "waived <agent> <finding>", rationale. No silent skips.
4. Warnings/Info: apply or consciously defer; deferred items go to `## Notes` or follow-up tasks.
5. After C0 fixes change code, re-run the affected reviewer(s) on the new diff before proceeding to C1.

## Cost control

- Spawning gate agents is read-only → **Tier 2** (auto-proceed, no confirmation). Applying their fixes follows the normal tiers (see `references/confirmation-policy.md`).
- Non-code tasks (pure analysis, docs-only): drop the design gate and C0 at template generation.
- Run independent gate agents **in parallel** — e.g. the C0 board.
- Gate agents follow `@C:/Users/andre/.config/opencode/rules/software/team.md`: they never run builds or tests themselves and report findings-first with minimal output.
