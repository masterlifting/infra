# SQL/Database Testing

Scope: tests for migrations, repositories, raw SQL, and data-access behavior.

## Migration testing

- Test every migration in both directions when a down/rollback path exists; if rollback is impossible, state it explicitly and test the forward path on a copy of representative data.
- Verify migrations are idempotent or guarded (re-running must not fail or duplicate data).
- Test migrations against a schema state matching the currently deployed version, not just an empty database — rolling deploys run old code against the new schema.
- For data migrations, assert row counts and spot-check transformed values, not just successful execution.

## Integration testing

- Prefer tests against a real PostgreSQL instance (local or containerized) over in-memory fakes for repository and raw-SQL behavior; in-memory providers hide dialect, transaction, and constraint differences.
- Reset database state between tests (transaction rollback or truncation); tests must not depend on execution order.
- Use minimal, explicit fixtures over broad shared seed data.

## What to cover

- Constraint behavior: unique violations, foreign keys, null handling, and the error paths they trigger in application code.
- Transaction boundaries: partial-failure rollback, and behavior under concurrent access for read-modify-write flows.
- Query correctness on empty tables, single rows, and multi-row sets (set-based operations, pagination edges).
- Parameterization: verify inputs are parameterized, never interpolated (also a security check).

## Output discipline

- Report pass/fail plus relevant error lines only; never paste full test logs.
