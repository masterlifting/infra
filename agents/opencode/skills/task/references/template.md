# Task Template

Load this file only when creating a new task. Fill in required fields: Brief Summary, Context, Requirements / Acceptance Criteria, and Subtasks.

Drop rules at generation time:

- Non-code tasks (pure analysis, docs-only): drop subtask 3 (Design gate) and closing step C0.
- No commit/publish expected: drop C2, C3, and C4.

```markdown
# TASK-ID - Task Title

**Progress: 0/N subtasks complete** | **Status: In Progress** | **Created: YYYY-MM-DD**

## Brief Summary

(1-3 sentences describing what should work when complete)

## Continuity

<!-- Optional: instructions to run every time this task is loaded. Delete this section if not needed. -->

## Context

- Target repo(s):
  - `./repo-name` (branch: TBD after research) - why this repo is involved
  - `./other-repo` (branch: TBD after research) - why this repo is involved
- Problem / opportunity:
- Constraints:
- Related links/issues:

## Key Files

<!-- Map important files, configs, docs, and scripts as they are discovered. -->

| Purpose               | Path                             |
| --------------------- | -------------------------------- |
| (Main implementation) | `service/path/to/File.cs`        |
| (Config)              | `service/path/to/Config.cs`      |
| (Tests)               | `service/tests/path/to/Tests.cs` |

## Requirements / Acceptance Criteria

- (Build/test requirement)
- (Functional behavior requirement)
- (Safety/security/regression requirement)

## Non-Goals

- (Explicitly out of scope)
- (Explicitly out of scope)

## References

<!-- Keep links to external docs, analysis notes, tickets, and generated artifacts here. -->

- Ticket/spec: (link, if applicable)
- Notion/Drive/GitHub: (link, if applicable)
- Analysis docs: `.tasks/{TASK-ID}/docs/...`

## Subtasks

### 1. Research and define approach

Steps:

- [ ] Investigate relevant code paths and document findings
      Summary:
- [ ] Define expected behavior and constraints
      Summary:
- [ ] Confirm approach with the user
      Summary:

### 2. Clarify gaps before implementation

After subtask 1 research, surface clarification questions before implementation.
This subtask blocks implementation until all material questions are resolved or
the user explicitly accepts the uncertainty.

Steps:

- [ ] Compile clarification questions from research, acceptance criteria, edge cases, missing inputs/outputs, or conflicting constraints
      Summary:
- [ ] Post the full question list in chat and capture answers in `## Decisions` or `## Open Questions`
      Summary:
- [ ] Mark unresolved blockers with `[blocked]` notation and set `Status: Blocked` while waiting
      Summary:
- [ ] Confirm with the user that implementation can proceed
      Summary:

### 3. Design gate

Blocks implementation subtasks until the gate is clean or explicitly waived
(see `references/agent-gates.md`).

Steps:

- [ ] Design gate: language-matching architect verdict recorded
      Summary:
- [ ] Conditional gates run (sql-reviewer for schema/migrations; security rule loaded for sensitive surfaces) or explicitly not applicable
      Summary:

### 4. Branch setup across touched repos

- Branch format: `{TASK-ID}-description` when the repo does not define another format.
- Create or switch branches only after research identifies the repos that will actually be touched.
  Steps:
- [ ] Create or switch to a working branch in each touched repo
      Summary:
- [ ] Update `## Context` target repo branch entries with the selected branch names
      Summary:

### 5. Implement and validate

Steps:

- [ ] Add or update implementation code
      Summary:
- [ ] Unit tests added or updated and passing
      Summary:
- [ ] Integration tests added or updated and passing (if applicable)
      Summary:

## Closing Steps

<!-- Re-check this section after each task-specific subtask. If new work lands
     after a C-step was checked, treat that check as stale and re-run it. -->

### C0. Pre-commit review board

- Run `reviewer-1` and `reviewer-2` of the language-matching team in parallel on the full diff before any commit (see `references/agent-gates.md`).
  Steps:
- [ ] Review board verdicts recorded; Critical/Error findings fixed or waived in `## Decisions`
      Summary:
- [ ] Affected reviewers re-run after fixes changed the diff
      Summary:

### C1. Add or update documentation

- Update user-facing or developer docs when the change affects behavior,
  operations, architecture, or validation.
- If the repo has an established changelog, release-note, or tester-instruction
  format, use that format instead of creating a new one.
  Steps:
- [ ] Documentation updated or explicitly not needed
      Summary:
- [ ] Tester or reviewer instructions captured when relevant
      Summary:

### C2. Commit and publish (optional)

- Commit implementation changes after user confirmation.
- Push and open the repo's normal review artifact after user confirmation.
  Steps:
- [ ] Changes committed in target repo(s)
      Summary:
- [ ] Review artifact created and linked
      Summary:

### C3. Validate CI pipeline (optional)

- Validate the relevant CI or local equivalent after a review artifact exists.
- If failures are infrastructure-only, retry and re-validate.
- If failures are caused by scripts or code, investigate, fix, commit, push,
  and re-validate after user confirmation.
  Steps:
- [ ] Pipeline or local equivalent validated
      Summary:
- [ ] Script or code failures resolved (if any)
      Summary:

### C4. Resolve review threads (optional)

- Fetch unresolved review threads when the repo has a review artifact.
- Present open threads to the user and ask which to address.
- For each approved thread fix: read the file, explain the concern, propose
  the change, and wait for confirmation before implementing.
  Steps:
- [ ] Open threads reviewed with user
      Summary:
- [ ] Agreed fixes applied and pushed
      Summary:
- [ ] No open threads remaining, or the user explicitly declined the remaining items
      Summary:

## Decisions

| Date | Decision | Rationale |
| ---- | -------- | --------- |
|      |          |           |

## Open Questions

- (Any uncertainties or decisions that still need resolution)
- (Mark resolved questions using `- ~~[x] Question text~~`)

## Notes

- State:
- Evidence:
- Next:
```
