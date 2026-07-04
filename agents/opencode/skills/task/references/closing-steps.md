# Closing Steps (C0–C4)

Run after the last task-specific subtask, in order. Re-check from C1 onward whenever new work lands after a C-step was checked — treat the earlier check as stale. Every C-step action is Tier 1 (confirm first), except spawning the C0 review agents, which is Tier 2.

## C0. Pre-commit review board

- Run `reviewer-1` and `reviewer-2` of the language-matching team **in parallel** on the full `git diff` before any commit.
- Conditional members per `references/agent-gates.md`: `architect` if boundaries changed, `database/sql-reviewer` if migrations present, `devops/reviewer` if pipelines or deploy configs changed.
- Critical/Error findings block the commit: fix and re-run the affected reviewer(s), or record an explicit user waiver in `## Decisions`.

## C1. Add or update documentation

- Update user-facing or developer docs when the change affects behavior, operations, architecture, or validation.
- If the repo has an established changelog, release-note, or tester-instruction format, use that format instead of creating a new one.

## C2. Commit and publish (optional)

- Show the proposed commit message and wait for explicit confirmation before committing.
- Push and open the repo's normal review artifact (PR/MR) after user confirmation; link it from the task file.

## C3. Validate CI pipeline (optional)

- Validate the relevant CI pipeline or local equivalent after a review artifact exists.
- Infrastructure-only failures: retry and re-validate.
- Script/code failures: investigate, fix, commit, push, and re-validate after user confirmation.

## C4. Resolve review threads (optional)

- Fetch unresolved review threads when the repo has a review artifact.
- Present open threads to the user and ask which to address.
- For each approved thread fix: read the file, explain the concern, propose the change, and wait for confirmation before implementing.
- Done when no open threads remain or the user explicitly declines the remaining items.
