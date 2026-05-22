# C# Architecture Rules

## Scope

- Use this as the compact, canonical architecture rule set for C#/.NET systems.
- Prioritize clear boundaries, low coupling, testability, resilience, and evolvability.
- Prefer simple, reversible architecture decisions over premature complexity.

## Architectural Styles

- Prefer feature-oriented Vertical Slice architecture for modularity and cohesion (request/endpoint/handler/response per use case).
- Use layered architecture (Domain/Application/Infrastructure) when clear separation of concerns is needed.
- Enforce inward dependency flow: Infrastructure depends on Application/Domain, never the reverse.
- Keep Domain pure (entities, value objects, aggregates) with no persistence/framework coupling.
- Keep module boundaries explicit and avoid cross-feature leakage.

## Application and Domain Design

- Model consistency with aggregates as transactional boundaries.
- Prefer immutable domain models where practical (`record`, `init`, value objects).
- Make invalid states unrepresentable (constrained types over primitive obsession).
- Prefer explicit domain outcomes (`Result`/`Option`) for expected failures and absence.

## Patterns and Coordination

- Use CQRS when read/write concerns diverge meaningfully in scale, latency, or model shape.
- Use Mediator patterns to reduce direct dependency webs in use-case orchestration.
- Use Strategy/Policy patterns for variable business rules.
- Use Observer/Event-driven patterns for decoupled reactions to state changes.
- Use higher-order functions selectively where they improve composability and testability.

## Data Access and Transactions

- Hide persistence details behind application-facing contracts where it improves isolation and testing.
- Use Unit of Work semantics to keep multi-repository writes atomic and consistent.
- Keep transaction boundaries explicit and aligned with aggregate/use-case boundaries.
- Avoid leaking ORM-specific concerns into Domain logic.

## Functional Core, Imperative Shell

- Keep core decision logic pure and deterministic when possible.
- Isolate side effects (database, HTTP, messaging, file I/O) at boundary layers.
- Keep impure adapters thin and focused; keep orchestration explicit.

## API and Boundary Design

- Validate external input at the boundary (for example with FluentValidation).
- Keep transport DTOs separate from domain entities.
- Use mapping intentionally (manual or tooling such as AutoMapper) and avoid hidden magic.
- Apply explicit API versioning strategy (URI, header, or media type) for public APIs.

## Code Quality and Refactoring

- Continuously remove bloaters and couplers (long methods, large classes, message chains, feature envy).
- Prefer small, safe refactorings (`Extract Method`, `Move`, `Factory` transitions) over rewrites.
- Replace brittle branching trees with polymorphism/pattern-based dispatch where clearer.
- Keep architectural debt visible and prioritize high-impact simplifications.

## Testing Alignment

- Unit test Domain and Application behavior.
- Integration test Infrastructure and composition boundaries.
- Verify critical cross-cutting concerns (transactions, retries, authz, observability) with focused integration tests.

## Operational Architecture Defaults

- Design for idempotency and retry safety in distributed workflows.
- Make failure modes explicit (timeouts, cancellations, fallback behavior).
- Require observability at boundaries (structured logs, metrics, traces, health checks).
- Keep dependency direction, ownership, and contracts clear in code and docs.
