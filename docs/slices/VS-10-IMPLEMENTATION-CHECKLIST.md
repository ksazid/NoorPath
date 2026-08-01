# VS-10 — Confirmation Implementation Checklist

## Development mode
- [ ] VS-09 is merged and its contracts are stable.
- [ ] Keep the PR Draft while implementation is changing.
- [ ] Apply `certify` only after all confirmation and exception states are implemented.

## Contract and ownership
- [ ] Payment settlement is Payments-owned and authenticated.
- [ ] Durable commitment is Inventory-owned.
- [ ] Confirmation and exception state are Booking-owned.
- [ ] Cross-module changes use explicit commands/events and outbox evidence.

## Product completeness
- [ ] Settled payment starts confirmation exactly once.
- [ ] Active hold converts to one durable commitment without oversell.
- [ ] Event replay, outbox replay, retry and worker restart are idempotent.
- [ ] Confirmed booking retains the immutable booking snapshot.
- [ ] Unsafe confirmation enters an explicit `ConfirmationException`.
- [ ] Approved recovery resumes safely without duplicate effects.
- [ ] Processing, confirmed, delayed and action-required customer states are complete.
- [ ] Confirmed booking links to My Journey.

## Persistence and failure safety
- [ ] Booking/Inventory migrations are deterministic with required constraints and indexes.
- [ ] Clean database apply and snapshot parity pass.
- [ ] Transaction/outbox failure cannot falsely confirm or lose settlement evidence.
- [ ] Capacity-one and competing-confirmation tests prove no oversell.
- [ ] Mismatched and out-of-order facts are rejected and audited.

## Certification gates
- [ ] Formatting and static analysis pass.
- [ ] Unit, integration, contract and architecture tests pass.
- [ ] Migration and PostgreSQL validation pass.
- [ ] Authentication, authorization, privacy and secret scanning pass.
- [ ] Accessibility and rendered regression evidence pass.
- [ ] Route graph and customer journey linking pass.
- [ ] Logs, traces, metrics, outbox and exception evidence are verified.
- [ ] Product Owner accepts the exact certified SHA.

## Final merge gate
- [ ] Full CI passed on the exact final SHA.
- [ ] Rendered Slice Review passed on the exact final SHA.
- [ ] Evidence artifact and certification comment reference the exact final SHA.
- [ ] No unresolved review thread or known regression remains.
- [ ] `po-approved` is present only after Product Owner review.
- [ ] NoorPath Merge Gate is successful.
