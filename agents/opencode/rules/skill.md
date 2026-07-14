# Skill Authoring Rule

Use this rule whenever creating or updating files under `skills/*/SKILL.md`.

## Goal

Keep skills easy for the harness to route, easy for humans to execute, and low-noise for token usage.

## Required Structure

Each skill must include YAML frontmatter:

- `name`: unique, short, kebab-case.
- `description`: concise trigger-first routing logic. This is the primary harness routing signal.

Body sections should follow this order when relevant:

1. `## Purpose`
2. `## Guardrails`
3. `## Workflow`
4. `## Output`

Do **not** add a `## Trigger` section unless explicitly required by a specific runtime/tooling behavior. Prefer trigger logic in `description`.

## Description Rules (Routing-Critical)

- Start with trigger conditions (`Run when ...`, `Use when ...`, `Do not use when ...`) in one compact block.
- Include positive and negative routing hints.
- Keep it concrete (systems, artifacts, commands, domains).
- Avoid vague wording like "sometimes", "generally", or "as needed".
- Keep it short (ideally 1-3 sentences).

## Guardrails

- Preserve global confirmation gates (commit/push/external/destructive operations).
- Never require reading secrets from repo files.
- Prefer least-privilege, read-only posture unless mutation is explicitly the skill's purpose.
- Mark risky or unsupported patterns explicitly rather than implying support.

## Workflow Guidance

- Keep steps minimal and deterministic.
- Prefer OpenCode-native surfaces:
  - skill = reusable procedure
  - command = repeated prompt shortcut
  - agent = specialized task behavior
  - rules = constraints/policy
  - script = repeated local automation
- Avoid embedding long background unless needed for safe execution.

## Route Reference Conventions

- Keep references aligned with current repository routes:
  - For global/shared OpenCode infrastructure under `C:/Users/andre/.config/opencode`, use absolute rule routes like `@C:/Users/andre/.config/opencode/rules/...`.
  - For project-local repositories that own their own `rules/` directory, project-local rule routes like `@rules/...` are acceptable.
  - Agents: `agents/...` for file paths, `software/...` for subagent invocation names.
  - Commands: `/opencode/...` for runnable command routes (example: `/opencode/youtrack`).
- Do not use deprecated or ambiguous route forms in skill text.
- Prefer canonical, existing paths over aliases.

## Outdated Content Checks

- Remove stale file paths after moves/renames.
- Remove references to deleted skills, commands, scripts, agents, or rule files.
- Verify command names match actual files under `commands/`.
- Verify rule file references match actual files under `rules/`.
- Verify agent names in instructions match actual files under `agents/`.
- Replace vague legacy wording and typos with explicit current behavior.

## Output Guidance

- Define a compact, actionable output format.
- Prefer executor-ready instructions when implementation handoff is likely.
- Limit verbosity; include only evidence needed for decisions.

## Validation Checklist

Before finalizing skill edits:

- Frontmatter parses and includes `name` + trigger-first `description`.
- Section order is consistent (`Purpose`, `Guardrails`, `Workflow`, `Output`).
- No redundant trigger prose in body.
- No conflicts with `AGENTS.md` guardrails.
- Rule/agent/command routes resolve to current repository structure.
- No stale references to removed or renamed files.
- `git diff --check` is clean.
