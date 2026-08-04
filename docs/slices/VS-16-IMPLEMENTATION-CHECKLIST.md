# VS-16 — Cancellation & Refunds Implementation Checklist

## Planning and Product Owner gate

- [x] Register VS-16 without reusing an existing slice identifier.
- [x] Confirm whole-booking cancellation is the minimum MVP boundary.
- [x] Confirm Booking, Payments and Inventory retain separate ownership and states.
- [x] Record explicit exclusions for partial traveller cancellation, chargebacks, vouchers and supplier-refund integrations.
- [ ] Product Owner approves cancellation windows and governing timezone.
- [ ] Product Owner approves fee rules for every window.
- [ ] Product Owner approves refundable and non-refundable components.
- [x] Product Owner approves human-review versus auto-approval rules: every MVP customer request requires operator review.
- [ ] Product Owner approves fulfilment milestone restrictions.
- [ ] Product Owner approves refund processing expectation and escalation threshold.
- [ ] Product Owner approves operator-initiated cancellation policy.
- [ ] Product Owner approves policy applicability and publication authority.

Runtime implementation may build configurable and fail-closed behavior, but production monetary behavior remains disabled until the policy decisions above are approved.

## Contract and domain design

- [x] Define immutable `CancellationPolicyVersion` contract.
- [x] Define Booking-owned cancellation request lifecycle and allowed transitions.
- [x] Define immutable and explainable cancellation entitlement snapshot.
- [x] Define Payments-owned refund instruction and append-only refund record lifecycle.
- [x] Define Inventory-owned committed-reservation release command and idempotency contract.
- [ ] Define customer-safe and operator-safe status mappings.
- [x] Define reason-code vocabulary without free-form internal enum leakage.
- [x] Define expected-version and idempotency requirements for every mutation.
- [x] Define cross-module contracts; prohibit direct historical financial editing.
- [ ] Record an ADR only if an existing durable architecture decision changes.

## Persistence and migrations

- [x] Add module-owned cancellation request persistence model.
- [x] Persist policy version and exact entitlement snapshot used for the decision.
- [x] Add optimistic concurrency tokens and uniqueness for one active request per booking.
- [x] Add idempotency uniqueness for customer request and refund execution.
- [x] Add append-oriented audit/history records.
- [x] Add Payments-owned refund ledger entries without modifying settled payment rows.
- [x] Add provider reference uniqueness where applicable.
- [ ] Create deterministic forward-only migrations and generated model snapshots.
- [ ] Register every new DbContext or changed migration in the migration registry.
- [ ] Prove fresh-database migration and zero pending model changes.

## Customer API and My Journey

- [x] Return policy summary, eligibility and authoritative estimate for an account-owned booking.
- [x] Return safe not-found for foreign-account bookings.
- [x] Submit a whole-booking cancellation request using expected version and idempotency key.
- [x] Recover duplicate retries with the existing request result.
- [x] Reject invalid booking states and duplicate active requests safely.
- [x] Recalculate or reject stale entitlement when authoritative state changes.
- [x] Expose requested, under-review, approved, rejected and cancellation-completed states through API projections.
- [x] Expose authorized refund amount/currency and pending, partial, settled or recovery-required state through API projections.
- [ ] Preserve support access using the booking reference in the customer UI.
- [x] Never derive refund amounts in the browser.

## Operator and support API

- [x] Add permission- and operator-scoped cancellation queue.
- [x] Add cancellation case detail composed from module-owned projections.
- [x] Approve or reject using expected version, reason and current entitlement validation.
- [x] Prevent arbitrary operator-entered refund amounts.
- [x] Record actor, operator, reason, policy version, correlation and outcome.
- [x] Add explicit Booking-, Inventory- and Payments-owned recovery commands where needed.
- [ ] Integrate recovery-required cases into VS-14 without direct state editing.
- [x] Return safe not-found for foreign-operator resources.

## Booking, inventory and refund orchestration

- [x] Commit booking cancellation exactly once after approval.
- [x] Preserve booking, commercial snapshot, travellers, documents, visa cases and history.
- [x] Release committed inventory through the Inventory-owned command exactly once.
- [x] Record inventory recovery-required state without duplicating release.
- [x] Authorize refund from authoritative settled-payment facts and approved entitlement.
- [x] Submit provider refund idempotently when provider execution is explicitly enabled.
- [ ] Verify provider callback authenticity and idempotency.
- [x] Preserve original settled payment facts when refund succeeds or fails.
- [x] Support provider-delayed, partial, failed and reconciliation-required domain outcomes.
- [x] Keep booking cancellation and refund settlement as separate customer-visible facts.

## Security, privacy, audit and telemetry

- [x] Enforce booking-owner isolation server-side.
- [x] Enforce operator membership, permission and operator scope server-side.
- [x] Bound and protect customer reason notes.
- [x] Exclude tokens, provider payloads, payment secrets, traveller data and document data from logs.
- [x] Use opaque identifiers in logs and metrics.
- [x] Audit every request, decision, cancellation, inventory release, refund authorization, provider submission and recovery action.
- [x] Add privacy-safe telemetry for eligibility, stale state, duplicate recovery, provider latency/failure and reconciliation.
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

## Navigation and reachability

- [x] Add the permanent navigation verification standard at `docs/06-engineering/NAVIGATION-VERIFICATION-GATE.md`.
- [x] Create the VS-16 route matrix at `docs/slices/VS-16-NAVIGATION-VERIFICATION.md`.
- [ ] Link the customer cancellation experience from the existing My Journey booking flow.
- [ ] Link the operator cancellation queue from the existing operator workspace.
- [ ] Link cancellation/refund exceptions from Operational Support to the exact actionable destination.
- [ ] Verify all new pages and sections by clicking from their intended source; direct URL tests alone are insufficient.
- [ ] Verify breadcrumbs, back-navigation, refresh/deep links and generated API action targets.
- [ ] Verify desktop and mobile navigation, including responsive or collapsed controls.
- [ ] Verify authorized, forbidden-role, unauthenticated, foreign-account and foreign-operator navigation outcomes.
- [ ] Mark every identity-restricted path as `BLOCKED_IDENTITY` with required identity/permission, attempted evidence and follow-up action.
- [ ] Resolve every `PENDING` and `FAILED` row before exact-head certification.
- [ ] Summarize the completed navigation matrix and any `BLOCKED_IDENTITY` rows in PR #70 before Product Owner approval.

## Automated verification

- [x] Domain tests cover core allowed and forbidden cancellation transitions.
- [x] Policy tests cover representative cancellation windows, cutoff and fee caps.
- [ ] Policy tests cover every approved cancellation window and boundary timestamp after production values are approved.
- [ ] Money tests prove explicit currency, rounding and reconciliation.
- [ ] Idempotency tests cover duplicate customer requests, approvals, inventory releases, provider submissions and callbacks.
- [ ] Concurrency tests cover stale booking, cancellation request and refund state.
- [ ] Integration tests verify account isolation and operator isolation.
- [ ] Integration tests verify booking history preservation.
- [ ] Integration tests verify append-only payment/refund facts.
- [ ] Migration registry and fresh-database tests pass.
- [ ] Playwright covers customer request and status tracking on desktop and mobile.
- [ ] Playwright covers operator review, stale recovery and permission denial.
- [ ] Playwright proves click-through navigation from My Journey, Operator workspace and Operational Support.
- [ ] Accessibility checks cover serious/critical axe findings, keyboard, focus, target size and reflow.
- [ ] Certification evidence is generated on the exact unchanged head SHA.

## Release controls

- [ ] Provider refund credentials are configured only in the approved environment.
- [ ] Production policy version is explicitly approved.
- [x] Production refund execution remains disabled until security and operational readiness are signed off.
- [ ] Rollback preserves cancellation and refund facts and cannot recreate released inventory silently.
- [ ] Operational runbook covers provider delay, duplicate callback, partial refund, inventory-release failure and reconciliation.
- [ ] Product Owner approves the exact certified runtime SHA before merge.
- [ ] Production deployment requires a separate explicit approval after merge.
