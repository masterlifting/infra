# Confirmation Policy

Replaces the flat "confirm before each subtask" rule. Tiered by risk and reversibility. The global hard guardrails in `AGENTS.md` (explicit confirmation for external sends and commits) always apply on top of this policy.

## Tier 1 — Always confirm before acting

- Creating or switching branches
- Committing, pushing, force-pushing
- Creating/updating/merging PRs or MRs
- Changing task `Status` to `Complete`
- Destructive ops (`reset --hard`, deleting branches, `git clean`)
- Sending data to external services (trackers, chat, wikis, secret stores)

## Tier 2 — Auto-proceed (no confirmation)

- Build via the team's engineer agent; test via the team's tester agent
- Metadata-only git commands explicitly allowed by global configuration (`git status`, branch identity, remotes, revisions, tree/file names)
- File reads, grep, glob
- Automatic task-progress synchronization through the global plugin; manual F# helper invocations follow the configured bash permission prompt (task and audit scripts are auto-allowed; document conversion requires confirmation)
- Updating the task file's progress counter / checkbox state for already-completed work
- Spawning architect and reviewer gates per `references/agent-gates.md`
- Assigning in-scope implementation, build, and test work to the owner agents

## Tier 3 — Brief surface, no wait

When continuing obviously-related work the user has already approved (e.g., applying the next file in a batch the user OK'd):

- Announce in one short sentence what you're about to do.
- Proceed immediately. Do not wait for "go ahead".
- Stop and confirm if anything unexpected appears (build break, type error, missing file).

## Tier 4 — External comment text

Tier 1 special case. Always confirm the **exact text** of any comment posted to an external system (tracker, chat, review thread) before posting. Never post on the user's behalf without reading the draft back first.

## Stricter per-turn policy

The user may require confirmation for additional Tier 2 or Tier 3 actions. Tier 1 and global confirmation gates cannot be waived by a general instruction such as "continue" or "do everything"; confirmation must name the specific gated action.

## Ambiguous remote effects

If a remote write's result is ambiguous (push, PR/MR, publication, deployment, tracker/comment, or other remote write), inspect the observable target (remote refs, PR/MR list, published artifact, deployment state, tracker issue/comment) before retrying. Never retry blindly — retrying without observation could duplicate remote effects. Do not keep a durable effect journal; observe the target each time.
