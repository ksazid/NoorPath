# VS-09 — Booking & Payment

## Status
Specification prepared. Implementation remains Draft until exact-head certification and Product Owner acceptance.

## Outcome
A customer can create exactly one booking from a valid quote and active inventory hold, then initiate and reconcile an approved provider-hosted payment without duplicate financial effects.

## Actor
Authenticated customer. Payment state changes may also originate from authenticated, replay-safe provider webhooks.

## Dependencies
- VS-08 Inventory Hold is complete.
- Pricing remains authoritative for the immutable quote.
- Inventory remains authoritative for the active hold.

## Domain ownership
- **Booking** owns the booking reference, immutable commercial snapshot and booking lifecycle.
- **Payments** owns payment attempts, provider identifiers, webhook verification, reconciliation and payment lifecycle.
- **Inventory** owns hold validity and consumption eligibility.
- **Pricing** owns quote validity and the money snapshot that Booking copies without recomputation.

No module may update another module's tables directly.

## Required state model

### Booking
`PendingPayment -> PaymentInProgress -> PaymentSucceeded | PaymentFailed | PaymentCancelled`

Payment success does not confirm the booking. VS-10 owns confirmation.

### Payment attempt
`Created -> ProviderPending -> Succeeded | Failed | Cancelled | RequiresAction`

Terminal states must not regress because of duplicate or out-of-order provider events.

## Customer journey
1. Customer reaches the Plan page with a valid quote and active hold.
2. Customer reviews travellers, occupancy, commercial totals and hold expiry.
3. Customer creates a booking using an idempotency key.
4. NoorPath returns one booking reference and initiates provider-hosted payment.
5. Customer returns to a truthful pending/succeeded/failed/cancelled payment state.
6. A settled payment becomes the input to VS-10 confirmation.

## Security and privacy
- Never receive or persist raw PAN, CVV or unrestricted payment payloads.
- Verify webhook signature, provider account, event identity and booking/payment correlation.
- Scope booking, payment initiation and reads to the authenticated customer account.
- Store the minimum provider evidence required for reconciliation.
- Redact tokens, signatures, personal data and provider secrets from logs.

## Idempotency and concurrency
- Booking creation is account-scoped and idempotent.
- The same idempotency key and request returns the original booking.
- The same key with a different fingerprint returns a deterministic conflict.
- Hold consumption, booking creation and payment-attempt creation must not produce partial state.
- Duplicate and out-of-order webhooks must not duplicate financial effects.

## Acceptance criteria
- [ ] One active, customer-owned quote/hold creates one immutable booking.
- [ ] Booking snapshots departure, travellers, occupancy, currency, total, due-now, remaining, instalments and price version.
- [ ] Replayed booking creation returns the original response.
- [ ] Expired, released, foreign or consumed holds cannot create a booking.
- [ ] Payment initiation uses provider-hosted or tokenised handling.
- [ ] Webhook authentication, replay protection and state monotonicity are proven.
- [ ] Pending, failure, cancellation, retry and provider-error UX states are complete.
- [ ] Booking/payment telemetry is useful without exposing sensitive data.
- [ ] The Plan -> Booking -> Payment route journey is connected on desktop and mobile.

## Explicit exclusions
- Final confirmation and durable inventory commitment.
- Refunds, chargebacks, disputes and operator financial correction.
- Multiple providers, stored cards, wallets and offline cash.
- Multi-room, child/infant pricing, promotions and dynamic pricing.

## Merge rule
Do not merge until every implementation checklist item passes on the exact final SHA, the rendered artifact is available, no unresolved review thread remains, and Product Owner acceptance is recorded.
