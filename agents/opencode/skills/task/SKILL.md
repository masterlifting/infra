---
name: task
description: Use when creating, updating, validating, or resuming `.tasks/{TASK-ID}/TASK.md`, or when structuring multi-step implementation into resumable project task tracking. Do not use for one-off edits or informational requests that do not need a task record.
---

# Task Tracker

## Purpose

Track project `.tasks/` items with a resumable, gated workflow. Task files live at `.tasks/{TASK-ID}/TASK.md` at the repository root.

## References (load on demand)

- `C:/Users/andre/.config/opencode/skills/task/references/template.md` — full task template with drop rules (load when creating a new task)
- `C:/Users/andre/.config/opencode/skills/task/references/validation.md` — invariants checked by `scripts/ValidateTask.fsx`
- `C:/Users/andre/.config/opencode/skills/task/references/closing-steps.md` — bounded review, cleanup, and conditional commit/publish procedure
- `C:/Users/andre/.config/opencode/skills/task/references/agent-gates.md` — software-agent ownership, review profiles, and bounded review state machine
- `C:/Users/andre/.config/opencode/skills/task/references/confirmation-policy.md` — tiered confirmation: when to confirm vs. auto-proceed
- `C:/Users/andre/.config/opencode/skills/task/references/clarification.md` — clarification procedure: gate questions from research before implementation
- `C:/Users/andre/.config/opencode/skills/task/references/behavioral-spec.md` — optional behavioral specification convention for requirements-heavy tasks (load when deciding whether to create `.tasks/{TASK-ID}/SPEC.md`)

## Guardrails

- Apply global defaults from `~/.config/opencode/AGENTS.md`; repository-local rules take precedence on conflict.
- Do not commit changes inside `.tasks/` without explicit confirmation.
- Always ask for the task ID before creating a new task when it is missing.
- Confirmation follows the tiered policy in `references/confirmation-policy.md`, not a flat per-subtask stop.
- Keep task prose concise; expand only where brevity would hide ordering, assumptions, safety, security, financial/legal impact, or architecture tradeoffs.
- Preserve exact code, paths, commands, URLs, error strings, API names, dates, versions, and placeholders when tightening task prose.

## Workflow

1. **Ask for the task ID** if creating a new task and the user hasn't provided one.
2. **Generate from template** - read `references/template.md`, apply its drop rules, and create `.tasks/{TASK-ID}/TASK.md` through `scripts/CreateTask.fsx <TASK-ID> <title> [--non-code] [--no-commit]`. Never overwrite an existing task; route it to resume. Create `docs/` by default; add `imgs/`/`scripts/` only when required.
3. **Create an optional behavioral specification** - only when the task materially involves user-facing behavior, business rules, public/API contracts, state transitions, or important edge cases, create `.tasks/{TASK-ID}/SPEC.md` per `references/behavioral-spec.md` and reference it from the task's `## References`. Never require `SPEC.md` for ordinary tasks.
3. **Clarify before implementing** — classify uncertainty per `references/clarification.md`; interrupt only for `BLOCKING` questions and record accepted assumptions.
4. **Prepare code-task implementation during subtasks 1-3** — draft and classify the task-specific implementation and validation work during research, refine it after clarification, and finalize it through the design gate. Set `Implementation plan` in `## Context` to `non-complex` or `complex`. A task is complex when one implementation-and-validation cycle would hide independent slices, ordering, ownership, or materially different components or repositories. For a complex task, replace the generic subtask 5 with numbered `Implement: ...` and `Validate: ...` subtasks starting at 5. For a non-complex task, retain subtask 5 and add the task-specific implementation and validation steps inside it. Do not begin implementation or complete the design gate while `Implementation plan` is `TBD` or generic planning placeholders remain.
5. **Enforce agent gates** - `references/agent-gates.md` is the canonical software orchestration procedure. Freeze one solution after independent architecture proposals, then run implementation, build, tests, one Discovery review, triage, bounded remediation, and targeted Verification. Preserve required gate checkboxes when refining or replacing subtask 5.
6. **Confirm per policy** - follow `references/confirmation-policy.md` as the single source of truth. Never infer approval for a Tier 1 action from a general instruction.
7. **Summaries per item** — for every completed checklist item, add an indented nested `- Summary:` bullet describing the concrete work, findings, or verification. If scripts derived implementation details, save them in `.tasks/{TASK-ID}/scripts/` and reference them from `TASK.md`.
8. **Update progress automatically** — the global task-progress plugin recomputes progress after OpenCode edits to `TASK.md`. After external/manual changes or when the plugin is unavailable, run `dotnet fsi "C:/Users/andre/.config/opencode/skills/task/scripts/RecomputeProgress.fsx" <path-to-TASK.md>`.
9. **Validate periodically** — the task-progress plugin surfaces validation findings after OpenCode edits. Run `dotnet fsi "C:/Users/andre/.config/opencode/skills/task/scripts/ValidateTask.fsx" <path-to-TASK.md>` for explicit validation; pass `--fix` to auto-repair drift.
10. **Closing steps** - after implementation, build, and tests, run applicable closing steps per `references/closing-steps.md`. Cleanup is required; review and commit/publish are conditional. Do not restart Discovery after the accepted finding set freezes.
11. **Verification records** — record proportionate verification for non-trivial work: command, result, and the relevant excerpt in `TASK.md`; store full raw output under `.tasks/{TASK-ID}/docs/` only when it is useful evidence.

## On Resume

When loading an existing task rather than creating a new one:

1. Read the task file first and surface the current status header line plus remaining incomplete subtasks.
2. Ask for explicit confirmation before creating or switching to target branches named in the task.
3. If the task has a `## Continuity` section, execute or follow those steps before proceeding.
4. Refresh `## Key Files`, `## References`, `## Decisions`, and `## Open Questions` if context changed.
5. Preserve resume-critical facts in the task file: target branches, touched repos, files modified this session, verification status, open TODOs, and the next approved or pending subtask.
6. When refining implementation structure, replace an untouched generic subtask 5 in place. If subtask 5 has checked items, summaries, or execution evidence, preserve its ID and evidence, rename/refine it as needed, and append new numbered subtasks rather than deleting or reusing history.
7. If implementation decomposition changes after the solution freezes, reopen architecture only when `references/agent-gates.md` defines a hard invalidation condition.
8. If local `.opencode/operating-model.md` or `.opencode/verification.md` exists, follow it and reference it from the task when useful.

## Status transitions

- `In Progress` — actively being worked on
- `Blocked` — cannot proceed; reason recorded in Notes or the blocked step
- `Paused` — intentionally halted (waiting for review, context-switching)
- `Complete` - ask for explicit confirmation for this status transition and record a dated `complete status confirmed` decision. Require all lifecycle items complete; if the user intentionally closes with incomplete items, record a dated `complete status waiver` and rationale. Add `Completed: YYYY-MM-DD` and remind the user about remaining manual or environment-dependent steps.

## Subtask numbering rules

- Follow the canonical numbering and completion contracts in `references/validation.md` (invariants 5 and 10); do not restate or reinterpret them here.
- Blocked notation: `- [ ] [blocked] Step - reason` (the reason is required); update when unblocked.
- When adding a `## Decisions` row, fill the `Date` column with today's date (`YYYY-MM-DD`) unless the decision date is known.
- Prefer free-form notes shaped as `State`, `Evidence`, `Next`.

## Optional external context

When a repo-local workflow mentions an external tracker, connector, or ticket system, use it only if the relevant connector or CLI is available and the user has given enough context. On lookup failure (access, network, permissions), fall back to manual drafting and note the gap in the task file.

## Retention

- Completed tasks remain in `.tasks/` for reference; never delete task files — they are part of the implementation record.
- Tasks older than 3 months with `Status: Complete` may be moved to `.tasks/_archive/` to reduce clutter.

## F# scripting and helpers

- Shared guidance: `C:/Users/andre/.config/opencode/scripts/README.md`; shared helpers: `scripts/*.fsx` (reuse via `#load`).
- Prefer F# scripts for non-trivial automation; place task-specific scripts under `.tasks/{TASK-ID}/scripts/`.

## Output

- Keep `TASK.md` concise, resumable, and synchronized with completed work, decisions, verification, blockers, and the next pending step.
- Report progress, validation findings, and confirmation gates without reproducing the full task document in chat.
