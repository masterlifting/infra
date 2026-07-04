# C# Engineering Rules

## Scope

- Canonical rule set for C# code; prioritize correctness, readability, maintainability, and predictable behavior.
- Prefer modern C#/.NET features when they improve clarity or safety.
- Enforce style with analyzers and `.editorconfig`; run `dotnet format`; keep CI warning-clean.
- Keep project defaults deterministic: SDK, TFM, language version, and nullable settings.

## Type Declarations

- Use `var` when the RHS type is obvious (`new`, literal, explicit cast, LINQ projection); use explicit types otherwise.
- Use `const` for compile-time constants and two or more occurrences;
- Use `readonly`/`init`/records for runtime immutability.
- Use explicit element types in `foreach` when element type is unclear.
- Use `dynamic` only when runtime binding is genuinely required; avoid `object` as an "any type".

## Nullability

- Enable non-nullable reference types by default; add `?` only for meaningful nullable states.
- Treat nullable warnings as design feedback; do not suppress without rationale.
- Prefer explicit null checks over `!`; use `!` only when invariants are guaranteed by context.
- Use `required` for mandatory fields; keep null guards at public boundaries (`ArgumentNullException.ThrowIfNull`).
- Annotate APIs with flow-analysis attributes (`NotNullWhen`, `MaybeNull`, `AllowNull`, `MemberNotNull`) when needed.
- Arrays of non-nullable references are initialized with `null` elements; guard accordingly.
- `default` on a struct can contain `null` reference fields; account for this in initialization paths.

## Pattern Matching and Equality

- Prefer `is null` / `is not null` over `==` in framework-style code (operator can be overloaded).
- Prefer pattern matching (`is`, type/property/list/relational patterns) for shape-safe branching.
- Use `switch` expressions with explicit fallback (`_`); resolve non-exhaustive pattern warnings.
- Use declaration patterns for combined null + type checks (`x is SomeType value`).
- Prefer list/slice patterns for irregular sequence shapes over brittle index arithmetic.
- Avoid custom implicit conversions and surprising equality operators unless domain-justified.

## Deconstruction and Discards

- Deconstruct tuples with `var (...)` or explicit element types; avoid mixed styles.
- Expose `Deconstruct(out ...)` on user-defined types only for meaningful views; distinguish overloads by arity.
- Use `_` discards for intentionally ignored tuple elements, pattern inputs, and `out` values.
- Do not name real variables `_`; it conflicts with discard semantics.

## Methods and API Design

- Keep signatures minimal, explicit, and intention-revealing; overload by parameter shape, never by return type.
- Pass by value by default; use `ref` for intentional caller-visible mutation, `out` for required multi-value output, `in` for large read-only value types.
- Use `params` for variable-length arguments.
- Prefer named arguments when readability improves or optional arguments are skipped; place optional after required.
- Prefer return values over input mutation; use named tuples for small multi-value returns.
- Keep methods small and focused; use expression-bodied members for trivial one-expression members.
- Keep property getters fast, side-effect free, and transparent: properties should expose already-available state or cheap calculated values only. If access performs non-trivial work, allocates defensive copies, does I/O, mutates state, queries services, or otherwise hides runtime logic, model it as an explicit method (`Get...`, `Create...`, `Load...`, etc.) instead of a property.
- Keep constructors simple and predictable. Do not hide non-trivial initialization, I/O, service calls, heavy computation, or business logic in constructors; use factories, initialization methods, lazy/static cached data, or explicit load/create methods when work can fail or has meaningful cost.

## Option and Result Modeling

- Prefer `Option<T>` for expected absence and `Result<T, TError>` for expected failures over `null`/exceptions.
- Model outcomes as explicit sum types (`Some/None`, `Ok/Error`) consumed with exhaustive pattern matching.
- Reserve exceptions for truly exceptional/system failures, not normal domain flow.
- Avoid implicit conversions from `Option`/`Result` to raw values; require explicit handling.
- Keep error payloads typed and actionable (`TError` should convey enough to act on).
- Use a `Unit`-like marker type instead of `void` in generic result pipelines.

## Async and Concurrency

- Use `async`/`await` for I/O-bound work; avoid sync-over-async.
- Prefer `Task`/`Task<T>`; use `async void` only for event handlers.
- Use `ValueTask` only in proven hot paths where allocation reduction is measured.
- Avoid `.Result`/`.Wait()`; they cause deadlocks under certain synchronization contexts.
- Use `CancellationToken` for cooperative cancellation; never ignore a passed token.
- Lock on a private object; never on `this`, `Type`, or a public object.
- Prefer `SemaphoreSlim` for async-compatible locking; use `lock` for simple sync cases.
- Use `await using` for async disposables in `async` methods.
- Use `ConfigureAwait(false)` in library code to avoid forcing a return to the caller's sync context.
- Use `Parallel.ForEach` or PLINQ for CPU-bound parallel work; do not parallelize I/O-bound work with `Parallel`.

## Collections, LINQ, and Iteration

- Prefer collection expressions for clear initialization.
- Use meaningful variable names and aliases in LINQ projections.
- Place `Where` early to reduce downstream query work.
- Use `foreach` for `IEnumerable<T>`; use `await foreach` for `IAsyncEnumerable<T>`.
- Prefer generic collection interfaces over non-generic versions.
- Use iterator methods with `yield return` for lazy streaming; do not mix `return` and `yield return` in the same method.
- Avoid LINQ in hot paths where iterator allocations are measurable; prefer `for`/`foreach`.
- Use `Dictionary<K,V>` or `HashSet<T>` for O(1) lookups; avoid O(n) `List` searches.

## Object Modeling and Encapsulation

- Use `required` + `init` to prevent partially initialized objects.
- Prefer `record` for immutable DTO/value-like models; prefer `readonly struct` for immutable value types.
- Use file-scoped namespaces; place `using` directives outside namespace declarations.
- Use `file`-scoped types for helpers that must remain file-local.
- Use extension members to add behavior without modifying original types.
- Use default interface implementations sparingly.

## Memory and Allocation

- Use `Span<T>`/`ReadOnlySpan<T>` to slice arrays and strings without heap allocation.
- Avoid boxing: pass value types as their concrete type or generic parameter, not `object`.
- Rent large temporary buffers from `ArrayPool<T>.Shared` on hot paths.
- Use `stackalloc` for small, short-lived buffers on performance-critical paths.
- Set initial capacity on `List<T>`/`StringBuilder` when size is known upfront.
- Prefer `struct` for small, short-lived value models to reduce heap pressure.
- Mark leaf classes `sealed` to enable JIT devirtualization.

## Strings

- Use `StringBuilder` for repeated concatenation in loops.
- Use `StringComparison.Ordinal` for non-user-facing comparisons.
- Use interpolated string handlers only with logging APIs that optimize interpolation.
- Use `string.Intern` only for a small set of known, frequently repeated strings; avoid it for general use due to memory retention risks.

## Logging and Diagnostics

- Use structured logging templates by default.
- Use `nameof(...)` for refactor-safe member names in logs and validation messages.
- Use `[CallerArgumentExpression]` to improve guard and validation diagnostics.
- Prefix lifecycle log messages with the component or integration name in brackets, for example `[Payments]` or `[Braze]`.
- Use consistent lifecycle wording for bookend logs: `Start <details>` on operation entry, `Finish <details>` on successful completion, and `Failed <description>` for failures.
- Keep structured placeholders in log templates, for example `{UserId}` or `{OrderId}`; do not collapse diagnostics into pre-formatted strings.

## Exception Handling

- Order `catch` blocks from most specific to least specific.
- Use exception filters (`when`) for conditional handling.
- Catch only exceptions you can handle; let others propagate.
- Re-throw with `throw;` to preserve stack traces.
- Do not use exceptions for normal control flow.
- Throw specific exception types with actionable messages; prefer framework exceptions before custom ones.
- Use custom exceptions only for meaningful domain-level categories.

## EF Core

### Schema and Modeling

- Use descriptive entity names and explicit domain models.
- Prefer Fluent API for complex mappings; use annotations for simple cases.
- Keep mappings in `IEntityTypeConfiguration<T>` classes; keep `OnModelCreating` readable.
- Specify precise column types/lengths; avoid `nvarchar(max)` unless required.
- Prefer non-nullable columns when the domain requires a value.
- Index foreign keys and frequent `WHERE`/`ORDER BY` columns.
- Commit migration + designer + snapshot files together; use descriptive migration names; review generated SQL for destructive changes.
- Keep connection strings in environment-specific config files, not in code.

### Query Performance

- Project only required columns with `Select(...)`.
- Filter early (`Where`) before expensive joins/projections.
- Use `AsNoTracking()` for read-only queries.
- Paginate large result sets with `Skip`/`Take`.
- Use compiled queries (`EF.CompileQuery`) for hot paths with repeated shapes.
- Use `AsSplitQuery()` for large `Include` graphs to avoid cartesian explosions.
- Apply global query filters for cross-cutting concerns (soft delete, tenancy).
- Use async terminal APIs (`ToListAsync`, `FirstOrDefaultAsync`, `SaveChangesAsync`) on I/O paths.

### Write Patterns and Safety

- Use eager loading (`Include`) when related data is definitely needed; avoid N+1 patterns.
- Use lazy loading only when intentional and measured.
- Batch inserts with `AddRange`/`AddRangeAsync`.
- Prefer `ExecuteUpdate`/`ExecuteDelete` for large set-based writes over per-entity loops.
- Never concatenate untrusted input into raw SQL; always use parameterized queries.
- Prefer optimistic concurrency (concurrency token/`rowversion`) by default; use pessimistic locking only when strict coordination is required.
- One `DbContext` per unit-of-work scope; never run concurrent operations on the same instance.
- Use interceptors for cross-cutting persistence concerns (auditing, soft delete, policy enforcement).
