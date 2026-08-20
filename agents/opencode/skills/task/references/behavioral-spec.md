# Optional Behavioral Specification

Canonical convention for `.tasks/{TASK-ID}/SPEC.md`.

`TASK.md` remains the lifecycle and state authority. `SPEC.md` is an optional
artifact for tasks whose requirements are easier to express as observable
behavior than as implementation description.

## When to create

Create `SPEC.md` only when the task materially involves one or more of:

- user-facing behavior;
- business rules;
- public/API contracts;
- state transitions;
- multiple behavioral scenarios;
- important edge cases;
- requirements whose meaning is clearer when expressed independently of implementation.

Do not create it for ordinary engineering tasks. A trivial task must not acquire
specification ceremony.

## What it contains

`SPEC.md` describes **what the system must do**, not how it is implemented.

Compact structure:

```markdown
# Behavioral Specification

## Requirement: <name>

<Normative requirement>

### Scenario: <name>

Given ...
When ...
Then ...
```

- Requirements may use normative language such as `MUST`, `SHALL`, or equivalent.
- Each `## Requirement:` is a normative statement.
- Each `### Scenario:` under a requirement demonstrates observable behavior.
- Include explicit non-goals or boundary notes only where they remove ambiguity.

## What it must not contain

- Implementation details, file layouts, or chosen solutions.
- Proposals, plans, or change descriptions.
- A second task lifecycle or competing tracking structure.
- Duplication of the full specification inside `TASK.md`.

## Integration with `TASK.md`

- Reference `SPEC.md` from the `## References` section of `TASK.md`:

  `- Behavioral specification: .tasks/{TASK-ID}/SPEC.md`

- Derive relevant `Requirements / Acceptance Criteria` and the frozen Solution
  Contract from `SPEC.md`; they must conform to it.
- A conflict between the implementation or design and the behavioral
  specification is a blocking correctness problem and must be resolved before
  completion.
- If `SPEC.md` is referenced from `TASK.md`, it must exist and contain at least
  one `## Requirement:` section; a missing or empty specification is a
  deterministically detected defect.
- Do not restate the whole specification in `TASK.md`; reference it.
