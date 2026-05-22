# DevOps Engineering Rules

## Scope

- Canonical rule set for CI/CD, container, deployment, and infrastructure work; prioritize reproducibility, safety, observability, and fast recovery.
- Prefer declarative, version-controlled configuration over imperative one-off changes.
- Treat pipelines and infrastructure as code: reviewed, tested, and rolled back like application code.
- Optimize for short feedback loops (fast builds, fast tests, fast rollback) over clever one-off optimizations.

## Source Control and Branching

- Protect long-lived branches (`main`/`master`, release branches) with required reviews and required green pipelines.
- Prohibit direct pushes to protected branches; require MR/PR for all changes.
- Keep branches short-lived; rebase or merge frequently to reduce drift.
- Use a documented branching model (trunk-based, GitFlow, or environment branches); apply it consistently.
- Sign tags and releases when supply-chain integrity matters.
- Never rewrite history on shared branches; force-push only on personal feature branches.

## CI Pipelines

- Every commit on every branch must trigger build + unit tests; failing tests block merge.
- Treat warnings as errors in CI even when local builds tolerate them.
- Run linters, formatters, and static analysis in CI; do not rely on developer discipline alone.
- Cache dependencies (package manager, build artifacts) keyed by lockfile hash.
- Make jobs idempotent and independent; do not assume order or shared mutable state between jobs.
- Fail fast: order quick checks (lint, format, unit) before slow ones (integration, e2e, image build).
- Pin tool versions (CI runner image, language SDK, formatter) so a passing pipeline today still passes tomorrow.
- Keep pipeline duration visible; investigate regressions in build time as bugs, not background noise.

## Build and Artifacts

- One build per commit; promote the same artifact through environments rather than rebuilding per stage.
- Tag artifacts with immutable identifiers (commit SHA, semantic version, build number) — never just `latest`.
- Store build outputs in a versioned artifact registry; do not rebuild from source for deploys.
- Embed build metadata (commit SHA, build time, version) into the artifact so a running instance can identify itself.
- Generate and publish an SBOM for production artifacts.
- Verify reproducibility: same inputs produce the same artifact bit-for-bit where feasible.

## Containers and Images

- Use minimal base images (distroless, alpine, scratch) where the runtime allows.
- Pin base images by digest (`sha256:...`) for production; pin by tag minimum for everything else.
- Run containers as a non-root user; drop unneeded Linux capabilities.
- Use multi-stage builds to keep build tooling out of the final image.
- Add a `HEALTHCHECK` or equivalent so orchestrators can detect liveness/readiness.
- Mount configuration and secrets at runtime; never bake secrets into image layers.
- Set explicit resource requests and limits (CPU, memory) on every container in production.
- Run vulnerability scans (Trivy, Grype, Snyk) on every image build; gate promotion on severity thresholds.
- Use `.dockerignore` to exclude `.git`, build caches, secrets, and local artifacts from the build context.

## Configuration and Secrets

- Externalize configuration; do not hardcode environment-specific values in source.
- Treat configuration as code: version it, review it, test it.
- Never commit secrets to source control; scan with `gitleaks`/`trufflehog` in CI.
- Store secrets in a dedicated secret manager (Vault, AWS Secrets Manager, GCP Secret Manager, Kubernetes Secrets sealed at rest).
- Rotate secrets on a schedule and on personnel/role changes; rotation must be a tested procedure, not a fire drill.
- Inject secrets via environment variables, mounted files, or sidecar — never log them, never print them in error messages.
- Use distinct credentials per environment; a leaked test credential must not grant prod access.
- Mask known-secret patterns in CI logs even when the value is expected to be safe.

## GitOps and Declarative Operations

- Define desired state declaratively (manifests, Helm values, Terraform); do not encode imperative steps.
- Treat a git repository as the single source of truth for cluster and infrastructure state.
- Store desired state in versioned, immutable storage (signed tags or commit SHAs); never let a human apply a one-off cluster change that git doesn't know about.
- Prefer pull-based reconciliation (ArgoCD, Flux) over push-based deploys when the target is a long-lived cluster — narrower attack surface, continuous drift correction.
- Separate the application source repo from the deployment/state repo so a deploy is a config change, not a code rebuild.
- Detect and remediate drift automatically; an out-of-band change should either be reconciled back or surfaced as an alert.
- Promote between environments by promoting the manifest reference (image digest, chart version), not by re-running the build.

## Deployment Strategies

- Use rolling, blue/green, or canary deploys to avoid full-fleet downtime.
- Make every deploy step idempotent and re-runnable.
- Define readiness probes that gate traffic; a pod is not "ready" until it can serve real requests.
- Define liveness probes that restart genuinely broken pods, not slow ones.
- Validate health post-deploy with smoke tests against the deployed environment before declaring success.
- Promote artifacts forward through environments (dev → test → staging → prod); never deploy untested code straight to prod.
- Gate prod deploys behind explicit human approval for any system with real users or money.
- Schedule risky deploys outside peak traffic windows and away from end-of-week when possible.

## Rollback and Recovery

- Every deploy must have a documented rollback path; if a rollback isn't possible, the deploy isn't ready.
- Auto-rollback on failed health checks, crash loops, or timeout during deploy.
- Keep N previous artifact versions retrievable for fast rollback; do not rely on rebuilding old commits.
- For destructive data changes (schema drops, data deletes), separate the deploy from the destruction by at least one release.
- Test rollback procedures regularly; an untested rollback is not a rollback plan.
- Prefer forward-fix only when rollback is genuinely impossible (already-migrated data, external side effects).

## Infrastructure as Code

- Define all infrastructure in code (Terraform, Pulumi, CloudFormation, Helm); no console-only changes in production.
- Store state remotely with locking; never share state files via Slack or local disk.
- Plan before apply: review the diff (`terraform plan`, `helm diff`) before any change.
- Pin provider/module versions; avoid `latest` floating dependencies in IaC.
- Use modules for repeated infra patterns; avoid copy-paste drift across environments.
- Keep environment differences in variable files, not in branching infra code.
- Treat IaC like application code: code review, CI validation, linting (`tflint`, `tfsec`, `checkov`).

## Observability

- Treat observability as three pillars: logs, metrics, and traces; ship all three from production services.
- Prefer OpenTelemetry SDKs and OTLP exporters for vendor-neutral instrumentation.
- Emit structured logs (JSON) with consistent field names (`timestamp`, `level`, `service`, `trace_id`, `span_id`).
- Avoid logging secrets, PII, or full request bodies by default; redact at the source.
- Export metrics for the four golden signals: latency, traffic, errors, saturation.
- Use distributed tracing across service boundaries; propagate W3C trace context through every hop.
- Tag telemetry with environment, service, version, and instance identifiers so dashboards can slice consistently.
- Alert on symptoms users feel (error rate, p99 latency), not just on causes (CPU, memory).
- Keep alert volume tractable; every alert must be actionable, page-worthy, and have a runbook link.
- Retain logs and metrics long enough to diagnose incidents (typically 30 days hot, longer cold for compliance).

## Reliability and SLOs

- Define explicit SLOs (availability, latency) and burn-rate alerts before adding more features.
- Apply timeouts, retries with backoff and jitter, and circuit breakers to every external call.
- Make retries safe via idempotency keys; never retry a non-idempotent operation blindly.
- Design for partial failure: degraded service beats total outage.
- Set sensible resource limits to prevent one workload from starving its neighbors.
- Practice failure: chaos drills, game days, and dependency-loss simulations — not just postmortems.

## Security

- Apply least privilege everywhere: service accounts, IAM roles, network policies, database grants.
- Use short-lived credentials (OIDC, IAM role assumption, workload identity) over long-lived static keys.
- Patch base images, runtimes, and dependencies on a defined cadence; do not let critical CVEs linger.
- Shift security left: run SCA (Software Composition Analysis) before SAST/DAST in the pipeline so vulnerable libraries are caught at the earliest stage.
- Run SAST on every commit, DAST against a deployed staging environment, and IaC scanning (`tfsec`, `checkov`, `kube-linter`) on infra changes.
- Scan dependencies for known vulnerabilities in CI (`dependabot`, `renovate`, `osv-scanner`); gate merge on severity thresholds.
- Restrict outbound network egress from production workloads to known destinations.
- Audit access: log who did what, where, and when; review audit logs periodically.
- Threat-model new components touching authentication, payments, or PII before they ship.

## Dependencies and Supply Chain

- Pin all dependencies (application packages, container images, CI actions/runners) to specific versions or digests.
- Use lockfiles (`package-lock.json`, `go.sum`, `Cargo.lock`, `requirements.txt` with hashes) and commit them.
- Automate dependency updates with PR-based bots; review the diff before merging.
- Verify package signatures and checksums where the ecosystem supports it.
- Prefer first-party / well-maintained packages; treat abandoned dependencies as technical debt.
- Mirror critical third-party artifacts in an internal registry to survive upstream outages.

## Environments

- Maintain at least three environments: development, staging/pre-prod, production.
- Staging must mirror production topology (same versions, same scaling shape) as closely as cost allows.
- Use realistic, scrubbed data in non-prod; never copy raw production PII into dev/test.
- Document environment-specific URLs, credentials sources, and on-call ownership in one place.
- Never test destructive operations directly in production; rehearse in staging first.

## Release Hygiene and Versioning

- Use semantic versioning (or a documented alternative) for shipped artifacts; communicate breaking changes explicitly.
- Maintain a changelog generated from commits or PRs; humans, not just machines, should be able to read it.
- Tag every released artifact in source control; the tag and the artifact must agree.
- Separate deployment from release: ship code dark, then enable via feature flag / config.
- Use feature flags for risky changes; remove flags promptly once the change is stable.

## Documentation and Runbooks

- Document every production system: what it does, who owns it, where logs/metrics live, how to deploy/rollback.
- Keep runbooks next to the code they describe (not on a separate wiki that drifts).
- Every alert that pages a human must link to a runbook with concrete remediation steps.
- Post-incident: write a blameless postmortem with timeline, contributing factors, and concrete action items with owners.
- Track postmortem actions to completion; an action item without a deadline is a wish.

## Measurement and Delivery Performance

- Track the DORA metrics: deployment frequency, lead time for changes, change failure rate, and time to restore service.
- Include reliability/availability (SLO attainment) as the fifth DORA signal alongside the original four.
- Analyze the metrics together; high deployment frequency with high change failure rate is not improvement.
- Automate metric collection from CI/CD and incident systems; do not rely on hand-maintained spreadsheets.
- Use the metrics to guide investment (where is lead time slow? where do failures cluster?), not to grade individuals.
- Establish a baseline before changing process; measure the delta after.

## Cost and Resource Awareness

- Treat cloud cost as an engineering signal: surface per-service and per-environment cost in the same dashboards as latency and errors.
- Set resource requests close to observed usage; oversized requests waste capacity and undersized ones cause throttling/evictions.
- Auto-scale based on demand; do not provision for peak 24/7 unless the workload genuinely requires it.
- Clean up unused resources (old snapshots, idle load balancers, orphaned volumes) on a schedule; tag resources with an owner.
- Use spot/preemptible instances for fault-tolerant batch workloads.
- Tag every resource with environment, owner, and cost-center so spend can be attributed.

## Workflow and Tooling

- Automate any task done more than twice; manual repetition is a defect.
- Prefer well-known tools over bespoke scripts; bespoke is appropriate when the gap is real, not for novelty.
- Keep developer tooling reproducible: documented setup, scripted bootstrap, ideally containerized dev env.
- Run the same checks locally that CI runs; surprise CI failures erode trust in the pipeline.
- Make the right thing the easy thing: defaults should land developers in the safe, conventional path.