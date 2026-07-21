---
name: audit-infra
description: Use when auditing, comparing, migrating, optimizing, or changing broad OpenCode infrastructure including AGENTS.md, opencode.json, agents, commands, skills, rules, scripts, plugins, MCPs, and project .opencode files. Do not use for active-session-only analysis; use audit-session instead.
---

# Audit Infrastructure

## Purpose

Route broad OpenCode infrastructure reviews to the existing `audit-infra` subagent. Keep the audit checklist authoritative in `agents/audit-infra.md`.

## Guardrails

- Default to read-only analysis unless the user explicitly requests apply, edit, migrate, or update behavior.
- Preserve all confirmation gates and secret-handling restrictions from `AGENTS.md`.
- Do not fetch external sources or send local context externally without explicit confirmation for that action.

## Workflow

1. Resolve the requested infrastructure roots, comparison sources, goals, and mutation intent.
2. Delegate the comparative audit to the `audit-infra` subagent with exact local paths and requested focus areas. If already running as `audit-infra`, execute the assigned audit instead of delegating again.
3. If mutation was explicitly requested, apply only the smallest supported recommendations after reviewing the subagent findings.
4. Run `npm run validate:infra` and `git diff --check` after infrastructure edits.

## Output

Report ranked findings, accepted and skipped adaptations, changed files, validation results, and any action requiring separate confirmation.
