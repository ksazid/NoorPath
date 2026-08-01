# VS-10 — Confirmation

## Status
Specification prepared. Implementation begins only after VS-09 is complete and remains Draft until exact-head certification and Product Owner acceptance.

## Outcome
A successfully settled payment and valid inventory commitment produce one confirmed booking, with an explicit recoverable exception path whenever confirmation cannot safely complete.

## Actor
NoorPath confirmation orchestration reacting to an authenticated `PaymentSettled` fact. Customers and authorized operators receive only the visibility and recovery actions appropriate to their scope.

## Dependencies
- VS-09 Booking & Payment is complete.
- Booking, Payments and Inventory contracts are stable.
- Transactional outbox delivery is available for durable cross-module facts.

## Domain ownership
- **Payments** owns whether money is settled.
- **Inventory** owns temporary-hold conversion and durable capacity commitment.
- **Booking** owns confirmation state and confirmation exceptions.
- Cross-module state changes occur through explicit commands/events, never shared-table mutation.

## Required state model
`PendingConfirmation -> Confirming -> Confirmed | ConfirmationException`

A terminal confirmed booking must not regress. An exception requires an approved retry or recovery command and complete audit evidence.

## Core invariants
- One settled payment can confirm one matching booking only once.
- One active hold converts to at most one durable commitment.
- Payment replay, outbox replay and worker restart are idempotent.
- Money received without safe inventory commitment becomes `ConfirmationException`; it is never silently discarded or falsely confirmed.
- Confirmation retries resume from durable evidence and cannot duplicate inventory or booking effects.

## Customer journey
1. Customer completes payment in VS-09.
2. Booking displays payment received and confirmation processing.
3. Confirmation orchestration validates booking/payment/hold correlation.
4. Inventory creates the durable commitment.
5. Booking becomes confirmed, or enters an explicit action-required exception state.
6. Confirmed bookings route to VS-11 My Journey.

## Acceptance criteria
- [ ] Authenticated payment settlement starts confirmation exactly once.
- [ ] Active hold converts to one durable inventory commitment without oversell.
- [ ] Duplicate events, retries and worker restarts are idempotent.
- [ ] Confirmed booking retains the immutable VS-09 commercial snapshot.
- [ ] Unsafe confirmation creates a visible, auditable `ConfirmationException`.
- [ ] Customer states cover processing, confirmed, delayed and action-required outcomes.
- [ ] Approved operator recovery cannot bypass module ownership or concurrency rules.
- [ ] Telemetry correlates booking, payment, commitment, outbox and outcome.

## Explicit exclusions
- Documents, visa and detailed travel readiness.
- Refund execution, chargebacks and manual financial settlement.
- Supplier airline/hotel confirmation integrations.
- General-purpose workflow or saga framework.
- Full operational support work queue.

## Merge rule
Do not merge until all confirmation, replay, exception, rendered, migration, security and Product Owner gates pass on the exact final SHA.
