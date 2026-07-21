---
name: audit-session
description: Use only when auditing or improving the active OpenCode session, including workflow, tool calls, token use, delegation, reusable automation, and repeatable checks. Do not use for broad configuration or infrastructure review; use audit-infra instead.
---

# Audit Session

## Purpose

Route active-session workflow audits to the `audit-session` subagent. Keep the audit procedure and output contract authoritative in `agents/audit-session.md`.

## Guardrails

- Use `audit-infra` for broad infrastructure review rather than expanding the session audit.
- Pass only visible session evidence; never include secrets or private context not already needed for the active task.
- Preserve global confirmation gates. Recommendations do not authorize edits, installs, external research, or other side effects.

## Workflow

1. Capture the active goal, visible friction or repetition, constraints, approvals, blockers, and next pending step.
2. Delegate exactly once to the `audit-session` subagent with that bounded evidence. If already running as `audit-session`, execute the assigned audit instead of delegating again.
3. Relay the subagent result without expanding or reformatting it.

## Output

Return the agent's compact recommendations or three-line handoff. Do not apply recommendations unless the user separately requests implementation.
