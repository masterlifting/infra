# Closing Steps

Run after implementation, build, and tests. Review state follows `agent-gates.md`; cleanup, commits, publishing, and other explicitly gated actions retain their confirmation requirements. Before setting `Status: Complete`, make `## Notes` terminal-current: `State`/`Evidence` reflect the final outcome and `Next` states explicitly that no task work remains, or lists only real manual/optional follow-up (e.g., a known dependent record to refresh — never mutate it).

## C0. Discovery and Verification

- Run the frozen review profile independently and in parallel against the same baseline.
- Triage once, freeze the accepted finding set, remediate it, and run targeted Verification.
- Permit at most one additional targeted remediation and Verification pass under `agent-gates.md`.
- Do not restart Discovery or pursue unrelated improvements after the finding set freezes.

## C1. Clean up temporary artifacts

Before committing, remove only temporary artifacts created for the task outside the target repositories. Every removal is Tier 1 and requires explicit confirmation.

**Remove:**

- Working files created for this task under the session scratchpad or a temp dir (probe scripts, intermediate data, one-off outputs).

**Never remove:**

- Source-tree files, including debug logging and stray files in touched repositories. Clean these before Discovery so review covers the intended diff.
- The task's own `.tasks/<TASK-ID>/TASK.md`, `docs/`, and `scripts/` — these are intentional records.
- Anything already committed as part of the change.
- Files the user authored or edited by hand (confirm before deleting anything you didn't create).

## C2. Commit and publish

- Include C2 only when the task requires a commit or review artifact.
- Show the proposed commit message and wait for explicit confirmation before committing.
- Push and open the repo's normal review artifact (PR/MR) after user confirmation; link it from the task file.
- If a remote write's result is ambiguous (push, PR/MR, publication, deployment, tracker/comment, or other remote write), inspect the observable target before retrying per `references/confirmation-policy.md` (ambiguous remote effects); never retry blindly, which could duplicate remote effects.
