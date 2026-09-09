# OpenCode Backlog Alignment Evidence Snapshot

**Snapshot date:** 2026-09-09  
**Repository:** `masterlifting/infra`  
**Static preservation branch:** `dev`  
**Integrity snapshot commit:** `7ddcc2c8bf69e7de98aa4b340aa9526537f5b4c9`

This document records evidence needed to reconstruct and verify the backlog alignment after acceptance of the Universal Task Framework.

## Architecture integrity

Reconstructed proposal SHA-256:

`d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`

The proposal hash is specified in `universal-task-framework.snapshot/manifest.md`.

The seven GitHub snapshot part blobs were compared with Git blob SHAs computed from the accepted local proposal slices and all matched exactly.

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

The total equals the accepted proposal byte length. Reconstruction must verify the full SHA-256 above.

## Full issue replacements

- `masterlifting/infra#12` -> `OpenCode 4/7: Implement the Universal Task Framework and deterministic Task Runtime`.
- `masterlifting/happy-life#1` -> `Migrate Happy Life from life-ops to the Universal Task Framework and retire OPS`.

Both were re-fetched after replacement.

## Temporary authoritative alignment comments

| Issue | Comment ID | Purpose |
| --- | ---: | --- |
| `infra#9` | `5599070013` | proposal/backlog/static preservation migration |
| `infra#10` | `5599074226` | capability/platform authority vs Task authority |
| `infra#11` | `5599077971` | WorkItem owner -> Assignment/AgentResult |
| `infra#13` | `5599081850` | Task Runtime exposure boundary |
| `infra#14` | `5599083359` | typed Task Evidence integration |
| `infra#15` | `5599088756` | Profile capability vs knowledge authorization |
| `infra#17` | `5599091016` | audit remediation -> Task materialization |
| `infra#16` | `5599097450` | roadmap alignment, applied last |

#9 must fold these into destination canonical issue bodies during cutover.

## Verified invariants

- implementation order: `#9 -> #10 -> #11 -> #12 -> #13 -> #14 -> #15 -> #17`;
- #16 is sequencing/ownership only;
- #12 owns Task domain/runtime;
- #10 owns role/capability/platform-authority semantics;
- #11 owns Assignment/AgentResult;
- #13 owns runtime exposure/boundary selection;
- #14 owns .NET verification, not generic Task Evidence lifecycle;
- #15 owns knowledge authorization/loading, distinct from Profile capability envelopes;
- #17 remains read-only;
- Happy Life keeps ActionGuard/connectors downstream;
- historical TASK/OPS are not mass rewritten.

## Superseded concepts

Do not resurrect:

- `kind = general | research | implementation`;
- required `domain` dimension;
- `general/default`, `research/default`, `software/implementation`, `life/general` taxonomy;
- persisted `ready` WorkItem state;
- fixed global Research -> Clarification -> Design -> Branch -> Implement lifecycle;
- C0/C1/C2 ceremony;
- `linear | stateful` mode;
- ActionGuard in shared Task Core;
- arbitrary workflow/state-machine DSL.

## Re-verification

1. read `README.md`;
2. reconstruct/verify the proposal from the snapshot manifest + seven parts;
3. read `backlog-migration-ledger.md`;
4. read `implementation-roadmap.md`;
5. fetch listed issues/comments;
6. compare against the invariants above;
7. after #9 cutover, use the destination shared OpenCode repo as canonical and treat this package as historical evidence.

## Cutover destination and mapping

- Canonical repository: `masterlifting/opencode:main`
- Operational/historical repository: `masterlifting/infra`
- Destination commit: `911df41384abad67be84948c043bc6d7ead45a79`

Exact destination mapping (`infra#9–#17 → opencode#1–#9`):

| Historical infra issue | Destination OpenCode issue |
| --- | --- |
| `infra#9` | `opencode#1` |
| `infra#10` | `opencode#2` |
| `infra#11` | `opencode#3` |
| `infra#12` | `opencode#4` |
| `infra#13` | `opencode#5` |
| `infra#14` | `opencode#6` |
| `infra#15` | `opencode#7` |
| `infra#16` | `opencode#8` |
| `infra#17` | `opencode#9` |
