# F# Engineering Rules

## Scope

- Canonical rule set for modern F# code; prioritize domain clarity, correctness, composability, and maintainable performance.
- Prefer idiomatic F# first; use interop-friendly shapes at .NET boundaries.
- Enforce style with Fantomas as the canonical formatter; never hand-format against formatter output.
- Run `dotnet format` / Fantomas in CI; keep CI warning-clean and treat compiler warnings as design feedback.
- Keep project defaults deterministic: pin SDK, TFM, and language version; avoid machine-dependent settings.
- For libraries, use explicit API surface controls (access modifiers, optional `.fsi` for stable APIs).

## Type Declarations

- Default to immutability; introduce mutation only where measured and justified.
- Prefer `let mutable` over `ref` cells for local mutable values.
- Encapsulate mutable state inside local scopes or dedicated types.
- Prefer explicit domain types over primitive obsession (`string`, `int`) in core logic.
- Use single-case unions for semantic primitives (IDs, email, money units).
- Use meaningful names for generic parameters in domain-facing APIs.
- Use type abbreviations only when they improve readability without hiding structure.

## Nullability

- Avoid propagating `null` in F# domain code; convert nullable inputs to `option` at boundaries.
- Use `Option.ofObj` and explicit null checks at interop edges.
- Leverage F# nullability syntax (`| null`) for API-boundary clarity where applicable.
- Do not use `AllowNullLiteral`, `DefaultValue`, or `Unchecked.defaultof<_>` unless strictly necessary.
- Validate all external input at boundaries before constructing domain values.

## Pattern Matching and Equality

- Prefer exhaustive `match` expressions for domain branching; resolve non-exhaustive warnings rather than suppressing them.
- Keep match arms small; refactor complex branches into named functions.
- Use active patterns when they improve readability and isolate classification logic.
- Prefer explicit, total branching over clever partial logic.
- Rely on structural equality for records/DUs; override equality only when domain semantics require it.
- Avoid custom `op_Equality`/comparison operators unless domain-justified.

## Deconstruction and Discards

- Use tuple and record deconstruction (`let (a, b) = ...`, `{ Field = x }`) where it improves clarity.
- Use `_` discards for intentionally ignored bindings, match wildcards, and unused tuple elements.
- Do not name real values `_`; it conflicts with discard semantics.
- Prefer named field destructuring on records over positional access on large shapes.

## Functions and API Design

- Keep public function signatures explicit and intention-revealing.
- Avoid point-free/currying-heavy public APIs when they reduce discoverability or tooling clarity.
- Use partial application internally to reduce boilerplate and improve composition.
- Prefer small pure functions plus orchestration layers over monolithic workflows.
- Keep public function arguments ordered for pipelining: data-last for most domain APIs.
- Use named arguments at call sites when readability improves.

## Option and Result Modeling

- Use `Option<'T>` for expected absence and `Result<'T,'E>` for expected domain failures.
- Model errors as typed domain cases (DUs), not stringly typed payloads.
- Consume `option`/`result` via pattern matching or module combinators (`map`, `bind`, `defaultValue`).
- Avoid nested result pyramids by composing with computation expressions or focused helper modules.
- Keep `Result`/`Option` usage intentional; they complement exceptions, not replace all exceptions.
- Reserve exceptions for truly exceptional/system failures, not normal domain flow.

## Async and Concurrency

- Prefer `async {}` workflows for idiomatic F# composition-heavy async code.
- Prefer `task {}` where .NET task interop is the dominant concern.
- Convert intentionally between models (`Async.AwaitTask`, `Async.StartAsTask`) at boundaries.
- Use `Async.Parallel` for independent work; preserve cancellation and error semantics deliberately.
- Thread `CancellationToken` explicitly through long-running operations; never ignore a passed token.
- Keep `try/with` around I/O boundaries; avoid broad catch-all async wrappers.
- Avoid blocking calls (`Async.RunSynchronously`, `.Result`, `.Wait()`) outside of script entry points.

## Collections, Pipelines, and Iteration

- Use `List`, `Array`, `Seq`, and `Map` based on semantics (immutability, indexing, laziness, lookup).
- Prefer collection module functions and pipelines over imperative loops in non-hot paths.
- Place filters early in pipelines to reduce downstream work; avoid repeated traversals when costly.
- Use `Seq` laziness intentionally; materialize when lifetime/re-evaluation could surprise callers.
- Keep pipelines readable: one stage per line when expressions become non-trivial.
- Use `Map`/`Set` for O(log n) lookups; avoid linear scans of `List` in hot paths.

## Object Modeling and Encapsulation

- Make illegal states unrepresentable with discriminated unions and records.
- Prefer discriminated unions over small inheritance hierarchies for domain state machines and trees.
- Validate constrained values at constructors/boundaries, not ad hoc across call sites.
- Prefer namespaces at top level for broadly consumable code; use modules to group related functions and workflows.
- Use `[<RequireQualifiedAccess>]` when names are likely to collide or clarity improves.
- Apply `[<AutoOpen>]` sparingly; avoid polluting caller scope.
- Keep `open` statements topologically ordered to reduce accidental shadowing.
- Hide representations likely to evolve (private union cases, module encapsulation, optional `.fsi`).

## Memory and Allocation

- Optimize with profiling data, not assumptions.
- Consider `struct` records/tuples/unions only for small, short-lived hot-path types; measure before and after.
- Avoid boxing across generic and `obj` boundaries on hot paths.
- Materialize `Seq` pipelines once when reuse would re-evaluate expensive sources.
- Prefer `Array` over `List` for tight numeric loops where index access dominates.
- Use `Span<'T>` / `ReadOnlySpan<'T>` at .NET interop boundaries when slicing without allocation matters.

## Strings

- Use `StringBuilder` for repeated concatenation in loops.
- Use `System.String.Equals` with `StringComparison.Ordinal` for non-user-facing comparisons.
- Prefer `sprintf` / interpolated strings for readability; reserve `String.Format` for interop.
- Avoid leaking sensitive payloads through `sprintf "%A"` of arbitrary records.

## Logging and Diagnostics

- Use structured logging templates by default; avoid pre-formatted strings as log messages.
- Use `nameof` for refactor-safe member names in logs and validation messages.
- Keep logs free of secrets and sensitive payloads in error messages.
- Prefer explicit behavior over implicit magic in shared and long-lived code.

## Exception Handling

- Use exceptions for exceptional or environmental failures (I/O, infrastructure, runtime faults).
- Raise specific exceptions (`nullArg`, `invalidArg`, `invalidOp`) instead of generic `failwith` when possible.
- Order `with` patterns from most specific to least specific.
- Catch only what you can handle meaningfully; do not swallow diagnostic context.
- Re-raise with `reraise()` to preserve stack traces.
- Convert exceptions to domain errors only at boundaries where the mapping is well-defined.
- Do not use exceptions for normal control flow.

## Interop with .NET

- For .NET-facing APIs, prefer namespace + type/member organization over module-only surfaces.
- Expose `Task`/`Task<'T>` to non-F# consumers; convert from `Async` at the boundary.
- Prefer `Func`/delegates and standard .NET collection interfaces on public interop boundaries.
- Avoid exposing F#-specific shapes (`option`, curried signatures, F# function types) in vanilla .NET APIs unless intentional.
- Use `[<CompiledName>]` only when needed to provide a clearer non-F# API surface.
- Convert nullable inputs to `option` immediately on entry and back at the exit boundary.

## Scripting and Automation

- Prefer `.fsx` for automation scripts unless another language is required by contract.
- Name `.fsx` files in CamelCase/PascalCase (`ValidateTask.fsx`, not `validate-task.fsx`).
- Keep reusable, feature-agnostic helpers in `C:/Users/andre/.config/opencode/scripts/*.fsx` and load them from project-local scripts with absolute `#load` paths; do not copy-paste common helpers into each repo.
- Use `#load` for shared `.fsx` source files; reserve `#I` for assembly search paths and package sources, not script helper discovery.
- Keep scripts thin; compose shared helpers rather than duplicating utility code.
- Use explicit script entry flow and fail-fast argument validation.
- Pin script dependencies (`#r "nuget: Pkg, x.y.z"`) for reproducibility.

## Testing and Reliability

- Test domain logic primarily with pure function tests and property-based tests where useful.
- Keep boundary tests for I/O, serialization, and interop contracts.
- Ensure error paths are tested explicitly, not only happy-path behavior.
- Prefer expression-style assertions that surface offending values on failure.

## Versioning and Evolution

- Evolve public APIs additively where possible; avoid breaking shape changes in widely used modules/types.
- Hide representations likely to evolve (private union cases, module encapsulation, optional `.fsi`).
- Document migration expectations when changing domain types or error contracts.
