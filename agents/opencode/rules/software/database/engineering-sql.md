# SQL Engineering Rules

## Scope

- Canonical rule set for SQL, schema design, migrations, and data-access code; prioritize correctness, durability, predictable performance, and safe evolution.
- Prefer set-based, declarative SQL over imperative row-by-row processing.
- Treat the schema as a contract: changes must be safe under concurrent traffic and rolling deploys.
- Optimize for read/write workloads with measurement (`EXPLAIN ANALYZE`, slow query logs), not guesswork.

## Schema Design

- Start at Third Normal Form (3NF); denormalize only when measured read patterns justify it.
- Use `snake_case` for table, column, index, and constraint names; pluralize tables consistently (`users`, `orders`) or singularize consistently — pick one and hold the line.
- Name foreign key columns after the referenced table (`user_id`, `order_id`); name junction tables after both sides (`users_roles`).
- Keep one logical concept per table; resist wide tables that mix unrelated lifecycles.
- Name indexes and constraints explicitly (`ix_orders_user_id_created_at`, `uq_users_email`, `fk_orders_user_id`) so migrations and error messages stay readable.
- Separate hot transactional tables from archival/append-only tables; do not let an analytics table grow inside an OLTP schema unbounded.

## Data Types

- Choose the narrowest type that fits the domain; `BIGINT` over `INT` only when the table will exceed 2 billion rows.
- Store enums as strings (or as native ENUM types where the platform supports safe evolution), not as opaque integers.
- Store timestamps in UTC; use `timestamp with time zone` (PostgreSQL `timestamptz`) when you need wall-clock semantics, `timestamp without time zone` when storing already-normalized UTC instants — pick one policy per database and document it.
- Use `DATE` for calendar dates without a time component; do not encode dates as strings or integers.
- Use `NUMERIC`/`DECIMAL` for money and any value where rounding errors matter; never use `FLOAT`/`DOUBLE` for currency.
- Prefer `TEXT` over `VARCHAR(n)` in PostgreSQL unless a length constraint is genuinely required; in MySQL/SQL Server, choose lengths deliberately.
- Use native UUID, JSON, ARRAY, and INET types where supported instead of overloading `TEXT`.
- Use `BYTEA`/`VARBINARY` for binary blobs; do not base64-encode into text columns.

## Primary Keys and Identifiers

- Use a surrogate primary key (`BIGSERIAL`/`IDENTITY`/UUID) by default; promote natural keys to `UNIQUE` constraints, not to primary keys.
- Use `BIGSERIAL` / `IDENTITY` for internal-only tables where sequential IDs are acceptable and index size matters.
- Use UUID (preferably UUIDv7 or ULID for index locality) when IDs are public-facing, generated client-side, or must be globally unique across services.
- Avoid composite primary keys unless the natural composite is the identity (junction tables); add a surrogate key when downstream joins would be awkward.
- Never reuse IDs of deleted rows; soft-delete or archive instead.

## Constraints and Integrity

- Use `NOT NULL` for every column where absence is not a meaningful domain state.
- Add `CHECK` constraints to enforce domain invariants (`amount >= 0`, `status IN (...)`); push validation into the database, not just the application.
- Add `UNIQUE` constraints for any column or column-set that must be unique; do not rely on application-level checks alone.
- Declare every foreign key explicitly; pick `ON DELETE` / `ON UPDATE` behavior deliberately (`RESTRICT`, `CASCADE`, `SET NULL`).
- Always index foreign key columns — without an index, `JOIN`s and `ON DELETE CASCADE` scans degrade to full scans.
- Use deferrable constraints (`DEFERRABLE INITIALLY DEFERRED`) only when a multi-row transaction genuinely needs cross-row consistency at commit.

## Indexing

- Index columns appearing in `WHERE`, `JOIN ... ON`, `ORDER BY`, and `GROUP BY` clauses on frequent queries.
- Match composite index column order to query predicate and `ORDER BY` order; a `(a, b)` index serves `WHERE a = ? AND b = ?` and `WHERE a = ?` but not `WHERE b = ?`.
- Match index direction to `ORDER BY` direction; mixed-direction sorts need explicit `(a ASC, b DESC)` index definitions.
- Use covering indexes (`INCLUDE` columns in PostgreSQL/SQL Server) to satisfy queries without table lookups when the column set is stable.
- Use partial indexes for sparse predicates: `CREATE INDEX ... WHERE deleted_at IS NULL` shrinks indexes used for soft-delete queries.
- Use expression indexes (`CREATE INDEX ... ON users (lower(email))`) when queries filter on a computed expression.
- Pick the right index type: B-tree by default; GIN/GIST for full-text, JSONB, and array containment; BRIN for very large append-only tables.
- Remove indexes that no query plan uses; every index pays a write tax on every `INSERT`/`UPDATE`/`DELETE`.
- Monitor index bloat and rebuild (`REINDEX CONCURRENTLY`) on a schedule for hot tables.

## Query Patterns and Style

- Use parameterized queries for every value; never interpolate user input into SQL strings.
- List columns explicitly in `INSERT` (`INSERT INTO t (col1, col2) VALUES (...)`); never rely on declaration order.
- Avoid `SELECT *` in application queries; select only the columns the caller needs.
- Use `IS NULL` / `IS NOT NULL` for null checks; `= NULL` is always false.
- Use `IS DISTINCT FROM` / `IS NOT DISTINCT FROM` for null-safe comparison.
- Prefer `EXISTS` over `IN (SELECT ...)` for correlated subqueries when the inner set may be large.
- Qualify column references with table aliases in any query touching more than one table.
- Format multi-table queries across multiple lines; one-line joins lose readability fast.

## Set-Based Operations

- Prefer joins, CTEs, and bulk `INSERT`/`UPDATE`/`DELETE` over per-row loops; the database is optimized for set logic.
- Use `INSERT ... ON CONFLICT (...) DO UPDATE` (Postgres) / `MERGE` (standard) / `INSERT ... ON DUPLICATE KEY UPDATE` (MySQL) for idempotent upserts.
- Use multi-row `INSERT` (`VALUES (...), (...), (...)`) or `COPY` (Postgres) for bulk inserts; do not loop single-row inserts in application code.
- Use `UPDATE ... FROM` / `UPDATE ... JOIN` for set-based updates rather than per-row updates in code.
- Materialize CTEs deliberately; in some engines (older PostgreSQL) a CTE is an optimization fence — use `WITH ... AS MATERIALIZED` / `NOT MATERIALIZED` to control.

## Pagination

- Use keyset (seek) pagination for large result sets: `WHERE (created_at, id) < (:cursor_created_at, :cursor_id) ORDER BY created_at DESC, id DESC LIMIT n`.
- Reserve `OFFSET` pagination for small datasets or admin tools; cost grows linearly with offset.
- Return the next-page cursor explicitly in the API response; do not let clients reconstruct it.
- Always include a tiebreaker column (typically the primary key) in `ORDER BY` to make ordering deterministic.
- Pair keyset pagination with a composite index matching the `ORDER BY` order.

## Performance

- Use `EXPLAIN ANALYZE` (or the engine equivalent) to inspect plans on representative data; a query fast on 100 rows may be catastrophic on 100M.
- Flag N+1 query patterns: any query inside an application-level loop is a candidate to rewrite with a `JOIN` or `IN (...)`.
- Set per-statement and per-transaction timeouts (`statement_timeout`, `idle_in_transaction_session_timeout`) so a runaway query cannot hold locks indefinitely.
- Apply `LIMIT` to any query that does not have a bounded result set by construction.
- Batch large `UPDATE`/`DELETE` operations in chunks of fixed size; a single 50M-row update will block writers and inflate WAL/redo.
- Route heavy read-only queries to replicas where the platform supports it; do not run analytics against the OLTP primary.
- Watch for implicit type casts in predicates (`WHERE id = '123'` against a `BIGINT`); they often prevent index use.

## Transactions and Isolation

- Use the lowest isolation level that satisfies the workload; the default (`Read Committed`) is fine for most application code.
- Use `Serializable` (or `Repeatable Read` with explicit checks) for financial and inventory operations where read-then-write races would corrupt state.
- Keep transactions short; do not perform external HTTP/RPC calls, file I/O, or long computations inside a transaction.
- Hold locks for the minimum scope necessary; acquire them in a consistent order across code paths to avoid deadlocks.
- Always handle serialization failures with retry logic on the application side when using stricter isolation.
- Open transactions only at the boundary that owns the unit of work; do not open nested transactions across multiple repositories.

## Concurrency Control

- Prefer optimistic concurrency (row version / `xmin` / `ROWVERSION` column) for hot rows with low contention; cheaper than locks under read-heavy workloads.
- Use pessimistic locking (`SELECT ... FOR UPDATE`, `FOR NO KEY UPDATE`) for hot-spot rows where contention is real and retries would cascade.
- Combine MVCC for general traffic with selective pessimistic locks on known hot rows.
- Use `SKIP LOCKED` to implement queue-like consumption patterns without blocking workers.
- Never read-compute-write across separate statements without a lock or version check; that pattern silently loses updates under concurrency.

## Idempotency

- Add a `UNIQUE` constraint on a request/idempotency key column for any state-mutating operation that may be retried; the database rejects duplicates without application logic.
- Use `INSERT ... ON CONFLICT DO NOTHING` (or `MERGE`) to make inserts safely retryable.
- Make `UPDATE` statements idempotent by including the prior state in the predicate (`WHERE status = 'pending'`) so a retry that arrives after success becomes a no-op.
- Persist idempotency keys with a TTL or archive policy; do not let the table grow unbounded.

## Outbox and Event Consistency

- When a domain change must produce an external event, write the event to an outbox table in the same transaction as the domain change.
- Have a separate dispatcher process read from the outbox and publish to the broker with at-least-once delivery.
- Mark outbox rows as published only after the broker acknowledges; design downstream consumers to deduplicate.
- Never write to the database and then publish an event in two sequential non-transactional steps — the partial-failure window guarantees inconsistency.
- Do not use a cache (Redis, etc.) as the system of record for state that must survive failure.

## Migrations: Zero-Downtime

- Treat migrations as production code: reviewed, version-controlled, tested against production-like data volumes.
- Use the expand/contract pattern for any change that is not purely additive: expand (add new schema), backfill, switch reads/writes, contract (remove old schema) — each in a separate deploy.
- Never delete or rename a column in the same release as code that no longer references it; old pods will be running until rollout completes.
- Never delete or rename a table without an explicit multi-release plan.
- Acquire a short lock-timeout on every migration statement so a stuck migration does not stall production.
- For long-running data migrations, run them out-of-band (background job, batched), not in the migration transaction.
- Test every migration against a snapshot of production data before shipping; a millisecond migration on a test DB can take hours in prod.

## Migration Safety

- Adding a `NOT NULL` constraint on a populated table: add the column as nullable with a default, backfill, then add `NOT NULL` via `ADD CONSTRAINT ... CHECK (col IS NOT NULL) NOT VALID` followed by `VALIDATE CONSTRAINT` to avoid a long exclusive lock (PostgreSQL).
- Create indexes online (`CREATE INDEX CONCURRENTLY` in PostgreSQL; `ONLINE` in MySQL/SQL Server) so writes are not blocked.
- New columns should be nullable with a default, or backfilled in batches; never run a full-table rewrite synchronously in the migration.
- Avoid `ALTER TABLE` operations that rewrite the table (changing column type when no implicit cast is safe); use expand/contract instead.
- For destructive changes, run a "shadow read" period where the new code reads from both old and new locations and logs divergence before contracting.
- Every `INSERT` in a migration must specify column names explicitly.

## SQL Safety

- Parameterize every value; never concatenate user input into SQL, including in dynamic queries.
- Restrict dynamic identifier interpolation (table/column names) to a whitelist; do not pass identifiers from user input.
- Use the database's prepared-statement or parameter-binding API, not string formatting in the application language.
- Review every raw SQL query against the OWASP injection checklist; SQL injection remains the most cited vulnerability class for a reason.
- Grant the application user only the privileges it needs; do not connect as a superuser or table owner from application code.
- Log SQL errors with enough context (query template, parameter shape) to diagnose without leaking values.

## Data Access Layer

- Hide raw SQL behind repository or query-service abstractions when the storage may evolve; expose intent (`getActiveOrdersForUser`), not raw SQL strings.
- Use a connection pool sized to the workload; never open per-request connections to the database.
- Keep one transaction per unit of work; do not interleave repository calls under different transactions in one request.
- Return domain entities or DTOs from repositories; do not leak ORM-mapped row types into application/domain code.
- Keep business logic out of repositories; repositories own persistence, services own decisions.
- Map between persistence and domain explicitly; deriving everything from auto-mappers obscures schema-to-domain divergence.

## Observability

- Log slow queries (`log_min_duration_statement` in PostgreSQL, slow query log in MySQL) and review the top offenders.
- Tag queries with the calling endpoint/handler in a SQL comment so DB-side traces can attribute load.
- Surface query duration, rows scanned, and locks acquired in application metrics for hot paths.
- Capture `pg_stat_statements` (or equivalent) and review weekly for new top-N consumers.
- Alert on transaction count, deadlock count, replication lag, connection saturation, and disk usage — not just CPU.

## Backup and Recovery

- Take physical backups + WAL/redo archives for point-in-time recovery; logical dumps alone are insufficient for production.
- Test restore procedures regularly; an untested backup is not a backup.
- Document the RPO (recovery point objective) and RTO (recovery time objective); choose backup cadence and retention to meet them.
- Encrypt backups at rest; control access to backup storage as carefully as the live database.
- Validate backups by spinning up a copy and running smoke queries; do not assume a successful dump implies a usable restore.

## Review Checklist

- All values parameterized; no string-built SQL.
- `INSERT` statements name columns explicitly.
- Foreign keys defined and indexed.
- New or changed predicates have supporting indexes.
- Migrations are expand/contract for any non-additive change.
- Indexes created `CONCURRENTLY` / `ONLINE`.
- State-mutating operations have an idempotency mechanism (`UNIQUE` on request key, `ON CONFLICT`, or version check).
- Financial writes use serializable isolation, optimistic concurrency, or atomic SQL — never read-then-write without a guard.
- DB writes that must emit external events use an outbox in the same transaction.
- No `SELECT *` in production paths.
- No external HTTP/RPC calls inside a transaction.
- Transaction scope aligns with one unit of work.
- Statement and transaction timeouts set on the connection.