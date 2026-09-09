- `Required -> Optional`: User after baseline.

No command accepts a caller-supplied `Authority = User` value.

### 22.3 Task lifecycle command authority

| Lifecycle command | Minimum authority / guard |
| --- | --- |
| `PauseTask` | Coordinator |
| `ResumeTask` | Coordinator |
| `CompleteTask` | Coordinator + `CanCompleteTask = true` |
| `AbortTask` while Draft | Coordinator |
| `AbortTask` after baseline | User |
| `ReopenTask` from Complete | Coordinator or User under the reopen contract |
| `ReopenTask` from Aborted | User |

`CompleteTask` remains side-effect-free; lifecycle authority never substitutes for external-effect confirmation. Every lifecycle command requiring User authority uses the same Section 9.2 authorization resolver: either current `ConfirmedUserInvocation` or an exact pre-existing target-bound User Decision.


## 23. Contract fingerprint and out-of-band edits

Contract-critical semantic Markdown is fingerprinted, including at least:

- Objective;
- Scope;
- Non-Goals;
- Acceptance Criteria text/IDs.

If the file changes outside Task Runtime and the fingerprint no longer matches, surface:

```text
CONTRACT_DRIFT
```

Do not automatically treat an external edit as a user decision.

While drift is unresolved:

- read-only analysis is allowed;
- material state-changing commands are rejected;
- runtime produces a structured diff where possible;
- runtime may propose which ACs/Guard receipts should be invalidated.

Reconciliation does **not** have special authority. It is converted into the normal contract operations:

```text
accept strengthening/addition
  -> Coordinator if the corresponding patch allows it

reject external edit / restore current canonical contract
  -> Coordinator

accept weakening/removal/material Objective/Scope/AC rewrite
  -> User through trusted confirmation path
```

`ReconcileContractDrift` may exist as an adapter convenience, but internally it must resolve to ordinary `ContractPatch`/authority rules rather than bypass them.


# Part V — Commands and deterministic runtime

## 24. Functional core / imperative shell

Use F#/.NET for substantive deterministic domain logic.

Boundary:

```text
TASK structured state (untrusted JSON)
    -> strict Wire DTO
    -> validated Domain types / DUs
    -> pure decide/evolve
    -> DTO
    -> persistence
```

Do not serialize domain DUs directly as the external JSON contract. Keep a strict DTO boundary using `System.Text.Json` or equivalent built-in platform facilities.

Unknown/invalid structural values fail parsing/validation.

---

## 25. Command model and authority context

Commands carry required semantic payloads in the type itself.

```fsharp
type SkipRequest =
    | SkipOptional of Reason
    | MarkNotApplicable of DecisionRef option
    | WaiveRequired of DecisionRef option

type ReclassificationRequest = {
    Kind: Kind option
    Profile: ProfileId option
    Reason: Reason
}

type TaskCommand =
    | StartWorkItem of WorkItemId
    | CompleteWorkItem of WorkItemId * WorkItemCompletion
    | WaitWorkItem of WorkItemId * ResumeCondition
    | BlockWorkItem of WorkItemId * Blocker
    | ResumeWorkItem of WorkItemId * ObservationRef option
    | ReopenWorkItem of WorkItemId * Reason
    | SkipWorkItem of WorkItemId * SkipRequest
    | RebindOwner of WorkItemId * Owner * Reason
    | SetWorkItemRequirement of WorkItemId * WorkRequirement * DecisionRef option

    | AddEvidence of EvidenceTarget * Evidence
    | SupersedeEvidence of EvidenceId * Reason
    | VerifyAcceptanceCriterion of AcceptanceId * NonEmptyList<EvidenceRef>
    | WaiveAcceptanceCriterion of AcceptanceId * DecisionRef option
    | MarkGuardNotApplicable of GuardId * DecisionRef option
    | WaiveGuard of GuardId * DecisionRef option

    | AddDecision of DecisionDraft
    | AddQuestion of QuestionDraft
    | ResolveQuestion of QuestionId * DecisionRef

    | ApplyContractPatch of ContractPatch
    | ReconcileContractDrift of ReconciliationPlan
    | ReclassifyTask of ReclassificationRequest

    | PauseTask of Reason
    | ResumeTask
    | CompleteTask of TerminalHandoff
    | AbortTask of Reason * DecisionRef option
    | ReopenTask of ReopenRequest
```

Canonical apply boundary:

```text
task_apply(
  taskId,
  expectedStateRevision,
  trustedInvocationAuthority,
  command
)
```

The model supplies `command`; the environment supplies `trustedInvocationAuthority`.

For ordinary model-driven calls:

```text
trustedInvocationAuthority = CoordinatorInvocation
```

For commands requiring User authority, either:

- a target-matching existing User `DecisionRef` is supplied; or
- the adapter invokes the same command through the separate human-confirmed path using `ConfirmedUserInvocation`, and runtime creates the exact target-bound User Decision atomically.

`ProfilePolicy` Decisions do not pass through this apply boundary; Profile Resolver/Task Runtime creates them only from trusted structured Profile policy.

### 25.1 Reclassification command authority

`ReclassifyTask` implements the semantics from Section 17.5:

- Draft Kind/Profile change: Coordinator;
- baselined Kind change: User;
- baselined Profile change: Coordinator only when reconciliation is monotonic/non-weakening;
- any baselined reclassification that weakens/removes an obligation requires User authority.

Reclassification always re-resolves the Effective Profile, materializes new/stronger mandatory Guards, preserves history, and never silently discards prior obligations.

The unified Section 9.2 authorization resolver applies: a baselined reclassification requiring User authority may use either the current `ConfirmedUserInvocation` or an exact pre-existing target-bound User Decision when such a target is represented.

### 25.2 Payload and authorization invariants

- `WaitWorkItem` requires `ResumeCondition`;
- `BlockWorkItem` requires `Blocker`;
- `ReopenWorkItem` requires `Reason`;
- `RebindOwner` requires `Reason`;
- `SkipOptional` requires only `Reason`;
- `MarkNotApplicable` requires authorization satisfying the exact WorkItem target/policy;
- `WaiveRequired` requires User authorization for the exact WorkItem target;
- `SetWorkItemRequirement Required -> Optional` after baseline requires User authorization for the exact WorkItem target;
- `WaiveAcceptanceCriterion` requires User authorization for the exact AC target;
- `MarkGuardNotApplicable` / `WaiveGuard` require authorization for the exact Guard target according to its policy;
- `AbortTask` requires `Reason`; after baseline it requires User authorization for `AbortTaskTarget(taskId)`;
- `VerifyAcceptanceCriterion` requires non-empty `Valid` Evidence refs;
- `ReopenTask` requires non-empty invalidation targets and exact authorization according to lifecycle state.

Where a command contains `DecisionRef option`, `None` means "authorize from the trusted current invocation and create the durable target-bound Decision if required"; it never means "skip authorization".

Exact DU nesting may change during implementation; these semantic payload requirements are authoritative.


## 26. Command -> Decide -> Events -> Evolve

Preferred pure pipeline:

```text
Command
  -> decide(currentState, command)
  -> Result<Event list, DomainError list>
  -> evolve(currentState, events)
  -> new state
```

Events improve testability and reasoning clarity but do not imply event sourcing.

If a simpler pure `State * Command -> Result<State, Error>` implementation proves materially smaller, it is acceptable as long as the same invariants remain enforceable.

---

## 27. `CanCompleteTask` — canonical mechanical completion invariant

`CanCompleteTask` is deliberately limited to mechanically checkable facts.

It is true only if all of the following hold:

```text
1. no unresolved CONTRACT_DRIFT exists;

2. every Acceptance Criterion is:
      Verified
   OR validly Waived;

3. every WorkItem is terminal:
      Done
   OR validly Skipped;

4. every Required WorkItem is Done
   unless a valid User-authorized requirement change/waiver made another
   terminal disposition permissible;

5. every BeforeComplete Guard targeting any WorkItem/Task is:
      satisfied
   OR validly NotApplicable
   OR validly Waived;

6. no open TaskWide Question exists;

7. every WorkItem with `childTask` that must complete is backed by a child Task whose lifecycle is `Complete`, and no required cross-Task dependency condition remains structurally unresolved.
```

There are no prose-only external-effect or "looks current" clauses in this predicate.

External effects become completion requirements only through structured Acceptance Criteria and/or `EvidenceRequired(kind=ExternalEffect)` Guards.

### 27.1 Terminal handoff

`State / Evidence / Next` remains a durable continuity contract, but semantic freshness is an LLM/coordinator responsibility rather than part of `CanCompleteTask`.

`CompleteTask` is side-effect-free and accepts a terminal handoff payload:

```fsharp
type TerminalHandoff = {
    State: string
    EvidenceSummary: string
    Next: string
}
```

Runtime may require these fields to be non-empty and structurally valid; it cannot prove that prose is semantically current.

Coordinator close-out semantics:

- `State` describes the actual terminal state, not a stale pre-completion phase;
- `EvidenceSummary` summarizes current terminal evidence or points to canonical Evidence/sections;
- `Next` must not contain already-completed lifecycle work;
- when no Task work remains, state that explicitly (for example, `No task work remains.`);
- optional/manual follow-up may remain only when it is genuinely outside completed required work;
- if later evidence invalidates acceptance/completion, reopen the Task and refresh the handoff rather than leaving false terminal prose.

Merge, deploy, publish, send, archive, payment, or any other external mutation must be represented/executed/verified before `CompleteTask` when required by the task contract.


## 28. Progress and readiness

Task Runtime derives:

- readiness;
- operational status (`Active/Waiting/Blocked`);
- progress.

Progress is informational, not completion authority.

Recommended denominator:

- required leaf WorkItems;
- optional leaf WorkItems may be shown separately;
- `Done` counts complete;
- valid `Skipped` is terminal but does not masquerade as executed work;
- denominator may grow through additive design/revision;
- completed WorkItems remain in history.

Do not maintain a second manually editable progress source when runtime can derive it.

---

## 29. Task Runtime responsibilities

Task Runtime owns only task-state mechanics:

```text
profile discovery/resolution
mandatory guard materialization
profile fingerprinting
contract fingerprinting/reconciliation
strict parsing
Task ID allocation
Work Tree validation
readiness/status/progress derivation
command admissibility
Decision/Question/authority checks
guard satisfaction
acceptance state
CanCompleteTask
child-task correlation
CAS/persistence
legacy compatibility parsing
```

Task Runtime does **not** own arbitrary domain effects such as:

- `dotnet build`;
- Git push/merge;
- deployment;
- Gmail sends;
- payments;
- ActionGuard;
- database migrations.

Those belong to capability tools/connectors.

Canonical effect flow:

```text
LLM/coordinator decides effect
-> capability tool executes under its contract/permissions
-> structured ToolResult/Evidence
-> LLM interprets
-> Task Runtime command updates task state
```

---

# Part VI — Ownership and delegation

## 30. Work ownership

WorkItem ownership is a durable design-time responsibility.

Persist a logical owner, e.g. role + capability route or stable logical agent ID.

Do not persist as authority:

- provider/model;
- transient session;
- ephemeral subagent instance.

Children inherit the parent WorkItem owner unless explicitly overridden.

Rebinding requires:

```fsharp
RebindOwner of WorkItemId * Owner * Reason
```

Pure Task domain checks owner shape. Platform integration checks whether the effective route/agent exists.

### 30.1 Assignment integration

```text
WorkItem.owner
  -> resolve effective role/agent
  -> construct canonical Assignment
  -> invoke
  -> receive AgentResult
  -> synthesize
  -> attach Evidence/result
  -> apply Task command
```

WorkItem ownership does not replace Assignment/AgentResult.

---
