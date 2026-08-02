# VS-14 — Implementation Checklist

## 1. Contract and dependency audit

- [ ] Confirm `main` contains certified VS-10, VS-12 and VS-13 capabilities required by the slice.
- [ ] Inventory current Booking, Payments, Documents and Visa support projections and governed commands.
- [ ] Resolve gaps without direct cross-module persistence access.
- [ ] Define the initial exception catalogue, severity and ageing rules.
- [ ] Define explicit operational-support permissions and operator scope.
- [ ] Review the action catalogue with the Product Owner before implementation.

## 2. UX and interaction design

- [ ] Produce an exception-first queue using the existing NoorPath operator shell and design tokens.
- [ ] Define search by booking reference and the minimum approved filters.
- [ ] Define case-detail composition and module ownership labels.
- [ ] Define guarded action dialogs with reason, impact and version feedback.
- [ ] Cover loading, empty, no-results, permission-denied, not-found, partial-data, stale, failed and resolved states.
- [ ] Verify desktop, tablet and mobile reflow, keyboard navigation, focus management and reduced motion.

## 3. Application and domain contracts

- [ ] Add an Operational Support module only if the queue, assignment and activity timeline require owned persistence.
- [ ] Add read-only support projections from each participating module.
- [ ] Add explicit command adapters that invoke owning-module application services.
- [ ] Reject generic status setters and arbitrary patch commands.
- [ ] Preserve owning-module state machines, invariants and audit histories.
- [ ] Define idempotency where an approved action can be retried.

## 4. Data and migrations

- [ ] Define whether exception projections are computed or persisted.
- [ ] Persist only Operational Support-owned data such as assignment and support activity.
- [ ] Add optimistic concurrency for mutable support-owned records.
- [ ] Add deterministic migration, designer, snapshot and migration-registry entry if persistence changes.
- [ ] Ensure indexes support queue ordering, operator scope, status and booking-reference lookup.

## 5. API and authorization

- [ ] Add operator-scoped queue and case-detail APIs.
- [ ] Add explicit approved-action endpoints with version and reason.
- [ ] Require active operator membership and operational-support permission.
- [ ] Return safe not-found responses for foreign operator resources.
- [ ] Prevent Platform Administrator from implicitly bypassing operator scope.
- [ ] Keep payment, document and visa sensitive fields out of support DTOs.

## 6. Audit, observability and privacy

- [ ] Audit every attempted support action and outcome.
- [ ] Record target module, safe action name, actor, operator, timestamp, reason and correlation identifier.
- [ ] Avoid duplicating authoritative domain history.
- [ ] Add safe telemetry for queue latency, exception age/count, command outcome, stale conflict and projection failure.
- [ ] Verify logs exclude identity documents, payment credentials, provider payloads and visa references.

## 7. Testing

- [ ] Unit-test exception classification, prioritization and action availability.
- [ ] Integration-test cross-module projections and governed command dispatch.
- [ ] Verify operator isolation and permission denial.
- [ ] Verify stale concurrency rejection and idempotent retry behavior where applicable.
- [ ] Verify audit writes for success and failure.
- [ ] Add Playwright coverage in `apps/web/e2e/operational-support.spec.ts`.
- [ ] Cover accessibility, responsive layout and recoverable failures.
- [ ] Run format, static analysis, architecture, migration, security and full regression gates.

## 8. Delivery and acceptance

- [ ] Register affected modules and routes in delivery metadata.
- [ ] Keep implementation PR Draft until exact-head certification passes.
- [ ] Attach rendered evidence for queue, case detail, guarded action and error states.
- [ ] Obtain exact-head Product Owner acceptance with `po-approved`.
- [ ] Merge only the unchanged certified SHA.
- [ ] Do not deploy production as part of the merge unless separately requested and approved.
