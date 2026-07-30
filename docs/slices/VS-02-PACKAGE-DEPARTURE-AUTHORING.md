# VS-02 — Package & Departure Authoring

Status: Implementation

## Outcome

An authorized operator staff principal can create and save a valid draft Umrah package and dated departure containing the minimum Makkah, Madinah, travel, inclusion, exclusion and journey facts needed for later pricing, inventory and publication slices.

## Traceability

- `INV-ID-003`–`INV-ID-006`
- `INV-CAT-001`, `INV-CAT-002`, `INV-CAT-005`, `INV-CAT-006`
- `INV-ACC-001`, `INV-ACC-002`
- VS-01 Operator Access
- API & Application Contract Baseline sections 4, 6, 7, 9, 11, 15 and 25

No stable approved PRD requirement ID is invented here.

## Actor and access

The actor is an authenticated principal with an active membership for an `Approved` operator and explicit `operator.admin.access` permission. Operator scope is always derived server-side from VS-01 access context; request payloads never choose or grant OperatorId.

Cross-operator reads and writes return `404` so private draft existence is not disclosed.

## Included

- create one operator-owned `PackageTemplate` with its first draft `PackageVersion` and one draft `DepartureBatch` in a single authoring operation;
- load the operator-owned draft for editing;
- save edits with optimistic concurrency using an explicit integer version;
- package title and summary;
- independent Makkah and Madinah accommodation facts: hotel/display name, factual classification text, factual distance disclosure, nights and confirmation state;
- travel facts: route summary, factual detail and confirmation state;
- departure origin, departure date and return date;
- ordered inclusions and exclusions;
- persistent create/update audit trail with actor, correlation ID, version and timestamp;
- operator authoring UX states: loading, editable draft, field validation, save-in-progress, saved, stale-version conflict, forbidden/unauthenticated and retryable error;
- Catalogue-owned PostgreSQL persistence and migration.

## Excluded

Pricing, occupancy prices, payment plans, capacity, availability, inventory, publication/review transitions, public discovery, cloning, bulk upload, supplier sync, OCR and booking behaviour.

## Draft rules

- `PackageTemplate` and `DepartureBatch` are separate concepts.
- A new authoring operation creates one mutable template, one draft package version and one draft departure.
- Only `Draft` package/departure state is implemented in VS-02.
- Makkah and Madinah accommodation are represented independently.
- Pending hotel/travel facts must be explicitly marked `Pending`; the UI/API never infer `Confirmed`.
- Package title, summary, origin, dates, both accommodation entries and route summary are required for a saved valid draft.
- Return date must be after departure date.
- Makkah/Madinah nights must be non-negative and at least one of the two stays must contain nights.
- Inclusion/exclusion lists are trimmed, empty entries removed and duplicates removed while preserving order.
- No draft field represents authoritative price, capacity or availability.

## API

### `POST /api/v1/operator/departures`

Creates the package template, first draft package version and draft departure for the current operator.

Returns `201` with:

- `packageTemplateId`
- `packageVersionId`
- `departureId`
- `version`
- `status: "draft"`

### `GET /api/v1/operator/departures/{departureId}`

Returns the editable draft only when it belongs to the current operator.

### `PUT /api/v1/operator/departures/{departureId}`

Saves the current operator's draft. The request includes `expectedVersion`; stale writes return `409` Problem Details with code `stale_version`.

Validation returns `422` Problem Details with field errors. Authentication/authorization uses the established VS-01 `401`/`403` contract.

## Persistence

VS-02 replaces the superseded S02 Catalogue runtime model with Catalogue-owned tables for package templates, package versions, departure batches, ordered content items and draft audit entries. The old Catalogue-owned price/capacity/availability/publication truth is removed from the target model because Pricing, Inventory and publication belong to later slices.

This is a pre-production schema reconciliation. Git history preserves obsolete S02 evidence; no production commercial data migration is claimed.

## Telemetry

Create/save logs include outcome, operatorId, departureId, version and correlationId. Logs do not include package free text, accommodation details or other content payloads.

## Test matrix

- domain validation and normalization;
- authenticated authorized create/load/update;
- unauthenticated and forbidden denial;
- cross-operator hidden-resource denial;
- stale expected-version conflict;
- PostgreSQL persistence and audit history;
- migration model parity and clean database application;
- architecture boundaries;
- web validation/loading/saved/conflict/error states;
- accessibility and responsive checks for the authoring route.

## Acceptance criteria

1. Approved VS-01 operator access can create a valid draft without supplying OperatorId.
2. Draft persists separate package-template/package-version/departure identities.
3. Saved draft contains independent Makkah and Madinah facts, travel facts, inclusions/exclusions and dated departure facts.
4. Price, capacity, availability and publication state are not authored by VS-02.
5. Operator A cannot read or mutate Operator B's draft.
6. Stale saves cannot overwrite a newer draft.
7. Create/update actions leave auditable actor/correlation/version evidence.
8. Applicable build, format, unit, PostgreSQL integration, migration, authorization, accessibility and CI gates pass.
9. UI implementation extends NoorPath's approved visual language and receives Product Owner visual acceptance before the slice closes.
