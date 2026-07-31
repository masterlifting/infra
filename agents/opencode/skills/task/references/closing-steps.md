# Closing Steps (C0–C2)

Run after the last task-specific subtask, in order.
Re-check from C0 onward whenever new work lands after a C-step was checked — treat the earlier check as stale.
Routine C0 review and fixes follow the normal auto-proceed policy. Cleanup, commits, publishing, and other explicitly gated actions retain their confirmation requirements.

## C0. Pre-commit review board

- Run every reviewer selected by `references/agent-gates.md` independently and in parallel on the full `git diff` before any commit.
- Run all conditional board members selected by `references/agent-gates.md` when their listed surfaces apply.
- Critical/Error findings block the commit: fix and re-run the affected reviewer(s), or record an explicit user waiver in `## Decisions`.
- Review and fix until the reviewers have no more questions or comments.

## C1. Clean up temporary artifacts

Before committing, remove only temporary artifacts created for the task outside the target repositories. Every removal is Tier 1 and requires explicit confirmation.

**Remove:**

- Working files created for this task under the session scratchpad or a temp dir (probe scripts, intermediate data, one-off outputs).

**Never remove:**

- Source-tree files, including debug logging and stray files in touched repositories. Clean these before C0 so review covers the committed diff.
- The task's own `.tasks/<TASK-ID>/TASK.md`, `docs/`, and `scripts/` — these are intentional records.
- Anything already committed as part of the change.
- Files the user authored or edited by hand (confirm before deleting anything you didn't create).

## C2. Commit and publish

- Include C2 only when the task requires a commit or review artifact.
- Show the proposed commit message and wait for explicit confirmation before committing.
- Push and open the repo's normal review artifact (PR/MR) after user confirmation; link it from the task file.
