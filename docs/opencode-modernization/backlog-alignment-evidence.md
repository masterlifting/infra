# OpenCode Backlog Alignment Evidence Snapshot

**Snapshot date:** 2026-09-09  
**Repository:** `masterlifting/infra`  
**Branch for static preservation package:** `dev`  
**Preservation snapshot commit:** `7ddcc2c8bf69e7de98aa4b340aa9526537f5b4c9`

This document records the evidence needed to reconstruct and verify the backlog-alignment migration performed after acceptance of the Universal Task Framework architecture.

## Artifact hashes after static preservation

- reconstructed `universal-task-framework.md` SHA-256: `d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`
- `backlog-migration-ledger.md` SHA-256: `564703fa856ca5b83d67abc3d8fd4dfda35ecd8d3227704e5fdff588e9f5330a`

The proposal hash is independently specified in `universal-task-framework.snapshot/manifest.md`; the ledger hash identifies the final static migration record committed after the preservation package itself was added.

## Static GitHub integrity verification

The seven proposal parts were verified after GitHub writes by comparing each GitHub blob SHA with the Git blob SHA calculated from the corresponding local source slice. All seven matched exactly.

Verified byte sizes:

```text
part-01  11085
part-02  12212
part-03  11875
part-04  13925
part-05  12351
part-06  11717
part-07  18890
----------------
total    92055
```

The total equals the original proposal byte length in the manifest. The snapshot is therefore lossless at the Git blob/content level; reconstruction must additionally verify the full SHA-256 from the manifest.

The committed ledger, roadmap, and preservation README were also verified against locally computed Git blob SHAs.

## Full issue replacements

### `masterlifting/infra#12`

Final title:

`OpenCode 4/7: Implement the Universal Task Framework and deterministic Task Runtime`

Purpose:

- bounded implementation issue for the accepted Task architecture;
- references the canonical architecture document instead of duplicating it;
- replaces superseded `general|research|implementation` / `domain` / `general/default` taxonomy.

### `masterlifting/happy-life#1`

Final title:

`Migrate Happy Life from life-ops to the Universal Task Framework and retire OPS`

Purpose:

- downstream migration to `Kind = research | execution` + project-local `life` Profile;
- preserves ActionGuard/connectors downstream;
- uses replay -> shadow -> low-risk canary -> selective active migration -> cutover.

## Authoritative temporary alignment addenda

These comments preserve mature issue bodies while adding the new cross-issue boundaries. #9 must fold their contents into destination canonical issue bodies during cutover.

| Issue | Comment ID | Purpose |
| --- | ---: | --- |
| `infra#9` | `5599070013` | canonical Task proposal/backlog migration; destination #4 naming; static preservation input |
| `infra#10` | `5599074226` | capability/role/platform-authority vs Task-domain authority |
| `infra#11` | `5599077971` | WorkItem owner -> Assignment/AgentResult integration |
| `infra#13` | `5599081850` | Task Runtime boundary; MCP not mandatory |
| `infra#14` | `5599083359` | typed Task Evidence integration |
| `infra#15` | `5599088756` | Profile capability envelope vs knowledge authorization/loading |
| `infra#17` | `5599091016` | Task materialization semantics for audit remediation |
| `infra#16` | `5599097450` | roadmap update, applied last; sequence unchanged |

## Invariants verified during migration

- mandatory implementation order remains `#9 -> #10 -> #11 -> #12 -> #13 -> #14 -> #15 -> #17`;
- #16 remains a sequencing/ownership tracker, not a requirements duplicate;
- #12 owns Task domain/runtime semantics;
- #10 remains the role/capability/platform-authority owner;
- #11 remains Assignment/AgentResult owner;
- #13 owns runtime exposure/boundary choice;
- #14 owns .NET verification, not generic Task Evidence lifecycle;
- #15 owns knowledge authorization/loading, distinct from Profile capability envelopes;
- #17 remains read-only and emits remediation proposals rather than backlog mutations;
- Happy Life keeps ActionGuard/connectors/project storage downstream;
- historical TASK/OPS records are not mass rewritten.

## Superseded Task concepts

The following are intentionally obsolete and must not be resurrected as canonical requirements:

- `kind = general | research | implementation`;
- required `domain` dimension;
- `general/default`, `research/default`, `software/implementation`, `life/general` as primary taxonomy;
- persistent `ready` WorkItem state;
- fixed global Research -> Clarification -> Design -> Branch -> Implement lifecycle;
- C0/C1/C2 fixed lifecycle ceremony;
- `linear | stateful` mode;
- ActionGuard inside shared Task Core;
- arbitrary workflow/state-machine DSL.

## Re-verification procedure

If there is doubt later:

1. read `README.md`;
2. verify/reconstruct the proposal from `universal-task-framework.snapshot/manifest.md` and the seven part files;
3. read `backlog-migration-ledger.md`;
4. read `implementation-roadmap.md`;
5. fetch the current GitHub issues and listed addendum comments;
6. compare against the invariants above;
7. when #9 cutover is complete, use the destination shared OpenCode repository as canonical and treat this package as historical evidence.
