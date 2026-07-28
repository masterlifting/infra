---
description: Run the local YouTrack helper for read and confirmed write operations.
agent: executor
---

Use the local YouTrack helper for this request.

First, load and follow the `youtrack` skill. Then interpret `$ARGUMENTS` as helper arguments for:

`dotnet fsi "C:/Users/andre/.config/opencode/skills/youtrack/scripts/YouTrackRest.fsx" -- $ARGUMENTS`

Follow all confirmation and verification gates from the `youtrack` skill.

Examples:
- `/opencode/youtrack me`
- `/opencode/youtrack search "for: me #Unresolved" --top 20`
- `/opencode/youtrack get PROJ-123`
