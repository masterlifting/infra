# OpenCode Backlog Migration Ledger — 2026-09-09

**Status:** Applied backlog-alignment record; static preservation completed in `masterlifting/infra` `dev`  
**Purpose:** Safely align the OpenCode modernization backlog with the accepted Universal Task Framework architecture without losing existing requirements or creating duplicate sources of truth.

## 1. Frozen architecture source

Canonical local proposal:

`universal-task-framework-proposal-final.md`

SHA-256:

`d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`

Architectural status in that document:

- architecture review closed;
- ready for issue decomposition;
- proposal owns Universal Task Framework semantics until explicitly superseded.

The proposal must become a repository-tracked canonical architecture document during/through the #9 repository cutover. Do not rely on a chat attachment as the long-term source of truth.

Recommended destination path after cutover:

`docs/architecture/universal-task-framework.md`

Exact path may follow the destination repository's documentation conventions; semantic ownership matters more than this illustrative path.

## 2. Migration safety rules

1. Do not mutate GitHub until all replacement issue bodies/patches are prepared offline.
2. Preserve one canonical owner per concern.
3. Do not copy the full Task proposal into multiple issues.
4. For every removed old requirement, classify it as:
   - superseded by accepted Task architecture;
   - moved to another canonical owner;
   - still required and retained.
5. After each GitHub write:
   - re-fetch the issue;
   - compare title/body to the prepared draft;
   - verify cross-issue links and non-goals;
   - do not retry an ambiguous write before observing actual GitHub state.
6. Update the sequencing tracker (#16) last so it describes the already-aligned backlog.
7. Preserve the mandatory execution order unless the user explicitly changes it:
   `#9 -> #10 -> #11 -> #12 -> #13 -> #14 -> #15 -> #17`.

## 3. Current issue snapshot

| Repository | Issue | Current title | Migration classification |
| --- | ---: | --- | --- |
| `masterlifting/infra` | #9 | OpenCode 1/7: Extract dedicated modular repository and define composition | **Targeted update** |
| `masterlifting/infra` | #10 | OpenCode 2/7: Capability-oriented agents, knowledge, and contracts | **Targeted update** |
| `masterlifting/infra` | #11 | OpenCode 3/7: Canonical delegation and agent communication contracts | **Targeted update** |
| `masterlifting/infra` | #12 | OpenCode 4/7: Universal /task framework with risk-driven lifecycle profiles | **Full rewrite** |
| `masterlifting/infra` | #13 | OpenCode 5/7: Establish F# MCP integration and thin JS/TS extension boundaries | **Targeted update** |
| `masterlifting/infra` | #14 | OpenCode 6/7: Deterministic .NET verification via F# MCP tools | **Minimal integration update** |
| `masterlifting/infra` | #15 | OpenCode 7/7: Composition-aware logical knowledge resolution and controlled loading | **Targeted integration update** |
| `masterlifting/infra` | #16 | OpenCode modernization roadmap — #9→#15, then #17 readiness audit | **Tracker update last** |
| `masterlifting/infra` | #17 | OpenCode post-modernization: Frontier-model readiness audit and model-routing eval | **Targeted integration update** |
| `masterlifting/happy-life` | #1 | Migrate life-ops to universal /task profiles and retire the standalone OPS workflow | **Full rewrite** |

## 4. Canonical ownership matrix

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

## 5. Accepted Task architecture that #12 must own

The implementation issue must reference, not duplicate, the accepted proposal and implement:

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

## 6. Explicitly superseded #12 concepts

The following must be removed from the current issue rather than accidentally preserved:

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

These are intentionally superseded by the accepted proposal, not accidentally dropped.

## 7. Existing requirements that must survive the rewrite

From the current backlog and earlier Task/OPS semantics, retain or map:

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

## 8. Issue-by-issue migration plan

### #9 — targeted update

Retain current repository/composition/cutover requirements.

Add:

- the accepted Universal Task Framework proposal is a canonical architecture artifact that must be copied/committed into the destination shared OpenCode repository during cutover;
- destination backlog recreation must use the **aligned** issue texts produced by this migration, not the stale pre-proposal #12/Happy Life wording;
- preferred destination #4 title should describe the Universal Task Framework rather than only "risk-driven task lifecycle".

Do not make #9 implement Task Runtime.

### #10 — targeted update

Retain capability/role/authority ownership.

Clarify integration:

- #10 owns platform role contracts and the semantic-preference vs contract vs native-permission distinction;
- #12 owns Task-domain `DecisionAuthority`, trusted human-confirmation integration, Guard/contract mutation authority and Profile capability-envelope consumption;
- Profile capability envelopes must not grant permissions or redefine role contracts;
- Task profile `required/default/allowed` capabilities operate only inside #9/#10 effective composition.

Avoid copying Task domain types into #10.

### #11 — targeted update

Retain Assignment/AgentResult protocol.

Add integration statements:

- durable WorkItem owner is planning state, not transport;
- coordinator resolves the logical owner into canonical Assignment at execution time;
- AgentResult evidence/verification is converted/referenced as typed Task Evidence by #12;
- WorkItem/child Task completion is not inferred solely from `AgentResult.completed`;
- remediation proposal provenance must remain available when #12 materializes candidate durable work.

Do not move WorkItem state-machine semantics into #11.

### #12 — full rewrite

Use the prepared `draft-infra-12.md`.

The accepted proposal is canonical architecture; #12 is the bounded implementation issue.

### #13 — targeted update

Retain current runtime-boundary hierarchy.

Add Task Runtime as a concrete consumer:

- start with pure F# library/scripts/CLI where sufficient;
- introduce local MCP only when repeated interactive calls/lifetime benefits justify it;
- if OpenCode managed-region protection requires a plugin/hook, keep it thin and delegate deterministic Task domain logic to F#;
- do not force Task Runtime behind MCP for architectural uniformity.

### #14 — minimal integration update

Retain .NET verification semantics.

Clarify:

- build/test/review results map into #12 typed Evidence/Guard requirements;
- a transport-successful MCP call is not verification evidence unless its semantic result is valid;
- superseded verification evidence must no longer satisfy current Task Guards/ACs.

Do not make #14 own Task Evidence domain.

### #15 — targeted integration update

Retain logical knowledge model.

Clarify:

- Profile selects the capability envelope; #15 resolves knowledge inside the effective enabled/authorized capability set;
- profile capability `required/default/allowed` is **not** the same concept as knowledge `required/allowed/hinted/lazy`;
- Assignment remains the invocation-level knowledge authorization boundary;
- Task/Profile composition may contribute caller-known routing input but cannot bypass #15 authorization semantics.

### #17 — targeted integration update

Retain audit/eval behavior.

Clarify:

- audit findings/remediation proposals may target Task/Profile/Guard/authority defects;
- auditor remains read-only;
- #12 materialization uses the accepted Task activation/Profile selection/authority rules;
- a remediation task completion still requires bounded re-observation when the audit finding is externally observable.

### `happy-life#1` — full rewrite

Use the prepared `draft-happy-life-1.md`.

Major changes:

- replace old `general/default` / `life/general` taxonomy with `Kind = research|execution` + project-local `life` Profile;
- use recursive Work Tree and the shared Guard/Evidence/Decision lifecycle;
- retain ActionGuard/connectors/project storage downstream;
- migration becomes replay -> shadow -> low-risk canary -> selective active-record migration -> cutover;
- historical OPS remains readable; no mass rewrite;
- one writable lifecycle after cutover.

### #16 — update last

Keep sequence unchanged.

Update #12 description to Universal Task Framework and add:

- accepted Task proposal is canonical for Task semantics;
- implementation issue #12 references it rather than duplicating it;
- #9 must carry the proposal and aligned backlog into the destination repository.

Do not copy proposal details into the tracker.

## 9. Recommended GitHub edit order

This is the **edit/migration order**, not implementation order:

```text
#9 small cutover/proposal migration patch
-> #10 boundary patch
-> #11 integration patch
-> #12 full rewrite
-> #13 integration patch
-> #14 minimal evidence integration patch
-> #15 integration patch
-> #17 integration patch
-> happy-life#1 full rewrite
-> #16 tracker update LAST
```

Implementation order remains:

```text
#9 -> #10 -> #11 -> #12 -> #13 -> #14 -> #15 -> #17
```

## 10. Verification after applying writes

After all updates:

- search `infra#9-#17` and `happy-life#1` for stale taxonomy:
  - `general/default`
  - `research/default`
  - `software/implementation`
  - `kind=implementation`
  - `kind=general`
  - required `domain`
  - `life/general`
  - persisted `ready`
- verify no issue makes ActionGuard shared core;
- verify #10/#11/#12 do not compete for role/communication/task-domain ownership;
- verify #13 owns runtime boundary, not Task semantics;
- verify #15 owns knowledge authorization/loading, not Profile semantics;
- verify #16 contains sequencing/ownership only;
- verify #17 remains read-only;
- verify Happy Life migration uses one authoritative writable lifecycle after cutover;
- re-fetch every changed issue and compare with the prepared draft/patch.

## 11. GitHub mutation status

**Applied on 2026-09-09.**

### Full issue-body replacements

- `masterlifting/infra#12`
  - new title: `OpenCode 4/7: Implement the Universal Task Framework and deterministic Task Runtime`
  - body replaced with the reviewed implementation draft;
  - re-fetched after write and verified.

- `masterlifting/happy-life#1`
  - new title: `Migrate Happy Life from life-ops to the Universal Task Framework and retire OPS`
  - body replaced with the reviewed downstream migration draft;
  - re-fetched after write and verified.

### Append-only authoritative alignment addenda

To avoid accidentally dropping mature requirements from large otherwise-correct issue bodies, the targeted updates were applied as authoritative alignment comments rather than destructive full-body replacements.

Each addendum explicitly requires #9 cutover to fold it into the destination canonical issue body, so the comments are temporary migration artifacts rather than a permanent second source of truth.

- `infra#9` — proposal/backlog migration and destination #4 naming;
- `infra#10` — capability/role/platform-authority vs Task-domain authority boundary;
- `infra#11` — WorkItem owner -> Assignment/AgentResult integration;
- `infra#13` — Task Runtime boundary selection; MCP is not mandatory;
- `infra#14` — typed Task Evidence integration;
- `infra#15` — Profile capability envelope vs knowledge authorization/loading;
- `infra#17` — final Task materialization semantics for audit remediation;
- `infra#16` — applied last; authoritative roadmap addendum with unchanged sequence and updated #12 ownership.

All addenda were re-fetched after creation and verified.

### Deliberately unchanged

- Mandatory implementation sequence remains:
  `#9 -> #10 -> #11 -> #12 -> #13 -> #14 -> #15 -> #17`.
- Existing mature implementation requirements in #9/#10/#11/#13/#14/#15/#17 were not rewritten merely to normalize wording.
- The accepted architecture proposal is now preserved in `infra/dev` as a lossless SHA-256-verifiable seven-part snapshot under `docs/opencode-modernization/universal-task-framework.snapshot/`. #9 still owns reconstructing/migrating it as one canonical architecture document in the destination repository.

## 12. Cutover obligations created by this migration

When #9 creates/cuts over to the dedicated shared OpenCode repository, it must:

1. commit the accepted Universal Task Framework proposal as the canonical architecture document;
2. recreate/migrate the aligned issue bodies;
3. fold every temporary `Backlog alignment addendum` from infra issues into the corresponding destination issue body;
4. avoid carrying stale superseded Task taxonomy as authoritative text;
5. leave the infra copies as historical pointers rather than independently editable canonical requirements;
6. verify the destination tracker still preserves the mandatory issue sequence.

This folding step is mandatory: the temporary comments are a migration-safety mechanism, not the desired final documentation topology.


## 13. Static GitHub preservation package

To remove dependence on ChatGPT/session-local files, the following records are preserved on `masterlifting/infra` branch `dev` under `docs/opencode-modernization/`:

- `README.md` — preservation-package index and canonicality rules;
- `implementation-roadmap.md` — mandatory issue order and ownership;
- `backlog-alignment-evidence.md` — issue/comment evidence and superseded-taxonomy snapshot;
- `backlog-migration-ledger.md` — this migration/ownership ledger;
- `universal-task-framework.snapshot/manifest.md` + `part-01.md` ... `part-07.md` — lossless proposal snapshot.

The proposal snapshot reconstructs to SHA-256:

`d14ab3779fe51728728a9925145d34e791b0d5e23f870f8fbcc8ee02239c085c`

#9 must migrate/reconstruct these records into the dedicated shared OpenCode repository, verify the hash, and then treat the `infra` package as historical evidence rather than a parallel editable source.
