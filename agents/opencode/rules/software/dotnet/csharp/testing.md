# C# Testing Rules

## Scope

- Use this as the compact, canonical testing rule set for C#/.NET code.
- Prioritize reliability, fast feedback, clear intent, and maintainable coverage.
- Keep tests aligned to architectural boundaries and real failure risks.

## Core Test Principles

- Structure tests with AAA (`Arrange`, `Act`, `Assert`).
- Keep tests deterministic, isolated, fast, and focused on one behavior.
- Use descriptive but not too long names (for example `Method_State_ExpectedResult`).
- Separate unit and integration tests into distinct projects/suites.
- Favor behavior verification over implementation-detail coupling.

## Unit Testing

- Use `xUnit` `[Fact]` for single-scenario tests and `[Theory]` for data-driven scenarios.
- Prefer `[Theory]` with inline or external data for multiple input variations; avoid redundant test methods.
- Use `InlineData`/`MemberData`/`ClassData` based on data complexity and reuse needs.
- Test async code with `async/await`; avoid blocking calls (`.Result`, `.Wait()`).
- Mock external dependencies (repositories, gateways, wrappers) to isolate business logic.
- Use realistic test data (for example via AutoFixture) to reduce brittle hand-crafted fixtures.
- Verify domain outcomes and expected exceptions for invalid paths.

## Integration Testing

- Use `WebApplicationFactory<TEntryPoint>` for end-to-end HTTP pipeline testing in memory.
- Replace selected production services with test doubles via `ConfigureTestServices` when needed.
- Test authentication/authorization paths with explicit test auth handlers and redirect settings.
- Handle antiforgery for form/POST flows when applicable.
- Manage shared fixture lifecycle with `IAsyncLifetime`.

## Infrastructure and External Dependencies

- Prefer production-like infrastructure in integration tests (for example Testcontainers over in-memory substitutes).
- Inject dynamic runtime settings (ports/connection strings) during host setup; avoid hardcoded values.
- Share expensive infrastructure with collection fixtures when safe.
- Use HTTP stubs (for example WireMock.Net) for unstable or hard-to-control third-party services.

## Performance, Parallelism, and Stability

- Enable parallel execution where tests are isolated and thread-safe.
- Prevent cross-test pollution with explicit cleanup/reset strategies.
- Minimize shared mutable state; isolate database state per test or fixture scope.
- Keep flaky test budget at zero: quarantine/fix immediately, do not normalize flakes.

## Coverage Strategy

- Unit tests should cover Domain and Application behavior.
- Integration tests should cover Infrastructure wiring and boundary behavior.
- Add regression tests for every production bug fix.
- Prefer risk-based coverage over raw percentage goals.

## Productivity and Workflow

- Use scoped test runs (playlists/sessions/filters) during development; run full suite before merge.
- Extract reusable test builders/fixtures when duplication becomes costly.
- Use IDE templates/snippets for repetitive test scaffolding.
- Keep assertion messages and failure output actionable.
