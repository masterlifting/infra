# OpenCode Backlog Migration Ledger — 2026-09-09

**Status:** Applied and statically preserved  
**Staging repository:** `masterlifting/infra`  
**Static preservation branch:** `dev`  
**Static package:** `docs/opencode-modernization/`

## Canonical architecture integrity

Accepted Universal Task Framework proposal SHA-256:

`d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`

Architecture review is closed. The proposal owns Universal Task Framework semantics until explicitly superseded.

It is preserved losslessly under `universal-task-framework.snapshot/`; #9 must reconstruct it into one canonical destination file and verify the hash.

## Mandatory implementation order

```text
#9 -> #10 -> #11 -> #12 -> #13 -> #14 -> #15 -> #17
```

#16 owns sequencing/cross-issue boundaries only.

## Canonical ownership

| Concern | Owner |
| --- | --- |
| repository ownership / cutover / composition | #9 |
| capabilities / role contracts / platform authority / native permissions | #10 |
| Assignment / AgentResult | #11 |
| Universal Task Framework / deterministic Task Runtime | accepted proposal + #12 |
| runtime exposure boundary (native / JS/TS / F# MCP / hook) | #13 |
| .NET verification | #14 |
| knowledge authorization/resolution/loading | #15 |
| sequencing | #16 |
| model-readiness audit/eval | #17 |
| Happy Life OPS migration / ActionGuard downstream | `happy-life#1` |

## Applied backlog changes

### Full replacements

- `infra#12` -> `OpenCode 4/7: Implement the Universal Task Framework and deterministic Task Runtime`.
- `happy-life#1` -> `Migrate Happy Life from life-ops to the Universal Task Framework and retire OPS`.

Both were re-fetched after write.

### Temporary authoritative addenda

| Issue | Comment ID |
| --- | ---: |
| #9 | 5599070013 |
| #10 | 5599074226 |
| #11 | 5599077971 |
| #13 | 5599081850 |
| #14 | 5599083359 |
| #15 | 5599088756 |
| #17 | 5599091016 |
| #16 | 5599097450 |

#9 must fold these addenda into destination canonical issue bodies during cutover.

## Accepted #12 semantics that must survive implementation

- `Kind = research | execution`;
- Profile primary extension point, any Profile valid with either Kind;
- project-local Profiles / constrained same-ID overlays;
- `required/default/allowed` capability envelope;
- recursive Work Tree;
- derived readiness and derived Task operational status;
- typed Acceptance/Evidence/Decision/Question/Guard model;
- trusted target-bound User authority;
- Draft/Baselined contract lifecycle and exact ContractPatch authorization;
- deterministic F# Task Runtime and strict DTO boundary;
- CAS/stateRevision;
- side-effect-free `CompleteTask` and mechanical `CanCompleteTask`;
- external-effect confirmation / observe-before-retry / fail-closed ambiguity;
- dependency handoff and terminal `State/Evidence/Next`;
- read-only audit remediation proposals until coordinator materialization;
- ActionGuard remains downstream;
- legacy TASK/OPS not mass rewritten.

## Explicitly superseded concepts

Do not restore:

- `kind = general | research | implementation`;
- required `domain` axis;
- `general/default`, `research/default`, `software/implementation`, `life/general` taxonomy;
- persisted `ready`;
- fixed Research -> Clarification -> Design -> Branch -> Implement lifecycle;
- C0/C1/C2 ceremony;
- `linear | stateful` mode;
- ActionGuard in shared Task Core;
- generic custom workflow/state-machine DSL.

## Static package integrity

```text
docs/opencode-modernization/
  README.md
  implementation-roadmap.md
  backlog-alignment-evidence.md
  backlog-migration-ledger.md
  universal-task-framework.snapshot/
    manifest.md
    part-01.md ... part-07.md
```

All seven proposal part Git blobs matched the accepted local proposal slices. Their byte sizes total exactly 92,055 bytes.

Proposal reconstruction must verify:

`d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`

Integrity snapshot commit:

`7ddcc2c8bf69e7de98aa4b340aa9526537f5b4c9`

## #9 mandatory cutover obligations

#9 must:

1. use `masterlifting/infra` branch `dev`, `docs/opencode-modernization/` as mandatory migration input;
2. reconstruct the proposal into one destination architecture document;
3. verify its SHA-256;
4. migrate the aligned backlog;
5. fold temporary addenda into destination issue bodies;
6. retain roadmap/ledger/evidence as useful migration history;
7. exclude superseded taxonomy;
8. make the destination repository canonical and `infra` copies historical;
9. preserve the mandatory issue sequence.

## Destination mapping

`masterlifting/opencode:main` is canonical. `masterlifting/infra` is
operational/historical. The destination commit is
`911df41384abad67be84948c043bc6d7ead45a79`.

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
