# Agent Gates

Mandatory agent involvement per task phase for code tasks.
Gates are **enforced via checkboxes** in the task template — a subtask cannot be ticked complete while its gate checkbox is open.

Language match: pick the team under `agents/software/` that matches the touched code - `dotnet/csharp`, `dotnet/fsharp`, or `rust`.
Use `database/sql-engineer`/`database/sql-reviewer` for DB-heavy work and `devops/engineer`/`devops/reviewer` for CI/infra surfaces.
For a language without a dedicated team, assign `executor` as the editable implementation/build/test owner and separate `general` invocations for independent design and review. For DevOps-only work, use the read-only `devops/engineer` for design analysis, `executor` for approved edits and project-native validation, and `devops/reviewer` for review.

## Gate matrix

| Phase                       | Always                                                           | Conditional (by touched surface)                                                                                                        |
| --------------------------- | ---------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| Design gate (subtask 3)     | language-matching `architect`; otherwise an independent `general` design review | `devops/engineer` for DevOps-only work; `database/sql-reviewer` if schema/migration planned; load `rules/security.md` context for sensitive surfaces |
| Implementation (each)       | engineer implementation/build verdict; one reviewer if substantive | `database/sql-reviewer` for migrations, repositories, raw SQL; `devops/reviewer` for CI/deploy files                                  |
| Tests                       | language-matching `tester` designs, implements, and runs tests   | If no tester exists, assign test work to the implementation owner                                                                        |
| C0. Pre-commit review board | language `reviewer-1`; otherwise an independent `general` review; add matching `reviewer-2` only after explicit provider-B approval | `architect` if boundaries changed; `database/sql-reviewer` if migrations present; `devops/reviewer` for DevOps-only or deploy changes |

"Substantive" = more than ~50 changed lines or new files in core source folders — skip the per-subtask review for trivial edits, never skip C0.

When unsure whether a surface is touched, run the agent — a clean verdict is cheap; a missed Critical is not.

## Verdict policy

1. Each gate agent returns findings. Record the one-line verdict in the gate's checkbox line, e.g. `- [x] Design gate: architect - approved (2 suggestions adopted)`.
2. **Critical / Error findings block the gate.** Fix and re-run the agent, or get an explicit user waiver.
3. Waivers are recorded in `## Decisions`: date, "waived <agent> <finding>", rationale. No silent skips.
4. Warnings/Info: apply or consciously defer; deferred items go to `## Notes` or follow-up tasks.
5. After C0 fixes change code, re-run the affected approved reviewer(s) on the new diff before proceeding to C1.

## Cost control

- Provider-A read-only gates are Tier 2. Provider-B gates always require explicit approval for the specific assignment. Applying findings follows `references/confirmation-policy.md`.
- Non-code tasks (pure analysis, docs-only): drop the design gate and C0 at template generation.
- Run independently approved gate agents in parallel when possible.
- Agent ownership and coordinator routing follow `@C:/Users/andre/.config/opencode/rules/software/team.md`.
