---
name: audit
description: Use when auditing, comparing, migrating, optimizing, or changing OpenCode infrastructure including AGENTS.md, opencode.json, agents, commands, skills, rules, scripts, plugins, MCPs, and project .opencode files. Do not use for product-code work.
---

# Audit Infrastructure

## Purpose

Route broad OpenCode infrastructure reviews to the read-only `auditor` subagent. Keep its audit checklist authoritative in `agents/auditor.md`.

## Guardrails

- All audit analysis is read-only. Mutations occur only after an explicit user request through the gated coordinator workflow, never directly through `auditor`.
- Preserve all confirmation gates and secret-handling restrictions from `AGENTS.md`.
- Do not fetch external sources or send local context externally without explicit confirmation for that action.
- Keep temporary working artifacts in an explicitly temporary ignored location and remove or intentionally promote them before completion.
- For important changes to skills, rules, agents, or orchestration logic, optionally run behavioral evaluation scenarios from `references/behavioral-evaluation.md` in fresh agent contexts; behavioral evaluation supplements, never replaces, deterministic validation.

## Workflow

1. Resolve the requested infrastructure roots, comparison sources, goals, and mutation intent.
2. Delegate the comparative audit to the `auditor` subagent with exact local paths and requested focus areas. If already running as `auditor`, execute the assigned audit instead of delegating again.
3. Select session, harness, or project scope. Inspect only relevant infrastructure; audit-only mode ends after ranked findings freeze.
4. For explicitly requested mutation, the primary coordinator accepts a finite scope, chooses and freezes the smallest sufficient solution, and records the acceptance matrix in a task or session record. The `auditor` subagent remains read-only.
5. Already explicitly authorized frozen batches may execute; global >10-file confirmation pauses and action-specific safety gates remain authoritative. Run applicable deterministic validation, then perform targeted semantic Verification rather than another broad audit.
6. When the change affects observable agent behavior and behavioral evidence is required, run the relevant scenarios from `references/behavioral-evaluation.md` in fresh contexts and record pass/fail against the predefined criteria.
7. Freeze the accepted finding set. Allow at most two remediation passes; stop and report a blocking finding that remains after pass 2.
8. Treat a later generic re-review of a frozen result as Verification unless the user explicitly requests redesign or proves a hard invalidation condition.

## Output

Report changed files, frozen decisions, validation results, intentional deviations, and unresolved blockers. Audit-only output reports ranked findings and recommendations.
