# Problem Context

The current OpenCode harness has several systemic problems that this migration is intended to solve.

## 1. Review loops do not converge

The current workflow can repeatedly execute:

```text id="qw2h3j"
review
→ finding
→ fix
→ fresh review
→ new finding
→ fix
→ fresh review
→ ...
```

LLM reviewers can almost always discover another lower-priority issue, alternative design, edge case, or speculative improvement when given a fresh review mandate.

The result is:

- unbounded token consumption;
- repeated context loading;
- progressively lower-value findings;
- tasks continuing until context or token budgets are exhausted;
- difficulty determining when work is actually complete.

The desired behavior is one Discovery phase followed by bounded remediation and Verification, not repeated fresh Discovery.

## 2. Reviewers implicitly optimize for finding something

A generic instruction such as “review the implementation” encourages agents to search until they can produce findings.

This creates false pressure against returning `APPROVE`.

Typical low-value findings include:

- stylistic preferences;
- equally valid alternative designs;
- hypothetical future requirements;
- optional refactors;
- unrelated technical debt;
- speculative defensive handling;
- premature performance concerns.

Reviewers must instead determine whether the implementation is acceptable against an explicit frozen contract.

Finding no material defect is a successful outcome.

## 3. Fixes currently expand the search space again

After a reviewer finding is fixed, reviewers may receive the changed code as a new general review target.

That causes previously closed work to become open-ended again.

The remediation set must therefore become finite and frozen.

After remediation, reviewers should verify accepted findings rather than search the entire implementation again.

## 4. Architecture decisions can drift after implementation starts

Architects, reviewers, testers, and engineers may independently suggest alternative approaches after a design has already been selected.

Without an explicit solution freeze, a valid task can evolve like:

```text id="kv03mz"
solution A
→ implementation
→ reviewer prefers B
→ refactor toward B
→ another reviewer prefers C
→ refactor toward C
```

This creates unnecessary redesign and makes the implementation unstable.

The desired behavior is:

```text id="vuk5m4"
independent architectural proposals
→ coordinator chooses/synthesizes
→ solution freezes
→ downstream agents work within that decision
```

Architecture should reopen only when the chosen design is demonstrably invalid, not merely because another valid design exists.

## 5. Multiple architects can accidentally increase complexity

Independent architects are useful because different models may expose different risks and solutions.

However, naïvely merging both proposals can produce an overengineered union containing:

- abstractions from both proposals;
- additional layers;
- multiple extension mechanisms;
- unnecessary interfaces;
- extra dependencies;
- speculative capabilities.

The coordinator must therefore synthesize by **subtraction**, not accumulation.

The goal is the smallest sufficient solution that preserves the strongest justified properties from the proposals.

## 6. Reviewer responsibilities currently overlap too much

Multiple reviewers reviewing the same code with broad identical mandates leads to duplicated reasoning and high token consumption.

Model diversity alone is not enough reason for three agents to inspect the same conceptual surface.

The desired design keeps the same implementation baseline but gives reviewers distinct primary responsibilities:

- correctness/regressions;
- architecture conformity/complexity;
- contracts/test adequacy.

Independent findings remain comparable because the reviewers share the same frozen task and diff baseline.

## 7. Security and other specialists can be invoked unnecessarily

Dedicated security, SQL, DevOps, and performance agents are useful for high-risk surfaces, but invoking them conservatively on every uncertain task increases cost substantially.

Ordinary concerns should be handled through shared rules and normal reviewers.

Dedicated specialist agents should run only when concrete evidence shows their owned risk surface is materially affected.

## 8. Testing should not depend on review convergence

If tests are delayed until reviewers report no warnings, test execution becomes blocked by a potentially non-convergent LLM process.

Tests are deterministic engineering evidence and should be available to reviewers.

The desired order is:

```text id="a0y4wn"
implementation
→ build
→ test design/implementation/execution
→ semantic review
```

Reviewers should evaluate both production code and its verification evidence.

## 9. The harness has duplicated orchestration responsibility

Software-team behavior currently exists across global instructions, team rules, task rules, individual agents, and review rules.

This causes:

- duplicated policy;
- stale references;
- conflicting ownership;
- larger context;
- harder maintenance.

The intended ownership model is:

```text id="7kqczs"
AGENTS.md
    global constraints only

/task
    complex software workflow

/audit-infra
    harness workflow

shared rules
    role/domain semantics

agents
    specialized capabilities

scripts
    deterministic mechanics
```

Workflow orchestration should have one canonical owner.

## 10. `/task` is too heavy for ordinary harness maintenance

The durable task workflow is useful for complex product/software work, but applying the same lifecycle to skills, rules, agents, and harness scripts creates unnecessary overhead.

Harness maintenance typically needs:

```text id="0okxg4"
inspect
→ choose bounded change
→ implement
→ deterministic validation
→ targeted verification
→ done
```

It should not automatically create persistent task state, full architecture boards, tester/reviewer teams, and repeated review gates.

## 11. Session auditing and infrastructure auditing overlap

A separate session-audit skill/agent consumes additional tokens to diagnose token usage and workflow inefficiency.

Session problems usually indicate a gap in the harness itself.

Therefore session analysis should become an input scope of `/audit-infra`:

```text id="98uczq"
observe session friction
→ identify concrete harness gap
→ inspect relevant infrastructure
→ fix/validate that gap
```

A separate session-audit workflow is unnecessary.

## 12. Too many mechanical checks are delegated to LLMs

LLMs currently reason about some properties that can be enforced deterministically, such as:

- file naming;
- frontmatter;
- required sections;
- stale paths;
- broken routes;
- duplicated exact prose;
- task-template invariants.

This wastes tokens and introduces nondeterministic validation.

Mechanical invariants should move into scripts such as `ValidateInfrastructure.fsx`.

The intended principle is:

> LLMs reason about semantics. Scripts enforce mechanics.

## 13. Re-review itself currently changes the artifact

A concrete example of the convergence problem occurred while designing this migration.

Repeated requests to “review the instructions again” resulted in additional refinements each time, even though the previous version was already substantially correct.

This demonstrates that “review until no new idea exists” is not a viable completion criterion for LLM systems.

A frozen artifact needs explicit semantics:

```text id="2jl5tx"
first review
    = Discovery

later generic review
    = Verification
```

A frozen artifact returns to Discovery only when:

- the user explicitly asks for redesign/reconsideration; or
- a hard invalidation condition is demonstrated.

## 14. Completion criteria are currently weaker than discovery capability

An LLM's ability to continue finding possible improvements is effectively unbounded relative to practical engineering needs.

Therefore completion cannot mean:

```text id="aknr2p"
no agent can think of another improvement
```

Completion must instead mean:

```text id="vykq1w"
frozen requirements satisfied
+ deterministic verification passes
+ no unresolved accepted blocking findings
+ bounded verification completed
```

That is the convergence definition this migration must enforce.

# Desired Result

After the migration, both primary workflows should have monotonic state progression.

For `/task`:

```text id="ypkhud"
uncertainty decreases
→ architecture freezes
→ implementation completes
→ tests establish evidence
→ finding set freezes
→ blockers decrease
→ verification completes
→ done
```

For `/audit-infra`:

```text id="enrdlo"
observed gap
→ accepted scope freezes
→ smallest solution freezes
→ harness changes
→ deterministic validation
→ targeted verification
→ done
```

No phase should casually reopen an earlier phase.

The system should prefer bounded completion over indefinite pursuit of theoretical perfection.
