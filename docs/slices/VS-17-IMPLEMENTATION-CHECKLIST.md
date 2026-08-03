# VS-17 — Production Readiness Implementation Checklist

## Planning and Product Owner gate

- [x] Register VS-17 without reusing an existing slice identifier.
- [x] Confirm VS-17 is the final MVP closure slice and not a new feature bundle.
- [x] Confirm production deployment remains separate from planning approval, merge approval and runtime approval.
- [x] Record explicit exclusions for redesign, speculative infrastructure and unapproved provider enablement.
- [ ] Product Owner confirms the exact release-scope slices and deferred capabilities.
- [ ] Product Owner approves pilot traffic, latency, error-rate and concurrency thresholds.
- [ ] Product Owner approves recovery-point and recovery-time expectations.
- [ ] Product Owner approves backup retention, restore ownership and evidence requirements.
- [ ] Product Owner approves release roles, change window, rollback authority and go/no-go participants.
- [ ] Product Owner approves the minimum monitoring, alerting and escalation set.
- [ ] Product Owner approves known-risk acceptance criteria and release blockers.
- [ ] Product Owner confirms which integrations and high-risk capabilities remain fail-closed at launch.
- [ ] Product Owner approves the post-deployment smoke tests and observation period.

Runtime certification must not invent thresholds, operational authority or production enablement decisions.

## Release scope and traceability

- [ ] Create the release-candidate record with exact commit SHA.
- [ ] Map every release-scope requirement to its slice, invariant owner, tests and operational evidence.
- [ ] Confirm all intended release-scope planning and runtime PRs are merged.
- [ ] List every explicitly deferred capability and customer/operator impact.
- [ ] Identify environment-specific behavior and launch flags.
- [ ] Confirm no unresolved critical policy decision prevents truthful testing.
- [ ] Record known risks, owner, mitigation, acceptance decision and expiry/review date.
- [ ] Invalidate certification automatically when the candidate SHA changes.

## Critical customer journeys

- [ ] Verify published package discovery with truthful price and availability.
- [ ] Verify package details, inclusions, exclusions, itinerary and occupancy pricing.
- [ ] Verify authoritative quote creation, expiry and stale-price handling.
- [ ] Verify inventory hold creation, expiry, release and concurrent oversell protection.
- [ ] Verify booking creation and immutable commercial snapshot.
- [ ] Verify payment initiation, authenticated callback handling and duplicate-event idempotency.
- [ ] Verify confirmation success and explicit confirmation-exception recovery.
- [ ] Verify My Journey confirmed, empty, delayed and unavailable states.
- [ ] Verify document upload/review/correction behavior where storage is enabled.
- [ ] Verify visa action-required, approved, rejected and unavailable states.
- [ ] Verify family party and Mahram snapshot behavior.
- [ ] Verify cancellation request, operator review and customer refund-status behavior.
- [ ] Verify customer support entry uses the booking reference without leaking internal data.
- [ ] Verify foreign-account resources return safe not-found behavior.

## Critical operator journeys

- [ ] Verify operator authentication, membership and permission boundaries.
- [ ] Verify package/departure authoring and validation.
- [ ] Verify pricing and inventory configuration with concurrency protection.
- [ ] Verify review and publish gates.
- [ ] Verify document review queue, case detail and governed decisions.
- [ ] Verify visa queue, lifecycle transitions and stale-version recovery.
- [ ] Verify operational support composition without direct cross-module state editing.
- [ ] Verify cancellation/refund review, rejection, approval and recovery behavior.
- [ ] Verify foreign-operator resources return safe not-found behavior.
- [ ] Verify permission-denied and recoverable-error states are accessible and actionable.

## Security and privacy

- [ ] Reverify authentication and session behavior for customer and operator surfaces.
- [ ] Reverify deny-by-default authorization for privileged routes.
- [ ] Reverify account ownership, operator membership, permission and operator-scope enforcement.
- [ ] Verify webhook/provider callback authentication where integrations are enabled.
- [ ] Run repository-history and release-artifact secret scanning.
- [ ] Review production-like configuration without printing secret values.
- [ ] Verify no access tokens, provider secrets or credentials appear in logs or artifacts.
- [ ] Verify no traveller, passport, document, visa or payment payloads appear in ordinary telemetry.
- [ ] Verify safe errors do not reveal resource existence across account/operator boundaries.
- [ ] Verify document access remains short-lived, authorized and private where enabled.
- [ ] Verify privileged production access is MFA-protected where supported.
- [ ] Use synthetic or explicitly approved test data only.
- [ ] Complete threat-model review for release-specific changes.
- [ ] Record and resolve all critical/high findings or obtain explicit Product Owner risk acceptance.

## Database and migration verification

- [ ] Validate the persistence registry includes every module DbContext.
- [ ] Apply all registered migrations to an empty database.
- [ ] Apply all registered migrations from the approved prior-release baseline.
- [ ] Prove zero pending model changes for every registered context.
- [ ] Verify required uniqueness, foreign-key, check and concurrency constraints.
- [ ] Verify migration failure produces safe startup/readiness behavior.
- [ ] Verify no release-hardening change introduces cross-module table ownership.
- [ ] Capture exact migration commands, timestamps and results as release evidence.
- [ ] Verify migrations do not erase or rewrite append-only booking, payment, audit or refund history.

## Backup, restore and data integrity

- [ ] Create a backup in the approved production-like environment.
- [ ] Record backup timestamp, owner, retention and storage location.
- [ ] Restore the backup into an isolated verification environment.
- [ ] Start the application successfully against the restored database.
- [ ] Verify representative booking and commercial snapshot integrity.
- [ ] Verify payment and append-only financial/refund integrity.
- [ ] Verify inventory hold, commitment and release integrity.
- [ ] Verify document metadata and retention/legal-hold integrity where applicable.
- [ ] Verify visa history and transition audit integrity.
- [ ] Verify family/Mahram snapshot integrity.
- [ ] Verify cancellation/refund and audit history integrity.
- [ ] Measure restore duration against the approved recovery expectation.
- [ ] Document failed-restore diagnosis and escalation behavior.

## Configuration and launch flags

- [ ] Inventory all required environment variables and secret references.
- [ ] Classify each setting as required, optional, sensitive and environment-specific.
- [ ] Verify required missing settings fail closed or produce truthful readiness failure.
- [ ] Confirm production refund execution state explicitly.
- [ ] Confirm cancellation policy publication state explicitly.
- [ ] Confirm private document storage and malware-scanning state explicitly.
- [ ] Confirm payment, OCR, WhatsApp/email and other external-provider states explicitly.
- [ ] Confirm customer/operator/admin redirect and access configuration.
- [ ] Confirm no development or demo credential is present in production configuration.
- [ ] Capture a redacted configuration manifest for release evidence.

## Health, observability and alerting

- [ ] Verify liveness reports process health only.
- [ ] Verify readiness reports required dependency readiness truthfully.
- [ ] Verify normal, dependency-failed and recovered health behavior.
- [ ] Verify structured logs include correlation and opaque resource identifiers.
- [ ] Verify logs exclude sensitive payloads and secrets.
- [ ] Verify traces connect critical cross-module operations without exposing private data.
- [ ] Verify metrics exist for payment, confirmation, inventory, document, visa, cancellation/refund and migration failures.
- [ ] Define alert severity, threshold, owner and notification route.
- [ ] Exercise every release-blocking alert and verify receipt.
- [ ] Link every critical alert to a usable runbook.
- [ ] Verify dashboards can identify current release version and environment.
- [ ] Verify operational evidence is retained for the approved period.

## Performance and capacity

- [ ] Record the approved pilot workload and concurrency profile.
- [ ] Define latency percentile and error-rate thresholds.
- [ ] Test discovery and package-detail read paths.
- [ ] Test quote and inventory-hold concurrency.
- [ ] Prove load does not oversell inventory.
- [ ] Test booking and payment initiation idempotency under retry/load.
- [ ] Test My Journey reads.
- [ ] Test operator queues and representative case detail.
- [ ] Test document metadata, visa and cancellation/refund operations where enabled.
- [ ] Verify database connection and resource consumption remain within approved limits.
- [ ] Capture test inputs, environment, results and bottlenecks.
- [ ] Make only measured, approved performance changes.
- [ ] Rerun correctness, security and accessibility checks after performance changes.

## Accessibility and visual regression

- [ ] Identify critical customer and operator screens for release certification.
- [ ] Verify keyboard-only operation.
- [ ] Verify visible focus and logical focus order.
- [ ] Verify accessible names, landmarks and status announcements.
- [ ] Verify contrast and non-color status communication.
- [ ] Verify minimum target sizes.
- [ ] Verify 200% text scaling.
- [ ] Verify mobile reflow without clipped or overlapping content.
- [ ] Verify reduced-motion behavior.
- [ ] Run serious/critical automated accessibility checks.
- [ ] Run desktop Chromium and mobile WebKit rendered regression.
- [ ] Compare against the approved NoorPath landing/package visual authority.
- [ ] Confirm header, footer, tokens, typography, cards and controls remain consistent.
- [ ] Treat visual fixes as regression correction, not redesign.

## Deployment verification

- [ ] Confirm the manual deployment workflow requires exact approved SHA input.
- [ ] Confirm stale-main and mismatched-approval protections work.
- [ ] Confirm automatic per-slice production deployment remains disabled.
- [ ] Run preflight configuration and dependency checks.
- [ ] Verify migration readiness before application promotion.
- [ ] Deploy the exact certified artifact to the approved production-like environment.
- [ ] Verify liveness and readiness after deployment.
- [ ] Run the approved customer and operator smoke tests.
- [ ] Verify monitoring, traces and alerts identify the deployed version.
- [ ] Record deployment actor, timestamp, SHA, environment and outcome.
- [ ] Do not deploy to production without a separate explicit Product Owner approval.

## Rollback and failure recovery

- [ ] Define application rollback procedure and authority.
- [ ] Define configuration rollback procedure and authority.
- [ ] Define migration forward-fix, restore or rollback behavior explicitly.
- [ ] Exercise rollback in the approved production-like environment.
- [ ] Verify health and critical reads after rollback.
- [ ] Verify rollback does not erase booking, payment, inventory, document, visa, family or refund facts.
- [ ] Verify retries after rollback do not duplicate payment, inventory or refund effects.
- [ ] Record rollback duration and compare with the approved expectation.
- [ ] Capture rollback evidence and any manual step requiring future automation.

## Operational runbooks

- [ ] Deployment failure and rollback runbook.
- [ ] Database migration failure runbook.
- [ ] Database unavailable/degraded runbook.
- [ ] Authentication provider outage runbook.
- [ ] Payment callback delay/duplication runbook.
- [ ] Confirmation exception runbook.
- [ ] Inventory hold/commit/release inconsistency runbook.
- [ ] Document storage or malware scanner failure runbook where enabled.
- [ ] Visa processing exception runbook.
- [ ] Refund provider delay/failure and reconciliation runbook.
- [ ] Suspected secret or personal-data exposure runbook.
- [ ] Each runbook names owner, impact, evidence sources, safe actions, prohibited actions, escalation and recovery verification.
- [ ] No runbook uses direct database editing as the standard recovery path.
- [ ] Conduct a tabletop review of the highest-impact runbooks.

## Automated certification

- [ ] Run format, static analysis, unit and complete solution tests.
- [ ] Run architecture and module-boundary validation.
- [ ] Run persistence registry and all migration validation.
- [ ] Run secret scanning.
- [ ] Run authorization, account-isolation and operator-isolation tests.
- [ ] Run idempotency and concurrency suites for critical operations.
- [ ] Run financial reconciliation tests.
- [ ] Run critical Playwright journeys on desktop and mobile.
- [ ] Run accessibility and rendered regression checks.
- [ ] Run performance and capacity checks using approved thresholds.
- [ ] Generate one certification summary for the exact unchanged SHA.
- [ ] Upload machine-readable evidence and retain it for the approved period.
- [ ] Fail certification when any required evidence is missing, stale or belongs to another SHA.

## Release decision and controls

- [ ] Review all automated and manual evidence.
- [ ] Confirm no unresolved release-blocking issue or review thread.
- [ ] Review known risks and deferred capabilities.
- [ ] Confirm launch flags and disabled integrations.
- [ ] Confirm support ownership and escalation contacts.
- [ ] Confirm rollback authority and change-window availability.
- [ ] Product Owner approves the exact certified runtime SHA before merge.
- [ ] Merge without triggering production deployment.
- [ ] Revalidate the merged main SHA before any production action.
- [ ] Obtain separate explicit Product Owner approval for production deployment.
- [ ] Run post-deployment smoke tests and observation period before declaring stable.
- [ ] Record final go/no-go decision and release evidence.
