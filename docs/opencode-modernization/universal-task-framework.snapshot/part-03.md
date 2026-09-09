### 12.1 Requirement

```fsharp
type WorkRequirement =
    | Required
    | Optional
```

After baseline:

- `Optional -> Required`: minimum `Coordinator`;
- `Required -> Optional`: `User`.

Before baseline this is ordinary task design under Coordinator authority.

Requirement changes happen through the canonical semantic command; no WorkItem is physically deleted to simulate plan changes.

### 12.2 Parent/child state consistency

Starting a nested child must preserve a coherent active ancestry.

A child may start only when every ancestor is `Pending` or `Active`.

`StartWorkItem(child)`:

1. validates the child's readiness;
2. rejects the start if any ancestor is `Waiting`, `Blocked`, `Done`, or `Skipped`;
3. validates every applicable `BeforeStart` Guard on each still-`Pending` ancestor;
4. atomically transitions those `Pending` ancestors to `Active`;
5. then transitions the selected child to `Active`.

The caller does not need to issue ceremony-only `StartWorkItem(parent)` commands first.

Skipping a parent is deliberately **not cascading**.

`SkipWorkItem(parent, ...)` is valid only when every descendant is already terminal under the current contract. If an entire branch should be abandoned, the coordinator must explicitly disposition the affected descendants first. One parent skip must never silently waive several required children.

### 12.3 Canonical WorkItem transition table

The command/state transition table is authoritative:

| Command | Allowed source state(s) | Result state | Required checks/payload |
| --- | --- | --- | --- |
| `StartWorkItem` | `Pending` | `Active` | `Readiness = Ready`; ancestor rules; `BeforeStart` Guards |
| `CompleteWorkItem` | `Active` | `Done` | completion payload; child/subtree/childTask invariants; `BeforeComplete` Guards |
| `WaitWorkItem` | `Active` | `Waiting` | non-empty `ResumeCondition` |
| `BlockWorkItem` | `Pending`, `Active`, `Waiting` | `Blocked` | non-empty `Blocker` |
| `ResumeWorkItem` | `Waiting`, `Blocked` | `Pending` | resume/re-observation semantics for the recorded condition/blocker |
| `ReopenWorkItem` | `Done`, `Skipped` | `Pending` | non-empty `Reason`; preserve prior history |
| `SkipWorkItem` | `Pending`, `Active`, `Waiting`, `Blocked` | `Skipped` | requirement/disposition/authority rules; parent descendants already terminal |

All unlisted source-state/command combinations are invalid and return a structured `DomainError`.

`SupersedeEvidence` may deterministically reopen `Done` WorkItems through the Section 8.2 invalidation cascade; that is a derived consequence of evidence invalidation, not an alternate user-authored transition.


## 13. WorkItem dependencies

`WorkItem.dependsOn` may reference only WorkItems inside the same Task.

Allowed:

- sibling/cousin/cross-branch dependencies.

Forbidden:

- self;
- ancestor;
- descendant;
- external Task WorkItem IDs.

Cross-task dependencies use Task/child-task relationships, not `dependsOn`.

Runtime validates dependency cycles deterministically.

---

## 14. WorkItem completion and parent semantics

Completion is atomic with result/evidence attachment.

```fsharp
type WorkItemCompletion = {
    Result: string option
    EvidenceRefs: EvidenceRef list
}

CompleteWorkItem of WorkItemId * WorkItemCompletion
```

`Done` itself carries no duplicate result/evidence payload. `Result` and `EvidenceRefs` live once on the WorkItem.

A parent WorkItem is **not** automatically `Done` when its children become terminal.

`CompleteWorkItem(parent, payload)` is explicit because a parent may own:

- synthesis/result;
- its own Evidence;
- its own Guards;
- acceptance traceability.

Before a parent can become `Done`, runtime requires:

1. every direct child is terminal (`Done` or validly `Skipped`);
2. every Required child is `Done` unless an authorized requirement change/waiver made another terminal disposition valid;
3. all `BeforeComplete` Guards targeting the parent are satisfied/validly disposed;
4. if the WorkItem references `childTask`, that child Task lifecycle is `Complete` — `Aborted` does not satisfy the parent;
5. the completion payload is structurally valid.

Because this check applies recursively, a completed parent implies its subtree is terminal under the task contract.

If a referenced child Task is `Aborted`, the parent owner must explicitly replan, replace the child, or disposition the parent WorkItem through the normal requirement/skip authority model.

Canonical flow:

```text
receive completion payload
-> validate children/subtree terminality
-> validate childTask = Complete when present
-> attach result/evidence
-> evaluate BeforeComplete guards
-> if valid: Done
-> otherwise: structured DomainError
```


## 15. Guards

Profile prose is not sufficient enforcement.

Mandatory mechanically enforceable Profile policy is materialized deterministically into **Guard slots** in the Effective Task Contract.

### 15.1 Guard requirement types

Use one evidence requirement model rather than parallel test/review requirement systems.

```fsharp
type EvidenceRequirement = {
    Kind: EvidenceKind
    MinimumCount: int
    ProducerRole: string option
    RequireIndependentProducer: bool
}

type DecisionRequirement = {
    Kind: DecisionKind option
    MinimumAuthority: MinimumAuthority
}

type GuardRequirement =
    | EvidenceRequired of EvidenceRequirement
    | DecisionRequired of DecisionRequirement
```

A review requirement is represented as typed Evidence, for example:

```text
EvidenceRequired {
  Kind = Review
  MinimumCount = 1
  ProducerRole = Some "reviewer"
  RequireIndependentProducer = true
}
```

If implementation later proves Review needs semantics that cannot be expressed through this model, add them deliberately; do not pre-create a second receipt system.

### 15.2 Target, checkpoint, and origin

```fsharp
type GuardTarget =
    | TaskTarget
    | WorkItemTarget of WorkItemId

type GuardCheckpoint =
    | BeforeStart
    | BeforeComplete

type GuardOrigin =
    | ProfileMaterialized
    | TaskDesign

type GuardDisposition =
    | Applicable
    | NotApplicable of DecisionRef
    | Waived of DecisionRef

type GuardSlot = {
    Id: GuardId
    Target: GuardTarget
    Checkpoint: GuardCheckpoint
    Origin: GuardOrigin
    Requirement: GuardRequirement
    Applicability: ApplicabilityPolicy
    Waiver: WaiverPolicy
    Disposition: GuardDisposition
}
```

Every Guard has a stable ID and one explicit target/checkpoint/origin.

- `WorkItemTarget + BeforeStart` participates in that WorkItem's readiness / `StartWorkItem`;
- `TaskTarget + BeforeStart` must be satisfied before **any material WorkItem starts** in the Task;
- `BeforeComplete` Guards participate in WorkItem or Task completion.

On the first material start, baseline is established first, then Task-level and WorkItem/ancestor `BeforeStart` Guards are evaluated. A failed Guard may leave the Task Baselined while preventing work from starting.

This is deliberately bounded: two checkpoints, not a user-defined workflow/state-machine language.

### 15.3 Applicability and waiver authority

```fsharp
type ApplicabilityPolicy =
    | Always
    | ExplicitDecision of MinimumAuthority

type WaiverPolicy =
    | NotWaivable
    | WaivableBy of MinimumAuthority

type MinimumAuthority =
    | CoordinatorAuthority
    | UserAuthority
```

`NotApplicable` and `Waived` are distinct:

- `NotApplicable`: requirement does not apply;
- `Waived`: requirement applies but an authorized Decision accepts not satisfying it.

A Profile that permits N/A must declare the minimum authority. The LLM cannot remove a Guard with prose.

Examples:

```text
routine test applicability:
  applicability = ExplicitDecision(Coordinator)
  waiver        = WaivableBy(User)

architecture review:
  applicability = ExplicitDecision(User)
  waiver        = WaivableBy(User)

hard safety guard:
  applicability = Always
  waiver        = NotWaivable
```

### 15.4 Guard satisfaction

Guard satisfaction is a pure runtime function over:

- GuardSlot;
- `Valid` target-scoped Evidence;
- referenced target-matching Decisions;
- target owner/producer identity when independence is required.

Examples:

```text
EvidenceRequired(Test)
  -> matching Valid Test Evidence on the guarded target

EvidenceRequired(Review, role=reviewer, independent=true)
  -> matching Valid Review Evidence on the guarded target
     with compatible ProducerRole and distinct concrete ProducerId

DecisionRequired(...)
  -> matching DecisionRef with sufficient trusted authority
     and a DecisionTarget that matches the Guard/operation
```

`RequireIndependentProducer = true` is mechanically satisfiable only when both the relevant owner/producer identities are concrete IDs. Roles alone are insufficient to prove independence. If identity cannot be established, the Guard remains unsatisfied rather than guessing.

No free-form `looks good` text satisfies a structured Guard.

### 15.5 Guard materialization and history

After `Kind + Effective Profile` are selected, the Profile Resolver/Task Runtime deterministically materializes all mandatory structured Guards.

The LLM may:

- add stronger task-specific Guards;
- propose applicability/waiver Decisions when policy permits;
- provide semantic rationale/evidence.

The LLM may not omit mandatory Profile Guards.

Before baseline, only a `TaskDesign` Guard may be physically removed through the Draft-only contract operation. `ProfileMaterialized` Guards are regenerated from the Effective Profile rather than manually deleted.

After baseline, Guard identity/history is preserved: weakening/removal occurs only through an authorized disposition/revision; Profile drift never silently deletes it.

### 15.6 Gate taxonomy

Use one conceptual gate taxonomy so the LLM understands why a requirement exists while the runtime enforces it through the existing authority/Question/Guard/effect contracts.

```text
Safety gate
  -> trusted human confirmation + runtime permission/effect-tool contract

Decision gate
  -> resolve a material uncertainty/choice before affected work starts
     using Question / Decision / BeforeStart Guard semantics

Quality gate
  -> require sufficient typed Evidence before completion
     using BeforeComplete Guards

Bookkeeping
  -> durable/derived state synchronization only;
     never a human gate by itself
```

Profiles may strengthen Decision/Quality requirements but cannot weaken Core safety/permission rules.

Do not create a second gate state machine; this taxonomy maps onto the existing canonical domain contracts.


## 16. WorkItem versus child Task

Keep work as a WorkItem when it:

- shares the parent Task objective;
- shares Kind/Profile;
- does not need independent acceptance/lifecycle;
- can be represented by the parent Work Tree and evidence model.

Promote work to a child Task only when it materially needs its own:

- objective/scope;
- Kind;
- Profile;
- Acceptance Criteria;
- durable multi-session context;
- independent external effects/gates;
- substantial blockers/dependencies;
- completion semantics.

Child Task completion does not automatically prove the parent's real-world unblock condition. The parent re-observes external truth when required.

Automatic recursive child-task generation is out of scope.

---

# Part III — Profile system

## 17. Profile: primary extension point

A Profile answers:

> Given this class of task, what task-design policy, capabilities, evidence expectations, roles, and structured guards apply?

Profile is compatible with both Kinds by definition.

### 17.1 Minimal structural Profile contract

Every Profile has:

```text
schemaVersion
id
description
idPrefix?

routing
  positive[]
  negative[]

capabilityEnvelope
  required[]
  default[]
  allowed[]

policy
  guards[]
  requiredSections[]
  roleDefaults[]

semanticGuidance
  common?
  research?
  execution?
```
