# .NET Testing Rules

Load this file when adding or reviewing .NET tests.

## Philosophy

- Test behavior, not implementation.
- Unit tests should be fast, isolated, and deterministic.
- Integration tests may use real databases, caches, queues, web app fixtures, or test containers when needed.
- New public service behavior needs tests.

## Unit Tests

- Use xUnit.
- Use NSubstitute for mocks.
- Use AutoBogus/AutoFixture-style generators for test data where available.
- Use FluentAssertions when the repo already uses it.
- Follow Arrange-Act-Assert.
- One scenario per test.
- Test happy paths, edge cases, validation failures, and exception paths.
- Do not test private methods directly, simple property getters/setters, framework behavior, or generated code.

## Naming

- Use `MethodName_Scenario_ExpectedResult` or the repo's existing close variant.
- Keep names specific enough to explain the behavior under test.

## Integration Tests

- Use the repo's fixture collections and initialization conventions.
- Keep test data isolated.
- Clean up state or use separate isolated data where required.
- Prefer integration tests for repositories, complex SQL, database constraints, triggers, and endpoint workflows.
- Be explicit when tests require local Kubernetes, Docker/Podman, VPN, or other external dependencies.

## Running Tests

- Run focused unit tests first for fast feedback.
- Run broader `dotnet test` or integration tests when the change touches workflows, data access, or shared behavior.
- Report command, result, failed test name, relevant assertion, and short stack excerpt only.

## Review Checks

- Missing tests for new public behavior.
- Tests coupled to implementation details.
- Shared mutable test state.
- Real network/DB calls in unit tests.
- Weak assertions that only check non-null or no exception.
- Integration tests without isolation or cleanup.
