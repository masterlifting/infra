# Part VII — Persistence and managed state

## 31. One authoritative machine-state representation

The framework must have exactly one authoritative structured machine-state representation.

Preferred option:

```text
TASK.md
  semantic Markdown
  + runtime-managed structured state region
```

This option is selected only if actual OpenCode integration can reliably prevent ordinary edits from silently mutating the managed region.

If managed-region enforcement cannot be implemented robustly, use a runtime-owned sidecar instead.

This is an implementation spike, not a change to domain semantics.

### 31.1 Semantic Markdown versus structured state

Do not duplicate the same mutable fact in two authoritative places.

Examples:

- WorkItem state/evidence IDs live in structured state;
- AC text lives in canonical semantic contract sections and is fingerprinted;
- operational status/progress are derived views;
- generated display summaries must not become second writable state.

---

## 32. Safe persistence and concurrency

Use optimistic concurrency from the start:

```text
stateRevision
expectedRevision
```

Canonical single-file persistence flow:

```text
acquire short-lived per-task transient lock
-> read latest whole task document
-> check expected stateRevision
-> check contract fingerprint
-> parse strict DTO
-> decide/evolve
-> increment stateRevision
-> patch freshly read document
-> write temp in same directory
-> flush
-> atomic replace
-> release lock
```

Do not hold the replace target itself open in a way that prevents atomic replacement on Windows; use a separate short-lived lock mechanism/file.

CAS protects runtime writers from lost updates. Contract fingerprint detects out-of-band semantic edits.

Complex two-file crash-consistency is required only if implementation chooses a sidecar.

Locks are transient runtime state and are never stored as durable Task context.

---

# Part VIII — External effects and continuity

## 33. External-effect invariant

Universal Core invariant:

> External, destructive, irreversible, or user-visible writes require the applicable exact confirmation/permission contract before execution.

Ambiguity invariant:

> An ambiguous effect result does not prove failure. Observe the target state before retrying.

If observable target state cannot reliably establish whether the effect occurred or whether retry is safe, **do not retry**. Preserve the ambiguity as `Waiting`/`Blocked`/reconciliation work (as appropriate) and require further observation or manual decision.

This applies to push, PR/MR creation, publication, deployment, tracker/comment creation, bookings/submissions/payments/messages, and other remote writes.

`CompleteTask` never performs an effect.

Profiles/projects may strengthen external-effect machinery; they cannot weaken the Core invariant.

Happy Life may retain ActionGuard as a stronger downstream effect protocol.

---

## 34. Durable continuity and freshness

Durable task context includes only information needed for portable/resumable continuity:

- objective/scope/AC;
- decisions;
- open questions;
- meaningful findings;
- evidence references;
- blockers/waits;
- dependencies;
- remaining work;
- `State / Evidence / Next`.

Do not use Task as storage for:

- hidden reasoning;
- raw private message dumps;
- large command output;
- locks;
- machine-specific transient state;
- temporary execution journals unless a project-specific safety system owns them.

Before material work relies on volatile facts, revalidate only the relevant volatile facts.

A universal `Last Loaded Context` table is not required. A Profile may add a bounded source cache when it has concrete value.

### 34.1 Dependency handoff

Core durable relationships such as `origin`, `parentTask`, `unblocks`, and `unblockCondition` are lightweight handoff/provenance metadata, not a dependency-graph framework.

When a Task resolves a known blocker elsewhere:

- preserve enough stable reference/provenance for the owning workflow/task to find the result;
- surface the dependent record/condition that should be refreshed;
- do not automatically mutate unrelated dependent records;
- child/dependency Task `Complete` never proves an external dependent condition by itself;
- the owning workflow re-observes the `unblockCondition` before relying on it.

This keeps task composition portable without turning Task Runtime into a generic graph scheduler.

---

# Part IX — Audit remediation integration

## 35. Audit-to-task boundary

Auditor remains read-only.

```text
Auditor
  -> findings
  -> optional remediation proposals

Coordinator
  -> synthesize
  -> dedupe against existing work
  -> reject / merge / materialize

Task Framework
  -> choose Kind + Profile
  -> create durable Task if warranted
```

A remediation proposal is not backlog state, not authorization, and not a Task ID.

When materialized, preserve concise provenance back to the audit/report/finding IDs and evidence that motivated the work. Materialization then follows normal Task activation, Kind/Profile selection, dedupe, authority, and external-backlog-write rules.

Profile selection occurs when candidate remediation is materialized.

---

# Part X — Migration

## 36. Remove/demote legacy global Task ceremony

Do not preserve the following as universal mandatory concepts:

- `code | non-code` task kind;
- `complex | non-complex` implementation-plan marker;
- fixed Research/Clarification/Design/Branch/Implement sequence;
- exact canonical checkbox wording;
- fixed numbered software lifecycle subtasks;
- fixed `C0/C1/C2`;
- mandatory `Summary:` under every completed checkbox;
- mandatory repository/branch fields for non-software work;
- explicit human confirmation solely to mark local `Complete`;
- duplicate manually maintained progress/status.

Retain software-specific requirements in `software`/`opencode` Profiles where materially applicable.

---

## 37. Remove/demote legacy OPS ceremony

Do not promote the following into universal Core:

- fixed Intake/Plan/Execute/Handoff phases;
- mandatory `Last Loaded Context`;
- mandatory Execution Log;
- OPS-specific source-system tables;
- OPS-specific emojis/status presentation;
- `.inbox/life-ops/...` conventions;
- ActionGuard journal semantics;
- life-specific scratch retention.

Preserve reusable semantics:

- Waiting versus Blocked;
- observable resume conditions;
- durable continuity;
- evidence-backed completion;
- external-effect safety;
- stale completion/reopen;
- sensitive/raw data separation as a downstream Profile/project policy.

---

## 38. Legacy records

Do not mass-rewrite completed historical TASK/OPS records.

### Completed records

Keep historical/readable.

### Active/resumable records

Migrate only when they need continued work:

1. parse legacy record;
2. infer Kind/Profile from explicit semantics;
3. preserve objective/scope/acceptance/decisions/evidence;
4. convert remaining work to Work Tree;
5. preserve provenance;
6. never mark work complete merely because migration succeeded.

---

## 39. Happy Life staged migration

Happy Life migration sequence:

```text
1. deterministic scenario replay
2. shadow Tasks while OPS remains authoritative
3. low-risk canary with /task + life authoritative
4. broader migration
5. retire standalone ops
```

During early migration, keep ActionGuard/connector effect safety intact; migrate orchestration first.

There must never be two independently writable authoritative life workflows after cutover.

---

# Part XI — Implementation strategy

## 40. Walking-skeleton delivery

Prefer vertical slices that produce a usable `/task` early.

### Slice 1 — Minimal end-to-end `general + execution`

Deliver a real working Task immediately:

- minimal Task/WorkItem/Acceptance domain;
- built-in static `general` Profile;
- shallow Work Tree;
- strict DTO/serialization;
- `stateRevision` + `expectedRevision` CAS from the first persisted mutation;
- `task_create`;
- `task_get`;
- minimal `task_apply`;
- `task_validate`;
- atomic single-file persistence for the chosen managed-state representation;
- one simple `general + execution` task completing end-to-end.

No generic Profile registry/overlay engine is required yet.

### Slice 2 — `general + research`

Prove the second Kind using the same runtime:

- research guidance;
- evidence-backed AC verification;
- additive research contract refinement;
- no separate research framework.

### Slice 3 — Harden domain semantics

Add:

- trusted invocation authority / user-confirmation path;
- Decisions/Questions;
- typed Evidence;
- Guard checkpoints/requirements/dispositions;
- parent WorkItem completion;
- `CanCompleteTask`;
- WorkItem requirement changes;
- child-task/dependency semantics;
- targeted `ReopenTask`.

### Slice 4 — Contract integrity hardening

Add:

- Draft -> Baselined transition;
- post-baseline `ContractPatch`;
- contract fingerprint/drift reconciliation;
- profile-independent semantic contract invalidation rules;
- managed-region enforcement spike;
- lock/crash robustness around already-existing CAS.

### Slice 5 — Generic Profile Resolver

Add:

- Profile registry and compact routing metadata;
- deterministic active-Profile discovery;
- Coordinator selection/reclassification integration;
- project overlays;
- deterministic mandatory Guard materialization;
- `required/default/allowed` capability-envelope semantics;
- profile fingerprints/drift;
- monotonic strengthening behavior for baselined Tasks.

### Slice 6 — `software`

Migrate software-specific design/build/test/review semantics into Profile policy/Guards.

### Slice 7 — `opencode`

Add harness-specific Profile semantics and validate against real OpenCode infrastructure tasks.

### Slice 8 — OpenCode integration boundary

Choose the smallest integration surface consistent with actual OpenCode capabilities.

Start with library/scripts/CLI. Move repeated interactive operations to local F# MCP only if measurements demonstrate value.

### Slice 9 — Happy Life `life` Profile and migration

Execute replay -> shadow -> canary -> cutover while retaining ActionGuard until parity is proven.


# Part XII — Required scenario tests

## 41. Domain scenario matrix

Before declaring base domain semantics stable, cover at least:

### Scenario A — software execution with mandatory review

- Profile resolver materializes a `BeforeComplete` Review Evidence Guard;
- LLM cannot omit it;
- `CompleteWorkItem` without matching Review Evidence fails;
- valid target-scoped Review Evidence allows completion.

### Scenario B — software research

- no implementation/build/test Guard appears merely because Profile=`software`;
- research-specific evidence/conclusion requirements apply;
- new research dimensions can be added through Coordinator-authorized additive AC revision after baseline.

### Scenario C — applicability authority

- Profile allows N/A for a Guard only under declared `MinimumAuthority`;
- Coordinator N/A is rejected where User is required;
- ordinary `task_apply` cannot forge a User Decision;
- trusted confirmed-user path succeeds with a valid receipt.

### Scenario D — parent completion

- children become terminal;
- parent does not auto-complete;
- parent `CompleteWorkItem` fails while a required child/Guard is unsatisfied;
- parent can attach its own result/evidence and then become `Done`.

### Scenario E — TaskWide Question

- open TaskWide Question makes all pending work `NotReady`;
- `StartWorkItem` is rejected;
- `CanCompleteTask = false`;
- resolving the Question through a Decision restores normal readiness.

### Scenario F — Draft -> Baselined

- Coordinator freely patches Draft contract;
