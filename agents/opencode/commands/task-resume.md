---
description: Load a .tasks item and summarize progress, blockers, and next steps.
agent: build
---

Resume the task identified by `$ARGUMENTS`.

If `$ARGUMENTS` is empty, ask for the task ID or path. Read `.tasks/<TASK-ID>/TASK.md` or the provided task path. Surface the progress line, status, target repos/branches, incomplete subtasks, open questions, verification state, and the next pending decision.

Do not implement a subtask until the user explicitly approves the specific subtask plan.
