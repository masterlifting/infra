# YouTrack REST Patterns

Authoritative docs:

- REST overview: https://www.jetbrains.com/help/youtrack/devportal/youtrack-rest-api.html
- URL and endpoints: https://www.jetbrains.com/help/youtrack/devportal/api-url-and-endpoints.html
- Request headers: https://www.jetbrains.com/help/youtrack/devportal/yt-api-headers.html
- Resources reference: https://www.jetbrains.com/help/youtrack/devportal/api-resources.html

## Base URL

The REST API base is hardcoded in the F# helper:

```text
https://gizmopowered.myjetbrains.com/youtrack/api
```

Configured service URL:

- `https://gizmopowered.myjetbrains.com/youtrack/api`

Other examples:

- `https://example.youtrack.cloud/api`
- `https://example.myjetbrains.com/youtrack/api`
- `https://youtrack.example.com/api`

## Headers

Use these headers for JSON calls:

```text
Authorization: Bearer <token>
Accept: application/json
Content-Type: application/json
```

`Content-Type` is only required for calls with a JSON body, but including it consistently is acceptable for PowerShell helpers.

## PowerShell Helper Pattern

Prefer the bundled F# helper for routine calls:

```powershell
dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\youtrack.fsx" -- me
dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\youtrack.fsx" -- search "for: me #Unresolved" --top 20
dotnet fsi "$env:USERPROFILE\.config\opencode\skills\youtrack\scripts\youtrack.fsx" -- request GET "/users/me?fields=id,login,fullName,email"
```

Use this direct PowerShell pattern only when the helper does not cover the operation:

```powershell
$baseUrl = "https://gizmopowered.myjetbrains.com/youtrack"
$token = [Environment]::GetEnvironmentVariable("YOUTRACK_API", "Process")
if ([string]::IsNullOrWhiteSpace($token)) {
  $token = [Environment]::GetEnvironmentVariable("YOUTRACK_API", "User")
}
$headers = @{
  Authorization = "Bearer $token"
  Accept = "application/json"
}

Invoke-RestMethod `
  -Method Get `
  -Uri "$baseUrl/api/users/me?fields=id,login,fullName,email" `
  -Headers $headers
```

For JSON body calls:

```powershell
$body = @{
  summary = "Example issue"
  description = "Created through the YouTrack REST API."
  project = @{ id = "0-0" }
} | ConvertTo-Json -Depth 8

Invoke-RestMethod `
  -Method Post `
  -Uri "$baseUrl/api/issues?fields=idReadable,summary" `
  -Headers ($headers + @{ "Content-Type" = "application/json" }) `
  -Body $body
```

## Authentication Check

Run this before other operations:

```text
GET /api/users/me?fields=id,login,fullName,email
```

If it fails with unauthorized/forbidden, stop and ask the user to fix the token or permissions.

## Issue Search

Endpoint:

```text
GET /api/issues?query={encoded-query}&fields={fields}&$top={n}&$skip={n}
```

Useful fields:

```text
id,idReadable,summary,description,project(shortName,name),updated,customFields(name,value(name,login,fullName,isResolved))
```

Use `$top` and `$skip`; most collection resources have default limits.

## Issue Read And Update

Read:

```text
GET /api/issues/{issueID}?fields=id,idReadable,summary,description,project(shortName,name),customFields(name,value(name,login,fullName,isResolved)),comments(text,author(login,fullName),created)
```

Update:

```text
POST /api/issues/{issueID}?fields=idReadable,summary,customFields(name,value(name,login,fullName,isResolved))
```

Issue IDs can usually be the readable ID such as `PROJ-123` or the database ID such as `2-24`.

When updating custom fields, preserve the exact field `id`, `name`, and `$type` shape observed from a read or from project metadata.

## Issue Creation

Endpoint:

```text
POST /api/issues?fields=idReadable,summary
```

Required body fields:

```json
{
  "summary": "Issue title",
  "description": "Issue details",
  "project": { "id": "0-0" }
}
```

Read project metadata first if only a project short name is known.

## Comments

Endpoint:

```text
/api/issues/{issueID}/comments
```

Use comments for durable notes, implementation summaries, or links back to local work. Keep comments concise and avoid leaking local paths, secrets, or private logs unless the user explicitly wants that material in YouTrack.

## Commands

Use the Commands resource for command-style operations such as changing state, adding tags, or assigning users when that matches the team workflow better than custom-field JSON. Read back the issue after applying a command.

## Pagination

For most collection reads, use:

```text
$top=50&$skip=0
```

Continue with `$skip=50`, `$skip=100`, and so on only when the previous page is full and the user needs the full set.
