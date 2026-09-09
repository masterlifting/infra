# Universal Task Framework — Final Architecture Proposal

**Status:** Final architecture — accepted; architecture review closed  
**Scope:** Shared OpenCode `/task` framework and downstream project extension model  
**Primary migration targets:** current global `task` skill and `happy-life` `ops` / `OPS.md` workflow  
**Implementation status:** Architecture accepted; historical coverage audit incorporated; ready for issue decomposition  
**Intent:** Canonical architecture source for implementation issues. If another document conflicts with this proposal, this proposal owns the task-framework semantics unless a later accepted architecture decision explicitly supersedes it.

---

## 1. Executive decision

Replace the current split between:

- the global software/project-oriented `task` workflow; and
- the `happy-life` life/operations-oriented `ops` workflow

with one reusable **Universal Task Framework**.

The framework is deliberately smaller than a generic workflow engine. It provides:

1. one stable **Task Core**;
2. two universal orthogonal **Kinds**:
   - `research`
   - `execution`
3. **Profile** as the primary task-design extension point;
4. one recursive **Work Tree** based on a bounded WorkItem contract;
5. a deterministic **Task Runtime** for mechanically enforceable contracts;
6. LLM reasoning for semantic classification, planning, decomposition, interpretation, and adaptation;
7. project-local Profile creation and constrained Profile overlays;
8. selective migration of reusable OPS semantics without importing life-specific machinery into the shared Core.

Canonical flow:

```text
User intent
    |
    v
LLM coordinator
    |  decide whether durable Task is warranted
    |  choose Kind + Profile
    |  design objective/scope/AC/work/owners
    v
Profile Resolver + Task Runtime
    |  resolve Effective Profile
    |  materialize mandatory structured policy
    |  validate task/work/authority/guards
    v
Effective Task Contract
    |
    +--> Assignment / agents / capability tools
    |
    +<-- structured AgentResult / ToolResult / Evidence
    |
    v
LLM interpretation
    |
    v
semantic Task command
    |
    v
Task Runtime decide -> events -> evolve -> persist
```

Core principle:

> **LLM decides what the work means and what should happen. Deterministic machinery resolves, validates, executes, and records what can be represented as a reliable contract.**

---

## 2. DRY ownership model

This proposal intentionally assigns each concept one canonical owner.

| Concern | Canonical owner |
| --- | --- |
| Universal lifecycle invariants | Task Core |
| Primary outcome semantics | Kind |
| Domain/task-design specialization | Profile |
| Mandatory mechanically enforceable profile policy | Effective Task Contract |
| Work decomposition and local state | Work Tree |
| Decisions / questions / authority | Task domain model |
| Mechanical state transitions | Task Runtime |
| Semantic planning and interpretation | LLM coordinator |
| Specialist knowledge/agents/tools | Capabilities |
| External deterministic effects | Capability tools / connectors |
| Hard operation permissions | OpenCode/runtime permissions |
| Project specialization | Project Profile overlay |

Rules should be referenced from their owner rather than restated in several skills/references.

---

## 3. Explicit non-goals

Do not build:

- Temporal/Airflow-style orchestration;
- a user-authored workflow/state-machine DSL;
- a generic scheduler;
- arbitrary task-script execution;
- a second agent transport;
- a second capability/knowledge resolver;
- an LLM-based structural validator;
- one Profile per feature type, life activity, technology, or edge case;
- automatic recursive child-task generation;
- automatic `audit -> issue -> implementation` loops;
- event sourcing as a persistence requirement;
- a semantic rule language for proving natural-language applicability.

The Task Runtime is a deterministic substrate, not a workflow engine.

---

# Part I — Core domain model

## 4. Effective Task Contract

The effective task is:

```text
Task Core
+ Kind
+ Effective Profile
+ task-specific contract
= Effective Task Contract
```

There is no mandatory `domain` axis and no persistent `linear | stateful` mode.

Statefulness emerges from the Work Tree when it materially uses waiting, blocking, dependencies, resume conditions, or independent branches.

The **Effective Task Contract** is the deterministic contract that the runtime enforces. Mandatory structured Profile policy must be materialized into it by the Profile Resolver/Task Runtime; the LLM cannot omit mandatory guards by forgetting to mention them.

---
### 4.1 When a durable Task is warranted

The framework is universal, but not every user request becomes a durable Task.

The coordinator creates/materializes a Task when at least one of the following materially applies:

- the user explicitly asks for durable task tracking;
- the work is multi-step or expected to span sessions;
- resumability matters;
- work can become `Waiting`/`Blocked` or depends on future/external state;
- an evidence/audit trail is materially useful;
- protected external effects are part of the work;
- the work is substantial enough that durable objective/scope/decisions/remaining-work context should survive the current conversation.

A bounded question or one-shot explanation that can be completed safely in the current interaction does not require a Task merely because `/task` exists.

This is a semantic LLM/coordinator decision; the runtime validates a Task once materialized but does not force every request into durable state.


## 5. Task identity and durable lifecycle

Every durable Task has at least:

```text
id
title
created
kind
profile
profileFingerprint
contractRevision
stateRevision
lifecycle
```

Optional relationships:

```text
completed
origin
parentTask
unblocks
unblockCondition
```

Profiles may suggest a stable `idPrefix` for user readability. The final Task ID is allocated by deterministic task-creation machinery when an allocator exists; the LLM does not invent collision-prone IDs ad hoc.

Allocation must validate the prefix/path, create atomically, never overwrite an existing Task, and remain collision-safe. Legacy Task/OPS IDs remain valid through compatibility handling.

### 5.1 Durable lifecycle

Persist only lifecycle states that are not derivable from the Work Tree:

```fsharp
type TaskLifecycle =
    | Open
    | Paused of Reason
    | Complete
    | Aborted of Reason
```

`Active`, `Waiting`, and `Blocked` are **derived operational status**, not independently persisted lifecycle states.

Conceptual projection for `Open` tasks:

```text
any executable WorkItem exists            -> Active
none executable + blocking condition      -> Blocked
none executable + expected wait condition -> Waiting
```

This avoids redundant state synchronization.

### 5.2 Reopening

Terminal Tasks are reopened explicitly and **with invalidation targets**. Reopening only the lifecycle flag is invalid because the previous completion predicate would otherwise remain true.

```fsharp
type ReopenTarget =
    | AcceptanceCriterionTarget of AcceptanceId
    | WorkItemTarget of WorkItemId
    | GuardTarget of GuardId

type ReopenRequest = {
    Reason: Reason
    DecisionRef: DecisionRef option
    Targets: NonEmptyList<ReopenTarget>
}
```

Authority:

- `Complete -> Open`: `Coordinator` or `User` when new evidence invalidates previous completion;
- `Aborted -> Open`: `User` only.

Runtime semantics:

- selected Acceptance Criteria return to `Pending`;
- selected terminal WorkItems return to a valid non-terminal state, normally `Pending`;
- selected Guard dispositions/receipts are invalidated and must be satisfied again;
- prior Evidence, Decisions, results, and receipts remain immutable history and are marked/superseded rather than deleted;
- reopening must leave `CanCompleteTask = false`.

The exact storage representation for historical/superseded receipts is an implementation detail; preservation of audit history is not.


## 6. Kind

`kind` answers only:

> What is the primary outcome of the Task?

Initial and intended stable vocabulary:

```text
research
execution
```

Any Profile must be meaningful with either Kind. There is no `supportedKinds` matrix.

### 6.1 `research`

Primary outcome:

> Produce sufficiently supported knowledge, conclusion, recommendation, or decision.

Conceptual guidance:

```text
Frame
-> Gather evidence
-> Investigate
-> Synthesize
-> Validate conclusion
-> Complete
```

Research does not silently become implementation.

Research acceptance criteria should normally express stable outcome quality, for example:

- provide an evidence-backed conclusion;
- evaluate materially relevant alternatives when needed;
- preserve important uncertainty/limitations.

Newly discovered research dimensions should normally be handled by **additive contract revision**, not by weakening the baseline.

### 6.2 `execution`

Primary outcome:

> Produce and verify an observable change in the target system or real world.

Conceptual guidance:

```text
Define target state
-> Prepare
-> Execute
-> Verify resulting state
-> Complete
```

Software changes, OpenCode harness changes, bookings, submissions, payments, configuration changes, and other real-world actions are all `execution` when changed state is the primary deliverable.

---

## 7. Acceptance Criteria

Every Acceptance Criterion is mandatory **by definition**.

There is no `required | optional` Acceptance Criterion distinction.

If something is optional, it belongs in one of:

- optional WorkItem;
- follow-up;
- recommendation;
- future Task.

Canonical acceptance state:

```fsharp
type AcceptanceState =
    | Pending
    | Verified of NonEmptyList<EvidenceRef>
    | Waived of DecisionRef
```

Rules:

- `Pending` means the criterion is not established;
- `Verified` requires at least one Evidence reference;
- `Waived` requires a Decision whose authority permits waiver;
- connector/tool `success` alone does not prove an external effect;
- WorkItem completion never automatically verifies an AC.

WorkItems may carry:

```text
acceptanceRefs: [AC1, AC3]
```

This means **contributes to / produces evidence for**, not `satisfies`.

---

## 8. Evidence and receipts

Evidence is a small typed domain object, not an evidence database.

```fsharp
type EvidenceKind =
    | Build
    | Test
    | Review
    | Observation
    | ExternalEffect
    | Research
    | DecisionEvidence
    | Other of string

type Evidence = {
    Id: EvidenceId
    Kind: EvidenceKind
    Source: EvidenceSource
    Subject: string option
    ProducerRole: string option
    ProducerId: string option
    Reference: string option
    Summary: string
}

type EvidenceValidity =
    | Valid
    | Superseded of Reason

type EvidenceRecord = {
    Evidence: Evidence
    Validity: EvidenceValidity
}
```

Rules:

- raw/unbounded output stays outside canonical task state;
- `Reference` may point to a log, artifact, external record, commit, run, or local evidence file;
