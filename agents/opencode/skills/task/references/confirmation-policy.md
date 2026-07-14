# Confirmation Policy

Replaces the flat "confirm before each subtask" rule. Tiered by risk and reversibility. The global hard guardrails in `AGENTS.md` (explicit confirmation for external sends and commits) always apply on top of this policy.

## Tier 1 — Always confirm before acting

- Creating or switching branches
- Committing, pushing, force-pushing
- Creating/updating/merging PRs or MRs
- Any step in **Closing Steps C0–C2** (spawning the C0 review agents themselves is Tier 2; acting on their findings is Tier 1)
- Destructive ops (`reset --hard`, deleting branches, `git clean`)
- Sending data to external services (trackers, chat, wikis, secret stores)

## Tier 2 — Auto-proceed (no confirmation)

- Build via the team's engineer agent; test via the team's tester agent
- Read-only git (`git status`, `git diff`, `git log`, `git branch --list`)
- File reads, grep, glob
- Automatic task-progress synchronization through the global plugin; manual F# helper invocations still follow the configured bash permission prompt
- Updating the task file's progress counter / checkbox state for already-completed work
- Spawning read-only agent gates (architect / reviewer-1 / reviewer-2 / sql-reviewer / tester) per `references/agent-gates.md` — the agents only read and report; applying their fixes follows Tier 1/3

## Tier 3 — Brief surface, no wait

When continuing obviously-related work the user has already approved (e.g., applying the next file in a batch the user OK'd):

- Announce in one short sentence what you're about to do.
- Proceed immediately. Do not wait for "go ahead".
- Stop and confirm if anything unexpected appears (build break, type error, missing file).

## Tier 4 — External comment text

Tier 1 special case. Always confirm the **exact text** of any comment posted to an external system (tracker, chat, review thread) before posting. Never post on the user's behalf without reading the draft back first.

## Override

User can override per-turn with phrases like:

- "just do all of these without asking" → temporarily promote Tier 1 → Tier 3 for the named batch.
- "stop and confirm everything" → demote Tier 2/3 → Tier 1 for the rest of the session.
