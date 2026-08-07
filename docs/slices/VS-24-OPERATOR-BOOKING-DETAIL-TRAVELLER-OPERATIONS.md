# VS-24 — Operator Booking Detail and Traveller Operations

## Outcome
Approved operator staff can open one operator-owned booking and service it from a consolidated operational detail workspace without bypassing the modules that own payments, documents, visa, support or cancellation behavior.

## Scope

The slice extends the existing `/operator/bookings` management workspace with a real booking-detail destination. The detail view composes existing Booking, Payments, Documents, Visa and Catalogue data into an operator-facing read model, preserving operator isolation and safe not-found semantics.

The UI must remain an extension of the existing NoorPath staff shell. It should emphasize booking reference, package/departure facts, traveller roster, occupancy, payment progress, document readiness, visa progress and governed links to module-owned actions.

## Domain and authorization boundaries

- Booking remains the source of truth for booking state, occupancy and travellers.
- Payments remains the source of truth for successful payment totals and instalment progression.
- Documents remains the source of truth for requirement/submission state.
- Visa remains the source of truth for per-traveller visa state.
- Support and Cancellation remain separate operational workflows.
- The endpoint must resolve the caller's active operator membership and return safe not-found for a booking owned by another operator.
- No mutation is added to the detail page in this slice.

## Primary route

`/operator/bookings/{bookingId}`

Primary API contract:

`GET /api/v1/operator/bookings/{bookingId}`

## Required experience

The detail page must provide:

- booking reference, package name, origin and travel dates;
- booking lifecycle status and occupancy;
- traveller roster with identity-safe operational facts;
- total, paid, outstanding and instalment timeline;
- document readiness summary and per-traveller/requirement context where the current data model supports it;
- visa readiness summary and per-traveller status;
- navigation to existing document, visa, support, cancellation and departure workspaces;
- explicit loading, forbidden, not-found and recoverable error states;
- responsive and accessible presentation on desktop and mobile.

## Explicit exclusions

- Payment capture, refunds or manual financial adjustments.
- Editing traveller identity or passport details.
- Document approval/rejection from this page.
- Visa transitions from this page.
- New support or cancellation domain commands.
- Staff shell/sidebar redesign.
- Production deployment.

## Merge rule

Keep the implementation PR Draft until the `certify` label is applied and every required exact-head gate has actually run and passed. Skipped required checks are not passes. Product Owner approval applies only to the unchanged certified SHA. Production deployment is separately approved and is not implied by merge approval.
