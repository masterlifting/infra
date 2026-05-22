---
description: Generate a dated model inventory and routing table for connected OpenCode providers.
agent: executor
---

Generate an OpenCode model advisor report for the connected providers.

Run `dotnet fsi "$env:USERPROFILE\.config\opencode\commands\opencode\scripts\AuditModels.fsx" -- $ARGUMENTS`. The script prints the temporary Markdown report path. Read the report path it prints, then summarize the top routing recommendations and where the table was written.

Use `$ARGUMENTS` for options. Supported options: `--refresh` to refresh OpenCode model metadata before generating the table, `--included-providers=...`, `--avoid-providers=...`, and `--priority-models=...` to adapt routing without editing code.

Keep these report caveats in mind when summarizing results:
- OpenCode may report zero cost for some subscription-backed providers; treat configured included providers as `included` rather than generic free APIs.
- Ratings are for routing work, not absolute benchmark truth; validate important agent changes with real tasks.
- Use `--refresh` when you want OpenCode to refresh its model metadata cache before generating the table.

Do not read auth secret files. Do not edit agent, skill, command, or rule models unless the user explicitly asks for a specific follow-up change.

Arguments: $ARGUMENTS
