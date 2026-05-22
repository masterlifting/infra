# Rust Architecture Rules

## Scope

- Canonical architecture rule set for Rust systems and libraries.
- Prioritize clear crate boundaries, low coupling, testability, resilience, and evolvability.
- Prefer simple, reversible architecture decisions over premature complexity.
- Use the type system to encode constraints; let the compiler enforce architectural invariants.

## Architectural Styles

- Prefer feature-oriented module organization (`mod orders/`, `mod billing/`) over technical layering when features are cohesive.
- Use layered architecture (Domain / Application / Infrastructure) when separation of concerns dominates.
- For multi-crate systems, use a Cargo workspace; split into crates along stable, slow-changing boundaries.
- Enforce inward dependency flow: infrastructure crates depend on domain/application crates, never the reverse.
- Keep domain crates free of I/O, framework, and runtime dependencies.
- Keep module and crate boundaries explicit; avoid cross-feature leakage via `pub(crate)`/`pub(super)`.

## Workspace and Crate Boundaries

- Promote code into its own crate when it has stable, well-defined responsibilities and multiple dependents.
- Do not split a crate that is fewer than ~500 lines with a single consumer — premature granularity hurts more than it helps.
- Eliminate circular dependencies by extracting shared interfaces/types into a third crate that both sides depend on.
- Declare workspace-wide dependency versions in the root `Cargo.toml`'s `[workspace.dependencies]` to prevent drift.
- Pin the workspace MSRV centrally; treat MSRV changes as breaking for downstream consumers.
- Separate `lib` and `bin` crates; keep `bin` thin (argument parsing, wiring, runtime init).

## Application and Domain Design

- Model invariants with the type system: newtypes for semantic primitives, enums for closed variant sets, sealed traits for closed protocols.
- Make invalid states unrepresentable — prefer `enum` over `bool` flags when variants carry distinct data.
- Use immutable domain types by default; introduce interior mutability only with `Cell`/`RefCell` (single-threaded) or `Mutex`/`RwLock` (multi-threaded) when required.
- Prefer explicit domain outcomes (`Result<T, DomainError>`, `Option<T>`) for expected failures and absence.
- Keep domain types free of serde/sqlx/HTTP attributes; map to transport DTOs at the boundary.

## Patterns and Coordination

- Prefer composition (structs holding collaborators behind trait objects or generics) over inheritance-style hierarchies.
- Use trait-based polymorphism for variation points; choose `dyn Trait` for runtime selection or heterogeneous collections, generics for monomorphized performance.
- Use the builder pattern for construction with many optional fields.
- Use the typestate pattern (encoding state in the type) to make illegal transitions a compile error.
- Use the newtype pattern to add semantics or restrict APIs without runtime cost.
- Reach for higher-order functions and closures when they improve composability; do not pass closures across `await` points without considering `Send` bounds.

## Data Access and Transactions

- Hide persistence behind a repository or query-service trait when the storage choice may evolve; do not abstract prematurely if there is only one backend.
- Use Unit of Work / transactional scopes for multi-statement writes; align transactions with aggregate boundaries.
- Prefer compile-time-checked queries (`sqlx::query!`) over string-built SQL where the toolchain supports it.
- Never concatenate user input into SQL; always parameterize.
- Use a connection pool (`sqlx::PgPool`, `deadpool`) sized to the workload; do not open per-request connections.
- Keep migrations versioned, reversible where feasible, and reviewed alongside the code that depends on them.
- Avoid leaking ORM/query-builder types into domain logic; map at the repository boundary.

## Concurrency Architecture

- Prefer message passing (`mpsc`, `broadcast`, actor patterns) over shared mutable state when components have distinct lifetimes.
- Apply bounded channels for back-pressure; document fan-in/fan-out shape.
- Use `tokio::sync::Mutex` only when a lock must span an `await`; otherwise prefer `std::sync::Mutex` or restructure to avoid sharing.
- Treat cancellation as a first-class concern: every long-running task accepts a `CancellationToken` or equivalent.
- Choose runtime model deliberately — single-threaded `current_thread` for deterministic ordering, `multi_thread` for parallelism — and document the choice.

## Functional Core, Imperative Shell

- Keep core decision logic pure and deterministic where possible — pure functions over `&Input` returning `Result<Output, Error>`.
- Isolate side effects (DB, HTTP, FS, message bus) at the boundary; pass them in as trait-bound dependencies for testability.
- Keep adapters thin; push branching and decisions into the pure core.
- Prefer free functions over methods when behavior does not depend on state.

## API and Boundary Design

- Validate external input at the boundary using a validation crate (`validator`, `garde`) or explicit parsing into domain types.
- Keep transport DTOs (serde-annotated request/response structs) separate from domain entities.
- Map between transport and domain explicitly; do not derive serde on domain types just to skip the conversion.
- Use newtypes at boundaries to enforce parsing (`EmailAddress::parse(&str) -> Result<EmailAddress, _>`).
- Apply explicit API versioning (URL prefix, header, media type) for public HTTP APIs.
- Treat your crate's public items (everything `pub` in `lib.rs`) as a contract; bump semver accordingly and use `cargo semver-checks` in CI.

## Public API Surface and Evolution

- Re-export the supported public surface from `lib.rs` via `pub use`; hide internal module paths.
- Mark items that must be public for macros but unsupported with `#[doc(hidden)]`.
- Use sealed traits to prevent downstream impls when adding methods later would otherwise be breaking.
- Use `#[non_exhaustive]` on public enums and structs where future variants/fields are likely.
- Document MSRV changes, behavior changes, and breaking changes in a `CHANGELOG.md`.

## Code Quality and Refactoring

- Continuously remove smells: long functions, large modules, deep matches, leaky abstractions.
- Prefer small, safe refactorings (`Extract Function`, `Move Type`, `Split Module`) over rewrites.
- Replace deep `match`/`if` ladders with trait dispatch or table-driven logic where clearer.
- Treat `clippy` warnings as design feedback; if a lint is wrong for your case, `#[allow(...)]` it with a comment explaining why.
- Keep architectural debt visible (TODO/FIXME with issue links) and prioritize high-impact simplifications.

## Testing Alignment

- Unit-test domain and pure logic in inline `#[cfg(test)] mod tests` blocks.
- Integration-test wiring and boundary behavior under `tests/` at the crate root.
- Use doctest examples to verify the public API contract stays accurate.
- Use property-based tests (`proptest`, `quickcheck`) for invariants over large input spaces.
- Verify cross-cutting concerns (timeouts, cancellation, retries, authz) with focused integration tests.

## Operational Architecture Defaults

- Design for idempotency in distributed workflows; use idempotency keys or natural keys.
- Make failure modes explicit (timeouts, cancellation, fallback) at every external call.
- Apply timeouts, retries with backoff/jitter, and circuit breakers (`tower` middleware, `tokio-retry`) to external calls.
- Require observability at boundaries: structured `tracing` spans, metrics, health checks.
- Keep dependency direction, ownership, and contracts clear in code and module-level docs.
- Make every binary report its version (commit SHA, build date) on startup and `/version` endpoint.
