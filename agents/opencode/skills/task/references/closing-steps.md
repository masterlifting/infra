# Closing Steps

Run after implementation, build, and tests. Review state follows `agent-gates.md`; cleanup, commits, publishing, and other explicitly gated actions retain their confirmation requirements. Before setting `Status: Complete`, make `## Notes` terminal-current: `State`/`Evidence` reflect the final outcome and `Next` states explicitly that no task work remains, or lists only real manual/optional follow-up. When `Unblocks:` and `Unblock condition:` are recorded, surface both for the dependent to refresh and revalidate; never mutate it or infer its real-world condition from this task's completion.

## C0. Discovery and Verification

- Run the frozen review profile independently and in parallel against the same baseline.
- Triage once, freeze the accepted finding set, remediate it, and run targeted Verification.
- Permit at most one additional targeted remediation and Verification pass under `agent-gates.md`.
- Do not restart Discovery or pursue unrelated improvements after the finding set freezes.

## C1. Clean up temporary artifacts

Before committing, remove only temporary artifacts created for the task outside the target repositories. Every removal is Tier 1 and requires explicit confirmation, except manifest-proven owned scratch (below).

Owned task scratch is created and tracked by `skills/task/scripts/TaskScratch.fsx` under the machine-local canonical root `<temp>/opencode/tasks/<TASK-ID>/<RUN-ID>/`. It is the sole bounded deletion surface:

- Promote durable evidence first (`promote` copies and byte-verifies into the current task's `docs/` or `scripts/`).
- `seal` records that no active dependency remains; run it only after closeout review and verification complete.
- `clean` deletes only manifest-registered, non-promoted file entries from a sealed, valid root, without per-file confirmation, and reports everything retained. A malformed, mismatched, escaped, or reparse manifest fails closed and deletes nothing. On non-Windows platforms every helper operation fails closed and nothing is deleted.

**Remove:**

- Manifest-registered disposable owned scratch (`TaskScratch.fsx clean` after `seal`) — automatic, no per-file confirmation.
- Working files created for this task under the session scratchpad or a temp dir (probe scripts, intermediate data, one-off outputs) — explicit confirmation.

**Never remove:**

- Unknown, unregistered, promoted, reparse, or otherwise ambiguous scratch material — `clean` reports and retains it.
- Source-tree files, including debug logging and stray files in touched repositories. Clean these before Discovery so review covers the intended diff.
- The task's own `.tasks/<TASK-ID>/TASK.md`, `docs/`, and `scripts/` — these are intentional records.
- Anything already committed as part of the change.
- Files the user authored or edited by hand (confirm before deleting anything you didn't create).

## C2. Commit and publish

- Include C2 only when the task requires a commit or review artifact.
- Show the proposed commit message and wait for explicit confirmation before committing.
- Push and open the repo's normal review artifact (PR/MR) after user confirmation; link it from the task file.
- If a remote write's result is ambiguous (push, PR/MR, publication, deployment, tracker/comment, or other remote write), inspect the observable target before retrying per `references/confirmation-policy.md` (ambiguous remote effects); never retry blindly, which could duplicate remote effects.
