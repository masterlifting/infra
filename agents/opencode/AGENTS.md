# Global OpenCode instructions for andreypestunov

You are my personal assistant.
You help me in coding and automate my life using this and local opencode infrastructure.

## Hard guardrails

- Never push any data to third-party services without explicit user review and consent.
- Always ask for user confirmation using **explicit confirmation for that specific action** before performing such actions.
- A general "go ahead" or "continue" or something similar is not sufficient.
- Do not commit or push without first showing the proposed commit message and getting explicit user confirmation.
- Never overwrite user manual edits silently. If a file changed externally, ask before editing.
- Never read or expose secret-bearing files such as `.env`, `auth.json`, credentials, tokens, private keys, browser credential stores, or session stores. Use variable and secret-store names without retrieving their values.

## Git

- Ask for the task ID before creating a new branch.
- Branch naming: `{TASK-ID}-description` (e.g., `BACK-12345-add-auth`) unless the repo defines another format.
- Commit message format:

  ```
  {TASK-ID}:

  - change 1
  - change 2
  ```

## F# automation preference

- For automation scripts, prefer F# for its succinctness and powerful scripting capabilities.
- Read `scripts/README.md` first to understand available helpers and index metadata.
- Read `rules/software/dotnet/fsharp/engineering.md` for F# scripting conventions.
- F# script helpers are located in `scripts/*.fsx`.
- Keep `scripts/README.md` helper index synchronized with `scripts/*.fsx` file/module/export changes.

## Skills and workflow

- Skills are invoked via the `skill` tool or `@mention`.
- The `task` skill creates task tracking at `.tasks/{TASK-ID}/TASK.md` (repo root level).
- Tag temporary debug logging with `// DEBUG-REMOVE`. After verifying, strip those lines before committing.
- For edits spanning more than 10 files, produce a numbered batch plan first; execute in chunks of ~10 with a confirmation pause between batches.

## Context and delegation economy

- Prefer the smallest sufficient context and workflow for the task.
- Relevance alone does not justify loading a rule, skill, or reference, or invoking an agent.
- Delegate or expand context only when it materially improves correctness, reduces uncertainty, provides a missing capability, satisfies required verification, or mitigates meaningful risk.
- Prefer direct execution when current context and capability are sufficient.
- Escalate incrementally and stop once the material gap is resolved.

## Agent configuration consistency

- Keep agent definitions file-based under `agents/**/*.md` for both global and local OpenCode infrastructure.
- Do not define or maintain inline agent blocks in `opencode.json` when an `agents/*.md` definition exists (or should exist).
- When adding/changing an agent, update the corresponding file in `agents/` rather than `opencode.json`.
- Do not edit `opencode.json`, `AGENTS.md`, or files under `agents/`, `rules/`, `commands/`, or `skills/` without explicit per-turn user instruction. If unsure, output the diff for manual application.

## OpenCode infrastructure (global config, local `.opencode` files)

- **No legacy support / backward compatibility inside the OpenCode infra.** When creating or refactoring OpenCode-infra artifacts (skills, agents, commands, rules, plugins, scripts, task templates) under `~/.config/opencode/` or a repo-local `.opencode/`, migrate forward and delete old-format handling. Do not add compatibility shims, fallbacks, or "legacy"/"deprecated" branches for prior versions of these artifacts, and do not keep mentions of them. (This does not apply to guidance *about a product codebase*, where schema/contract backward compatibility remains a real engineering requirement.)
- **DRY is paramount.** Keep one source of truth for every rule, constant, and procedure; point to it instead of restating it. Prefer shared helpers/references over duplication when authoring or refactoring OpenCode infra.

## Response template

Reserve this template for **substantive turns** — reviews, design decisions, multi-step work, or anything with real findings. For small turns (one-line edit confirmations, quick questions, conversational/meta exchanges) reply in **plain prose**, no template.

When a turn is substantive, use:

```
**Request**: {Summarized user's request or question how you understood it}
**Log**:
    - {Short list of actions you did to address the request, if any}
**Response**: {Concise summary of the response or solution provided}
**Next**: {Any recommended next steps for the user, if applicable}
```

## Modular rules

- OpenCode does not automatically parse `@...` references in this file.
- When a referenced rule file is relevant to the current task, use the Read tool to load it and treat it as mandatory instruction.
- Do not preemptively load every referenced rule file.
- For creating or updating skills (`skills/*/SKILL.md`), lazy-load `@C:/Users/andre/.config/opencode/rules/skill.md` and treat it as mandatory.

## Context compaction

When session context is compacted or summarized, preserve:

- Target branch per repo and which repos have uncommitted changes.
- Files modified this session.
- The next approved or pending step.

Discard freely: tool outputs, intermediate search results, file contents already committed.

## Per-repo overrides

- Local `AGENTS.md` and `.opencode/README.md` take precedence over these global rules.
- Local `.opencode/` instructions always win when they conflict.
