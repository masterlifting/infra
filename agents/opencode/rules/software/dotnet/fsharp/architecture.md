# F# Architecture Rules

## Scope

- Use this as the compact, canonical architecture rule set for F#/.NET systems.
- Prioritize domain clarity, composition, explicit dependency flow, and evolvable boundaries.
- Prefer simple, reversible architecture decisions over premature abstraction.

## Architectural Styles

- Prefer feature-oriented or workflow-oriented modules when they keep use cases cohesive and dependency direction obvious.
- Use layered or onion-style architecture when explicit Domain/Application/Infrastructure separation improves maintainability.
- Keep dependency flow inward: infrastructure and frameworks depend on domain/application code, never the reverse.
- Keep domain types and pure decision logic isolated from persistence, transport, and framework concerns.
- Organize files and folders in dependency order so each layer depends only on earlier layers.

## Application and Domain Design

- Separate behavior from data: keep records and discriminated unions simple, and place behavior in focused modules.
- Prefer discriminated unions and records to make illegal states unrepresentable and domain flows explicit.
- Use single-case unions for semantic primitives when they improve correctness at boundaries.
- Model expected domain failures with `Result` and expected absence with `Option`.
- Keep validation, mapping, orchestration, and side effects explicit instead of hiding them in large object graphs.

## Module and Dependency Design

- Prefer namespaces at the top level for broadly consumable code and modules for cohesive functional behavior.
- Use shared types modules early in compilation order when multiple features depend on common domain shapes.
- Keep private helper types close to the module that owns them.
- Prefer composition over inheritance; use interfaces or object expressions only where interop or substitution is needed.
- Encapsulate mutable infrastructure state behind narrow APIs or dedicated types.

## Functional Core, Imperative Shell

- Keep core workflows pure and deterministic where practical.
- Push I/O, persistence, HTTP, messaging, file system access, and framework code to boundary modules.
- Keep orchestration explicit in application/use-case modules rather than distributing it across many side-effecting helpers.
- Wrap mutable or stateful implementations in immutable, intention-revealing interfaces.

## API and Boundary Design

- Convert nullable and framework-specific inputs to F# domain shapes at boundaries.
- Keep transport DTOs separate from domain types when external contracts diverge from core models.
- Expose interop-friendly shapes (`Task`, standard .NET collections, classes/interfaces) only at .NET-facing boundaries.
- Keep public API signatures explicit and stable; avoid exposing overly curried or point-free public surfaces.

## Code Quality and Refactoring

- Prefer small, focused modules and safe refactorings over broad rewrites.
- Break cyclic dependencies by moving shared types down and orchestration up, not by adding speculative layers.
- Keep architectural debt visible and simplify high-friction module boundaries early.
- Use names and module structure to make workflows and ownership obvious to new contributors.

## Testing Alignment

- Unit test pure domain and workflow logic directly.
- Integration test infrastructure, serialization, persistence, and .NET interop boundaries.
- Add property-based tests when invariants, transformations, or state transitions benefit from generated coverage.
- Verify critical cross-cutting concerns such as transactions, retries, authorization, and observability with focused boundary tests.

## Operational Architecture Defaults

- Design workflows to make failure paths, retries, cancellation, and idempotency explicit.
- Keep dependency direction, module ownership, and boundary contracts clear in code and docs.
- Require structured observability at side-effecting boundaries.
- Treat secrets, trust boundaries, and external integrations as first-class architecture concerns.
