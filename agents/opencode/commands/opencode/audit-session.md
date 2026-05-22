---
description: Review the active session and OpenCode setup for high-ROI workflow improvements.
agent: audit-infra
---

Review the active session and the relevant OpenCode surfaces. Return a short executor-ready optimization brief without derailing the user's main task.

Read only what is relevant:

- Session context: user goals, repeated friction, misses, unresolved decisions, noisy context, token-heavy patterns, and handoff needs.
- Local repo: `AGENTS.md`, `.opencode/README.md`, `.opencode/**`, commands, scripts, task files, and relevant project-specific rules.
- Global OpenCode config: `~/.config/opencode/AGENTS.md`, `opencode.json`, `agents/`, `commands/`, `skills/`, `rules/`, `scripts/`, `package.json`, plugins, and MCP config.
- Model routing when already available or clearly useful, including `/opencode/audit-models` output.

Focus only on high-ROI improvements:

- Session handoff compression.
- Missing, duplicated, over-broad, or mistimed skills or commands.
- Misplaced, stale, contradictory, or overly long rules.
- Agent model, permission, mode, or delegation mismatches.
- Supported hook, plugin, or MCP opportunities with clear benefit and low risk.
- Repeated local automation that should become an `.fsx` helper.
- Token reduction by lazy-loading rules, narrowing searches, cheaper model routing, or moving verbose guidance out of always-loaded files.

Classify each recommendation as:

- `portable`: supported and safe as-is.
- `translated`: useful, but should be implemented through a different OpenCode-native surface.
- `unsupported`: not supported or not verifiable from available context.
- `risky`: possible, but needs explicit confirmation, auth review, install approval, or destructive-action review.

Guardrails:

- Do not edit files, install packages, modify MCP or plugin config, commit, push, delete, or perform external actions.
- Do not read auth secret files such as `auth.json`, `.env`, tokens, credentials, or browser or session stores.
- Do not suggest broad rewrites unless the session shows repeated concrete pain.
- Prefer deleting or shrinking instructions over adding new always-loaded prose.
- Prefer commands for repeated prompts, skills for reusable procedures, agents for specialized behavior, rules for domain constraints, and scripts only when manual repetition is costly.
- Preserve existing confirmation gates for commits, pushes, external writes, tracker updates, installs, payments, and destructive actions.

Return at most 5 recommendations, ordered by ROI. If there are no worthwhile changes, say so and provide one session-handoff improvement if useful.

Use this exact structure:

```markdown
## Audit-Session Brief
Context: <one sentence about the session pattern reviewed>

1. [P1|P2|P3] [portable|translated|unsupported|risky] <short title>
Surface: <skill|command|rule|agent|hook|plugin|mcp|script|session>
Target: `<exact path or config key>`
Change: <one sentence with the concrete edit or creation>
Why: <one sentence focused on token reduction, reliability, speed, or safety>
Executor task: <imperative instruction that can be pasted to `executor`>
Validation: <smallest useful check, e.g. `git diff --check`, command help, dry run>
Gate: <none|ask before install|ask before external action|ask before deletion|ask before commit>
```

Good output rules:

- Use exact file paths and command names.
- Avoid speculative implementation details when the relevant file was not read.
- Prefer one high-impact recommendation over many weak ones.
- Include removals or consolidations when they reduce token load safely.
- If recommending an MCP, plugin, or hook, include why existing commands or skills are insufficient.
- Make each `Executor task` independently actionable.

Arguments: $ARGUMENTS
