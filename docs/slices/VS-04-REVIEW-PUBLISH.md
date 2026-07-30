# VS-04 — Review & Publish

Status: Implementation

## Outcome

An authorized operator can submit a complete departure for review, and a separately authorized NoorPath platform publication approver can publish it only after current Catalogue, Pricing, Inventory and operator-eligibility checks pass.

## Product-owner decision

NoorPath uses a two-step MVP publication policy:

1. operator staff submits the departure for review;
2. NoorPath platform staff approves and publishes.

Operator staff cannot approve or publish their own departure. Platform publication access is independent of operator membership.

## Traceability

- `INV-ID-003`–`INV-ID-006`
- `INV-OP-001`
- `INV-CAT-001`–`INV-CAT-006`
- `INV-ACC-003`
- `INV-PRI-001`, `INV-PRI-002`, `INV-PRI-004`, `INV-PRI-006`
- `INV-INV-001`, `INV-INV-003`, `INV-INV-006`
- `INV-AUD-001`, `INV-AUD-002`
- `INV-SEC-001`, `INV-SEC-006`
- VS-01 Operator Access
- VS-02 Package & Departure Authoring
- VS-03 Pricing & Inventory
- MVP Vertical Slice Map VS-04

No stable approved PRD requirement ID is invented here.

## Actors and authorization

### Operator submitter

Requires an authenticated principal with an active membership for the owning `Approved` operator and `operator.admin.access`. Operator scope is derived server-side. Cross-operator and unknown departures remain hidden as `404`.

### NoorPath publication approver

Requires an authenticated principal whose normalized internal AccountId is explicitly configured in `Authorization:PlatformPublicationApproverAccountIds`. Production identity provisioning must enforce privileged MFA. The approver does not receive operator scope and does not need operator membership.

The test identity scheme remains restricted to Development/Test environments.

## Lifecycle

- `Draft` — editable in VS-02/VS-03.
- `ReadyForReview` — frozen after operator submission.
- `Published` — approved by NoorPath platform staff and immutable for this MVP slice.

Submitting increments the Catalogue departure version and freezes Catalogue, Pricing and Inventory writes through their existing draft-state guard.

Publishing:

1. revalidates operator eligibility;
2. revalidates Catalogue completeness;
3. verifies matching, positive saleable Pricing and Inventory occupancy facts;
4. verifies expected Catalogue, Pricing and Inventory versions;
5. creates or reuses an immutable Pricing-owned `PriceVersion`;
6. transitions PackageVersion and Departure to `Published`;
7. appends publication audit evidence; and
8. records `PackageVersionPublished` and `DeparturePublished` in the Catalogue transaction outbox.

## Commercial readiness rule

Publication requires:

- one to three configured `double|triple|quad` prices;
- the same occupancy keys configured in Inventory;
- a positive price for every offered occupancy;
- positive capacity for every offered occupancy; and
- explicit currency.

Zero-capacity or partially matched commercial rows block submission/publication instead of being silently omitted from the public offer.

## Included

- operator review summary and submit transition;
- platform pending-publication queue and detail summary;
- separate platform publication permission;
- latest-version revalidation and stale conflict handling;
- immutable Pricing-owned price snapshot;
- Catalogue publication audit;
- transactional Catalogue outbox records for the two publication events;
- loading, empty, validation, permission, stale/conflict, retry, success and reduced-motion UI states;
- responsive operator/admin layouts that extend the existing NoorPath operator visual language.

## Excluded

Customer discovery, customer package queries, booking, quotes, holds, payment plans, checkout, pause/resume/cancel, post-publication editing, approval delegation UI, rejection/request-changes workflow, notifications and outbox dispatch consumers.

## API

### Operator

- `GET /api/v1/operator/departures/{departureId}/publication-review`
- `POST /api/v1/operator/departures/{departureId}/submit-review`

Submission requires:

- `expectedDepartureVersion`
- `expectedPricingVersion`
- `expectedInventoryVersion`

### NoorPath platform

- `GET /api/v1/platform/publications`
- `GET /api/v1/platform/publications/{departureId}`
- `POST /api/v1/platform/publications/{departureId}/publish`

Publishing requires the same three expected versions returned by the review projection.

## Failure contract

- `401` unauthenticated
- `403` missing operator or platform permission
- `404` hidden/unknown operator-owned departure
- `409 stale_publication_review` when any expected version changed
- `409 departure_not_reviewable` for an invalid lifecycle transition
- `422 publication_not_ready` with explicit validation checks

Problem Details include a safe stable code and correlation ID.

## Telemetry and privacy

Logs contain outcome, actor AccountId, operator ID, departure ID, versions and correlation ID. Logs and events exclude prices, free-text package content and inventory adjustment reasons.

## Acceptance criteria

1. Operator staff can inspect readiness and submit only its own complete draft.
2. Operator staff cannot publish, even if it has `operator.admin.access`.
3. A configured NoorPath platform approver can view submitted departures and publish them.
4. An ordinary authenticated principal cannot access the platform publication endpoints.
5. Submission and publication reject stale Catalogue, Pricing or Inventory versions.
6. Publication is blocked when operator eligibility or any required Catalogue/Pricing/Inventory rule fails.
7. Publication creates an immutable Pricing-owned price version.
8. Publication transition, audit record and both outbox events commit together in Catalogue.
9. Published Catalogue and commercial drafts cannot be edited in place.
10. Applicable unit, PostgreSQL integration, migration, authorization, accessibility, responsive and CI gates pass.
11. UI extends the approved NoorPath operator language and receives Product Owner visual acceptance before the slice closes.
