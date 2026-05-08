# .NET Architecture Rules

Load this file when reviewing application structure, module boundaries, dependencies, resilience, configuration, or observability.

## Principles

- Keep domain/application logic independent from infrastructure details.
- Prefer feature- or domain-based organization when it improves locality and matches existing project style.
- Runtime behavior belongs in typed configuration; secrets live in the approved secret store.
- Resilience is mandatory for external calls and unreliable dependencies.

## Module Boundaries

- Keep dependencies pointing inward toward domain/application code.
- Avoid direct references from core logic to infrastructure implementation details.
- Keep public contracts stable and version breaking changes intentionally.
- Avoid hidden coupling through shared mutable state, global state, or service locator patterns.

## Dependency Injection

- Prefer built-in .NET DI.
- No scoped service injected into singleton.
- Avoid service locator patterns in business code.
- Register interfaces to keep code testable.
- Singleton services must be thread-safe.

## Config And Secrets

- Config classes use the project’s typed configuration convention.
- Defaults in code are for test/beta only and must be safe.
- Production secrets are never hardcoded; they live in the approved secret store.
- Critical config should be validated on startup.

## Resilience

- External HTTP calls require timeout, retry, and circuit breaker behavior.
- Background or retried workflows should be idempotent.
- Multi-step workflows should make transaction boundaries and failure behavior explicit.

## Observability

- Use `ILogger<T>` and structured logging.
- Use `BeginScope` at entry points where correlation matters.
- Business metrics use the project’s established `Meter` naming convention.
- Expose health or diagnostic checks where the application is long-running or externally operated.

## Anti-Patterns

- Circular dependencies between modules.
- Infrastructure concerns leaking into domain logic.
- Hidden global state or shared mutable state.
- God objects and broad feature coupling.
- Configuration or secrets embedded in code.
- Premature frameworks or abstract factory factories where a small function would work.
