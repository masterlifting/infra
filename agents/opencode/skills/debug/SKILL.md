---
name: debug
description: Use when debugging non-trivial failures, defects, intermittent behavior, or issues whose root cause is unknown and needs systematic analysis. Do not use for trivial one-line fixes where the cause is already established.
---

# Debug

## Purpose

Provide a systematic debugging procedure that prevents speculative patch stacking. Change production behavior only after evidence identifies the likely root cause.

## Guardrails

- Do not modify production code merely because a fix appears plausible.
- Distinguish `symptom`, `observation`, `hypothesis`, `experiment`, `root cause`, `fix`, and `verification`. Do not conflate them.
- No universal TDD: choose the smallest reliable verification mechanism for the failure.
- Do not impose an automatic architecture review after every failed attempt. Escalate to an architect or specialist only when evidence justifies it.
- Preserve all confirmation boundaries and secret-handling restrictions.
- If the failure cannot be reproduced or the root cause cannot be established, stop and report `BLOCKED` with the evidence collected; do not stack patches.

## Workflow

```text
Reproduce
    ↓
Collect evidence
    ↓
Form root-cause hypothesis
    ↓
Run minimal falsification experiment
    ↓
Refine or reject hypothesis
    ↓
Add regression test when appropriate
    ↓
Apply smallest sufficient fix
    ↓
Verify
```

1. **Reproduce** - establish the observed failure with the exact input, steps, and environment.
2. **Collect evidence** - gather evidence around the failing boundary: logs, traces, state dumps, input/output pairs, environment facts.
3. **Form a hypothesis** - state an explicit, testable root-cause hypothesis.
4. **Run a minimal falsification experiment** - prefer the smallest experiment that can disprove the hypothesis. Do not begin a second hypothesis until the first is tested.
5. **Refine or reject** - update the hypothesis from experiment results. If several materially different hypotheses fail, stop and reassess assumptions, architecture, external contracts, concurrency/state model, environment/configuration, and whether the symptom is downstream of the true defect.
6. **Add a regression test when appropriate** - reproduce the defect with a failing regression test before or alongside the fix when practical. When it is not practical, record the reason and use the smallest reliable verification (integration test, reproducer script, log/trace evidence, environment validation, or manual verification).
7. **Apply the smallest sufficient fix** - change production behavior only after sufficient evidence identifies the likely cause.
8. **Verify** - confirm the fix against the reproduced failure and the regression coverage.

## Output

Report the observed failure, evidence, hypothesis, experiment, root cause, fix, and verification. If no root cause was established, report `BLOCKED` with evidence and the next candidate investigation, without speculative patches.

For test design and execution semantics, follow `@C:/Users/andre/.config/opencode/rules/software/testing.md`.
