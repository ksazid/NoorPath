# VS-16 — Cancellation & Refunds Implementation Checklist

## Planning and Product Owner gate

- [x] Register VS-16 without reusing an existing slice identifier.
- [x] Confirm whole-booking cancellation is the minimum MVP boundary.
- [x] Confirm Booking, Payments and Inventory retain separate ownership and states.
- [x] Record explicit exclusions for partial traveller cancellation, chargebacks, vouchers and supplier-refund integrations.
- [ ] Product Owner approves cancellation windows and governing timezone.
- [ ] Product Owner approves fee rules for every window.
- [ ] Product Owner approves refundable and non-refundable components.
- [ ] Product Owner approves human-review versus auto-approval rules.
- [ ] Product Owner approves fulfilment milestone restrictions.
- [ ] Product Owner approves refund processing expectation and escalation threshold.
- [ ] Product Owner approves operator-initiated cancellation policy.
- [ ] Product Owner approves policy applicability and publication authority.

Runtime implementation must not begin monetary or production behaviour until the policy decisions above are approved.

## Contract and domain design

- [ ] Define immutable `CancellationPolicyVersion` contract.
- [ ] Define Booking-owned cancellation request lifecycle and allowed transitions.
- [ ] Define immutable and explainable cancellation entitlement snapshot.
- [ ] Define Payments-owned refund instruction and append-only refund record lifecycle.
- [ ] Define Inventory-owned committed-reservation release command and idempotency contract.
- [ ] Define customer-safe and operator-safe status mappings.
- [ ] Define reason-code vocabulary without free-form internal enum leakage.
- [ ] Define expected-version and idempotency requirements for every mutation.
- [ ] Define cross-module contracts; prohibit direct table access.
- [ ] Record an ADR only if an existing durable architecture decision changes.

## Persistence and migrations

- [ ] Add module-owned cancellation request persistence.
- [ ] Persist policy version and exact entitlement snapshot used for the decision.
- [ ] Add optimistic concurrency tokens and uniqueness for one active request per booking.
- [ ] Add idempotency uniqueness for customer request and refund execution.
- [ ] Add append-oriented audit/history records.
- [ ] Add Payments-owned refund ledger entries without modifying settled payment rows.
- [ ] Add provider reference uniqueness where applicable.
- [ ] Create deterministic forward-only migrations and generated model snapshots.
- [ ] Register every new DbContext in the migration registry.
- [ ] Prove fresh-database migration and zero pending model changes.

## Customer API and My Journey

- [ ] Return policy summary, eligibility and authoritative estimate for an account-owned booking.
- [ ] Return safe not-found for foreign-account bookings.
- [ ] Submit a whole-booking cancellation request using expected version and idempotency key.
- [ ] Recover duplicate retries with the existing request result.
- [ ] Reject invalid booking states and duplicate active requests safely.
- [ ] Recalculate or reject stale entitlement when authoritative state changes.
- [ ] Expose requested, under-review, approved, rejected and cancellation-completed states.
- [ ] Expose authorized refund amount/currency and pending, partial, settled or recovery-required state.
- [ ] Preserve support access using the booking reference.
- [ ] Never derive refund amounts in the browser.

## Operator and support API

- [ ] Add permission- and operator-scoped cancellation queue.
- [ ] Add cancellation case detail composed from module-owned projections.
- [ ] Approve or reject using expected version, reason and current entitlement validation.
- [ ] Prevent arbitrary operator-entered refund amounts.
- [ ] Record actor, operator, reason, policy version, correlation and outcome.
- [ ] Add explicit Booking-, Inventory- and Payments-owned recovery commands only where needed.
- [ ] Integrate recovery-required cases into VS-14 without direct state editing.
- [ ] Return safe not-found for foreign-operator resources.

## Booking, inventory and refund orchestration

- [ ] Commit booking cancellation exactly once after approval.
- [ ] Preserve booking, commercial snapshot, travellers, documents, visa cases and history.
- [ ] Release committed inventory through the Inventory-owned command exactly once.
- [ ] Record inventory recovery-required state without duplicating release.
- [ ] Authorize refund from authoritative settled-payment facts and approved entitlement.
- [ ] Submit provider refund idempotently.
- [ ] Verify provider callback authenticity and idempotency.
- [ ] Preserve original settled payment facts when refund succeeds or fails.
- [ ] Support provider-delayed, partial, failed and reconciliation-required outcomes.
- [ ] Keep booking cancellation and refund settlement as separate customer-visible facts.

## Security, privacy, audit and telemetry

- [ ] Enforce booking-owner isolation server-side.
- [ ] Enforce operator membership, permission and operator scope server-side.
- [ ] Bound and protect customer reason notes.
- [ ] Exclude tokens, provider payloads, payment secrets, traveller data and document data from logs.
- [ ] Use opaque identifiers in logs and metrics.
- [ ] Audit every request, decision, cancellation, inventory release, refund authorization, provider submission and recovery action.
- [ ] Add privacy-safe telemetry for eligibility, stale state, duplicate recovery, provider latency/failure and reconciliation.
- [ ] Verify privileged production access remains MFA-protected.

## Customer and operator experience

- [ ] Extend approved NoorPath header, footer, tokens, typography, cards and controls.
- [ ] Show policy version and a plain-language entitlement breakdown before confirmation.
- [ ] Make whole-booking impact explicit before submission.
- [ ] Implement loading, unavailable, ineligible, validation, stale, duplicate, review and outcome states.
- [ ] Implement provider-delayed and recovery-required states without implying money is settled.
- [ ] Provide safe retry paths that cannot duplicate requests or refunds.
- [ ] Meet keyboard, visible-focus, target-size, 200% text, reflow and reduced-motion requirements.
- [ ] Verify desktop and mobile against approved NoorPath visual authority.

## Automated verification

- [ ] Domain tests cover every allowed and forbidden cancellation transition.
- [ ] Policy tests cover every approved cancellation window and boundary timestamp.
- [ ] Money tests prove explicit currency, rounding and reconciliation.
- [ ] Idempotency tests cover duplicate customer requests, approvals, inventory releases, provider submissions and callbacks.
- [ ] Concurrency tests cover stale booking, cancellation request and refund state.
- [ ] Integration tests verify account isolation and operator isolation.
- [ ] Integration tests verify booking history preservation.
- [ ] Integration tests verify append-only payment/refund facts.
- [ ] Migration registry and fresh-database tests pass.
- [ ] Playwright covers customer request and status tracking on desktop and mobile.
- [ ] Playwright covers operator review, stale recovery and permission denial.
- [ ] Accessibility checks cover serious/critical axe findings, keyboard, focus, target size and reflow.
- [ ] Certification evidence is generated on the exact unchanged head SHA.

## Release controls

- [ ] Provider refund credentials are configured only in the approved environment.
- [ ] Production policy version is explicitly approved.
- [ ] Production refund execution remains disabled until security and operational readiness are signed off.
- [ ] Rollback preserves cancellation and refund facts and cannot recreate released inventory silently.
- [ ] Operational runbook covers provider delay, duplicate callback, partial refund, inventory-release failure and reconciliation.
- [ ] Product Owner approves the exact certified runtime SHA before merge.
- [ ] Production deployment requires a separate explicit approval after merge.
