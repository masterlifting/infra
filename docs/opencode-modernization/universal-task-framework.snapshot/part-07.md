- first material `StartWorkItem` establishes baseline before starting;
- mandatory Profile Guards are present;
- subsequent weakening follows User-authority rules.

### Scenario G — CAS conflict

- two callers read the same `stateRevision`;
- first apply succeeds and increments revision;
- second apply with stale revision returns structured conflict without lost update.

### Scenario H — life execution state changes

```text
Pending -> Active -> Waiting -> Blocked -> Waiting -> Done
```

with typed payloads and observable resume conditions.

### Scenario I — post-baseline contract revision

- Coordinator adds AC/Guard;
- Coordinator cannot weaken/remove existing AC;
- User-authorized rewrite creates a new contract revision;
- only affected verification is invalidated.

### Scenario J — Profile drift

- new mandatory Guard is materialized after baseline;
- removed shared Guard is not silently deleted;
- hard safety strengthening applies immediately.

### Scenario K — stale Complete Task

- new evidence invalidates completion;
- Coordinator reopens `Complete` with non-empty ReopenTargets;
- selected AC/WorkItem/Guard state is invalidated;
- old Evidence remains historical;
- `CanCompleteTask` becomes false.

### Scenario L — Aborted reopen

- Coordinator cannot reopen `Aborted`;
- trusted User Decision can reopen it with targets/reason.

### Scenario M — dependency scope

- sibling/cross-branch dependency valid;
- self/ancestor/descendant/cross-task WorkItem dependency rejected;
- cycle rejected.

### Scenario N — out-of-band semantic edit

- fingerprint mismatch produces `CONTRACT_DRIFT`;
- material mutation is blocked;
- strengthening can be reconciled with Coordinator authority;
- weakening requires trusted User authority.

### Scenario O — external effect completion

- external action is represented by a required AC/ExternalEffect Evidence Guard;
- ambiguous tool result cannot satisfy it;
- observable confirmation Evidence satisfies it;
- `CompleteTask` itself performs no external effect.

### Scenario P — requirement demotion and skip

- optional work can be skipped by allowed Coordinator Decision;
- required -> optional after baseline fails for Coordinator;
- trusted User Decision permits demotion/waiver;
- `SkipDisposition` semantics remain distinct.

### Scenario Q — terminal handoff

- `CanCompleteTask` is true;
- `CompleteTask` rejects structurally empty terminal handoff;
- non-empty handoff is accepted;
- runtime does not claim semantic correctness of prose.

### Scenario R — Task activation and Profile selection

- trivial one-shot request completes without creating a durable Task;
- resumable/multi-step request materializes a Task;
- explicit valid Profile wins;
- no specialized match falls back to `general`;
- unregistered Profile ID is rejected;
- materially ambiguous selection is resolved before material execution.

### Scenario S — Profile reclassification

- Draft reclassification preserves useful contract/history without extra authority ceremony;
- post-baseline reclassification materializes stronger new Profile Guards;
- weakening old obligations is rejected without trusted authority;
- post-baseline Kind change is rejected without trusted User authority.

### Scenario T — capability envelope

- missing `required` capability blocks;
- `default` capability activates as defined by Profile;
- unused `allowed` capability is not loaded merely because it is permitted;
- Coordinator may activate an `allowed` capability when context requires it;
- worker attempt to activate outside-envelope capability is rejected/escalated.

### Scenario U — clarification mapping

- blocking uncertainty creates a scoped Open Question and blocks affected readiness;
- meaningful assumption becomes a durable Assumption Decision;
- non-blocking uncertainty is resolved without introducing a mandatory Clarification phase.

### Scenario V — ambiguous effect cannot be observed

- effect result is ambiguous;
- observation cannot establish whether it occurred;
- retry is rejected/fails closed;
- Task retains reconciliation/manual-observation work instead of guessing.

### Scenario W — dependency handoff

- child/dependency Task completes;
- parent/dependent external condition is not auto-satisfied;
- provenance/unblock condition is preserved;
- owner re-observes condition before proceeding.


### Scenario X — DecisionRef target binding

- trusted User Decision authorizes `WaiveAcceptanceTarget AC1`;
- using the same DecisionRef for AC2 is rejected;
- using it to skip a WorkItem or reopen a Task is rejected;
- a single Decision explicitly listing multiple targets may authorize exactly those targets and no others.

### Scenario Y — nested start and parent skip

- starting a Ready child atomically activates any Pending ancestors after their `BeforeStart` Guards pass;
- child start is rejected if an ancestor `BeforeStart` Guard fails;
- parent skip with non-terminal descendants is rejected;
- no implicit cascading waiver/skip occurs.

### Scenario Z — lifecycle authority

- Coordinator may Pause/Resume;
- Coordinator may Complete only when `CanCompleteTask = true`;
- Coordinator may Abort Draft;
- Coordinator Abort after baseline is rejected;
- trusted User authority can Abort the baselined Task.

### Scenario AA — ProfilePolicy provenance

- ordinary `task_apply` cannot create a `ProfilePolicy` Decision;
- Profile Resolver may create trusted ProfilePolicy provenance from structured policy;
- `ExplicitDecision` never accepts ProfilePolicy as minimum authority.

### Scenario AB — Evidence supersession

- previously Valid Evidence satisfies a Guard;
- reopen/contract revision supersedes that Evidence;
- the same Evidence no longer satisfies current Guard/AC verification;
- historical Evidence content remains visible/auditable.

### Scenario AC — canonical WorkItem transitions

- every command in Section 12.3 succeeds from each allowed source state when prerequisites hold;
- every unlisted command/source-state combination returns a structured `DomainError`;
- `ResumeWorkItem` returns `Waiting/Blocked -> Pending`, not directly to `Active`.

### Scenario AD — Evidence supersession cascade

- a Verified AC loses its last Valid Evidence and returns to `Pending`;
- a `Done` WorkItem loses Evidence required by a `BeforeComplete` Guard and reopens to `Pending`;
- a terminal parent depending on that WorkItem reopens transitively;
- a previously `Complete` Task whose `CanCompleteTask` becomes false transitions to `Open`;
- all superseded Evidence remains historical.

### Scenario AE — blocked/waiting ancestor

- child start under a `Pending` ancestor activates the ancestor atomically;
- child start under an `Active` ancestor succeeds if the child is Ready;
- child start under a `Waiting` or `Blocked` ancestor is rejected;
- child start under a terminal ancestor is rejected as structurally inconsistent.

### Scenario AF — two User-authorization paths

For the same exact target:

- `ConfirmedUserInvocation` without existing DecisionRef creates a target-bound User Decision and executes;
- ordinary Coordinator invocation with the created target-matching User DecisionRef executes;
- ordinary Coordinator invocation without the DecisionRef is rejected;
- a User DecisionRef targeting another operation is rejected.

### Scenario AG — exact ContractPatch confirmation

- runtime canonicalizes a proposed weakening patch to `ContractPatchId P1`;
- User confirms exactly `P1`;
- changing the payload produces `P2`, and the `P1` Decision cannot authorize it;
- successful application creates a new `ContractRevisionId` recorded as outcome, not as the pre-confirmation authorization target.

# Part XIII — Failure modes and safeguards

## 42. Profile explosion

Mitigation:

- Kind handles `research | execution`;
- Profile exists only for materially different task-design/capability policy;
- capabilities handle specialist technology/domain surfaces;
- one-off differences remain task context.

## 43. Workflow-engine creep

Mitigation:

- fixed WorkItem state vocabulary;
- no custom states;
- no arbitrary transition DSL;
- no automatic scheduler;
- no recursive auto-spawn;
- no generic semantic applicability DSL;
- coordinator orchestrates ready work.

## 44. Deterministic overreach

Runtime validates only mechanically reliable invariants.

It does not judge:

- correctness of research conclusions;
- architecture quality;
- semantic relevance of prose;
- whether natural-language evidence is substantively true;
- whether a free-form policy contradicts another free-form policy.

## 45. LLM under-enforcement

Mitigation:

- mandatory Profile policy is structured and materialized deterministically;
- typed commands require payloads;
- guards require typed receipts/authority;
- completion has one canonical `CanCompleteTask` invariant;
- contract weakening after baseline requires explicit authority.

## 46. State/history corruption

Mitigation:

- stable IDs;
- completed WorkItems never deleted;
- CAS/state revision;
- contract fingerprint;
- cycle checks;
- atomic persistence;
- explicit reopen/revision rather than history rewrite.

---

# Part XIV — Closed architecture decisions

## 47. Canonical decisions

The following are accepted and should not be reopened during implementation without new evidence:

1. `/task` is the universal durable bounded-work framework.
2. Kinds are `research | execution`.
3. Any Profile is valid with either Kind.
4. Profile is the primary task-design extension point.
5. Profiles define capability envelopes plus structured/semantic policy.
6. Mandatory structured Profile policy is materialized by resolver/runtime, not by LLM memory.
7. Same-ID project Profile means constrained overlay/specialization.
8. Materially incompatible project semantics require a new Profile ID.
9. No persistent `linear | stateful` mode.
10. One recursive WorkItem contract is used at every depth.
11. `Ready` and `Active/Waiting/Blocked` are derived projections.
12. Persisted WorkItem states are `Pending/Active/Waiting/Blocked/Done/Skipped`.
13. `SkipDisposition = Optional | NotApplicable | Waived` with distinct authority semantics.
14. Durable WorkItems are never physically deleted; history is preserved.
15. Parent WorkItems complete explicitly; children becoming terminal never auto-completes the parent.
16. Independent lifecycle uses a child Task.
17. Work ownership is durable/logical; runtime invocation identity is transient.
18. Task Runtime is a deterministic task-state substrate, not an effect executor.
19. Functional domain core uses strict DTO boundaries; domain DUs are not the JSON wire contract.
20. Structured machine state mutates only through semantic Task commands.
21. `stateRevision`/CAS exists from the first working persisted slice.
22. User authority is derived only from a trusted human-confirmed invocation context and cannot be self-declared by LLM commands.
23. Contract-critical prose is fingerprinted and reconciled explicitly on drift.
24. Acceptance Criteria are mandatory by definition.
25. AC verification requires non-empty same-Task Evidence refs.
26. Required AC/WorkItem waivers require User authority unless a stricter policy forbids waiver.
27. `NotApplicable` and `Waived` are different states/policies.
28. Guards have stable ID, explicit target, checkpoint (`BeforeStart | BeforeComplete`), requirement, applicability policy, waiver policy, and disposition.
29. Review is represented through the common typed Evidence/Guard receipt mechanism rather than a parallel review-receipt system.
30. Guard applicability declares `MinimumAuthority`.
31. Guards are satisfied only by explicitly target-scoped typed receipts/authorized Decisions.
32. `CompleteTask` is side-effect-free.
33. `CanCompleteTask` contains only mechanically checkable invariants.
34. Terminal `State/Evidence/Next` is a separate handoff payload with structural, not semantic, runtime validation.
35. Post-baseline weakening of Objective/Scope/AC requires trusted User-authorized ContractPatch.
36. Additive AC/Guard strengthening may be Coordinator-authorized.
37. Draft uses the same patch model with Coordinator authority; no parallel draft command family exists.
38. Baselined Guard history is preserved; no generic physical Guard deletion shortcut exists.
39. Profile drift strengthens open tasks monotonically; removed Profile requirements do not silently weaken baselined tasks.
40. `Complete -> Open` may be Coordinator/User when new evidence invalidates completion; `Aborted -> Open` requires trusted User authority.
41. `ReopenTask` requires explicit non-empty invalidation targets and preserves historical Evidence.
42. `WorkItem.dependsOn` is same-task only and cannot target self/ancestor/descendant.
43. `acceptanceRefs` provide traceability but never auto-verify ACs.
44. TaskWide Questions deterministically block readiness of pending work and task completion.
45. Contract drift reconciliation uses normal ContractPatch authority; it is not an authority bypass.
46. ActionGuard remains downstream Happy Life machinery unless broader evidence justifies promotion.
47. Historical TASK/OPS records are not mass-rewritten.
48. Happy Life migration uses replay -> shadow -> canary -> cutover.
49. Implementation uses walking vertical slices, not a long infrastructure-first sequence.
50. Durable Task activation is semantic/adaptive; trivial one-shot requests need not create Tasks.
51. Profile selection uses explicit caller choice first, otherwise active-registry classification with `general` fallback; unregistered Profile IDs are forbidden.
52. Reclassification preserves history and uses normal post-baseline authority/monotonic-strengthening rules.
53. Capability envelope semantics are `required` (must exist/active), `default` (small automatic baseline), and `allowed` (Coordinator-selectable when context requires); workers cannot escape the envelope.
54. `BLOCKING / ASSUMPTION / NON-BLOCKING` remains reasoning policy mapped onto Questions/Decisions rather than a mandatory lifecycle phase.
55. Gate taxonomy is Safety / Decision / Quality / Bookkeeping and maps onto existing authority/effect/Question/Guard contracts rather than a second state machine.
56. Task presentation composes Core + Kind + Profile + project overlay fragments; no full template copy per combination.
57. Software execution keeps risk-driven architecture/testing/review/debug semantics without restoring fixed legacy ceremony.
58. Ambiguous external effects fail closed when observable state cannot establish safe retry.
59. Dependency handoff preserves provenance/unblock conditions but never auto-mutates or auto-proves the dependent workflow state.
60. Terminal handoff semantic freshness is Coordinator responsibility; runtime validates only structural shape.
61. DRY rule ownership in Section 2 is authoritative.
62. Decision authorization is target-bound: a DecisionRef is valid only for its explicitly typed DecisionTarget(s); authorization is never transferable by prose similarity.
63. ProfilePolicy is trusted runtime provenance only, not a caller-selectable `MinimumAuthority` and not an ordinary `task_apply` authority source.
64. `EvidenceValidity = Valid | Superseded`; only Valid Evidence satisfies current Guards/ACs, while superseded evidence remains immutable history.
65. Starting a nested child atomically activates validated Pending ancestors; parent skip never cascades implicitly and requires terminal descendants.
66. WorkItems referencing child Tasks may become Done only when the referenced child Task is `Complete`; `Aborted` does not satisfy the parent.
67. Task lifecycle authority is explicit: Pause/Resume/Complete are Coordinator operations (Complete additionally requires `CanCompleteTask`), while Abort after baseline requires trusted User authority.
68. Draft-only `RemoveGuard` applies only to task-designed Guards; Profile-materialized Guards are re-resolved, and baselined Guards are never physically deleted.
69. `ReclassifyTask` is an explicit semantic command and follows the Kind/Profile authority rules in Section 17.5.
70. Independent-producer Guards require concrete producer/owner identity; roles alone cannot prove independence.
71. Optional WorkItem skip uses a Reason without mandatory Decision creation; NotApplicable/Waived skips require target-matching Decisions.
72. WorkItem command/source-state admissibility is defined by the canonical transition table in Section 12.3; unlisted transitions are invalid.
73. Superseding Evidence triggers deterministic transitive invalidation of dependent ACs, Guards, WorkItems, parent WorkItems, and Task completion.
74. A nested child may start only when every ancestor is Pending or Active; Waiting/Blocked/terminal ancestors reject the start.
75. User-required operations use one authorization resolver: either current trusted ConfirmedUserInvocation (runtime creates the exact target-bound Decision) or an exact pre-existing target-bound User DecisionRef.
76. User confirmation of a contract change targets a deterministic pre-apply ContractPatchId; the resulting ContractRevisionId is outcome/provenance, not the authorization target.


# Part XV — Deferred implementation choices

## 48. Intentionally deferred details

These do not block architecture acceptance:

- exact directory layout for Profiles/runtime after shared repository cutover;
- exact JSON/Markdown managed-region delimiter;
- embedded managed state versus sidecar after enforcement spike;
- exact Task ID allocation mechanism/width;
- exact Profile metadata file format;
- exact F# module/file organization;
- direct script/CLI versus local MCP for repeated runtime operations;
- exact atomic-replace implementation per OS.

Any implementation choice must preserve the domain/authority/guard/completion contracts above.

---

# Part XVI — Final evaluation

## 49. Evaluation

| Area | Assessment |
| --- | --- |
| Core abstraction | Strong |
| Kind/Profile separation | Strong |
| Project extensibility | Strong |
| Deterministic/LLM boundary | Strong |
| Guard enforcement | Strong after structured materialization |
| Completion semantics | Strong; one canonical invariant |
| Contract integrity | Strong; baseline + authority + fingerprint |
| State/history integrity | Strong; derived projections + CAS + stable history |
| Workflow-engine creep risk | Controlled by explicit non-goals |
| Migration risk | Moderate but bounded by compatibility + staged rollout |

Recommendation:

> **Proceed to implementation issue decomposition using this document as the canonical task-framework architecture.**

Implementation should remain subtractive: whenever an old `task` or `ops` mechanism is superseded by a canonical Core/Profile/Runtime rule, remove or demote the duplicate rather than retaining both indefinitely.
