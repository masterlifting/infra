- `ProducerRole`/`ProducerId` are optional structured attributes used by requirements such as independent review;
- pure Task domain validates shape; effective role/agent existence belongs to platform integration;
- Evidence content is immutable once recorded;
- later contract revision/reopen may change only `EvidenceValidity`, preserving the original Evidence as durable history;
- only `Valid` Evidence may satisfy current Guards or Acceptance Criteria.

### 8.1 Evidence scope

Guard scope is strict:

- a `WorkItemTarget` Guard may consume only `Valid` Evidence explicitly referenced by that WorkItem;
- a `TaskTarget` Guard may consume only `Valid` Task-level Evidence;
- descendant or unrelated Evidence is never searched implicitly;
- if descendant Evidence should satisfy a parent Guard, attach/reference the resulting Evidence explicitly on the parent.

Acceptance Criteria have a different scope:

> `VerifyAcceptanceCriterion` may reference any `Valid` Evidence inside the same Task.

This preserves task-level acceptance synthesis without weakening local Guard ownership.

`VerifyAcceptanceCriterion` always requires a non-empty Evidence reference list.

### 8.2 Evidence supersession cascade

`SupersedeEvidence` is not a cosmetic metadata update. After an Evidence record changes from `Valid` to `Superseded`, Task Runtime deterministically recomputes every persisted state that relied on that Evidence.

Canonical cascade:

1. mark the Evidence `Superseded(reason)`; never delete its content;
2. for every `Verified` Acceptance Criterion referencing it:
   - keep only still-`Valid` referenced Evidence;
   - if no `Valid` referenced Evidence remains, transition the AC to `Pending`;
3. recompute every affected Guard using the normal target-scoped Guard-satisfaction function;
4. if a `Done` WorkItem no longer satisfies one of its `BeforeComplete` Guards, reopen that WorkItem to `Pending`;
5. recursively reopen any terminal parent WorkItem whose subtree/child completion invariant is no longer true;
6. recompute Task completion; if a `Complete` Task no longer satisfies `CanCompleteTask`, transition its durable lifecycle to `Open`;
7. preserve all prior Evidence, Decision, result, and completion history.

No LLM judgment is required for this mechanical invalidation cascade. If the remaining Evidence is semantically questionable but still structurally `Valid`, the coordinator may separately supersede it through an explicit command.


## 9. Decisions and trusted authority

Decisions are first-class domain entities with stable IDs and **typed authorization targets**.

```fsharp
type DecisionAuthority =
    | User
    | Coordinator
    | ProfilePolicy

type DecisionKind =
    | UserDecision
    | DesignDecision
    | Assumption
    | PolicyApplication
    | ContractRevision
    | ApplicabilityDecision
    | WaiverDecision

type DecisionTarget =
    | WaiveAcceptanceTarget of AcceptanceId
    | GuardDispositionTarget of GuardId
    | SkipWorkItemTarget of WorkItemId
    | RequirementChangeTarget of WorkItemId
    | ReopenTaskTarget of TaskId * NonEmptyList<ReopenTarget>
    | AbortTaskTarget of TaskId
    | ContractPatchTarget of ContractPatchId
    | QuestionResolutionTarget of QuestionId
    | OtherDecisionTarget of string

type DecisionDraft = {
    Kind: DecisionKind
    Targets: NonEmptyList<DecisionTarget>
    Rationale: string
}

type Decision = {
    Id: DecisionId
    Authority: DecisionAuthority
    Kind: DecisionKind
    Targets: NonEmptyList<DecisionTarget>
    Rationale: string
    CreatedAt: DateTimeOffset
    ConfirmationRef: string option
}
```

A `DecisionRef` is not a generic permission token.

Runtime accepts a referenced Decision only when:

1. the Decision exists and is still applicable;
2. its trusted Authority satisfies the operation's minimum authority;
3. at least one typed `DecisionTarget` exactly matches the attempted semantic operation/target.

A Decision authorizing `WaiveAcceptanceTarget AC1` cannot be reused to waive `AC2`, skip `W3`, or reopen a Task.

If one human confirmation intentionally authorizes multiple actions, the resulting Decision must explicitly contain the complete target set. Authorization is never inferred from similar prose.

### 9.1 Authority is derived, never self-declared

A semantic command must **not** contain `Authority = User`.

The runtime receives authority through a trusted invocation context supplied by the environment:

```fsharp
type InvocationAuthority =
    | CoordinatorInvocation
    | ConfirmedUserInvocation of ConfirmationReceipt
```

Canonical boundary:

```text
ordinary LLM/task_apply
    -> CoordinatorInvocation

trusted human-confirmation path
    -> ConfirmedUserInvocation(receipt)
```

The OpenCode adapter/runtime integration must make the `ConfirmedUserInvocation` path unavailable to an ordinary model-authored `task_apply` call. A model string claiming that the user approved something is not a confirmation receipt.

This is a security/authority invariant, not a prompt convention.

### 9.2 One canonical authorization resolver

Every semantic operation declares:

- an exact typed `DecisionTarget`;
- a `MinimumAuthority`.

The runtime resolves authorization through one function conceptually equivalent to:

```text
authorize(target, minimumAuthority, decisionRef?, invocationAuthority)
```

A User-authorized operation is valid through either of two equivalent paths:

**Current confirmation**

```text
ConfirmedUserInvocation(receipt)
  -> runtime verifies the exact operation/target
  -> runtime creates a new target-bound User Decision atomically
  -> operation proceeds
```

**Previously confirmed Decision**

```text
DecisionRef
  -> referenced Decision.Authority = User
  -> DecisionTarget exactly matches the attempted operation
  -> operation proceeds
```

If both are present, they must not conflict.

For Coordinator-authorized operations, `CoordinatorInvocation` is sufficient; when durable decision provenance is required, runtime creates the target-bound Coordinator Decision rather than requiring the model to manufacture it first.

This means `DecisionRef` on a command is a reusable authorization/provenance input, **not** the only way to obtain authority.

### 9.3 ProfilePolicy provenance is runtime-only

`ProfilePolicy` is retained as Decision provenance for deterministic Profile materialization/application only.

ProfilePolicy Decisions:

- are created only by Profile Resolver/Task Runtime from trusted structured Profile policy;
- never enter through ordinary `task_apply`;
- cannot be forged or requested by the LLM;
- do not participate in `ExplicitDecision` minimum-authority checks;
- do not waive User/Coordinator decisions.

`ExplicitDecision` therefore uses only:

```fsharp
type MinimumAuthority =
    | CoordinatorAuthority
    | UserAuthority
```

### 9.4 Minimum authority

For semantic commands that permit either level:

```text
Coordinator < User
```

A higher semantic authority never grants operations denied by runtime permissions or violates a role contract.

### 9.5 Waiver/default rules

By default:

- required Acceptance Criterion waiver: `User`;
- required WorkItem waiver/demotion after baseline: `User`;
- optional WorkItem skip: Coordinator reasoning plus a durable `Reason`; no separate Decision entity is required;
- `NotApplicable` requires either a target-matching Decision or current trusted invocation satisfying the Guard/Work policy's declared authority;
- ProfilePolicy may materialize structured policy but cannot impersonate User authority;
- `NotApplicable` and `Waived` remain distinct.

A user-authority spoof attempt through the ordinary coordinator invocation path must fail deterministically.

### 9.6 Exact ContractPatch authorization

A contract revision requiring User authority is authorized against the **proposed patch**, not the revision that will be created afterward.

Before confirmation/runtime apply:

```text
canonical ContractPatch payload
    -> deterministic ContractPatchId / fingerprint
    -> DecisionTarget = ContractPatchTarget(patchId)
```

After successful apply, runtime creates the new `ContractRevisionId` and records it as outcome/provenance. The resulting revision ID is not the security target because it did not exist when the exact patch was confirmed.


## 10. Open Questions

Questions that materially affect continuation are first-class domain entities.

```fsharp
type QuestionImpact =
    | TaskWide
    | WorkItems of NonEmptyList<WorkItemId>

type QuestionState =
    | Open
    | Resolved of DecisionRef

type Question = {
    Id: QuestionId
    Text: string
    Impact: QuestionImpact
    State: QuestionState
}
```

Mechanics:

### `TaskWide`

While open:

- no `Pending` WorkItem derives `Ready`;
- `StartWorkItem` is rejected;
- `CanCompleteTask = false`.

Already `Active` WorkItems are not automatically stopped; the coordinator must explicitly block them if the question invalidates their current work.

### `WorkItems [...]`

Only the referenced WorkItems are prevented from deriving `Ready` / starting while the question remains open.

---
### 10.1 Uncertainty classification

Preserve the useful clarification policy from the previous task lifecycle without making Clarification a mandatory phase.

Classify material uncertainty as:

```text
BLOCKING
ASSUMPTION
NON-BLOCKING
```

Canonical mapping into the domain model:

- `BLOCKING` -> create/retain an Open Question with `TaskWide` or specific `WorkItems` impact; affected work cannot proceed until resolved;
- `ASSUMPTION` -> record a `Decision` with `Kind = Assumption`, rationale, and the available evidence/contract basis;
- `NON-BLOCKING` -> Coordinator resolves it without interrupting execution, recording a Decision only when it is materially useful for continuity/review.

Ask the user only for unresolved `BLOCKING` information that can materially alter observable behavior, architecture/contracts, acceptance, data integrity, safety/security, required inputs/outputs, or a protected external action.

This policy belongs to semantic task reasoning; the runtime enforces the resulting Questions/Decisions, not the natural-language classification itself.


# Part II — Work Tree and guards

## 11. Recursive Work Tree

Every Task has one recursive Work Tree using the same WorkItem contract at every depth.

```text
Task
└── WorkItem
    ├── WorkItem
    │   ├── WorkItem
    │   └── WorkItem
    └── WorkItem
```

Conceptual WorkItem:

```text
id
title
state
requirement
owner?
result?
evidenceRefs[]
acceptanceRefs[]
dependsOn[]
resumeCondition?
childTask?
guards[]
children[]
```

Omit empty ceremony fields.

### 11.1 Stable WorkItem IDs

Examples:

```text
W1
W2
W2.1
W2.2
W2.2.1
```

Once a WorkItem is durable:

- its ID is never reused for unrelated work;
- completed WorkItems are never physically deleted;
- task redesign uses `Skip`, `Reopen`, or new WorkItems rather than rewriting history.

---

## 12. WorkItem state and readiness

Persisted state:

```fsharp
type SkipDisposition =
    | Optional
    | NotApplicable
    | Waived

type WorkItemState =
    | Pending
    | Active
    | Waiting of ResumeCondition
    | Blocked of Blocker
    | Done
    | Skipped of SkipDisposition
```

Meanings:

- `Optional`: optional work intentionally not executed;
- `NotApplicable`: the work does not apply under the relevant policy/Decision;
- `Waived`: required/applicable work is intentionally not executed under sufficient authority.

`Ready` is **derived**, never persisted:

```fsharp
type Readiness =
    | Ready
    | NotReady of ReadinessReason list
```

A `Pending` WorkItem derives `Ready` only when all mechanically checkable prerequisites are satisfied:

- same-task dependencies are satisfied;
- no applicable open Question blocks the WorkItem;
- required child-task dependency state is structurally acceptable;
- every `BeforeStart` Guard targeting the WorkItem is satisfied, validly `NotApplicable`, or validly `Waived`;
- the effective contract otherwise permits execution.

A Task-wide open Question makes every `Pending` WorkItem `NotReady` and makes `StartWorkItem` fail. Already `Active` work is not stopped automatically.

