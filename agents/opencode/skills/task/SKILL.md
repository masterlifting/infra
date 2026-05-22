---
name: task
description: Run when the user asks to create, update, validate, or resume project task tracking in `.tasks/` at repo root, or asks to structure multi-step implementation work into a task file. Do not use for one-off edits that do not need task tracking.
---

# Task Tracker

## Purpose

Track project `.tasks/` items with a resumable, confirmation-gated workflow.

Use this skill when the user asks to create, update, or validate task files in a
project `.tasks/` folder at the repository root.

## Guardrails

- Apply global defaults from `~/.config/opencode/AGENTS.md`.
- If repository-local rules conflict, local rules take precedence.
- Use the target repository's task root: `.tasks/` at repo root.
- Do not commit changes inside `.tasks/` without explicit confirmation.
- Always ask for the task ID before creating a new task item when it is missing.
- Always ask for user confirmation before implementing each subtask.
- Respect the repository's existing ignore-file policy. Do not add or remove
  ignore rules unless the user explicitly asks.
- Keep task prose concise by default. Expand when brevity would hide ordering,
  assumptions, safety risk, security/auth concerns, financial/legal impact,
  architecture tradeoffs, or requested reasoning.

## Workflow

1. Locate the target repository root and any repo-local task guidance.
2. Use `.tasks/` as the only supported project task root (repository root level).
3. Keep `.tasks/` inside the parent repository workflow; do not create a nested `.git` in `.tasks/` unless explicitly requested.
4. If creating a new task and the user has not provided an ID, ask for one.
5. Create or update `.tasks/{TASK-ID}/TASK.md`.
6. Create `docs/` by default inside `.tasks/{TASK-ID}/`. Add `imgs/` and
   `scripts/` only when required.
7. Start from the **Template** section below and fill in required fields: Brief
   Summary, Context, Requirements / Acceptance Criteria, and Subtasks.
8. After research, surface clarification questions in chat before writing code.
   Block implementation subtasks until all material questions are resolved or
   the user explicitly accepts the uncertainty.
9. If scripts or commands are used to derive implementation details, save them
   in `.tasks/{TASK-ID}/scripts/` and reference them from `TASK.md`.
10. For every completed checklist item, add an indented `Summary:` line directly
   beneath it describing the concrete work completed, findings captured, or
   verification performed.
11. Before implementing each subtask, stop and ask the user for confirmation.
    Summarize what will be done and wait for explicit approval before starting
    that subtask.
12. After each change, update the task file so the task list, summaries, and
    closing-step state stay current.
13. Update the progress counter in `## Progress` every time a subtask is
    completed, added, removed, or materially re-scoped.
14. After the last task-specific subtask is completed, re-run the Closing Steps from
    C1 onward. Treat previously checked closing-step items as stale when new
    implementation, documentation, validation, or review-thread work changes
    their answer.
15. Record proportionate verification for non-trivial work, aligned with any
    repository-local verification guidance when present.
16. For noisy verification, record command, result, and the relevant excerpt in
    `TASK.md`; store full raw output under
    `.tasks/{TASK-ID}/docs/` only when it is useful evidence.

## Output

- Primary artifact: `.tasks/{TASK-ID}/TASK.md` updated using the template and current workflow state.
- Chat output: short status update with progress, blockers, next pending decision, and any explicit confirmation gate.

## Optional External Context

When a repo-local workflow mentions an external tracker, connector, or ticket
system, use that system only if the relevant connector or CLI is available and
the user has given enough context. If lookup fails because of missing access,
network, or permissions, fall back to manual task drafting and note the gap in
the task file.

## Continuity

When loading an existing task rather than creating a new one:

1. Read the task file first and surface the current `## Progress` line plus
   remaining incomplete subtasks.
2. Check out the target branch or branches named in the task before
   implementation work, if those branches exist and the user has not forbidden
   git manipulation.
3. If the task has a `## Continuity` section, execute or follow those steps
   before proceeding.
4. Refresh `## Key Files`, `## References`, `## Decisions`, and
   `## Open Questions` if context changed.
5. Preserve resume-critical facts in the task file: target branches, touched
   repos, files modified this session, verification status, open TODOs, and
   the next approved or pending subtask.
6. If local `.opencode/operating-model.md` or `.opencode/verification.md` exists,
   follow it and reference it from the task when useful.

## Completion

When all subtasks are marked complete, or the user says the task is done:

1. Remind the user about any remaining manual or environment-dependent tasks
   such as end-to-end testing or deployment verification.
2. If implementation changes need a commit or publish closing
   step, suggest commit message text based on completed work and ask for
   confirmation before committing.
3. Update the status to `Complete` and add the `Completed:` date.

## Retention

- Completed tasks remain in `.tasks/` for reference.
- Tasks older than 3 months with `Status: Complete` may be moved to
   `.tasks/_archive/` to reduce clutter.
- Never delete task files; they are part of the implementation record.

## Operational Rules

- Keep subtask ordering stable when possible, but it is acceptable to add,
  remove, or reorder subtasks as the task evolves.
- Prefer item-level summaries over section-level recap text so a task can be
  resumed without rereading all source material.
- Use decimal numbering for sub-phases (`3.1`, `3.2`, `3.3`) instead of
  renumbering existing top-level subtasks where possible.
- Blocked notation: mark blocked steps as
  `- [ ] [blocked] Step - reason` and update them when unblocked.
- When adding a row to `## Decisions`, fill the `Date` column with today's date
  (`YYYY-MM-DD`) unless the decision date is known.
- The progress counter tracks top-level `###` subtasks. If a subtask expands
  into `####` sub-subtasks, count the parent as complete only when all
  sub-subtasks are done.
- Status values:
  - `In Progress` for active work.
  - `Blocked` when progress cannot continue; capture the reason.
  - `Paused` when the work is intentionally halted.
  - `Complete` when all subtasks are done and the user considers the task
    finished.
- Record verification proportionate to the change instead of defaulting to the
  broadest possible build or test command.
- Prefer task notes shaped as `State`, `Evidence`, and `Next` when adding free-form notes.
- Preserve exact code, paths, commands, URLs, error strings, API names, dates,
  versions, and placeholders when tightening task prose.

## Template

```markdown
# TASK-ID - Task Title

**Progress: 0/N subtasks complete** | **Status: In Progress** | **Created: YYYY-MM-DD**

## Brief Summary
(1-3 sentences describing what should work when complete)

## Continuity
<!-- Optional: instructions to run every time this task is loaded. Delete this section if not needed. -->

## Context
- Target repo(s):
  - `./repo-name` (branch: TBD after research) - why this repo is involved
  - `./other-repo` (branch: TBD after research) - why this repo is involved
- Problem / opportunity:
- Constraints:
- Related links/issues:

## Key Files

<!-- Map important files, configs, docs, and scripts as they are discovered. -->

| Purpose | Path |
|---------|------|
| (Main implementation) | `service/path/to/File.cs` |
| (Config) | `service/path/to/Config.cs` |
| (Tests) | `service/tests/path/to/Tests.cs` |

## Requirements / Acceptance Criteria
- (Build/test requirement)
- (Functional behavior requirement)
- (Safety/security/regression requirement)

## Non-Goals
- (Explicitly out of scope)
- (Explicitly out of scope)

## References

<!-- Keep links to external docs, analysis notes, tickets, and generated artifacts here. -->

- Ticket/spec: (link, if applicable)
- Notion/Drive/GitHub: (link, if applicable)
- Analysis docs: `.tasks/{TASK-ID}/docs/...`

## Subtasks

### 1. Research and define approach
Steps:
- [ ] Investigate relevant code paths and document findings
  Summary:
- [ ] Define expected behavior and constraints
  Summary:
- [ ] Confirm approach with the user
  Summary:

### 2. Clarify gaps before implementation
After subtask 1 research, surface clarification questions before implementation.
This subtask blocks implementation until all material questions are resolved or
the user explicitly accepts the uncertainty.

Steps:
- [ ] Compile clarification questions from research, acceptance criteria, edge cases, missing inputs/outputs, or conflicting constraints
  Summary:
- [ ] Post the full question list in chat and capture answers in `## Decisions` or `## Open Questions`
  Summary:
- [ ] Mark unresolved blockers with `[blocked]` notation and set `Status: Blocked` while waiting
  Summary:
- [ ] Confirm with the user that implementation can proceed
  Summary:

### 3. Branch setup across touched repos
- Branch format: `{TASK-ID}-description` when the repo does not define another format.
- Create or switch branches only after research identifies the repos that will actually be touched.
Steps:
- [ ] Create or switch to a working branch in each touched repo
  Summary:
- [ ] Update `## Context` target repo branch entries with the selected branch names
  Summary:

### 4. Implement and validate
Steps:
- [ ] Add or update implementation code
  Summary:
- [ ] Unit tests added or updated and passing
  Summary:
- [ ] Integration tests added or updated and passing (if applicable)
  Summary:

## Closing Steps

<!-- Re-check this section after each task-specific subtask. If new work lands
     after a C-step was checked, treat that check as stale and re-run it. -->

### C1. Add or update documentation
- Update user-facing or developer docs when the change affects behavior,
  operations, architecture, or validation.
- If the repo has an established changelog, release-note, or tester-instruction
  format, use that format instead of creating a new one.
Steps:
- [ ] Documentation updated or explicitly not needed
  Summary:
- [ ] Tester or reviewer instructions captured when relevant
  Summary:

### C2. Commit and publish (optional)
- Commit implementation changes after user confirmation.
- Push and open the repo's normal review artifact after user confirmation.
Steps:
- [ ] Changes committed in target repo(s)
  Summary:
- [ ] Review artifact created and linked
  Summary:

### C3. Validate CI pipeline (optional)
- Validate the relevant CI or local equivalent after a review artifact exists.
- If failures are infrastructure-only, retry and re-validate.
- If failures are caused by scripts or code, investigate, fix, commit, push,
  and re-validate after user confirmation.
Steps:
- [ ] Pipeline or local equivalent validated
  Summary:
- [ ] Script or code failures resolved (if any)
  Summary:

### C4. Resolve review threads (optional)
- Fetch unresolved review threads when the repo has a review artifact.
- Present open threads to the user and ask which to address.
- For each approved thread fix: read the file, explain the concern, propose
  the change, and wait for confirmation before implementing.
Steps:
- [ ] Open threads reviewed with user
  Summary:
- [ ] Agreed fixes applied and pushed
  Summary:
- [ ] No open threads remaining, or the user explicitly declined the remaining items
  Summary:

## Decisions

| Date | Decision | Rationale |
|------|----------|-----------|
| | | |

## Open Questions
- (Any uncertainties or decisions that still need resolution)
- (Mark resolved questions using `- ~~[x] Question text~~`)

## Notes
- State:
- Evidence:
- Next:
```

## F# Scripting And Helpers

- Shared guidance: `scripts/README.md`
- Shared helper scripts: `scripts/*.fsx`
- When implementation or analysis requires non-trivial automation, prefer F#
  scripts (`.fsx`) and reuse prelude modules via `#load`.
- If task-specific scripts are created, place them under
  `.tasks/{TASK-ID}/scripts/` and optionally `#load` global prelude helpers.
