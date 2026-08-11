# OpenCode Harness Infrastructure — Final Convergence Refactor

Apply this specification to the OpenCode harness infrastructure.

This specification is **frozen architecture** for the migration.

Do not reinterpret, expand, or redesign it while implementing it. If an implementation detail is ambiguous, choose the smallest solution consistent with this specification and existing repository conventions.

A later request to “review” this migration means **verification against this specification**, not a new design/discovery pass, unless the user explicitly requests redesign.

---

# 1. Goals

The harness must optimize for:

1. preserving a deliberately chosen solution throughout execution;
2. independent reasoning before decisions;
3. one authoritative coordinator decision after independent reasoning;
4. bounded review and remediation;
5. preventing reviewer-driven redesign;
6. preventing infinite discovery of new findings;
7. preventing speculative overengineering;
8. reducing unnecessary agent calls and token consumption;
9. moving mechanical checks from LLM reasoning into deterministic scripts;
10. keeping product/software engineering separate from harness engineering.

The core convergence model is:

```text
independent evidence
        ↓
coordinator decision
        ↓
frozen solution
        ↓
implementation + deterministic verification
        ↓
independent review discovery
        ↓
coordinator triage
        ↓
frozen finding set
        ↓
remediation
        ↓
targeted verification
        ↓
FROZEN / DONE
```

---

# 2. Two primary workflows

The harness has exactly two primary engineering workflows.

## `/task`

Use `/task` for complex or resumable **product/software engineering**.

Examples:

- feature implementation;
- significant bug fixes;
- architectural changes;
- cross-module changes;
- multi-repository product work;
- work requiring durable task state;
- work likely to span sessions.

`/task` owns its complete software-engineering lifecycle.

## `/audit-infra`

Use `/audit-infra` for **OpenCode/harness engineering**.

Examples:

- improve agents;
- improve skills;
- improve rules;
- improve commands;
- improve plugins;
- improve OpenCode configuration;
- improve deterministic validation scripts;
- diagnose harness problems observed in the current session;
- audit global harness infrastructure;
- audit project-local `.opencode` or `AGENTS.md`;
- compare global harness behavior with project-specific harness behavior;
- improve reusable harness automation.

Do not use `/task` merely because a harness change is large.

Harness work remains `/audit-infra` and is decomposed into bounded batches when necessary.

---

# 3. Primary agent is the coordinator

Do not create a coordinator subagent.

The primary OpenCode agent is the single coordinator and decision owner for the current workflow.

The coordinator owns:

- specialist delegation;
- architecture synthesis;
- solution selection;
- solution freeze;
- review triage;
- finding-set freeze;
- progression between phases;
- final completion decision.

Subagents provide:

- independent proposals;
- implementation;
- tests;
- specialist analysis;
- review evidence.

Subagents do not own task-level decisions.

The same coordinator should normally own architecture synthesis and review triage for the entire execution.

---

# 4. Remove obsolete orchestration surfaces

Remove:

```text
agents/audit-session.md
skills/audit-session/
rules/software/team.md
```

Remove all stale references to them.

Do not replace them with:

- coordinator agent;
- session-audit agent;
- gaps-clarifier agent;
- simplifier agent;
- review-arbiter agent.

The responsibilities are absorbed by:

- the primary coordinator;
- `/task`;
- `/audit-infra`;
- canonical shared rules;
- deterministic scripts.

---

# 5. Simplify global `AGENTS.md`

Global `AGENTS.md` should contain only genuinely global behavior, such as:

- safety;
- explicit confirmations;
- Git restrictions;
- secret handling;
- configuration precedence;
- general OpenCode-infrastructure invariants;
- shared conventions that truly apply to every workflow.

Remove global software-team orchestration such as:

- universal engineer-owned builds;
- universal tester-owned tests;
- mandatory reviewer coordination;
- architecture-team routing.

Those belong specifically to `/task`.

Do not duplicate `/task` or `/audit-infra` procedures in `AGENTS.md`.

---

# 6. `/task` owns software-agent orchestration

Move any still-useful orchestration semantics from `rules/software/team.md` into the `/task` skill and its references.

Prefer:

```text
skills/task/SKILL.md
skills/task/references/agent-gates.md
```

for workflow ownership.

Shared rules should provide behavior/knowledge, not orchestrate the whole task.

---

# 7. `/task` high-level lifecycle

The normal lifecycle is:

```text
Research
   ↓
Clarification
   ↓
Independent architecture proposals
   ↓
Coordinator synthesis
   ↓
Solution freeze
   ↓
Implementation
   ↓
Build
   ↓
Tests
   ↓
Discovery review
   ↓
Coordinator triage
   ↓
Finding-set freeze
   ↓
Remediation
   ↓
Affected tests
   ↓
Verification
   ↓
Cleanup
   ↓
Commit/publish when applicable
```

The workflow must progress monotonically.

Later phases do not casually reopen earlier phases.

---

# 8. Clarification without another agent

Do not create a gaps-clarifier agent.

Improve the existing `/task` clarification procedure.

Classify uncertainty as:

```text
BLOCKING
ASSUMPTION
NON-BLOCKING
```

## `BLOCKING`

Use only when the unresolved question can materially change:

- observable behavior;
- architecture;
- acceptance criteria;
- public/external contracts;
- data integrity;
- security;
- required inputs/outputs.

Only blocking ambiguity interrupts autonomous/YOLO execution.

## `ASSUMPTION`

Resolve using this precedence:

1. explicit task requirements;
2. existing contracts/code;
3. repository conventions;
4. simplest reversible and least-surprising behavior.

Record meaningful assumptions so downstream agents operate from the same interpretation.

## `NON-BLOCKING`

Do not interrupt execution.

Do not ask questions merely because some ambiguity exists.

---

# 9. Two independent architects

Keep two architecture agents per supported language.

Both architects:

- receive the same task evidence;
- work independently;
- preferably run in parallel;
- do not see the other architect's proposal;
- produce one preferred solution each.

Common input should include:

- requirements;
- acceptance criteria;
- constraints;
- non-goals;
- relevant repository context.

Each architect should propose the **smallest sound solution** satisfying the task.

Do not ask each architect for several alternatives unless the user explicitly requests architectural exploration.

Independent diversity comes from:

- different agents/models;
- independent reasoning.

Not from generating many alternatives per architect.

---

# 10. Architecture synthesis

After both architects complete, the primary coordinator compares their proposals.

The coordinator must optimize for:

```text
correctness
+
requirement coverage
+
simplicity
+
reversibility
+
existing repository conventions
-
unnecessary complexity
```

Important rule:

> Synthesis is not union.

The coordinator should:

- choose Architect 1 unchanged when sufficient;
- choose Architect 2 unchanged when sufficient;
- merge only complementary parts required by concrete requirements;
- remove unnecessary elements from either proposal.

Reject:

- speculative abstractions;
- premature extensibility;
- unnecessary interfaces;
- unnecessary additional layers;
- unnecessary dependencies;
- unrelated refactors;
- generalized infrastructure for hypothetical future requirements;
- unmeasured optimization.

Do not combine two architectures merely to preserve ideas from both.

---

# 11. Frozen solution contract

After synthesis, create one authoritative solution contract.

It should contain enough durable information for downstream agents to stay aligned:

```text
Requirements
Acceptance criteria
Accepted assumptions
Non-goals
Chosen design
Important boundaries/contracts
Implementation constraints
Selected review profile
Significant rejected alternatives when relevant
```

The engineer, tester and reviewers operate against this contract.

They do not independently redefine the task.

---

# 12. Architecture reopening

Once frozen, architecture is closed by default.

Reopen it only on a **hard invalidation condition**, such as:

- the design cannot satisfy an acceptance criterion;
- a Critical/Error correctness problem exists in the design itself;
- a security vulnerability invalidates the design;
- a data-integrity flaw invalidates the design;
- a required external/public contract was materially misunderstood;
- implementation is technically impossible under an approved constraint.

Do not reopen architecture for:

- cleaner patterns;
- another valid architecture;
- stylistic preferences;
- optional abstractions;
- future extensibility;
- hypothetical scaling;
- speculative optimization;
- unrelated cleanup.

Those are rejected or deferred.

---

# 13. Engineer

The language engineer owns:

- production-code implementation;
- implementation changes required by accepted remediation;
- build execution.

The engineer implements the frozen solution.

The engineer must not use implementation as an opportunity to redesign or broaden scope.

If the frozen solution appears invalid, report the hard invalidation condition to the coordinator rather than silently redesigning.

---

# 14. Tester

Keep tester separate from engineer.

Tester owns:

- inspection of existing coverage;
- test strategy;
- test implementation;
- regression tests;
- test execution.

Normal ordering:

```text
Engineer implementation
→ Build
→ Tester analysis
→ Tester implementation
→ Tests
→ Review
```

Do not require reviewers to approve before tests are written.

Do not block test work on Warning/Info findings.

For unusually complex work, the tester may perform lightweight test-strategy analysis before implementation, but the main testing phase remains after production implementation.

---

# 15. Remove routine intermediate generic review

Do not require a generic reviewer after every substantive implementation subtask.

This duplicates the final Discovery review and increases cost without providing proportional value.

Intermediate review is allowed only when the frozen design explicitly marks a particular implementation slice as independently high-risk.

Normal semantic review occurs after:

- implementation;
- build;
- tests.

---

# 16. Three specialized regular reviewers

Keep three language reviewers but give them distinct primary mandates.

## Reviewer 1 — correctness

Primary scope:

- behavioral correctness;
- regressions;
- error handling;
- state transitions;
- concurrency where applicable.

## Reviewer 2 — architecture conformity

Primary scope:

- conformity to frozen architecture;
- module/service boundaries;
- dependency direction;
- maintainability;
- accidental implementation complexity.

Reviewer 2 must not replace the frozen design merely because another design is valid.

## Reviewer 3 — contracts and verification

Primary scope:

- public/internal contracts;
- edge cases;
- acceptance criteria;
- test adequacy;
- missing failure-path verification.

Review scope differentiation should come from mandates, not from unrelated input baselines.

---

# 17. Reviewer evidence

During Discovery, selected reviewers receive the same core evidence:

- frozen solution contract;
- requirements;
- assumptions;
- non-goals;
- same implementation/diff baseline;
- build result;
- test result.

Additional role-specific files/context are allowed.

Do not provide reviewers with each other's findings during Discovery.

This preserves independent evidence.

---

# 18. Conditional specialists

Keep specialist reviewers such as:

```text
security/reviewer
database/sql-reviewer
devops/reviewer
performance/reviewer
```

They are escalation capabilities, not default review-board members.

## Security

For ordinary security implications, a regular reviewer may load the shared security rule.

Invoke the dedicated security reviewer when the change materially affects high-risk security boundaries, including:

- authentication;
- authorization;
- tenant isolation;
- secrets;
- PII;
- cryptography;
- privilege boundaries;
- untrusted input;
- dangerous deserialization;
- sensitive public/network exposure.

Use comparable concrete applicability rules for:

- SQL/database;
- DevOps/deployment;
- performance.

Remove policies equivalent to:

> When unsure, run the agent.

Agent invocation must be justified by concrete task or diff evidence.

---

# 19. Risk-based review profiles

Do not run all reviewers for every task.

Support a small number of explicit review profiles.

Example:

## Standard

```text
Reviewer 1 — correctness
Reviewer 3 — contracts/testing
```

## Full / architecture-sensitive

```text
Reviewer 1
Reviewer 2
Reviewer 3
```

Then add specialist reviewers only when applicable.

The coordinator selects the review profile before Discovery.

Do not expand the profile later unless implementation materially changes the risk surface.

---

# 20. Canonical review rule

Make:

```text
rules/software/review.md
```

the canonical owner of reviewer behavior and convergence.

A valid finding requires:

- concrete affected location or surface;
- violated requirement/invariant/contract;
- credible failure scenario;
- evidence attributable to the reviewed change;
- actionable remediation;
- sufficient confidence.

Do not report:

- style preferences;
- equally valid alternative architecture;
- speculative future requirements;
- unrelated technical debt;
- optional cleanup;
- optional refactoring;
- hypothetical optimization;
- handling of impossible states with no contract requirement.

Limit findings to the highest-value material issues.

Do not reward exhaustive issue generation.

`APPROVE` is an expected successful result.

Reviewers must not manufacture findings to justify their invocation.

---

# 21. Review state machine

Every reviewable artifact has explicit states:

```text
NEW
 ↓
DISCOVERY
 ↓
REMEDIATION
 ↓
VERIFICATION
 ↓
FROZEN
```

## Discovery

Discovery searches for material issues.

It is allowed once per frozen solution + implementation baseline.

## Verification

Verification does not search for new improvements.

It checks:

- accepted findings;
- explicit requirements;
- hard correctness/security/integrity contradictions caused by remediation.

## Frozen

After successful Verification, the artifact is frozen.

A later generic request to:

```text
review again
check again
validate again
```

means **Verification**, not another Discovery pass.

Return to Discovery only when:

1. the user explicitly requests redesign/reconsideration; or
2. a hard invalidation condition is proven.

This rule applies to:

- code reviews;
- architecture decisions;
- task plans;
- harness migrations;
- skill reviews;
- infrastructure audits;
- coordinator-produced artifacts.

---

# 22. Discovery review happens once

During `/task` Discovery:

```text
Reviewer 1 ─┐
Reviewer 2 ─┼→ Coordinator
Reviewer 3 ─┘
Specialists ─┘
```

The coordinator:

- deduplicates findings;
- rejects out-of-scope findings;
- rejects design preferences;
- rejects speculative findings;
- rejects unrelated technical debt;
- assigns severity;
- accepts or rejects findings.

Then the coordinator creates the finite **accepted remediation set**.

That finding set is frozen.

No general Discovery occurs after this point.

---

# 23. Severity

Use:

```text
Critical/Error = blocking
Warning/Info   = non-blocking
```

Warning/Info findings can be:

- accepted explicitly;
- deferred;
- rejected.

They do not automatically block workflow progression.

They never restart Discovery.

---

# 24. Remediation

Engineer fixes:

- accepted Critical/Error findings;
- explicitly accepted non-blocking findings.

Do not introduce unrelated cleanup.

Tester reruns affected tests and adds regression coverage where the accepted defect warrants it.

---

# 25. Verification

Verification receives only:

- accepted finding IDs;
- remediation diff;
- relevant build/test evidence.

Expected result per finding:

```text
FIXED
NOT FIXED
REGRESSION INTRODUCED
```

Verification must not perform a fresh code review.

Do not report new Warning/Info findings.

A new finding is admissible only if it is:

- directly introduced by remediation; and
- Critical/Error.

---

# 26. Hard remediation bound

Normal path:

```text
Discovery
→ remediation pass 1
→ Verification
→ Done
```

If remediation introduces a new Critical/Error regression:

```text
targeted remediation pass 2
→ affected build/tests
→ targeted verification
→ Done
```

Pass 2 is the maximum automatic remediation pass.

It must not reopen general Discovery.

If a blocking issue remains afterward:

- stop automatic looping;
- report the unresolved blocker;
- do not continue until tokens/context are exhausted.

---

# 27. `/task` durable state should be concise

Generated `TASK.md` should store durable state rather than repeat the whole workflow manual.

Keep:

- context;
- requirements;
- acceptance criteria;
- assumptions;
- non-goals;
- chosen solution summary;
- implementation plan;
- subtasks;
- decisions;
- open questions;
- verification evidence;
- review state;
- next step.

Move procedural details into skill references.

Update task generation and validation scripts/tests accordingly.

Preserve resumability.

---

# 28. `/audit-infra` replaces `audit-session`

`/audit-infra` supports three scopes.

## Session scope

Analyze visible evidence from the current session for:

- loops;
- repeated reads/searches;
- unnecessary agent calls;
- token waste;
- routing errors;
- missing reusable skills;
- missing rules;
- missing deterministic automation.

After detecting a concrete session gap, inspect only relevant harness infrastructure.

Do not automatically audit the entire harness.

## Harness scope

Audit/improve global OpenCode infrastructure:

- `AGENTS.md`;
- `opencode.json`;
- agents;
- skills;
- rules;
- commands;
- scripts;
- plugins;
- MCP configuration.

## Project scope

Audit project-local harness infrastructure and interaction with global harness configuration.

Determine:

- local/global duplication;
- intentional overrides;
- whether behavior belongs globally or locally;
- whether an existing global skill should be reused;
- whether a local skill/script is justified;
- whether project routing and validation remain coherent.

---

# 29. `/audit-infra` audit-only mode

Flow:

```text
inspect
→ findings
→ ranked recommendations
→ FROZEN
```

No mutation.

Once the audit result is frozen, another generic request to review the same result performs verification only.

Do not continually search for additional low-value infrastructure improvements.

---

# 30. `/audit-infra` audit-and-fix mode

Flow:

```text
inspect
→ discover concrete gaps
→ coordinator accepts scope
→ choose smallest sufficient solution
→ freeze solution
→ edit
→ deterministic validation
→ targeted semantic verification
→ remediation if blocking
→ targeted re-verification
→ cleanup
→ FROZEN / DONE
```

The post-edit phase is **verification**, not another broad infrastructure audit.

Do not do:

```text
audit
→ fix
→ full audit
→ new findings
→ fix
→ full audit
→ ...
```

New unrelated improvement opportunities are deferred.

---

# 31. Large harness migrations

Do not automatically convert large harness work into `/task`.

If harness work contains several independent changes, decompose it into bounded batches.

Each batch must have:

- explicit scope;
- frozen solution;
- deterministic validation;
- bounded verification;
- terminal completion.

Do not create an unlimited sequence of batches.

---

# 32. Temporary harness artifacts

`/audit-infra` may create temporary local files for:

- comparisons;
- generated intermediate data;
- working notes;
- validation output;
- session analysis.

Use an explicitly temporary ignored location.

Temporary files are working memory, not permanent infrastructure.

Before successful completion:

- remove them; or
- explicitly promote a useful artifact to permanent documentation.

Do not leave accidental scratch files.

---

# 33. Deterministic validation

Continue using:

```text
skills/audit-infra/scripts/ValidateInfrastructure.fsx
```

as the primary structural validator.

Move mechanical invariants into deterministic code wherever practical.

Add checks where reliable for:

- removed `audit-session` references;
- removed `team.md` references;
- broken routes;
- duplicate/overlapping skill routing;
- sibling agents with unintentionally identical responsibilities;
- obsolete recursive review wording;
- Discovery/Verification invariants;
- orphan rules;
- orphan agents;
- orphan scripts;
- orphan skills where deterministically detectable;
- task-template/reference drift;
- duplicated workflow prose;
- temporary artifact conventions when statically enforceable.

Use warnings for heuristic checks.

Do not encode subjective architecture judgments as hard validation errors.

---

# 34. Rules versus scripts

Rules should primarily express semantic behavior.

Scripts should enforce mechanical invariants.

When deterministic validation already checks:

- naming;
- frontmatter;
- section structure;
- route existence;
- ordering;

do not repeat large mechanical checklists unnecessarily in LLM rules.

Principle:

> LLMs reason about semantics. Scripts enforce mechanics.

---

# 35. Preserve specialist capabilities

Keep:

- language architects;
- language engineers;
- language testers;
- language reviewers;
- security reviewer;
- database/SQL specialists;
- DevOps specialists;
- performance reviewer.

Do not merge engineer and tester.

Do not delete useful specialist definitions solely to reduce file count.

Optimize:

- invocation frequency;
- specialization;
- routing;
- convergence.

---

# 36. Model and temperature policy

Do not change model/provider, reasoning effort, temperature, or step-budget configuration as part of this migration unless necessary to remove an obsolete agent.

Workflow behavior must be stabilized first.

Model/temperature tuning is a separate experiment after the new workflow has been exercised on representative:

- `/task` executions;
- `/audit-infra` executions.

Do not mix sampling/model experimentation with the convergence migration.

---

# 37. Validation and tests

Update existing tests and deterministic validation to encode the new architecture.

At minimum verify:

- `agents/audit-session.md` removed;
- `skills/audit-session/` removed;
- `rules/software/team.md` removed;
- no stale references remain;
- `/task` owns complex software orchestration;
- `/audit-infra` owns session/harness/project harness work;
- no coordinator/gaps-clarifier/simplifier agent was added;
- architects remain independent;
- coordinator produces one frozen solution;
- routine per-subtask generic review is removed;
- tester runs before final Discovery review;
- reviewers have distinct mandates;
- reviewers share the same frozen baseline;
- specialist reviewers are conditional;
- Discovery is one-time;
- accepted findings are frozen;
- Verification cannot restart Discovery;
- generic re-review of frozen artifacts means Verification;
- remediation passes are hard-bounded;
- `/audit-infra` post-edit phase is Verification rather than re-audit;
- deterministic validation remains authoritative for mechanical invariants.

Run relevant existing checks:

```text
npm run validate:infra
npm run test:task
npm run test:safety
git diff --check
```

Run only applicable suites if a suite is genuinely unaffected, but preserve existing validation guarantees.

---

# 38. Implementation discipline

This migration is an implementation task, not a new architecture exercise.

Do not:

- redesign this specification;
- introduce compatibility shims for removed harness structures;
- retain deprecated routes;
- create a generalized workflow framework;
- create speculative abstractions;
- add new orchestration agents;
- broaden scope because unrelated opportunities are discovered;
- tune models/temperature;
- repeatedly re-review already frozen decisions.

Prefer existing canonical owners over new files.

When duplicate policy exists:

1. select one canonical owner;
2. move the rule there;
3. reference it elsewhere;
4. delete redundant prose.

Do not commit or push.

---

# 39. Completion criteria

The migration is complete when:

1. required obsolete surfaces are removed;
2. `/task` follows the bounded software-engineering workflow;
3. `/audit-infra` follows the bounded harness-engineering workflow;
4. architecture decisions freeze correctly;
5. review findings freeze correctly;
6. repeated generic review no longer starts Discovery;
7. deterministic validation/tests pass;
8. temporary artifacts are cleaned up;
9. no unresolved blocking migration violation remains.

At completion report only:

```text
Files added/changed/deleted
Implemented architectural decisions
Validation/test results
Intentional deviations from this specification
Unresolved blockers
```

Do not append new optional improvement suggestions after successful completion.

Return the migration as complete when the completion criteria are satisfied.
