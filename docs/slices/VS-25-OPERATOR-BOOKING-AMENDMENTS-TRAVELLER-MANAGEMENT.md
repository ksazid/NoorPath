# VS-25 — Operator Booking Amendments and Traveller Management

## Outcome

Approved operator staff can safely amend the booked traveller snapshot and occupancy for one operator-owned booking, understand the authoritative commercial impact before committing, and confirm the amendment without bypassing Booking, Pricing, Traveller, Payments, Documents, Visa or Cancellation boundaries.

## Product flow

`Bookings → Open booking → Amend booking → Edit travellers/occupancy → Review price impact → Confirm amendment → Updated booking`

The amendment workspace is an extension of the existing VS-24 booking detail experience. It does not redesign the operator shell.

## Domain ownership

- **Booking** owns whether an amendment is permitted, the booked traveller snapshot, occupancy, financial snapshot, concurrency version, amendment lifecycle and append-only audit evidence.
- **Pricing** owns authoritative repricing. UI code may format amounts but must never implement occupancy, discount, instalment or commercial calculations.
- **Traveller** owns reusable traveller identity. VS-25 changes only the booking snapshot required to service this booking.
- **Payments** owns payment attempts and settlement. Confirming an amendment must not rewrite payment history or create refunds.
- **Documents** owns passport/document facts and review states.
- **Visa** owns visa lifecycle.
- **Cancellation/Refunds** remains a separate governed workflow.

No module may directly write another module's tables to implement VS-25.

## Amendment policy

### Eligible lifecycle

The initial amendment boundary permits operator servicing only when the booking is `Confirmed`. Pending-payment, payment-processing, confirmation-processing, confirmation-exception and cancelled bookings must be rejected before a mutation is attempted. The policy is explicit so later product decisions can broaden the allowed set without scattering state checks across API/UI code.

### Traveller and occupancy rules

- Occupancy remains `Double`, `Triple` or `Quad`.
- The confirmed proposed traveller count must equal the occupancy-required traveller count.
- Every proposed traveller requires a non-empty name and a valid date of birth in the past.
- A traveller snapshot row is identified by its booking traveller/traveller identifier; adding a new booked traveller uses a new identifier owned by the governed workflow.
- Reordering travellers may change booking position only; it must not mutate identity/document/visa records.
- Passport numbers, document uploads, document review state, visa state and reusable identity attributes outside the booking snapshot are not editable here.

## Preview contract

Before confirmation, the server returns an amendment preview containing:

- booking id/reference and current version;
- current occupancy/traveller snapshot;
- proposed occupancy/traveller snapshot;
- current authoritative financial snapshot;
- proposed authoritative financial snapshot from Pricing;
- price delta and whether the amendment changes money;
- a short-lived preview token/fingerprint bound to the booking version and proposed request;
- policy/validation messages that prevent confirmation.

The browser does not derive a new price from the old booking.

## Confirmation contract

Confirmation requires:

- the exact preview token/fingerprint;
- the expected booking version used by preview;
- an explicit operator confirmation;
- an operator-provided amendment reason suitable for audit;
- the same authenticated operator ownership context.

The server re-validates ownership, lifecycle, preview integrity and concurrency immediately before commit. A stale version returns conflict and performs no partial mutation.

## Financial integrity

- Booking stores the newly accepted authoritative commercial snapshot after confirmation.
- Historical payment attempts and settlement facts are never edited by this slice.
- If the new booking total differs from the existing total, the amendment history records the delta.
- Any additional collection, credit or refund consequence is surfaced as follow-up context and handled by a separate Payments/Refund workflow; VS-25 does not silently settle it.
- Existing paid amounts must never be fabricated from the amended booking snapshot.

## Audit and concurrency

Every confirmed amendment appends an immutable record containing:

- amendment id and booking id;
- operator/account actor;
- reason;
- previous and resulting booking version;
- before/after occupancy, travellers and financial snapshots;
- price delta;
- preview/request fingerprint;
- correlation id;
- UTC timestamp.

Booking version is an optimistic concurrency token. Preview and confirmation are bound to that version.

## Authorization and privacy

- Operator access is deny-by-default.
- A foreign-operator booking id is returned as safe not-found rather than leaking tenancy.
- Traveller data shown/editable is limited to the identity-safe booking snapshot required for operations.
- Telemetry must not include passport/document payloads or other sensitive traveller content.

## UX states

The amendment route must cover:

- loading;
- populated editable state;
- client/server validation;
- price preview loading and success;
- no-price-change preview;
- price-changing preview requiring explicit confirmation;
- safe not-found/forbidden;
- booking no longer eligible;
- stale version conflict with refresh/retry guidance;
- recoverable API failure and retry;
- successful confirmation returning to/refeshing booking detail and history.

Controls follow the existing NoorPath operator design system, WCAG 2.2 AA expectations, minimum target sizes and mobile reflow rules.

## Explicit exclusions

- payment capture, adjustment or refund execution;
- document/passport mutation or document review;
- visa transitions;
- cancellation execution;
- reusable traveller identity editing outside the booking snapshot;
- shell/header/sidebar/footer redesign;
- production deployment.

## Certification boundary

VS-25 is complete only after applicable domain/unit/integration/architecture/migration/security/authorization/concurrency/financial-integrity checks pass plus rendered desktop/mobile and real click-through navigation coverage. A skipped required check is not a pass. Product Owner approval applies only to the unchanged certified head SHA.
