# Response and Continuity Rules

Load this file only when response formatting, evidence capture, noisy output, or handoff continuity matters for the current task.

## Compact Formats

- Progress: `Doing X. Found Y. Next Z.`
- Validation: `command -> result` plus relevant output only.
- Review finding: `file:line severity: problem. fix.`
- Task note: `state, evidence, next`.

## Evidence

- Keep chat summaries concise; do not dump long raw logs by default.
- Store full noisy logs only when useful, preferably under task-local `.tasks/<TASK-ID>/docs/` when a task exists.

## Continuity

- For complex multi-turn work without `.tasks`, create or update `HANDOFF.md` in the working directory when continuity would otherwise be lost.
- Include Summary, Decisions, Modified Files, Verified, and Next Steps.
- Treat `HANDOFF.md` as local working context unless the user explicitly asks to commit it.
