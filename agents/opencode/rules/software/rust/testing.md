# Rust Testing Rules

## Scope

- Canonical testing rule set for Rust code.
- Prioritize reliability, fast feedback, clear intent, and maintainable coverage.
- Keep tests aligned to crate boundaries and real failure risks.

## Core Test Principles

- Structure tests with AAA (`Arrange`, `Act`, `Assert`) or `Given/When/Then`.
- Keep tests deterministic, isolated, fast, and focused on one behavior.
- Use descriptive function names (`parses_iso8601_timestamp`, `returns_error_on_empty_input`); avoid `test_1`/`test_foo`.
- Separate unit tests (inline `#[cfg(test)]` modules) from integration tests (`tests/` directory).
- Favor behavior verification over coupling to internal types or call counts.
- Treat tests as first-class code: review them, refactor them, keep them clippy-clean.

## Unit Testing

- Place unit tests in an inline `#[cfg(test)] mod tests { ... }` block at the bottom of the module under test; this lets tests access `pub(crate)` items.
- Use `#[test]` for sync tests; `#[should_panic(expected = "...")]` only when panic is the documented behavior.
- Use `assert!`, `assert_eq!`, `assert_ne!` with messages that explain the failure: `assert_eq!(got, want, "case = {case}")`.
- Use `rstest` (or equivalent) for parameterized cases; avoid copy-paste test bodies that differ only in input.
- Test error paths explicitly: assert on the error variant and any carried context, not just `is_err()`.
- Avoid `unwrap()`/`expect()` outside assertions; let the test framework report the actual `Result` for clearer failures.

## Async Testing

- Use `#[tokio::test]` for async tests; never call `block_on` from inside a `#[test]`.
- Use `flavor = "current_thread"` (the default) for deterministic ordering; use `flavor = "multi_thread"` when the code under test requires real parallelism.
- Never call `std::thread::sleep` inside an async test — it blocks the executor; use `tokio::time::sleep`.
- Use `tokio::time::pause()` + `advance()` to make time-dependent tests instant and deterministic.
- Use `tokio::test(start_paused = true)` to start with virtual time paused.
- Ensure spawned tasks complete (or are explicitly cancelled) before the test ends; orphaned tasks cause flakes.

## Integration Testing

- Place integration tests in `tests/` at the crate root; each `.rs` file there compiles as its own crate and can only see the crate's public API.
- Put shared integration test helpers in `tests/common/mod.rs` (the `mod.rs` form prevents the helper from being compiled as its own test binary).
- Put test-only dependencies under `[dev-dependencies]`; they do not ship in the production binary.
- Spin up the system under test with realistic wiring; minimize stubbing of components owned by the crate.
- Reset external state between tests (database transactions rolled back, temp dirs cleaned) — do not rely on test order.

## Property-Based Testing

- Use `proptest` (preferred) or `quickcheck` to assert invariants over large input spaces.
- Prefer property tests for: serialization round-trips, parser/printer pairs, commutative/associative operations, ordering and equality laws.
- Shrink failures to a minimal counter-example; commit the regression seed (`proptest-regressions/`) so the failure stays reproducible.
- Keep individual property cases fast; expensive properties belong in a dedicated test suite, not in default `cargo test`.

## Documentation Tests

- Write rustdoc examples that compile and run via `cargo test --doc`; treat them as part of the API contract.
- Mark examples that should compile but not run with `no_run`; mark intentional compile-failure examples with `compile_fail`.
- Hide setup lines with leading `#` to keep examples focused while still compiling.
- Avoid examples that depend on network, filesystem, or external services in doctests; move those to integration tests.

## Test Doubles and Mocking

- Inject collaborators behind trait bounds and substitute test implementations; reach for `mockall` only when hand-rolled fakes become repetitive.
- Prefer hand-written fakes for simple traits; their behavior is explicit and they survive refactors better than auto-generated mocks.
- Use `wiremock` (or equivalent) to stub HTTP services; assert the calls made when the contract matters.
- Use `tempfile` for filesystem tests; never write to a fixed path that could collide between parallel runs.

## Infrastructure and External Dependencies

- Prefer production-like infrastructure in integration tests (Testcontainers, ephemeral Postgres) over in-memory substitutes when the divergence could mask bugs.
- Bind ephemeral ports (`:0`) and discover the actual port at runtime; never hardcode test ports.
- Share expensive setup (containers, DB connections) across tests with a static `OnceCell` / `LazyLock` initializer when safe; otherwise spin up per test.
- Isolate database state via per-test transactions or per-test schemas; do not rely on global cleanup.
- Stub network calls (`wiremock`, `httpmock`) for third-party APIs that are flaky, rate-limited, or paid.

## Performance, Parallelism, and Stability

- `cargo test` runs tests in parallel by default; ensure tests do not share mutable global state.
- Serialize tests that must run alone with `serial_test` (or equivalent); document why serialization is needed.
- Keep flaky tests at zero: quarantine immediately, root-cause within a defined window, fix or delete — do not normalize flakes.
- Avoid wall-clock sleeps; rely on time mocks, signals, or readiness checks instead.
- Run `cargo test --release` periodically to catch optimization-level-dependent bugs (overflow, ordering).
- Run `cargo miri test` on crates containing `unsafe` to catch undefined behavior in tests.

## Benchmarking

- Use `criterion` for stable, statistical benchmarking; do not rely on `#[bench]` (nightly-only, less rigorous).
- Place benches under `benches/` and gate them behind a feature or `[dev-dependencies]` so they do not impact normal test runs.
- Pin the benchmarked environment (CPU governor, isolated runner) when comparing across changes; otherwise treat numbers as directional.

## Coverage Strategy

- Use `cargo llvm-cov` (or `tarpaulin`) to measure coverage; check trends rather than chasing absolute percentages.
- Unit-test domain and pure logic; integration-test wiring and boundaries.
- Add a regression test for every production bug fix before shipping the fix.
- Prefer risk-based coverage (critical paths, error handling, concurrency) over raw line-percentage targets.

## Productivity and Workflow

- Use `cargo test -- --nocapture` to surface `println!`/`tracing` output while debugging a failing test.
- Use `cargo test some_test_name` (substring match) to scope runs during development; run the full suite before merge.
- Use `cargo nextest` for faster, more readable test runs on larger codebases.
- Extract reusable test builders (`fn make_user() -> User`) when fixture duplication becomes noisy.
- Keep assertion messages and failure output actionable; a failing test should tell the reader what broke without re-running with a debugger.
