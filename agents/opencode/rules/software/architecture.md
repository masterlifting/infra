# Engineering Principles

Load this file for architecture tradeoffs, larger refactors, financial workflows, or ambiguous design choices.

## Pragmatism

- Prefer small reversible changes over broad rewrites.
- Avoid speculative abstractions; introduce abstractions only when duplication or variation is concrete.
- Keep orthogonal concerns separated.
- Preserve observability and debuggability when simplifying code.

## Routine Design

- Keep methods cohesive and short enough to understand without jumping across many helpers.
- Prefer guard clauses over deep nesting.
- Keep variable scope narrow.
- Replace magic values with named constants when meaning is not obvious.
- Use comments for non-obvious intent, constraints, and operational decisions; do not narrate simple code.

## Concurrency And Financial Integrity

- Use idempotency keys/request IDs for externally retried operations.
- Use optimistic locking, row versions, `SELECT FOR UPDATE`, unique constraints, or serializable isolation where financial consistency requires it.
- Use Redis only for derived/transient state, not as the system of record.
- Use atomic Redis operations and TTLs for cached/lock state.

## Data And Contracts

- Multi-step data changes should be idempotent or safely retryable.
- Schema and contract evolution should be backward compatible where persisted data or external consumers exist.
- Add fields before removing or renaming fields that deployed code may still read.

## Resilience

- External calls need timeouts, retries with backoff/jitter, and circuit breakers where appropriate.
- Design failure isolation before adding more coupling.
