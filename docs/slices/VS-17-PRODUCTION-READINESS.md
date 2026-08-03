# VS-17 — Production Readiness

## Outcome

The complete NoorPath MVP is demonstrably safe, recoverable, observable and operable for a controlled production release.

VS-17 does not introduce a new customer feature. It converts the completed release-scope slices into one exact, evidence-backed release candidate and proves that NoorPath can be deployed, monitored, supported and rolled back without bypassing domain ownership or placing customer, traveller, document or payment data at unreasonable risk.

Production deployment is not part of planning approval or merge approval. Deployment remains a separate explicit Product Owner action for the exact certified commit.

## Product boundary

VS-17 closes the MVP release. It verifies the integrated behavior of VS-00 through VS-16 and addresses only gaps required to make the approved pilot safe and operable.

The slice may add tests, health behavior, telemetry, alerts, runbooks, configuration validation, backup/restore evidence, release automation hardening and narrowly scoped fixes discovered during certification.

It must not become a vehicle for unrelated feature development, architecture replacement, speculative scaling or unapproved provider enablement.

## Release candidate and evidence rules

A release candidate is one exact commit SHA with:

- an explicit list of included slices and known exclusions;
- all required automated checks complete;
- production-like configuration validated without exposing secrets;
- critical customer and operator journeys verified;
- migration, backup, restore, deployment and rollback evidence captured;
- accessibility, visual, security and performance evidence captured;
- open risks and accepted limitations recorded;
- Product Owner approval for the exact unchanged SHA.

Any subsequent commit invalidates exact-head certification and requires the affected evidence to be rerun.

Evidence must identify:

- exact commit SHA;
- environment and timestamp;
- command or workflow used;
- result and artifact location;
- operator or automated actor;
- relevant thresholds;
- unresolved risk or limitation.

Screenshots alone are not sufficient where machine-readable test, log, migration or workflow evidence is available.

## Release-scope journeys

### Customer commercial journey

Verify the critical path from published package discovery through package details, authoritative quote, inventory hold, booking creation, payment handling and confirmation.

Required assertions include:

- published facts and prices are authoritative;
- inventory cannot oversell under concurrent attempts;
- retry-sensitive commands remain idempotent;
- payment callbacks are authenticated and do not duplicate financial effects;
- confirmation either completes safely or enters an explicit recoverable exception;
- customer-visible states remain truthful when dependencies fail.

### Customer journey management

Verify authenticated My Journey behavior for:

- confirmed booking summary;
- payment and instalment state;
- traveller and family/Mahram projection;
- document readiness and correction;
- visa status and action-required states;
- cancellation policy, request and refund status;
- support access using the booking reference.

Foreign-account resources must return safe not-found behavior. Internal notes, provider payloads, document data and privileged operational details must not leak to the customer projection.

### Operator journey

Verify authorized operator flows for:

- package and departure authoring;
- pricing and inventory configuration;
- review and publish;
- document review;
- visa processing;
- operational exception handling;
- cancellation and refund review/recovery.

Permission denial, foreign-operator isolation, stale versions and recoverable failures must be verified. Operational surfaces may compose state but must invoke module-owned commands rather than mutate another module's persistence directly.

## Security and privacy verification

The release candidate must reverify:

- authentication and session behavior;
- account ownership enforcement;
- operator membership, permission and operator-scope enforcement;
- deny-by-default behavior for privileged routes;
- webhook or provider callback authentication where enabled;
- secret scanning across repository history and release configuration;
- absence of credentials, access tokens and provider secrets from artifacts and logs;
- absence of traveller, passport, document, visa and payment payloads from ordinary telemetry;
- bounded and authorized file access where document storage is enabled;
- safe error responses that do not reveal resource existence across security boundaries;
- MFA-protected privileged production access where supported by the hosting and identity providers.

Security verification must use synthetic or explicitly approved test data. Real customer passports, payment details or visa records are prohibited from certification fixtures.

## Database, migration and recovery verification

Every registered module database context must prove:

- deterministic forward-only migrations;
- successful migration from an empty database;
- successful migration from the approved prior release baseline;
- zero pending model changes;
- expected indexes, constraints and concurrency tokens;
- no cross-module ownership violation introduced by release hardening;
- safe startup behavior when a migration fails.

Backup and restore evidence must include:

- backup creation timestamp and environment;
- retention and ownership;
- restore into an isolated verification environment;
- application startup against the restored database;
- integrity checks for representative booking, payment, inventory, document, visa, family and cancellation/refund records;
- confirmation that append-only audit and financial history remain intact;
- measured recovery duration against the Product Owner-approved expectation.

A failed restore test is a release blocker until corrected or explicitly removed from the release boundary by the Product Owner.

## Deployment and rollback

Deployment must use the approved manual production workflow and exact certified SHA. The workflow must reject stale-main or mismatched approval input.

The release process must verify:

1. pre-deployment configuration and secret presence without printing secret values;
2. database connectivity and migration readiness;
3. deployment of the exact approved artifact;
4. liveness and readiness behavior;
5. critical smoke tests;
6. monitoring and alert reception;
7. release decision and evidence capture.

Rollback verification must demonstrate the approved recovery method for application code and configuration. Where database migrations are not safely reversible, the runbook must define forward-fix or restore behavior explicitly. Rollback must not silently erase bookings, payments, documents, visa cases, inventory facts, cancellation decisions or audit history.

No production deployment is authorized by merging VS-17. The Product Owner must separately approve deployment after reviewing the merged exact SHA and release evidence.

## Health, observability and alerting

Liveness must represent whether the process is running. Readiness must truthfully represent whether required dependencies allow the service to handle traffic safely.

Verification must cover normal, dependency-unavailable and recovered states for relevant hosts.

Logs, traces and metrics must allow an authorized operator to diagnose at least:

- failed or delayed payment processing;
- confirmation exceptions;
- inventory hold or commitment failures;
- document storage or malware-scanning failures where enabled;
- visa transition failures;
- cancellation and inventory-release failures;
- refund execution or reconciliation failures;
- migration and startup failures;
- authorization denials and stale-version conflicts without logging private payloads.

Alerts must have an owner, severity, threshold, notification route and linked runbook. An alert that has never been exercised or routed successfully does not count as release evidence.

## Performance and capacity

The Product Owner must approve a realistic pilot profile before performance certification, including expected concurrent customers, operator load and peak transaction rate.

At minimum, test:

- public discovery and package-detail reads;
- quote and inventory-hold concurrency;
- booking/payment initiation without duplicate effects;
- My Journey reads;
- operator queue reads;
- representative document metadata, visa and cancellation/refund operations where enabled.

Acceptance thresholds must include latency percentiles, error rate and concurrency expectations. Tests must also verify correctness under load: no overselling, duplicate payment/refund facts, broken optimistic concurrency or cross-account leakage.

Performance work must remain evidence-driven. VS-17 must not add Redis, queues, service decomposition or other infrastructure unless a measured release blocker requires it and the Product Owner approves the change.

## Accessibility and visual verification

Critical customer and operator screens must pass:

- keyboard-only operation;
- visible focus;
- semantic names and landmarks;
- target size;
- contrast;
- 200% text scaling;
- mobile reflow;
- reduced-motion behavior;
- serious and critical automated accessibility checks.

Rendered regression must compare release-scope screens against the approved NoorPath visual authority. Existing landing, package, header, footer, tokens, typography, cards and controls remain the source of truth. VS-17 may fix regressions but must not redesign completed journeys.

## Operational runbooks

Each critical failure mode requires a concise runbook containing:

- trigger and customer/operator impact;
- owner and escalation path;
- dashboards, logs or queries to inspect;
- safe diagnostic steps;
- approved recovery command or workflow;
- actions that are prohibited, especially direct database edits;
- verification that recovery completed;
- evidence and incident notes to retain.

Minimum runbook coverage:

- deployment failure and rollback;
- database migration failure;
- database unavailable or degraded;
- payment callback delay or duplication;
- confirmation exception;
- inventory hold/commit/release inconsistency;
- document storage or malware scanner failure where enabled;
- visa processing exception;
- refund provider delay/failure and reconciliation;
- authentication provider outage;
- suspected secret or personal-data exposure.

## Configuration and launch flags

The release configuration must explicitly record which optional or high-risk capabilities remain fail-closed at launch, including where applicable:

- production refund execution;
- private document storage and malware scanning;
- external payment, notification, OCR or other provider integrations;
- cancellation policy publication;
- environment-specific operator or admin access.

Defaults must remain safe. Missing required configuration must fail closed or produce truthful readiness failure rather than silently enabling unsafe behavior.

## Definition of Ready

Runtime work may begin when:

- all release-scope slice implementations intended for the pilot are merged;
- Product Owner confirms the release boundary and deferred capabilities;
- pilot performance thresholds are approved;
- backup/restore expectations are approved;
- release roles, change window and rollback authority are known;
- required production-like environment access is available;
- no unresolved critical product-policy decision blocks truthful testing.

## Definition of Done

VS-17 is complete only when:

- critical integrated journeys pass;
- exact-head CI and rendered review pass;
- security, privacy and authorization checks pass;
- all registered migrations and model checks pass;
- backup and restore are proven;
- deployment and rollback are proven in the approved non-production or production-like environment;
- health, monitoring and alert routing are verified;
- performance meets approved thresholds;
- accessibility and visual regression pass;
- runbooks are complete and usable;
- known risks and launch flags are approved;
- the Product Owner approves the exact certified SHA for merge.

## Explicit exclusions

- New unrelated product features.
- Partial redesign of established NoorPath journeys.
- Multi-region, Kubernetes, service decomposition or speculative platform replacement.
- Production tests using real customer, passport, visa or payment data.
- Direct database edits as an operational recovery mechanism.
- Unapproved enablement of production refunds or external providers.
- Automatic production deployment on merge.
- Long-term enterprise SRE capabilities beyond the approved pilot boundary.

## Merge and deployment rule

The VS-17 planning PR remains Draft until the Product Owner approves the release boundary, required evidence, thresholds, roles and exclusions.

Runtime implementation must use a separate Draft PR. It remains unmerged until exact-head CI, migration, backup/restore, security, privacy, performance, accessibility, rendered regression, deployment/rollback and Product Owner gates pass.

After merge, production deployment still requires a separate explicit Product Owner approval for the merged exact SHA. No workflow or agent may infer deployment approval from planning approval, runtime approval or merge approval.
