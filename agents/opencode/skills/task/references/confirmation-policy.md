# Confirmation Policy

Replaces the flat "confirm before each subtask" rule. Tiered by risk and reversibility. The global hard guardrails in `AGENTS.md` (explicit confirmation for external sends and commits) always apply on top of this policy.

## Tier 1 — Always confirm before acting

- Creating or switching branches
- Committing, pushing, force-pushing
- Creating/updating/merging PRs or MRs
- Invoking any provider-B agent, including `reviewer-2`, for the specific assigned context
- Changing task `Status` to `Complete`
- Any mutating step in **Closing Steps C0-C2**
- Destructive ops (`reset --hard`, deleting branches, `git clean`)
- Sending data to external services (trackers, chat, wikis, secret stores)

## Tier 2 — Auto-proceed (no confirmation)

- Build via the team's engineer agent; test via the team's tester agent
- Read-only git (`git status`, `git diff`, `git log`, `git branch --list`)
- File reads, grep, glob
- Automatic task-progress synchronization through the global plugin; manual F# helper invocations still follow the configured bash permission prompt
- Updating the task file's progress counter / checkbox state for already-completed work
- Spawning provider-A architect and reviewer gates per `references/agent-gates.md`
- Assigning in-scope implementation, build, and test work to the owner agents; provider-B invocation remains Tier 1

## Tier 3 — Brief surface, no wait

When continuing obviously-related work the user has already approved (e.g., applying the next file in a batch the user OK'd):

- Announce in one short sentence what you're about to do.
- Proceed immediately. Do not wait for "go ahead".
- Stop and confirm if anything unexpected appears (build break, type error, missing file).

## Tier 4 — External comment text

Tier 1 special case. Always confirm the **exact text** of any comment posted to an external system (tracker, chat, review thread) before posting. Never post on the user's behalf without reading the draft back first.

## Stricter per-turn policy

The user may require confirmation for additional Tier 2 or Tier 3 actions. Tier 1 and global confirmation gates cannot be waived by a general instruction such as "continue" or "do everything"; confirmation must name the specific gated action.
