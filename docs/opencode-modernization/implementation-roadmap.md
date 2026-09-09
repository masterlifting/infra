# OpenCode Modernization Implementation Roadmap

**Status:** Canonical sequencing snapshot for the current `masterlifting/infra` modernization backlog  
**Snapshot date:** 2026-09-09  
**Current staging repository:** `masterlifting/infra`  
**Current working branch for these static records:** `dev`

## Mandatory implementation order

```text
infra#9   Repository + composition foundation
  ↓
infra#10  Capability-oriented agents / knowledge / contracts
  ↓
infra#11  Canonical Assignment / AgentResult contracts
  ↓
infra#12  Universal Task Framework + deterministic Task Runtime
  ↓
infra#13  F# MCP integration + thin JS/TS extension boundaries
  ↓
infra#14  Deterministic .NET verification via F# MCP tools
  ↓
infra#15  Composition-aware logical knowledge resolution/loading
  ↓
infra#17  Post-modernization frontier-model readiness audit + routing eval
```

Canonical sequence:

```text
#9 -> #10 -> #11 -> #12 -> #13 -> #14 -> #15 -> #17
```

Do not start a later implementation issue before the previous mandatory issue has landed unless the user explicitly revises the roadmap.

## Canonical ownership

| Concern | Owner |
| --- | --- |
| repository ownership / cutover / composition / install | `infra#9` |
| capabilities / role contracts / platform authority / native permissions | `infra#10` |
| Assignment / AgentResult / delegation transport semantics | `infra#11` |
| Universal Task Framework / Task Runtime / Profiles / Work Tree / Guards | `infra#12` + proposal snapshot in this package |
| native vs JS/TS vs F# MCP vs hook boundary | `infra#13` |
| .NET verification | `infra#14` |
| logical knowledge authorization / resolution / loading | `infra#15` |
| roadmap sequencing | `infra#16` |
| model-readiness audit / eval | `infra#17` |
| Happy Life OPS migration / ActionGuard downstream integration | `masterlifting/happy-life#1` |

## Workflow transition

- `#9`–`#11` use the pre-#12 `/task` workflow.
- Once `#12` lands, `#13` onward use the Universal Task Framework.
- Do not use future #12 behavior early while implementing #9–#11.
- Do not preserve old pre-#12 ceremony as mandatory after #12 lands.

## #9 cutover obligation

When the dedicated shared OpenCode repository is created, #9 must migrate:

1. the lossless proposal snapshot, reconstructed into one canonical architecture file and SHA-256 verified;
2. `backlog-migration-ledger.md`;
3. this roadmap;
4. `backlog-alignment-evidence.md`;
5. the aligned canonical issue bodies/addenda.

Source package:

`masterlifting/infra` branch `dev`, `docs/opencode-modernization/`

Proposal integrity SHA-256:

`d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`

After cutover, the destination repository becomes canonical and these `infra` copies become historical migration evidence rather than a second editable source of truth.
