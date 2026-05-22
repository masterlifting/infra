# Common Security Rules

## Threat Modeling and Scope

- Identify assets, trust boundaries, entry points, and abuse cases before implementation.
- Prioritize mitigations by impact and likelihood; fix high-risk paths first.
- Treat all external input as untrusted: HTTP, files, CLI args, env vars, queues, and third-party APIs.

## Secrets and Credentials

- Never hardcode secrets, tokens, private keys, or connection strings in source code.
- Load secrets from secure configuration providers; keep them out of logs, tests, and examples.
- Rotate compromised credentials immediately and invalidate old tokens.
- Use least-privilege credentials scoped to exact runtime needs.

## Authentication and Session Security

- Use proven authentication standards and libraries; avoid custom auth protocols.
- Enforce MFA for privileged/admin access where possible.
- Store passwords only as salted, adaptive hashes (Argon2id, bcrypt, or PBKDF2 with strong parameters).
- Use secure session settings: short TTLs, server-side revocation, secure/HttpOnly/SameSite cookies.

## Authorization

- Enforce authorization on every sensitive action, not only at UI level.
- Default deny; grant minimal permissions per role/action/resource.
- Validate ownership and tenant boundaries server-side for every read/write/delete.
- Keep authorization logic centralized and testable.

## Input Validation and Output Encoding

- Validate format, length, range, and allowed characters at trust boundaries.
- Use allow-lists and typed parsing over regex-only filtering.
- Encode output for the target context (HTML, URL, SQL, shell, JSON) to prevent injection.
- Reject or sanitize dangerous file paths (`..`, absolute paths, mixed separators) before file access.

## Data Protection

- Classify data sensitivity and apply minimum collection/retention principles.
- Encrypt sensitive data in transit (TLS 1.2+) and at rest using managed key systems.
- Avoid exposing PII/secrets in API responses, logs, telemetry, crash dumps, and error messages.
- Use deterministic masking/redaction for observability data.

## Secure Coding and Dependencies

- Prefer parameterized queries and ORM-safe APIs; never concatenate SQL.
- Avoid unsafe deserialization of untrusted payloads.
- Pin and regularly update dependencies; remove unused packages.
- Run SCA/SAST checks in CI and address critical findings before release.

## API and Network Security

- Enforce HTTPS only; redirect/deny plaintext transport.
- Apply rate limiting, request size limits, and timeouts on all public endpoints.
- Use strict CORS policies (explicit origins, methods, and headers).
- Verify webhook signatures and replay protection using timestamp/nonce.

## Logging, Monitoring, and Incident Readiness

- Log security-relevant events: auth failures, privilege changes, data export, and policy denials.
- Keep logs tamper-evident and access-controlled with retention policies.
- Alert on suspicious behavior patterns and repeated failed access attempts.
- Maintain an incident response playbook with roles, escalation, and recovery procedures.

## Operational Hardening

- Keep systems patched; automate security updates where safe.
- Run services with least privilege; isolate workloads and restrict network egress.
- Disable unused endpoints, ports, protocols, and default accounts.
- Back up critical data and validate restore procedures regularly.

## Verification and Release Gates

- Add security-focused tests for authz, boundary checks, injection, and failure paths.
- Use peer review checklists that include explicit security checks.
- Block release on unresolved high/critical vulnerabilities unless formally accepted with expiry.
- Reassess security posture after major architecture or dependency changes.
