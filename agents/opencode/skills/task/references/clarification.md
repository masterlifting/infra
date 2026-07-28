# Clarification Procedure

After research, surface material gaps before implementation code is written.

## Chat Gate

1. Compile clarification questions from research:
   - Ambiguous acceptance criteria.
   - Undefined edge cases.
   - Unclear scope.
   - Missing inputs or outputs.
   - Conflicting constraints.
2. Post the full list in chat. The user may answer some directly.
3. Record resolved answers in `## Decisions` with today's date.
4. Note remaining items in `## Open Questions`.
5. Mark unresolved blockers with `[blocked]` notation and set `Status: Blocked` while waiting.
6. Refine the draft delivery and validation subtasks or steps from the resolved answers and accepted uncertainties.
7. Resume implementation or execution only after all material questions are resolved or the user explicitly accepts the uncertainty and, for code tasks, the design gate approves the final task-specific structure.

## Skip Rules

- No open questions after research: mark the clarification subtask complete in one pass.
- Repo-local tracker workflow exists: follow that workflow only after the user provides enough context and any required connector is available.
- External comments or tracker updates require explicit confirmation of the exact text before sending.
