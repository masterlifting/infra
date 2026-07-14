# Task File Invariants

Enforced by `scripts/ValidateTask.fsx`. Run `dotnet fsi scripts/ValidateTask.fsx <path-to-TASK.md>`; pass `--fix` to auto-repair the fixable ones. Exit 0 = clean; 1 = violations; 2 = bad invocation.

1. **H1 matches folder** — `# <TASK-ID> - Title` (hyphen or em-dash separator), where `<TASK-ID>` equals the `.tasks/<TASK-ID>/` folder name.
2. **No redundant title section** — `## Title` is forbidden; the H1 is the task title.
3. **Status header present and well-formed** — `**Progress: X/N subtasks complete** | **Status: <status>** | **Created: YYYY-MM-DD**`, with optional `| **Completed: YYYY-MM-DD**`. Status must be one of `In Progress`, `Blocked`, `Paused`, `Complete`.
4. **Completed-date consistency** — `Status: Complete` requires a `Completed:` date (auto-fixed with today's date); a `Completed:` date without `Status: Complete` is a violation.
5. **Progress counter accuracy** — declared `X/N` must match actual checkbox state. `N` counts column-zero `###` subtask headings (numbered and `C`-steps); a heading counts complete only when **all** its checkboxes are ticked. Auto-fixed.
6. **Blocked notation** — every `- [ ] [blocked]` item must carry a `- reason` (hyphen or em-dash).
7. **Decisions table dates** — every data row's `Date` cell must be `YYYY-MM-DD`.
8. **Target repos listed** — `## Context` must list at least one `` `./repo-name` `` entry.
9. **Branch naming** — when a repo entry carries a concrete `(branch: ...)` annotation (not `TBD`), the branch name must start with the task ID (`{TASK-ID}-description`).
10. **Stable subtask numbering** — subtask IDs must be strictly ascending in document order. Gaps are allowed after removal; never reuse or reorder an ID. Decimal subsections ascend within their parent. Letter suffixes are forbidden. Closing steps ascend and come after numbered subtasks; C0 may be omitted for non-code tasks, while C1 and C2 are required.
11. **Summary bullets** — every `Summary:` directly associated with a checklist item must be its own nested bullet: `  - Summary:`. Bare indented continuation lines are auto-fixed.
