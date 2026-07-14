---
name: task
description: Run when the user asks to create, update, validate, or resume project task tracking in `.tasks/` at repo root, or asks to structure multi-step implementation work into a task file. Do not use for one-off edits that do not need task tracking.
---

# Task Tracker

## Purpose

Track project `.tasks/` items with a resumable, gated workflow. Task files live at `.tasks/{TASK-ID}/TASK.md` at the repository root.

## References (load on demand)

- `C:/Users/andre/.config/opencode/skills/task/references/template.md` — full task template with drop rules (load when creating a new task)
- `C:/Users/andre/.config/opencode/skills/task/references/validation.md` — invariants checked by `scripts/ValidateTask.fsx`
- `C:/Users/andre/.config/opencode/skills/task/references/closing-steps.md` — detailed C0–C2 procedure (C2 = temp-artifact cleanup)
- `C:/Users/andre/.config/opencode/skills/task/references/agent-gates.md` — mandatory agent matrix per phase (design gate, per-subtask review, C0 board)
- `C:/Users/andre/.config/opencode/skills/task/references/confirmation-policy.md` — tiered confirmation: when to confirm vs. auto-proceed
- `C:/Users/andre/.config/opencode/skills/task/references/clarification.md` — clarification procedure: gate questions from research before implementation

## Guardrails

- Apply global defaults from `~/.config/opencode/AGENTS.md`; repository-local rules take precedence on conflict.
- Do not commit changes inside `.tasks/` without explicit confirmation.
- Always ask for the task ID before creating a new task when it is missing.
- Confirmation follows the tiered policy in `references/confirmation-policy.md`, not a flat per-subtask stop.
- Keep task prose concise; expand only where brevity would hide ordering, assumptions, safety, security, financial/legal impact, or architecture tradeoffs.
- Preserve exact code, paths, commands, URLs, error strings, API names, dates, versions, and placeholders when tightening task prose.

## Workflow

1. **Ask for the task ID** if creating a new task and the user hasn't provided one.
2. **Generate from template** — read `references/template.md`, apply its drop rules (design gate/C0 dropped for non-code tasks), and write `.tasks/{TASK-ID}/TASK.md`. Create `docs/` by default; add `imgs/`/`scripts/` only when required.
3. **Clarify before implementing** — surface clarification questions after research; block implementation subtasks until material questions are resolved or the user explicitly accepts the uncertainty.
4. **Enforce agent gates** — `references/agent-gates.md` is mandatory for code tasks: language-matching architect at the design gate (blocks implementation), reviewer per substantive subtask, tester for the suite, and the `reviewer-1`+`reviewer-2` board at C0 before any commit. Critical/Error findings block the gate; skips require a user waiver recorded in `## Decisions`.
5. **Confirm per policy** — follow the tiers in `references/confirmation-policy.md`. Tier 1 always confirms; Tier 2 auto-proceeds (reads, builds/tests via owner agents, helper scripts, spawning gate agents); Tier 3 announces and proceeds.
6. **Summaries per item** — for every completed checklist item, add an indented nested `- Summary:` bullet describing the concrete work, findings, or verification. If scripts derived implementation details, save them in `.tasks/{TASK-ID}/scripts/` and reference them from `TASK.md`.
7. **Update progress automatically** — the global task-progress plugin recomputes progress after OpenCode edits to `TASK.md`. After external/manual changes or when the plugin is unavailable, run `dotnet fsi "C:/Users/andre/.config/opencode/skills/task/scripts/RecomputeProgress.fsx" <path-to-TASK.md>`.
8. **Validate periodically** — the task-progress plugin surfaces validation findings after OpenCode edits. Run `dotnet fsi "C:/Users/andre/.config/opencode/skills/task/scripts/ValidateTask.fsx" <path-to-TASK.md>` for explicit validation; pass `--fix` to auto-repair drift.
9. **Closing steps** — after the last task-specific subtask, run C0–C2 per `references/closing-steps.md`. Treat previously checked C-steps as stale when new work changes their answer.
10. **Verification records** — record proportionate verification for non-trivial work: command, result, and the relevant excerpt in `TASK.md`; store full raw output under `.tasks/{TASK-ID}/docs/` only when it is useful evidence.

## On Resume

When loading an existing task rather than creating a new one:

1. Read the task file first and surface the current status header line plus remaining incomplete subtasks.
2. Check out the target branch(es) named in the task before implementation work, if they exist and the user has not forbidden git manipulation.
3. If the task has a `## Continuity` section, execute or follow those steps before proceeding.
4. Refresh `## Key Files`, `## References`, `## Decisions`, and `## Open Questions` if context changed.
5. Preserve resume-critical facts in the task file: target branches, touched repos, files modified this session, verification status, open TODOs, and the next approved or pending subtask.
6. If local `.opencode/operating-model.md` or `.opencode/verification.md` exists, follow it and reference it from the task when useful.

## Status transitions

- `In Progress` — actively being worked on
- `Blocked` — cannot proceed; reason recorded in Notes or the blocked step
- `Paused` — intentionally halted (waiting for review, context-switching)
- `Complete` — all subtasks done or the user declares the task finished. Add `Completed: YYYY-MM-DD` (validator auto-fixes if missing). Remind the user about remaining manual or environment-dependent steps (e2e testing, deployment verification) before closing.

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
