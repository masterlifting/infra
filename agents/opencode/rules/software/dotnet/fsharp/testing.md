# F# Testing Rules

## Scope

- Use this as the compact, canonical testing rule set for F#/.NET code.
- Prioritize reliability, fast feedback, expressive tests, and maintainable coverage.
- Keep tests aligned to architectural boundaries, domain invariants, and interop risks.

## Core Test Principles

- Keep tests deterministic, isolated, fast, and focused on one behavior or invariant.
- Prefer descriptive names that express the scenario and expected outcome.
- Favor behavior verification over implementation-detail coupling.
- Keep unit and integration tests clearly separated by project, namespace, or suite.

## Unit Testing

- Prefer `Expecto` as the default F# test framework.
- Write tests as composable values using `test`, `testCase`, `testAsync`, `testTask`, and `testList`.
- Prefer `Expect` assertions for clear, F#-native failure messages and expectation style.
- Prefer pure-function tests for domain logic and workflow steps.
- Test async code with `testAsync` and `testTask`; avoid blocking calls such as `.Result`, `.Wait()`, or `Async.RunSynchronously` outside explicit test harness boundaries.
- Keep unit tests easy to group and filter; use nested `testList` structure to mirror feature or module boundaries.
- Use realistic domain data and focused helpers to keep fixtures readable.
- Verify both expected outcomes and expected error cases for invalid paths.
- Prefer explicit setup/teardown functions, `use`/`IDisposable`, or `testFixture` helpers over attribute-heavy fixture models.
- Compile the test project as a runnable test assembly when using Expecto's integrated runner, and use `runTestsWithCLIArgs` or `runTestsInAssemblyWithCLIArgs` as the standard entry path.

## Property-Based Testing

- Use property-based testing with `Expecto.FsCheck` and FsCheck when invariants, transformations, parsers, reducers, or state transitions benefit from generated coverage.
- Prefer properties that express domain rules and algebraic behavior over duplicating example-based tests.
- Guide generators toward valid domain data and add custom generators/shrinkers when defaults produce low-value cases.
- Prefer `testProperty` and `testPropertyWithConfig` so property tests compose naturally with other Expecto test values.
- Keep custom FsCheck configuration explicit per test area; avoid thread-local registration patterns that conflict with Expecto's parallel execution model.
- Keep counterexamples understandable and minimize generated-data magic that obscures failures.

## Integration Testing

- Test persistence, HTTP, messaging, serialization, and external process boundaries with focused integration tests.
- Prefer production-like infrastructure or thin wrappers over unrealistic in-memory substitutes when boundary behavior matters.
- Verify cancellation, retry, timeout, auth, and configuration paths explicitly when they affect runtime safety.
- Keep integration fixtures explicit about lifecycle, setup, teardown, and shared-state isolation.

## Infrastructure and External Dependencies

- Mock or stub external dependencies only to isolate the behavior under test; prefer real integrations when wiring and contracts are the risk.
- Keep test-time configuration explicit and avoid hidden environment coupling.
- Isolate filesystem, network, database, and clock dependencies behind narrow seams when that improves determinism.
- Use captured samples or contract fixtures for serialization and interop boundaries when regressions are costly.

## Performance, Parallelism, and Stability

- Remember that Expecto runs tests in parallel and async by default; opt into sequenced execution only where shared state, timing, or ordering requires it.
- Use `testSequenced` or `testSequencedGroup` for tests that share mutable resources or have ordering constraints.
- Prevent cross-test pollution with explicit cleanup or disposable fixture boundaries.
- Keep flaky test budget at zero: fix or quarantine immediately.
- Treat nondeterministic async and timing-heavy tests as a design smell until proven necessary.

## Coverage Strategy

- Unit tests should cover domain and application behavior.
- Integration tests should cover infrastructure wiring and boundary behavior.
- Property tests should cover invariants and input-space exploration where example-based tests are weak.
- Add regression tests for every production bug fix.
- Prefer risk-based coverage over raw percentage targets.

## Productivity and Workflow

- Use focused test runs during development and run broader suites before merge.
- Use Expecto filtering, labels, and focused tests intentionally during local development, but fail CI on focused tests when practical.
- Prefer `dotnet run` or `dotnet watch run` for Expecto-driven test projects, and use the Visual Studio test adapter only when IDE integration is needed.
- Extract reusable builders, generators, and assertions only when they improve readability and reduce duplication.
- Keep failure messages and assertion output actionable.
- Prefer test helpers that preserve F# clarity over opaque fixture frameworks.
