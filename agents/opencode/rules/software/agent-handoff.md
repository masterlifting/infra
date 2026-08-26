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
- Batch TASK.md state coherently: recompute then validate each batch, and keep
  the durability boundary explicit — validated TASK.md facts are durable, while
  transient scratch state is not.
- Dispatch only already-required independent waves in parallel (architecture
  proposals, independent implementation slices, Discovery reviewers, independent
  remediation items, targeted Verification); keep dependent chains ordered.
- Do not add ceremony for trivial one-shot work.

## Engineer ownership

Language testers own applicable test design, implementation, and execution
(see `@C:/Users/andre/.config/opencode/rules/software/testing.md`); engineers
do not run tests. Language and specialist `engineer` agents own production implementation and
the single build point, plus implementation-native plan, static, and
configuration validation. Return implementation and build results
to the coordinator; if no project or compile surface exists, record build as
not applicable. When no applicable language tester exists, the implementation
owner owns the required tests for its assigned surface.

Engineers must not independently redesign a frozen solution. If implementation
encounters a hard invalidation condition (the design cannot satisfy an
acceptance criterion, contains a blocking correctness/security/data-integrity
defect, materially misunderstands a required external contract, or is
technically impossible under an approved constraint), return `BLOCKED` to the
coordinator with evidence and stop; do not redesign.

If the assigned production provider is unavailable or quota-exhausted, return
control to the coordinator or user. Do not automatically substitute another
paid provider.

## Subagent responsibilities

- Load and follow the handoff given by the coordinator, plus any rule files
  named in it.
- Return the required output contract. If the assignment cannot be completed
  because required evidence or context is missing, return `BLOCKED` with the
  exact gap instead of improvising.
- Read only the named artifacts unless the objective requires more; do not
  load unrelated context.
- Engineers follow the engineer-ownership invariant above, including the
  no-automatic-paid-fallback rule.

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
