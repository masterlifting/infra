
The collections may be empty for a minimal `general` Profile.

There is no generic `validationRequirements[]` field. Mechanically enforceable completion requirements belong in guards; other deterministic validation must have an explicitly defined consumer rather than becoming a second policy channel.

### 17.2 Initial shared Profiles

Start with:

```text
general
software
opencode
```

#### `general`

Fallback with minimal specialization.

#### `software`

`common` owns repository/codebase context, relevant branch/HEAD/working-tree freshness, and software engineering conventions.

`research` refines technical investigation without automatically importing implementation/build/test gates. It is read-only by default; experiments are evidence-gathering actions only when useful and authorized.

`execution` owns software implementation/verification policy:

- engineer owns implementation and implementation-side build evidence;
- tester owns materially applicable test design/implementation/execution when such a route exists;
- independent reviewers remain read-only;
- build/test/review requirements are materialized as typed Guards according to risk and applicability;
- repository/branch state is required/revalidated only when relevant to the next software action.

Architecture sensitivity and execution complexity are separate axes of reasoning:

- a large/complex but architecture-obvious change does not automatically require architecture escalation;
- a small change can still be architecture-sensitive when it affects public contracts, boundaries, persistence/data migration, concurrency, security, or cross-component ownership.

Independent review may be `NotApplicable` only when the applicable Profile Guard policy permits it and the task is demonstrably bounded/low-risk with sufficient deterministic evidence. If applicability is uncertain, retain the review requirement rather than silently dropping it.

Debugging inside `software + execution` follows evidence-driven semantics when materially applicable:

```text
reproduce / establish facts
-> form ranked hypothesis
-> run smallest distinguishing test
-> fix only after evidence supports the diagnosis
-> verify
```

Do not create a `debug` Kind/Profile unless repeated tasks demonstrate genuinely different lifecycle/acceptance semantics.

#### `opencode`

`common` owns harness-specific architecture/context:

- agents;
- skills;
- contracts;
- knowledge;
- composition;
- instruction authority;
- permissions;
- plugins/tools/MCP;
- model-facing Markdown;
- cross-file semantic consistency.

`research` owns evidence expectations for harness investigation, including current runtime/config behavior where version-sensitive.

`execution` owns harness modification/verification/review policy, including authority/composition impact and semantic cross-file consistency where material. OpenCode Profile policy composes with, rather than duplicates, the general software/capability contracts it actually needs.

### 17.3 Project-local Profiles

Projects may add Profiles, e.g. `life` in Happy Life.

A project-local Profile may own domain-specific policy such as:

- people/organization context;
- deadlines;
- source freshness;
- connector use;
- sensitive data;
- ActionGuard integration;
- external-effect verification;
- long-running waiting patterns.

Life-specific machinery remains downstream unless repeated cross-project evidence justifies promotion.

---
### 17.4 Profile selection

The coordinator selects exactly one primary active Profile before material execution.

It initially sees compact registry metadata only:

```text
id
description
positive routing triggers
negative/exclusion triggers
idPrefix?
```

Selection precedence:

1. an explicit valid active Profile selected by the user/caller;
2. otherwise semantic classification from task context and active registry metadata;
3. if no specialized Profile is materially justified, use `general`;
4. if ambiguity could materially change safety, capability authorization, required evidence/Guards, or acceptance semantics, resolve the ambiguity before material execution.

The LLM may choose only an active registered Profile and must not invent an ID.

Record rationale only when classification is non-obvious/material to resume/review; obvious selections need no ceremony.

### 17.5 Reclassification

New evidence may prove the selected Kind/Profile materially wrong.

Before baseline, Coordinator may reclassify through `ReclassifyTask` as ordinary Draft design.

After baseline, `ReclassifyTask` is a controlled contract change:

- preserve all Decisions, Evidence, history, and stable WorkItem/AC identity that remains meaningful;
- resolve the new Effective Profile and materialize new/stronger mandatory policy;
- never silently discard existing obligations/confirmations;
- weakening/removing baselined requirements follows the normal trusted authority rules;
- changing Kind after baseline changes the primary outcome semantics and therefore requires trusted User authority unless the existing contract explicitly anticipated that change.

Do not bounce between Profiles merely for stylistic preference.

### 17.6 Compositional task presentation

Task presentation/template composition follows the same DRY model as the domain contract:

```text
Core task presentation
+ Kind semantic fragment
+ Effective Profile required sections/guidance
+ Project overlay fragment
= effective TASK presentation
```

Do not maintain independent full `TASK.md` template copies for every Kind/Profile combination.

Examples:

- `research` adds question/evidence/findings/conclusion/uncertainty semantics;
- `execution` adds target-state/execution/verification semantics;
- `software` adds repository/solution/build-test-review surfaces only when applicable;
- `opencode` adds harness authority/composition/semantic-validation surfaces;
- project Profiles may add domain-specific sections such as deadlines/participants/sensitive-source pointers.

Structured state remains owned by the runtime; presentation fragments must not create a second writable source of truth.


## 18. Profile capability envelope versus roles

Keep capabilities and role routing distinct.

Example:

```yaml
capabilityEnvelope:
  required:
    - opencode
  default: []
  allowed:
    - dotnet
    - audit
    - security
    - devops

policy:
  roleDefaults:
    - purpose: architecture
      role: architect
```

Capabilities expose agents/knowledge/contracts/tools/runtime.

Canonical envelope semantics:

- `required`: always active for the Effective Profile/Task; if unavailable in the effective platform composition, task creation/execution blocks;
- `default`: the small baseline set automatically selected by the Profile unless an explicit supported project specialization changes those defaults;
- `allowed`: not activated merely by being listed; the Coordinator may activate an allowed capability when task context demonstrates need.

The effective capability set is therefore:

```text
required
+ applicable defaults
+ context-selected allowed subset
```

Workers/subagents cannot silently expand beyond the envelope. If execution reveals a need for an unavailable/not-allowed capability, return to Coordinator/Profile reasoning instead of broadening authority locally.

Roles/owners describe responsibility and remain distinct from capabilities.

Pure Task domain validates owner shape only. Whether a capability/route/agent actually exists in the effective platform composition is checked by the platform integration/resolver layer.

---

## 19. Project Profile overlays

Projects may:

1. add a new Profile ID; or
2. explicitly overlay an existing shared Profile with the same ID.

Same ID means **specialization**, not unrelated replacement.

Materially incompatible semantics require a new Profile ID.

### 19.1 Deterministic overlay resolution

Resolver:

1. discovers active shared Profiles;
2. discovers explicit project Profiles/overlays;
3. validates IDs/relationships;
4. merges structured metadata by fixed rules;
5. rejects ambiguous duplicate IDs;
6. constructs one Effective Profile;
7. computes `profileFingerprint`;
8. exposes routing metadata before loading full semantic guidance.

Filesystem order never determines authority.

### 19.2 Mechanically enforceable versus semantic conflicts

Structured weakening that is mechanically detectable fails resolution, e.g. invalid removal of a mandatory structured guard.

Natural-language semantic contradiction is **not** proven by the resolver. Semantic conflicts are handled by the platform authority model/auditor/LLM reasoning.

Do not build a generic natural-language policy prover.

---

## 20. Profile drift

A durable Task stores the Effective Profile fingerprint used to materialize its current contract.

On resume, if the Effective Profile fingerprint changes, surface:

```text
PROFILE_DRIFT
```

### 20.1 Before baseline

Re-resolve normally; Draft design absorbs the current Profile.

### 20.2 After baseline, while Task is Open/Paused

Re-resolve and reconcile **monotonically toward stronger requirements**:

- newly added mandatory Guards are materialized;
- strengthened mandatory Guards are re-evaluated/materialized;
- newly introduced hard safety/permission constraints apply immediately;
- removed/weakened Profile requirements do **not** silently remove existing baselined obligations;
- an existing Guard is never physically deleted from baselined history merely because the current Profile no longer emits it;
- weakening an existing task obligation requires the normal authorized contract-revision/disposition path.

### 20.3 Complete/Aborted history

Do not retroactively rewrite terminal history merely because a Profile changes.

If a Task is later reopened:

1. resolve the current Profile;
2. detect drift;
3. materialize new/stronger requirements;
4. retain old requirements/history unless explicitly and validly revised;
5. invalidate only the reopen targets/receipts required by the new contract.

Profile drift is not an authority bypass.


# Part IV — Contract baseline and mutation

## 21. Contract lifecycle

Task semantic contract has two phases:

```fsharp
type ContractState =
    | Draft
    | Baselined of ContractRevision
```

### 21.1 Draft

Before material execution, the Coordinator may design/refine Objective, Scope, Non-Goals, Acceptance Criteria, Work Tree, Guards, ownership, and requirement flags through the same canonical `ApplyContractPatch`/task-design commands.

Draft rules:

- ordinary Coordinator invocation is sufficient for any Draft contract patch that does not itself perform a protected external effect;
- `stateRevision` still increments for every persisted mutation and participates in CAS;
- Draft edits do not create post-baseline `contractRevision` history for every intermediate wording change;
- Profile mandatory policy must still be present before baseline.

There are no parallel `SetDraft*` command families.

### 21.2 Baseline

Material execution must not begin before the Task has a baseline.

The normal trigger is the first accepted `StartWorkItem` for material work. Immediately before starting, runtime:

1. validates the Draft;
2. ensures mandatory Profile Guards are materialized;
3. establishes the current contract fingerprint as baseline;
4. creates/increments the initial `contractRevision`;
5. then evaluates `BeforeStart` Guards/readiness and starts the WorkItem.

Research should baseline stable outcome-quality ACs, for example:

- evidence-backed conclusion;
- materially relevant alternatives considered where needed;
- uncertainties/limitations explicit.

Newly discovered research dimensions should normally be additive Coordinator revisions rather than premature conclusion-specific ACs.

---

## 22. ContractPatch and authority

Use one explicit operation-based patch model. Runtime does not judge fuzzy textual "materiality".

```fsharp
type ContractPatch =
    | AddAcceptanceCriterion of AcceptanceDraft
    | AddGuard of GuardDraft
    | RemoveGuard of GuardId
    | UpdateAcceptanceCriterion of AcceptanceId * ContractText
    | RemoveAcceptanceCriterion of AcceptanceId
    | SetObjective of ContractText
    | SetScope of ContractText
    | SetNonGoals of ContractText
```

### 22.1 Draft authority

In `Draft`, Coordinator authority is sufficient for all listed patches, with one restriction:

- `RemoveGuard` is allowed only for `Origin = TaskDesign`;
- `ProfileMaterialized` Guards are not manually removed; change/re-resolve the Profile/contract instead.

### 22.2 Baselined contract authority

| Mutation | Minimum authority |
| --- | --- |
| Add AC | Coordinator |
| Add/strengthen Guard | Coordinator |
| Update existing AC text | User |
| Remove AC | User |
| Set Objective | User |
| Set Scope | User |
| Set Non-Goals | User |
| Remove Guard physically | Forbidden |

Baselined Guards are not physically removed with `RemoveGuard`.

If a Guard must cease to apply:

- use `NotApplicable` if its applicability policy permits;
- use `Waived` if its waiver policy permits;
- otherwise revise/reclassify through the normal history-preserving contract rules; do not invent deletion as a shortcut.

Every proposed post-baseline patch is first canonicalized to a deterministic `ContractPatchId`. If User authority is required, the authorization target is `ContractPatchTarget(patchId)` so the human confirmation applies to the exact patch payload before mutation.

Every accepted post-baseline contract patch:

- uses the unified Section 9.2 authorization resolver;
- records a Decision with the exact `ContractPatchTarget(patchId)`;
- creates a new `contractRevision`;
- updates `contractFingerprint`;
- invalidates/supersedes only affected AC/Guard verification Evidence/receipts;
- preserves prior history.

Changing WorkItem `Required <-> Optional` uses the dedicated semantic command:

- `Optional -> Required`: Coordinator;
