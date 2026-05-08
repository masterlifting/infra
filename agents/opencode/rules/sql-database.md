# SQL And Migration Rules

Load this file when editing or reviewing migrations, repositories, raw SQL, or data access code.

## Migrations

- Migrations must be zero-downtime safe for rolling deploys.
- Never delete or rename columns in a single release; use a two-phase migration.
- Never delete or rename tables without explicit coordination.
- Avoid heavy `UPDATE` operations that lock large tables; use batched/background work.
- Every `INSERT` must specify column names explicitly.
- For PostgreSQL, create online indexes with `CREATE INDEX CONCURRENTLY` via the project's migration prepare mechanism where applicable.
- New columns should have safe defaults or nullable rollout semantics.
- Preserve data during transformations.
- Store enums as strings, not integers.
- Store timestamps as UTC `timestamp without time zone`.

## SQL Safety

- Use parameterized queries for all values (`@Parameter` syntax).
- Do not interpolate user input into SQL strings.
- Interpolating table or column constants from migrations is acceptable.
- Avoid `SELECT *` in production code.
- Watch for `= NULL`; use `IS NULL`.
- Use `ON CONFLICT` or uniqueness checks for idempotent inserts.

## Performance

- Flag N+1 query patterns, especially queries inside loops.
- Prefer set-based operations, joins, CTEs, and batch inserts/updates.
- Check that filtered columns have appropriate indexes.
- Use pagination or limits for large result sets.
- Heavy read operations should use the project’s approved scale-out/read strategy where one exists.

## Repository Pattern

- Prefer `IDbMapper` for normal data access.
- Use raw connection access only for justified edge cases.
- Keep transactions short and never include external HTTP calls inside a transaction.
- Use the project’s transaction helper for multi-step DB mutations.
- Return domain entities or DTOs, not raw accidental database models.

## Review Checks

- SQL injection risks.
- Unsafe migration operations.
- Missing indexes for new filters/joins.
- Missing transaction boundaries.
- Non-idempotent writes or migrations.
- Business logic leaking into repositories.
