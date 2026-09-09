# OpenCode Backlog Migration Ledger — 2026-09-09

**Status:** Applied backlog-alignment record; static preservation completed in `masterlifting/infra` `dev`  
**Purpose:** Safely align the OpenCode modernization backlog with the accepted Universal Task Framework architecture without losing existing requirements or creating duplicate sources of truth.

## 1. Frozen architecture source

Canonical accepted proposal integrity SHA-256:

`d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`

Architectural status:

- architecture review closed;
- ready for issue decomposition;
- proposal owns Universal Task Framework semantics until explicitly superseded.

Static GitHub preservation source before #9 cutover:

```text
repository: masterlifting/infra
branch: dev
path: docs/opencode-modernization/
```

The accepted proposal is preserved losslessly under `universal-task-framework.snapshot/` with a SHA-256 manifest. #9 must reconstruct it into one canonical destination architecture document and verify the hash.

Recommended destination path after cutover:

`docs/architecture/universal-task-framework.md`

Exact path may follow the destination repository's documentation conventions; semantic ownership matters more than this illustrative path.

## 2. Migration safety rules

1. Preserve one canonical owner per concern.
2. Do not copy the full Task proposal into multiple issues.
3. For every removed old requirement, classify it as:
   - superseded by accepted Task architecture;
   - moved to another canonical owner;
   - still required and retained.
4. After each GitHub write:
   - re-fetch the issue/file;
   - verify intended content/state;
   - do not retry an ambiguous write before observing actual GitHub state.
5. Update the sequencing tracker (#16) last so it describes the already-aligned backlog.
6. Preserve the mandatory execution order unless the user explicitly changes it:
   `#9 -> #10 -> #11 -> #12 -> #13 -> #14 -> #15 -> #17`.

## 3. Canonical ownership matrix

| Concern | Canonical owner | Task-framework relationship |
| --- | --- | --- |
| Repository ownership, cutover, composition/install | #9 | Task consumes the resulting shared/project composition |
| Capability ownership, role contracts, platform semantic authority, runtime-permission distinction | #10 | Profiles select capability envelopes; Task does not redefine platform roles/permissions |
| Assignment / AgentResult / delegation transport semantics | #11 | WorkItem owner is durable planning input; execution uses canonical Assignment/AgentResult |
| Universal Task Framework domain/runtime/profile/work-tree semantics | **Accepted Task proposal + #12** | Primary owner |
| Native vs JS/TS tool vs F# MCP vs hook boundary | #13 | Selects exposure mechanism for Task Runtime where needed |
| .NET build/test verification | #14 | Produces typed verification evidence consumed by Task/AgentResult |
| Logical knowledge authorization/resolution/loading | #15 | Supplies knowledge for capabilities selected by effective Task/Profile composition |
| Sequencing / cross-issue ownership | #16 | Tracker only; must not duplicate Task proposal |
| Model-readiness audit/eval | #17 | Produces findings/remediation proposals consumed through #11/#12 |
| Happy Life OPS migration / ActionGuard / connectors | `happy-life#1` | Downstream `life` Profile/project extension; not shared Task Core |

## 4. Accepted Task architecture that #12 must own

The implementation issue references, rather than duplicates, the accepted proposal and implements:

- Task Core and durable lifecycle;
- `Kind = research | execution`;
- Profile as the primary task-design extension point;
- project-local Profiles and constrained same-ID overlays;
- `required/default/allowed` capability-envelope semantics;
- task activation and Profile selection/reclassification;
- recursive Work Tree;
- derived readiness and derived Active/Waiting/Blocked operational status;
- WorkItem transition contract;
- WorkItem vs child Task boundary;
- durable logical ownership and owner inheritance/rebinding;
- Acceptance Criteria, typed Evidence/receipts, Evidence supersession;
- Decisions, Open Questions, trusted User authority, exact target-bound Decision reuse;
- Guard model with `BeforeStart | BeforeComplete`;
- Draft/Baselined contract lifecycle, ContractPatch authority, contract fingerprint/drift;
- deterministic Task Runtime / F# functional core;
- CAS/stateRevision and managed machine-state mutation boundary;
- side-effect-free `CompleteTask` and mechanical `CanCompleteTask`;
- external-effect confirmation + observe-before-retry + fail-closed ambiguity;
- durable `State / Evidence / Next`;
- dependency handoff;
- audit-remediation synthesis/materialization boundary;
- compatibility for legacy TASK records;
- staged Happy Life migration boundary, without importing ActionGuard/life-specific storage into Core.

## 5. Explicitly superseded #12 concepts

The following are intentionally superseded, not accidentally dropped:

- `kind = general | research | implementation`;
- a required `domain` axis;
- `general/default`, `research/default`, `software/implementation` as the primary taxonomy;
- Profile-specific top-level phase/state taxonomy as a universal requirement;
- `ready` as persisted WorkItem state;
- one fixed universal Research -> Clarification -> Design -> Branch -> Implement pipeline;
- fixed software lifecycle numbering/C0/C1/C2;
- mandatory `Summary:` checkbox ceremony;
- `code | non-code`;
- mandatory `Last Loaded Context`;
- mandatory OPS Intake/Plan/Execute/Handoff or Execution Log;
- ActionGuard in shared Task Core;
- arbitrary user-defined state/transition DSL.

## 6. Existing requirements preserved/mapped

- clarification policy: BLOCKING / ASSUMPTION / NON-BLOCKING;
- gate taxonomy: Safety / Decision / Quality / Bookkeeping;
- architecture sensitivity distinct from implementation complexity;
- risk-driven software review/testing;
- engineer/tester/reviewer/coordinator ownership via #10/#11;
- repository/branch/HEAD freshness when relevant to software work;
- volatile-state revalidation on resume;
- observe target before retry after ambiguous external effects;
- if ambiguity cannot be resolved safely, block/wait rather than retry;
- protected external writes remain separately confirmation-gated;
- terminal `State / Evidence / Next` freshness;
- lightweight `Origin / Unblocks / Unblock condition`;
- child/dependency completion does not automatically prove the parent's observable condition;
- audit remediation proposals remain advisory/read-only until coordinator materialization;
- historical TASK/OPS records are not mass-rewritten.

## 7. Applied GitHub alignment

### Full issue-body replacements

- `masterlifting/infra#12`
  - title: `OpenCode 4/7: Implement the Universal Task Framework and deterministic Task Runtime`;
  - body replaced with the reviewed implementation draft and re-fetched/verified.

- `masterlifting/happy-life#1`
  - title: `Migrate Happy Life from life-ops to the Universal Task Framework and retire OPS`;
  - body replaced with the reviewed downstream migration draft and re-fetched/verified.

### Append-only authoritative alignment addenda

To avoid accidentally dropping mature requirements from large otherwise-correct issue bodies, targeted updates were applied as authoritative comments rather than destructive full-body replacements.

Each addendum requires #9 cutover to fold it into the destination canonical issue body.

| Issue | Comment ID | Purpose |
| --- | ---: | --- |
| `infra#9` | `5599070013` | proposal/backlog/static-package migration and destination #4 naming |
| `infra#10` | `5599074226` | capability/role/platform-authority vs Task-domain authority boundary |
| `infra#11` | `5599077971` | WorkItem owner -> Assignment/AgentResult integration |
| `infra#13` | `5599081850` | Task Runtime boundary selection; MCP is not mandatory |
| `infra#14` | `5599083359` | typed Task Evidence integration |
| `infra#15` | `5599088756` | Profile capability envelope vs knowledge authorization/loading |
| `infra#17` | `5599091016` | Task materialization semantics for audit remediation |
| `infra#16` | `5599097450` | roadmap update, applied last; sequence unchanged |

## 8. Implementation order

Implementation order remains:

```text
#9 -> #10 -> #11 -> #12 -> #13 -> #14 -> #15 -> #17
```

#16 is the sequencing/ownership tracker and was aligned last.

Workflow transition:

- #9–#11 use the pre-#12 `/task` workflow;
- #13 onward use the Universal Task Framework after #12 lands.

## 9. Static GitHub preservation package

Preserved on `masterlifting/infra` branch `dev`:

```text
docs/opencode-modernization/
  README.md
  implementation-roadmap.md
  backlog-alignment-evidence.md
  backlog-migration-ledger.md
  universal-task-framework.snapshot/
    manifest.md
    part-01.md
    part-02.md
    part-03.md
    part-04.md
    part-05.md
    part-06.md
    part-07.md
```

The reconstructed proposal SHA-256 must equal:

`d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`

An integrity snapshot of the complete preservation package existed at commit:

`7ddcc2c8bf69e7de98aa4b340aa9526537f5b4c9`

Subsequent commits only strengthened cross-references/evidence/roadmap wording; the proposal snapshot bytes remained unchanged.

## 10. Verified preservation evidence

- all seven GitHub proposal part blobs matched Git blob SHAs computed from the local accepted proposal slices;
- proposal part byte sizes sum exactly to 92,055 bytes, matching the accepted local proposal;
- the manifest records reconstruction order, per-part hashes, and full SHA-256;
- roadmap, ledger and preservation README were committed to GitHub and independently fetched/listed after creation;
- issue #12 and Happy Life #1 were re-fetched after full replacement;
- every alignment addendum was re-fetched after creation;
- #16 alignment was performed last.

## 11. #9 cutover obligations

When #9 creates/cuts over to the dedicated shared OpenCode repository, it must:

1. use `infra/dev/docs/opencode-modernization/` as mandatory migration input;
2. reconstruct the seven-part proposal into one canonical destination architecture file;
3. verify SHA-256 `d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c` before declaring architecture migration complete;
4. recreate/migrate the aligned issue bodies;
5. fold every temporary `Backlog alignment addendum` into the corresponding destination issue body;
6. migrate/retain the roadmap/ledger/evidence as appropriate historical/migration records;
7. avoid carrying stale superseded Task taxonomy as authoritative text;
8. leave the infra copies as historical pointers/evidence rather than independently editable canonical requirements;
9. verify the destination tracker still preserves the mandatory issue sequence.

The temporary comments and infra preservation package are migration-safety mechanisms, not the desired final documentation topology.
