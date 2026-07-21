---
description: Run the local YouTrack helper for read and confirmed write operations.
agent: executor
---

Use the local YouTrack helper for this request.

First, load and follow the `youtrack` skill. Then interpret `$ARGUMENTS` as helper arguments for:

`dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\YouTrackRest.fsx" -- $ARGUMENTS`

Execution rules:
- Before every helper invocation, including `me`, `search`, `get`, and read-back verification, restate the exact operation, target, and outbound request data and wait for explicit confirmation for that specific external request.
- Confirm authentication with `me` before the first real operation when identity is not already verified in the current task; this request requires its own confirmation.
- Treat `search`, `get`, `me`, and explicit `request GET ...` calls as read operations.
- Before any write operation, restate the exact target and change and wait for explicit confirmation for that specific write.
- After a write, obtain separate confirmation before reading back the changed issue or returned fields, then summarize the result concisely.
- Never print or store tokens in repo files, task files, or final answers.

Examples:
- `/opencode:youtrack me`
- `/opencode:youtrack search "for: me #Unresolved" --top 20`
- `/opencode:youtrack get PROJ-123`

Arguments: $ARGUMENTS
