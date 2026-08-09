# VS-33 — Departure Fulfilment & Group Leader

Status: Implementation

## Outcome

Approved operator staff can see exactly which package a departure is fulfilling and record one accompanying group leader as departure-level operational metadata before final handover, without creating a traveller, changing readiness counts, or rewriting commercial/package facts.

## Traceability

- `INV-ID-003`, `INV-ID-004`, `INV-ID-005`, `INV-ID-006`
- `INV-CAT-001`, `INV-CAT-002`, `INV-CAT-005`
- `INV-TRV-002`, `INV-TRV-003`, `INV-TRV-004`
- `INV-JRN-001`, `INV-JRN-002`
- `INV-AUD-001`, `INV-AUD-002`
- VS-27 Departure Manifest & Pilgrim Operations
- VS-28 Final Departure Handover & Readiness
- VS-31 Unified Operator Shell & Navigation

No new product policy or stable PRD requirement ID is invented here.

## Product boundary

Catalogue remains authoritative for the existing PackageVersion → DepartureBatch relationship. VS-33 does not create another package link; the operator UI simply exposes the package being fulfilled using the existing departure preview route.

Booking owns the one optional group-leader name because it is departure operational metadata. A group leader is **not** silently inserted into Booking Travellers and never changes manifest traveller, ready or blocked counts. If the same person is also a customer traveller, that remains a separate explicit booking fact.

VS-27 remains authoritative for traveller readiness. VS-33 must not add a new readiness rule or attempt to fix the known VS-28 readiness-consumption architecture debt.

## Included

- a clearly labelled `View package being fulfilled` link from the pilgrim manifest and final handover;
- one optional group-leader name displayed in the departure fulfilment context;
- add, update and clear operations before handover completion;
- optimistic version validation using the existing departure handover operational record;
- deny-by-default operator scope and safe not-found behavior;
- append-oriented audit evidence with actor, action, correlation id, previous/resulting version and time;
- completed-handover immutability;
- manifest and handover UI feedback for saved, stale and completed states;
- responsive, keyboard and touch verification.

## Data contract

The existing Booking-owned `DepartureHandoverRecord` gains one nullable `GroupLeaderName` field (maximum 120 characters). This is the smallest existing aggregate that already owns departure-level operational closeout and concurrency. It avoids a new table/aggregate for one optional fact.

The existing `DepartureHandoverAuditRecord` records `group_leader_updated` and `group_leader_cleared` actions. The audit note contains only the minimum operational description. No phone, identity document, DOB, gender or other group-leader PII is introduced.

A forward-only Booking migration and updated EF model snapshot are required.

## API contract

Existing:
- `GET /api/v1/operator/departures/{departureId}/manifest`
- `GET /api/v1/operator/departures/{departureId}/handover`
- `POST /api/v1/operator/departures/{departureId}/handover/complete`

Added:
- `POST /api/v1/operator/departures/{departureId}/manifest/group-leader`

Request:
```json
{
  "name": "Amina Rahman",
  "expectedVersion": 0
}
```

Rules:
- `name` is trimmed and may be blank/null to clear an existing leader;
- non-empty names are 1–120 characters;
- `expectedVersion` must match the current departure operational record version (0 when no record exists);
- foreign departure → 404;
- stale version → 409 `departure_operations_stale`;
- completed handover → 409 `handover_completed`;
- successful write returns the resulting leader name and version;
- clearing when no record exists is an idempotent no-op and creates no audit record.

## Operator flow

`Departures → Pilgrim manifest → Departure fulfilment → View package / Add group leader → Review readiness → Final handover`

The fulfilment panel must keep the package name and dated departure context visible. The group-leader field is explicitly labelled `Accompanying group leader`; helper copy states that it does not add a booked traveller.

## Accessibility and responsive behavior

- group-leader text input and actions are keyboard-operable and at least 44px high;
- package link has a descriptive accessible name;
- save/clear success and stale/completed failures are announced through existing status/alert patterns;
- controls remain usable at 390px with no horizontal overflow;
- focus styles use the established operator tokens;
- no drag/gesture-only interaction is introduced.

## Trust and failure behavior

- no other Booking, Catalogue, payment, document, visa or accommodation state changes on a group-leader write;
- a stale write does not partially mutate the record or append a successful audit;
- final handover completion locks leader mutation along with the operational closeout;
- customer-facing package commitments remain unchanged;
- no sensitive group-leader data is logged or added to telemetry.

## Verification

- integration tests for owned assignment, audit/version progression, foreign safe-not-found, stale write and completed-handover immutability;
- existing Booking migration registry/model parity;
- rendered Playwright for package-link reachability, add/update/clear feedback and mobile reflow;
- exact-head CI, Slice Governance, Rendered Slice Review and Navigation Reachability Review.

## Acceptance

VS-33 closes only when the exact unchanged implementation head passes every required workflow, the standing Product Owner authorization is applied to that exact SHA, all ready/label-triggered required workflows also pass, and the PR is then merged to `main`. Production deployment is not part of this slice.
