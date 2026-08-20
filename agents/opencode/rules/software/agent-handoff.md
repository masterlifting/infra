# Agent Handoff Contract

Canonical source for coordinator-to-subagent handoffs. Shared by all
coordinatable agents (executor, engineers, testers, architects, reviewers, and
specialists). Reference this rule instead of duplicating its content.

## Purpose

Give each subagent the smallest sufficient context, keep coordinator prompts
thin, and make outputs structured enough for coordinator synthesis.

## Input envelope

A handoff should normally contain:

1. **Objective** - the bounded question or deliverable.
2. **Relevant constraints** - applicable confirmation boundaries, secret
   handling, destructive-operation limits, and role ownership (e.g., who owns
   builds vs. tests).
3. **Exact artifacts/paths** - the bounded set of files, rules, and references
   to inspect or change.
4. **Relevant frozen decisions** - the frozen solution, accepted findings, or
   contracts the work must conform to.
5. **Required output contract** - verdict/status, report or artifact path,
   material findings, evidence, and unresolved risks/blockers.

Do not send:

- whole session transcripts;
- unrelated task history;
- entire repositories when a bounded set of paths is sufficient;
- findings from other independent reviewers when independence is required.

For large inputs or outputs, prefer file-based artifacts and return their paths.

## Coordinator responsibilities

- Keep prompts thin; do not duplicate requirements that already exist in the
  canonical task, specification, or rule artifact. Reference the artifact.
- Match the smallest sufficient agent for the work.
- Use fresh/isolated subagent context where supported and materially beneficial
  (e.g., independent architecture proposals, reviewer Discovery, behavioral
  evaluation).
- Do not add ceremony for trivial one-shot work.

## Subagent responsibilities

- Load and follow the handoff given by the coordinator, plus any rule files
  named in it.
- Return the required output contract. If the assignment cannot be completed
  because required evidence or context is missing, return `BLOCKED` with the
  exact gap instead of improvising.
- Read only the named artifacts unless the objective requires more; do not
  load unrelated context.

## Reviewer independence

Reviewers receive the same frozen baseline and evidence, plus their assigned
mandate. Do not provide reviewers each other's findings during Discovery.
Independent reviewers must not coordinate with one another.

## Output shape

Prefer a compact, structured result:

```text
verdict/status
report or artifact path (when a detailed report was written)
material findings
unresolved risks/blockers
```
