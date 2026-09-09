# OpenCode Backlog Migration Ledger — 2026-09-09

**Status:** Applied backlog-alignment record; static preservation completed in `masterlifting/infra` `dev`  
**Purpose:** Safely align the OpenCode modernization backlog with the accepted Universal Task Framework architecture without losing existing requirements or creating duplicate sources of truth.

## Canonical preservation source before #9 cutover

```text
repository: masterlifting/infra
branch: dev
path: docs/opencode-modernization/
```

Accepted proposal integrity SHA-256:

`d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`

Architecture review is closed. The proposal owns Universal Task Framework semantics until explicitly superseded.

The proposal is preserved losslessly under `universal-task-framework.snapshot/` with a manifest. #9 must reconstruct it into one canonical destination architecture document and verify the hash.

Recommended destination path after cutover:

`docs/architecture/universal-task-framework.md`

## Migration safety rules

1. Preserve one canonical owner per concern.
2. Do not copy the full Task proposal into multiple issues.
3. Classify removed requirements as superseded, moved to another owner, or retained.
4. After a GitHub write, re-observe actual target state before retrying or relying on it.
5. #16 is updated last and owns sequencing only.
6. Mandatory implementation order remains:
   `#9 -> #10 -> #11 -> #12 -> #13 -> #14 -> #15 -> #17`.

## Canonical ownership matrix

| Concern | Canonical owner |
| --- | --- |
| repository ownership / cutover / composition/install | #9 |
| capability ownership / role contracts / platform authority / native permissions | #10 |
| Assignment / AgentResult / delegation transport | #11 |
| Universal Task Framework domain/runtime/Profile/Work Tree | accepted proposal + #12 |
| native vs JS/TS vs F# MCP vs hook boundary | #13 |
| .NET verification | #14 |
| logical knowledge authorization/resolution/loading | #15 |
| sequencing / cross-issue ownership | #16 |
| model-readiness audit/eval | #17 |
| Happy Life OPS migration / ActionGuard / connectors | `happy-life#1` |

## Accepted Task architecture owned by #12

#12 implements, without duplicating the proposal:

- Task Core and durable lifecycle;
- `Kind = research | execution`;
- Profile as primary task-design extension point;
- project Profiles and constrained same-ID overlays;
- `required/default/allowed` capability envelope;
- task activation and Profile selection/reclassification;
- recursive Work Tree;
- derived readiness and operational status;
- canonical WorkItem transition contract;
- WorkItem vs child Task boundary;
- durable logical ownership;
- Acceptance Criteria and typed Evidence/supersession;
- Decisions/Open Questions/trusted target-bound User authority;
- Guards with `BeforeStart | BeforeComplete`;
- Draft/Baselined contract lifecycle and exact ContractPatch authority;
- deterministic F# Task Runtime;
- CAS/stateRevision and managed-state boundary;
- side-effect-free `CompleteTask` and mechanical `CanCompleteTask`;
- external-effect confirmation / observe-before-retry / fail-closed ambiguity;
- `State / Evidence / Next` continuity;
- dependency handoff;
- audit remediation synthesis/materialization;
- legacy TASK compatibility;
- downstream Happy Life migration boundary without ActionGuard in Core.

## Explicitly superseded concepts

Do not resurrect as canonical requirements:

- `kind = general | research | implementation`;
- required `domain` axis;
- `general/default`, `research/default`, `software/implementation`, `life/general` taxonomy;
- persisted `ready` WorkItem state;
- fixed universal Research -> Clarification -> Design -> Branch -> Implement pipeline;
- C0/C1/C2 fixed ceremony;
- mandatory Summary checkbox ceremony;
- `code | non-code`;
- mandatory Last Loaded Context;
- mandatory OPS Intake/Plan/Execute/Handoff or Execution Log;
- ActionGuard in shared Task Core;
- arbitrary custom workflow/state-machine DSL.

## Preserved/mapped semantics

- BLOCKING / ASSUMPTION / NON-BLOCKING clarification policy;
- Safety / Decision / Quality / Bookkeeping gate taxonomy;
- architecture sensitivity distinct from implementation complexity;
- risk-driven software testing/review;
- role ownership via #10/#11;
- relevant branch/HEAD freshness and volatile-state revalidation;
- observe-before-retry; fail closed when ambiguous effect cannot be observed;
- separately confirmation-gated external writes;
- terminal State/Evidence/Next freshness;
- lightweight Origin/Unblocks/UnblockCondition;
- dependency completion does not auto-prove external condition;
- read-only audit remediation proposals until coordinator materialization;
- historical TASK/OPS not mass rewritten.

## Applied GitHub alignment

### Full issue replacements

- `infra#12` -> `OpenCode 4/7: Implement the Universal Task Framework and deterministic Task Runtime`.
- `happy-life#1` -> `Migrate Happy Life from life-ops to the Universal Task Framework and retire OPS`.

Both were re-fetched after write.

### Temporary authoritative alignment addenda

| Issue | Comment ID | Purpose |
| --- | ---: | --- |
| `infra#9` | `5599070013` | proposal/backlog/static-package migration |
| `infra#10` | `5599074226` | platform authority/capability vs Task authority |
| `infra#11` | `5599077971` | WorkItem owner -> Assignment/AgentResult |
| `infra#13` | `5599081850` | Task Runtime exposure boundary; MCP not mandatory |
| `infra#14` | `5599083359` | typed Task Evidence integration |
| `infra#15` | `5599088756` | Profile capability envelope vs knowledge authorization |
| `infra#17` | `5599091016` | audit remediation -> Task materialization |
| `infra#16` | `5599097450` | roadmap alignment, applied last |

These comments must be folded into destination canonical issue bodies during #9 cutover rather than preserved as a second long-term requirements layer.

## Static GitHub preservation package

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

Proposal reconstruction hash:

`d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`

Integrity snapshot commit:

`7ddcc2c8bf69e7de98aa4b340aa9526537f5b4c9`

Later documentation-only commits strengthened references/evidence; proposal snapshot bytes remained unchanged.

## Verified preservation evidence

- all seven proposal part GitHub blobs matched local expected Git blob SHAs;
- part byte sizes total exactly 92,055 bytes;
- manifest records ordering, per-part hashes, line ranges, and full SHA-256;
- package directory and snapshot directory were fetched from GitHub after creation;
- full issue replacements and all addendum comments were re-fetched after write;
- #16 alignment was performed last.

## #9 mandatory cutover obligations

#9 must:

1. use `masterlifting/infra` `dev` `docs/opencode-modernization/` as mandatory migration input;
2. reconstruct proposal parts into one destination architecture document;
3. verify SHA-256 `d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`;
4. migrate the aligned backlog;
5. fold all temporary alignment comments into destination issue bodies;
6. preserve roadmap/ledger/evidence as appropriate migration/history records;
7. exclude superseded taxonomy from authoritative destination requirements;
8. leave infra copies historical after cutover, not independently canonical;
9. verify destination #16 equivalent preserves `#9 -> #10 -> #11 -> #12 -> #13 -> #14 -> #15 -> #17`.
