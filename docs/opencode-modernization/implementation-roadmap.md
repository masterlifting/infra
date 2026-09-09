# OpenCode Modernization Implementation Roadmap

**Status:** Canonical sequencing snapshot for the current modernization backlog  
**Snapshot date:** 2026-09-09  
**Staging repository:** `masterlifting/infra`  
**Static branch:** `dev`

## Mandatory implementation order

```text
#9  Repository + composition foundation
 ↓
#10 Capability-oriented agents / knowledge / contracts
 ↓
#11 Canonical Assignment / AgentResult contracts
 ↓
#12 Universal Task Framework + deterministic Task Runtime
 ↓
#13 F# MCP integration + thin JS/TS extension boundaries
 ↓
#14 Deterministic .NET verification via F# MCP tools
 ↓
#15 Composition-aware logical knowledge resolution/loading
 ↓
#17 Post-modernization frontier-model readiness audit + routing eval
```

Canonical sequence:

`#9 -> #10 -> #11 -> #12 -> #13 -> #14 -> #15 -> #17`

Do not start a later implementation issue before the previous mandatory issue has landed unless the user explicitly revises the roadmap.

## Canonical ownership

| Concern | Owner |
| --- | --- |
| repository/cutover/composition | #9 |
| capabilities/roles/platform authority/native permissions | #10 |
| Assignment/AgentResult/delegation transport | #11 |
| Universal Task Framework/Task Runtime/Profiles/Work Tree/Guards | accepted proposal + #12 |
| runtime exposure boundary | #13 |
| .NET verification | #14 |
| knowledge authorization/resolution/loading | #15 |
| sequencing | #16 |
| model-readiness audit/eval | #17 |
| Happy Life OPS migration/ActionGuard downstream | `masterlifting/happy-life#1` |

## Workflow transition

- #9–#11 use the pre-#12 `/task` workflow.
- #13 onward use the Universal Task Framework after #12 lands.

## #9 mandatory preservation input

```text
masterlifting/infra
branch: dev
path: docs/opencode-modernization/
```

#9 must reconstruct the proposal into one canonical destination architecture file and verify SHA-256:

`d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`

It must migrate the aligned backlog and fold temporary addenda into destination canonical issue bodies. After cutover, destination becomes canonical and this package historical.
