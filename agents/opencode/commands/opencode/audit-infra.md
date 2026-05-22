---
description: Audit local OpenCode infrastructure and suggest supported improvements.
agent: audit-infra
---

Audit the local OpenCode infrastructure and suggest improvements.

Scope: `AGENTS.md`, `opencode.json`, `agents/`, `commands/`, `skills/`, `scripts/`, and relevant project-local `.opencode/` files if present.

Classify recommendations as `portable`, `translated`, `unsupported`, or `risky`. Keep suggestions generic and OpenCode-supported. Default to read-only review; do not edit files unless the command arguments explicitly include apply/edit/update intent.

When command arguments include optimization or apply/edit/update intent, run this review flow. Perform step 5 only when apply/edit/update intent is explicit:

1. Identify target pipeline(s).
   - Prefer explicit targets from command arguments (skills, agents, commands, scripts, MCP configs, `AGENTS.md`, `opencode.json`, or project-local `.opencode/` docs).
   - If ambiguous, use the narrowest reasonable assumption and state it.
2. Read source material first.
   - Accept pasted text, local paths, URLs, repositories, and examples of good/bad behavior.
   - If a URL/repo is referenced but not pasted, ask for explicit confirmation before fetching current source.
   - Do not fetch private/authenticated sources or send local context externally without explicit approval.
   - Preserve source paths/links in notes.
3. Extract and classify useful ideas.
   - Capture behavior, constraints, prompt/command patterns, validation tactics, and context-compression ideas.
   - Classify each idea as `portable`, `translated`, `unsupported`, or `risky`.
4. Map ideas to OpenCode-supported surfaces.
   - Skills for recurring procedures.
   - Agents for specialized task behavior.
   - `AGENTS.md` for startup/session defaults.
   - `opencode.json` for permissions, instructions, and MCP config.
   - Commands for repeatable workflow shortcuts.
5. Edit narrowly when edit/apply intent is explicit.
   - Update named target pipeline(s) first.
   - Avoid broad/global rewrites unless requested and clearly justified.
   - Preserve exact technical artifacts (paths, commands, placeholders, API names, env vars, versions).
6. Validate lightly.
   - If files were edited, run `git diff --check`.
   - Verify structural integrity for reviewed or touched files (frontmatter/config keys/placeholders).
7. Report mapping and outcome.
   - List accepted, translated, skipped ideas and why.
   - List changed files and validation results.

Guardrails:

- Never commit or push without explicit confirmation.
- Never perform external actions, including URL/repo fetches, without explicit confirmation for that action.
- Keep changes minimal, reviewable, and OpenCode-native.
- Avoid unsupported automations or hidden side effects.
- Keep chat concise; store noisy logs only when useful.

Arguments: $ARGUMENTS
