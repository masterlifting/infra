# Task File Invariants

Enforced by `scripts/validate.fsx`. Run `dotnet fsi scripts/validate.fsx <path-to-TASK.md>`; pass `--fix` to auto-repair the fixable ones. Exit 0 = clean; 1 = violations; 2 = bad invocation.

1. **H1 matches folder** — `# <TASK-ID> - Title` (hyphen or em-dash separator), where `<TASK-ID>` equals the `.tasks/<TASK-ID>/` folder name.
2. **Status header present and well-formed** — `**Progress: X/N subtasks complete** | **Status: <status>**` with optional `| **Created: YYYY-MM-DD**` and `| **Completed: YYYY-MM-DD**` pillars. Status must be one of `In Progress`, `Blocked`, `Paused`, `Complete`.
3. **Completed-date consistency** — `Status: Complete` requires a `Completed:` date (auto-fixed with today's date); a `Completed:` date without `Status: Complete` is a violation.
4. **Progress counter accuracy** — declared `X/N` must match actual checkbox state. `N` counts `###` subtask headings (numbered and `C`-steps); a heading counts complete only when **all** its checkboxes are ticked. Auto-fixed.
5. **Blocked notation** — every `- [ ] [blocked]` item must carry a `- reason` (hyphen or em-dash).
6. **Decisions table dates** — every data row's `Date` cell must be `YYYY-MM-DD`.
7. **Target repos listed** — `## Context` must list at least one `` `./repo-name` `` entry.
8. **Branch naming** — when a repo entry carries a concrete `(branch: ...)` annotation (not `TBD`), the branch name must start with the task ID (`{TASK-ID}-description`).
