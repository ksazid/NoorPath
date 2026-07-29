# NoorPath MVP Vertical Slice Map

## Purpose
Turn the approved product, domain, architecture, security, UX, deployment, and quality baselines into an implementation sequence of narrow end-to-end product outcomes.

## Slice rule
A vertical slice is complete only when its user/business outcome works end-to-end and all applicable quality gates pass. A slice may cross UX, API, application, domain, persistence, security, observability, CI and tests, but must implement only the minimum capability required for its outcome.

Traceability for every slice:
Requirement -> invariant -> owner -> state transition -> UX -> security -> API/data -> tests -> telemetry -> deployment evidence -> PO acceptance.

## Standard slice loop
1. Specify — outcome, actors, rules, states, acceptance criteria and exclusions.
2. Design — approved NoorPath visual baseline, UX analysis, Figma, responsive/accessibility/error states, PO approval.
3. Contract — domain ownership, commands/queries/events, API DTOs, persistence and security boundaries.
4. Build — smallest end-to-end implementation; no unrelated infrastructure.
5. Verify — unit, integration, contract, migration, security, accessibility, E2E and visual tests as applicable.
6. Review — PO validates working outcome against requirement and approved design.
7. Close — CI green, evidence captured, documentation/ADRs updated where needed; only then dependants may proceed.

## Entry criteria for a slice
- Outcome and actor are explicit.
- Acceptance criteria are testable.
- Required business rules/invariants are resolved.
- Domain owner and state transitions are known.
- Dependencies are completed or deliberately mocked behind an approved contract.
- UX states needed by the slice are approved before UI implementation.
- Security/privacy implications are identified.
- Data/API/migration impact is understood.

## Exit / Definition of Done
- Acceptance criteria pass end-to-end.
- No known violation of domain ownership or architecture tests.
- Authentication/authorization/resource scope verified where applicable.
- Database migrations are deterministic and migration-state validation passes.
- Retry-sensitive operations are idempotent where required.
- Logs/traces/metrics provide enough evidence to diagnose the outcome.
- Applicable unit/integration/contract/E2E/accessibility/visual/security checks pass in CI.
- No secrets or sensitive document/payment data leak into logs/events/client payloads.
- Responsive and error/empty/loading/conflict states required by the slice are implemented.
- PO accepts the working outcome.
- Documentation and decisions changed by the slice are updated.

## Implementation order

### VS-00 Platform Foundation
Outcome: NoorPath has the minimum deployable engineering and design foundation required for product slices.
Includes: modular-monolith boundaries, API/web/worker hosts as required, PostgreSQL module/schema conventions, migration governance, configuration/secrets pattern, authentication foundation, authorization primitives, transactional Outbox foundation, structured observability, CI quality gates, environment configuration, design tokens/components foundation.
Excludes: Redis, service bus, Kubernetes, search cluster, multi-region, speculative provider abstractions.
Depends on: planning baseline only.
Exit emphasis: clean build/test/format/migration validation and deployable skeleton.

### VS-01 Operator Access
Outcome: an approved operator can authenticate and access only permitted administration capabilities.
Includes: identity integration, operator state/role/scope checks, protected admin shell, forbidden/unauthenticated UX, audit context.
Depends on: VS-00.

### VS-02 Package & Departure Authoring
Outcome: an authorized operator can create and save a valid draft Umrah package/departure with the essential Makkah, Madinah, travel, inclusion and journey facts.
Includes: draft lifecycle, validation, version/concurrency basis, authoring UX, persistence and audit trail.
Excludes: pricing/inventory publication, cloning, bulk upload, supplier sync.
Depends on: VS-01.

### VS-03 Pricing & Inventory
Outcome: an authorized operator can configure supported occupancy pricing and capacity/availability for a draft departure.
Includes: authoritative price facts, currency, inventory pool/capacity, validation and concurrency controls.
Excludes: child/infant pricing, promotions, waitlist, dynamic supplier inventory.
Depends on: VS-02.

### VS-04 Review & Publish
Outcome: an authorized operator can review a complete departure and publish it only when catalogue, pricing and inventory rules are satisfied.
Includes: validation summary, preview, publish transition, optimistic concurrency, audit and required integration events.
Depends on: VS-02, VS-03.

### VS-05 Customer Discovery
Outcome: a customer can browse currently published Umrah departures with truthful headline price and availability.
Includes: public query contract, package cards/list, loading/empty/error states, responsive/accessibility coverage.
Excludes: advanced search, recommendations, comparison engine.
Depends on: VS-04.

### VS-06 Package Details
Outcome: a customer can understand a published package sufficiently to decide whether to begin booking.
Includes: Makkah/Madinah accommodation, itinerary/travel, inclusions/exclusions, supported occupancy pricing, availability representation and applicable policy facts.
Depends on: VS-05.

### VS-07 Travellers & Authoritative Quote
Outcome: a customer can provide the minimum traveller/occupancy inputs and receive an authoritative, expiring quote for the selected departure.
Includes: quote ownership in Pricing, exact money/currency, validation, quote expiry/versioning and price-change UX.
Excludes: OCR and advanced traveller automation.
Depends on: VS-06, VS-03.

### VS-08 Inventory Hold
Outcome: checkout can temporarily reserve required inventory for a valid quote without overselling.
Includes: hold creation/expiry/release, concurrency, idempotency, stale/expired hold UX and telemetry.
Depends on: VS-07.

### VS-09 Booking & Payment
Outcome: a customer can create a booking from a valid quote/hold and attempt payment through the approved provider without duplicate financial effects.
Includes: immutable commercial snapshot, booking reference, provider-hosted/tokenised payment initiation, webhook verification, idempotency, payment state and reconciliation evidence.
Depends on: VS-08.

### VS-10 Confirmation
Outcome: a successfully settled payment and valid inventory commitment produce a confirmed booking, with explicit handling when confirmation cannot safely complete.
Includes: confirmation orchestration, inventory commitment, PaymentSettled handling, ConfirmationException path, audit/outbox and customer-visible state.
Depends on: VS-09.

### VS-11 My Journey
Outcome: a confirmed customer has one trusted place to understand booking, payment and upcoming journey status.
Includes: booking summary, payment state, traveller/readiness placeholders where downstream capabilities are not yet implemented, support entry point and responsive/accessibility states.
Depends on: VS-10.

### VS-12 Documents
Outcome: required traveller documents can be uploaded securely, reviewed by authorized operators, corrected when needed and reflected in readiness.
Includes: private object storage, metadata ownership, access control, review states, correction loop, retention/privacy rules, malware/file validation as required by security baseline.
Depends on: VS-10/VS-11 and operator access.

### VS-13 Visa Tracking
Outcome: authorized operators can maintain the approved visa lifecycle and customers can see the appropriate non-sensitive visa status/action.
Includes: visa state machine, operator updates, audit, customer projection and action-required UX.
Depends on: VS-10/VS-11; document linkage only where explicitly required.

### VS-14 Operational Support
Outcome: operators can find and safely act on MVP booking exceptions across booking, payment, documents and visa without bypassing module ownership.
Includes: work queue/search required for MVP, exception-first views, approved operational commands, authorization, concurrency and audit.
Depends on: relevant preceding capabilities.
Excludes: full CRM/ticketing platform and unrestricted database-style admin UI.

### VS-15 Production Readiness
Outcome: the complete MVP is demonstrably safe and operable for production release.
Includes: critical E2E journeys, security/privacy verification, accessibility, visual regression, migration validation, backup/restore evidence, baseline performance/load checks, monitoring/alerting, deployment/rollback verification, secrets/config review and operational runbooks.
Depends on: all release-scope slices.

## Dependency chain
VS-00 -> VS-01 -> VS-02 -> VS-03 -> VS-04 -> VS-05 -> VS-06 -> VS-07 -> VS-08 -> VS-09 -> VS-10 -> VS-11.

After confirmation/journey foundation, VS-12 and VS-13 can progress independently where their contracts do not require each other. VS-14 composes completed operational capabilities. VS-15 closes the release.

## Anti-overengineering rules
- Do not build a future slice's infrastructure merely because the architecture could support it.
- Prefer explicit module contracts over generic frameworks.
- Add caching, queues, distributed services or additional datastores only against a measured requirement.
- Design extension points in contracts and ownership; do not pre-build future features.
- Each slice should leave the system deployable and preserve completed journeys.

## Slice planning artifact
Before implementation, each VS gets a short feature specification containing: outcome, actor, scenario, dependencies, in/out scope, business rules, state transitions, UX/Figma references, API/contracts, data/migrations, security/privacy, telemetry, test matrix, acceptance criteria, rollback/migration notes and PO sign-off.
