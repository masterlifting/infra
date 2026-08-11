# Clarification Procedure

After research, classify uncertainty before implementation code is written.

## Classification

- `BLOCKING`: unresolved input can materially change observable behavior, architecture, acceptance criteria, public contracts, data integrity, security, or required inputs/outputs. Ask the user and stop affected work.
- `ASSUMPTION`: resolve from explicit requirements, existing contracts/code, repository conventions, then the simplest reversible and least-surprising behavior. Record meaningful assumptions.
- `NON-BLOCKING`: does not materially affect delivery. Resolve without interrupting execution.

## Chat Gate

1. Classify each gap as `BLOCKING`, `ASSUMPTION`, or `NON-BLOCKING`.
2. Ask only the full set of `BLOCKING` questions in chat.
3. Record answers and meaningful assumptions in `## Decisions`; keep unresolved blockers in `## Open Questions`.
4. Mark unresolved blockers with `[blocked]` notation and set `Status: Blocked` while waiting.
5. Refine delivery and validation work from resolved answers and accepted assumptions.
6. Resume affected work when blockers are resolved or explicitly accepted and, for code tasks, the solution is frozen.

## Skip Rules

- No open questions after research: mark the clarification subtask complete in one pass.
- Repo-local tracker workflow exists: follow that workflow only after the user provides enough context and any required connector is available.
- External comments or tracker updates require explicit confirmation of the exact text before sending.
