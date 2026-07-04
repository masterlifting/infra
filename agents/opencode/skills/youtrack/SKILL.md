---
name: youtrack
description: Run when the user mentions YouTrack issues/tasks, YouTrack REST, tracker sync, or asks to search/read/create/update/comment YouTrack items or check YouTrack project/user metadata. Use YouTrack REST API flows from OpenCode; do not use when no YouTrack operation is involved.
---

# YouTrack REST

## Purpose

Use this skill to operate YouTrack through its REST API with explicit, auditable HTTP requests. Prefer the bundled F# helper for routine calls.

## Guardrails

- Do not write tokens into repo files, task files, command history summaries, or final answers.
- Require explicit user confirmation for each write operation to YouTrack (create, update, comment, command execution, delete), even if the user gave a general "go ahead" earlier.
- Confirm destructive operations (for example deletion) before executing.
- Treat issue visibility, comments, attachments, tokens, and user emails as sensitive.
- Keep requests scoped with explicit `fields` and narrow query parameters.

## Configuration

Require these runtime values before making calls:

- Base URL: set `YOUTRACK_BASE_URL` when needed. If unset, helper defaults to `https://gizmopowered.myjetbrains.com/youtrack`.
- `YOUTRACK_API`: permanent token with the minimum YouTrack permissions needed for the task. Read it from the process environment first, then the Windows user environment.
- Optional expected user account: set `YOUTRACK_EXPECTED_LOGIN` to enforce account checks. If unset, verify `me` before writes.

Never write tokens into repo files, task files, command history summaries, or final answers. Prefer environment variables:

```powershell
$env:YOUTRACK_API = "<token>"
```

## Workflow

1. Read `skills/youtrack/references/rest-patterns.md` before constructing requests.
2. Use `skills/youtrack/scripts/YouTrackRest.fsx` for routine REST calls; use direct HTTP only when the helper does not cover the operation.
3. Verify authentication with `GET /api/users/me?fields=id,login,fullName,email` before the first real operation.
4. Use explicit `fields` parameters on every request so responses stay small and predictable.
5. For issue search, URL-encode the `query` value and include `$top`/`$skip` for pagination.
6. For writes, read the current issue first unless the task is creating a new issue.
7. Before any write operation, restate the exact action and target issue(s) and wait for explicit confirmation.
8. After a create/update/comment/command call, read back the changed issue or response fields and report the resulting `idReadable`, summary, and changed fields.

## Output

- Return concise, auditable results with request intent, key response fields, and resulting issue IDs/summaries.
- For writes, include the before/after change summary and any follow-up confirmation gate.

## F# Helper

Run through `dotnet fsi`:

```powershell
dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\YouTrackRest.fsx" -- --help
dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\YouTrackRest.fsx" -- me
dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\YouTrackRest.fsx" -- search "for: me #Unresolved" --top 20
dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\YouTrackRest.fsx" -- get PROJ-123
```

The helper supports `YOUTRACK_BASE_URL` override, defaults the base URL when unset, reads `YOUTRACK_API`, prints JSON, and keeps token handling out of repo files.

## Common Tasks

- Search issues: `GET /api/issues?query=...&fields=id,idReadable,summary,project(shortName,name),customFields(name,value(name,login,fullName,isResolved))&$top=20`.
- Read one issue: `GET /api/issues/{issueID}?fields=id,idReadable,summary,description,project(shortName,name),customFields(name,value(name,login,fullName,isResolved)),comments(text,author(login,fullName),created)`.
- Create issue: `POST /api/issues?fields=idReadable,summary` with `summary` and `project.id` in the JSON body.
- Update issue fields: `POST /api/issues/{issueID}?fields=idReadable,summary,customFields(name,value(name,login,fullName,isResolved))`.
- Add comment: `POST /api/issues/{issueID}/comments?fields=id,text,author(login,fullName),created`.
- Apply a YouTrack command: use the Commands resource when changing state, assignee, tags, or other command-style updates.

## Safety (Detailed)

- Treat issue visibility, comments, attachments, tokens, user emails, and private project metadata as sensitive.
- Avoid broad reads unless the user asks for a report; prefer a narrow query and `$top`.
- Do not use `muteUpdateNotifications=true` unless the user explicitly asks and the token has the required permission.
- Do not guess custom field IDs. Read the issue or project field metadata first, then reuse exact names, IDs, and `$type` values.
