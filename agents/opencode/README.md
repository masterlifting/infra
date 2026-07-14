# OpenCode Infrastructure Conventions

Single source for naming, layout, and verification conventions in this config repo. This file is intentionally not auto-loaded; read it when changing OpenCode infrastructure.

## Naming

| Surface        | Convention                                                   | Examples                                                              |
| -------------- | ------------------------------------------------------------ | --------------------------------------------------------------------- |
| Agents         | Domain path plus role                                        | `software/dotnet/csharp/reviewer-1`, `software/database/sql-reviewer` |
| Commands       | Kebab-case workflow name under `commands/`                   | `youtrack`                                                            |
| Skills         | Kebab-case folder with `SKILL.md`                            | `task`, `youtrack`                                                    |
| Rules          | Domain path plus topic                                       | `software/dotnet/fsharp/engineering.md`, `security.md`                |
| Plugins        | Kebab-case JavaScript module                                 | `block-destructive.js`, `compaction-context.js`                       |
| Shared JS      | Kebab-case `.mjs` module under `lib/`                        | `destructive-patterns.mjs`, `destructive-patterns.test.mjs`           |
| Helper scripts | PascalCase `.fsx` in `scripts/`                              | `Cli.fsx`, `Result.fsx`                                               |
| Entry scripts  | PascalCase `.fsx` under a command or skill `scripts/` folder | `ValidateTask.fsx`, `YouTrackRest.fsx`                                |

## Folder Layout

```text
agents/                 # File-based agent definitions; no inline opencode.json agents
commands/               # Slash-command prompts and command-local scripts
lib/                    # Shared JavaScript logic and deterministic tests
plugins/                # Auto-loaded OpenCode hooks; no opencode.json entry needed
rules/                  # Lazy-loaded domain rules and policies
scripts/                # Reusable F# helper modules plus README index
skills/{name}/SKILL.md  # Skill body plus references/ and scripts/ when needed
opencode.json           # Global permissions, providers, MCP, plugins
AGENTS.md               # Always-loaded user and session defaults
```

## Frontmatter

- Agent files use frontmatter for `description`, `mode`, `model`, `steps`, and `permission`; the body is the prompt.
- Skill files include `name` and trigger-first `description`; follow `rules/skill.md` before editing global skills.
- Commands include a concise `description` and, when applicable, `agent` routing.

## Permissions Hygiene

- Keep risky operations guarded in `opencode.json` permissions and in `AGENTS.md` confirmation policy.
- Never add broad `allow` rules for destructive commands, installs, external writes, tracker writes, commits, pushes, or secret handling without a specific user request.
- Put broad bash rules before narrow denials because OpenCode uses the last matching permission rule.

## Verification

- For config edits, parse JSON and run `git diff --check`.
- For infrastructure documents and configuration, run `npm run validate:infra` to check concrete local routes, strict one-line frontmatter, F# naming, helper filenames, selected permission ordering, and exact duplicate prose.
- For skill edits, check frontmatter, route references, and `rules/skill.md` validation expectations.
- For plugin edits, run `node --check plugins/<name>.js` and the relevant deterministic test when one exists.
- After destructive-command policy changes, run `npm run test:safety` to verify the shared blocker patterns.
- For F# helper changes, keep `scripts/README.md` synchronized with helper file/module/export changes.
- After changing OpenCode config, agents, skills, commands, plugins, or rules, restart OpenCode for the running session to pick up changes.
