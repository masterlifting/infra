# Security And Privacy Rules

Load this file when editing auth, request handlers, PII, logs, secret-store config, partner integrations, financial operations, or security-sensitive code.

## Authorization

- Every endpoint must have explicit authorization or intentional `[AllowAnonymous]`.
- Client, admin, internal, and public endpoints must use the project’s explicit authorization attributes or policies.
- Never check roles directly; use permissions.
- Verify resource ownership to prevent horizontal privilege escalation.

## Input Validation

- Validate all public `RequestHandler<IN, OUT>` input.
- Use `ContractValidationException` for client validation errors.
- Trim and length-check strings.
- Range-check numbers.
- Validate enum values, GUIDs/IDs, emails, and currency/amount fields where applicable.

## PII And GDPR

- Mark all PII fields with the project’s redaction/privacy attribute where available.
- Encrypt names, addresses, passport data, tax IDs, nationality, banking details, and IP addresses using the project-approved crypto service.
- Searchable PII such as emails/phones must use the project’s deterministic/searchable encryption convention if one exists.
- Hash passwords, OTP codes, and auth tokens with salt; never encrypt these as reversible secrets.
- Do not put PII in URLs, errors, task files, test snapshots, or logs.
- Implement anonymization/deletion workflows for GDPR right-to-erasure where required.

## Secrets

- Secrets live in the approved secret store under clearly owned paths.
- Never hardcode API keys, connection strings, certificates, passwords, or encryption keys.
- Config classes bind through the project’s typed configuration convention and safe defaults only.
- Do not expose secrets in CI logs, chat summaries, or generated artifacts.

## Logging And Audit

- Use `ILogger<T>`, not legacy logging abstractions unless the repo requires them.
- Use structured logs, never string-concatenated sensitive values.
- Do not log card numbers, CVV, PIN, passwords, tokens, API keys, emails, phone numbers, names, national IDs, or passport numbers.
- Audit authentication, authorization failures, password changes, permission changes, admin actions, and data exports.

## Financial Safety

- Financial operations must be idempotent.
- Prevent double spend with DB constraints, distributed locks, optimistic concurrency, or appropriate transaction isolation.
- Validate amounts, currencies, limits, and signs.
- Avoid fragile dual writes to durable state plus external side effects without explicit consistency handling.

## Review Checks

- Missing auth/ownership checks.
- PII without `[PersonalData]` or encryption.
- Incorrect searchable vs non-searchable PII encryption mode.
- SQL injection or user-controlled URL SSRF.
- Hardcoded secrets or unsafe defaults.
- PII in logs/errors/URLs.
