# VS-13 — Visa Processing Tracker

Status: Registered for implementation

## Outcome

Authorized Operator Operations Staff manage a per-traveller visa lifecycle, while the Booking Owner sees a truthful customer-safe status and any required action.

## Lifecycle

`NotStarted → AwaitingDocuments → ReadyToSubmit → Submitted → Processing → Approved | ActionRequired | Rejected`

Corrections return an `ActionRequired` case to the appropriate non-terminal state through an explicit audited transition. Approved and Rejected cases cannot be silently reopened.

## Ownership

- Visa owns case state, transitions, customer-safe action and history.
- Booking owns booking/account/operator relationships.
- Traveller supplies stable traveller identity.
- Documents supplies readiness only; Visa never accesses private document content.
- Journey readiness is derived and is not mutable Visa state.

## Authorization

- Booking Owners read only travellers on their account-owned confirmed bookings.
- Operator Operations Staff read and transition only cases belonging to their active operator membership and explicit visa-processing permission.
- Foreign identifiers return safe not-found responses.
- Platform Administrator status does not implicitly grant operator case access.

## Customer projection

Expose traveller, simplified status, last-updated time and required action. Never expose internal notes, passport/document data, object keys, visa references, staff identity or provider payloads.

## Operator workflow

Queue by actionable state; open case; choose an allowed transition; supply a reason when entering ActionRequired or Rejected; submit with optimistic version; append audit history.

## Security and audit

All transitions are append-audited. Logs and telemetry use opaque case/booking correlation identifiers and exclude identity, passport, document and visa-reference data.

## Exclusions

No government/provider integration, automation of consequential decisions, new uploads, notifications, family/Mahram rules, bulk mutation or generic CRM.

## Product Owner gate

Certification and exact-head Product Owner approval are required before merge. Merge does not deploy production.
