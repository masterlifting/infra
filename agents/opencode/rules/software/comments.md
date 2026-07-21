# Code Comment Rules

Scope: source and test code written or changed by implementation and testing agents.

- Add comments only where intent, invariants, constraints, failure behavior, or a surprising tradeoff is not clear from the code itself.
- Keep comments short and information-dense. Explain why the code exists or why the obvious approach is unsafe; do not narrate what each statement does.
- Follow the language and repository's established comment or documentation format, capitalization, punctuation, and placement.
- Prefer clearer names and smaller code over explanatory comments when the code can be made self-explanatory.
- Do not add section banners, change-log narration, redundant comments, commented-out code, or TODOs without concrete context.
- Update or remove nearby comments when behavior changes so they cannot become misleading.
