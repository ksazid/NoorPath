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
- [x] Define customer-safe and operator-safe status mappings.
- [x] Define reason-code vocabulary without free-form internal enum leakage.
- [x] Define expected-version and idempotency requirements for every mutation.
- [x] Define cross-module contracts and prohibit historical financial editing.
- [x] Confirm no durable architecture decision changed; no ADR required.

## Persistence and migrations

- [x] Add module-owned cancellation request persistence.
- [x] Persist policy version and exact entitlement snapshot used for the decision.
- [x] Add optimistic concurrency tokens and uniqueness for one active request per booking.
- [x] Add idempotency uniqueness for customer request and refund execution.
- [x] Add append-oriented audit/history records.
- [x] Add Payments-owned refund ledger entries without modifying settled payment rows.
- [x] Add provider reference uniqueness where applicable.
- [x] Create deterministic forward-only migrations and generated model snapshots.
- [x] Register changed module contexts in the migration registry.
- [x] Prove fresh-database migration and zero pending model changes during VS-16 certification.

## Customer API and My Journey

- [x] Return policy summary, eligibility and authoritative estimate for an account-owned booking.
- [x] Return safe not-found for foreign-account bookings.
- [x] Submit a whole-booking cancellation request using expected version and idempotency key.
- [x] Recover duplicate retries with the existing request result.
- [x] Reject invalid booking states and duplicate active requests safely.
- [x] Recalculate or reject stale entitlement when authoritative state changes.
- [x] Expose requested, under-review, approved, rejected and cancellation-completed states.
- [x] Expose authorized refund amount/currency and pending, partial, settled or recovery-required state.
- [x] Preserve support access using the booking reference.
- [x] Never derive refund amounts in the browser.

## Operator and support API

- [x] Add permission- and operator-scoped cancellation queue.
- [x] Add cancellation case detail composed from module-owned projections.
- [x] Approve or reject using expected version, reason and current entitlement validation.
- [x] Prevent arbitrary operator-entered refund amounts.
- [x] Record actor, operator, reason, policy version, correlation and outcome.
- [x] Add explicit Booking-, Inventory- and Payments-owned recovery commands where needed.
- [ ] Complete VS-14 Operational Support navigation for cancellation and refund exceptions.
- [x] Return safe not-found for foreign-operator resources.

## Booking, inventory and refund orchestration

- [x] Commit booking cancellation exactly once after approval.
- [x] Preserve booking, commercial snapshot, travellers, documents, visa cases and history.
- [x] Release committed inventory through the Inventory-owned command exactly once.
- [x] Record inventory recovery-required state without duplicating release.
- [x] Authorize refund from authoritative settled-payment facts and approved entitlement.
- [x] Submit provider refund idempotently when execution is explicitly enabled.
- [ ] Verify production provider callback authenticity and idempotency after a provider integration is approved.
- [x] Preserve original settled payment facts when refund succeeds or fails.
- [x] Support provider-delayed, partial, failed and reconciliation-required outcomes.
- [x] Keep booking cancellation and refund settlement as separate customer-visible facts.

## Security, privacy, audit and telemetry

- [x] Enforce booking-owner isolation server-side.
- [x] Enforce operator membership, permission and operator scope server-side.
- [x] Bound and protect customer reason notes.
- [x] Exclude tokens, provider payloads, payment secrets, traveller data and document data from logs.
- [x] Use opaque identifiers in logs and metrics.
- [x] Audit every request, decision, cancellation, inventory release, refund authorization, provider submission and recovery action.
- [x] Add privacy-safe telemetry for eligibility, stale state, duplicate recovery, provider latency/failure and reconciliation.
- [ ] Verify privileged production access remains MFA-protected when production identity configuration is restored.

## Customer and operator experience

- [x] Extend approved NoorPath header, footer, tokens, typography, cards and controls.
- [x] Show policy version and a plain-language entitlement breakdown before confirmation.
- [x] Make whole-booking impact explicit before submission.
- [x] Implement loading, unavailable, ineligible, validation, stale, duplicate, review and outcome states.
- [x] Implement provider-delayed and recovery-required states without implying money is settled.
- [x] Provide safe retry paths that cannot duplicate requests or refunds.
- [x] Meet keyboard, visible-focus, target-size, reflow and reduced-motion requirements in rendered certification.
- [x] Verify desktop and mobile against approved NoorPath visual authority in rendered certification.

## Navigation and reachability

- [x] Add the permanent navigation verification standard at `docs/06-engineering/NAVIGATION-VERIFICATION-GATE.md`.
- [x] Create the VS-16 route matrix at `docs/slices/VS-16-NAVIGATION-VERIFICATION.md`.
- [ ] Prove the customer cancellation section is reached by clicking from `/journeys`, not only by direct route loading.
- [ ] Add and verify an explicit in-page link to the cancellation section.
- [ ] Prove the operator cancellation queue is reached from the Operator Overview and sidebar.
- [ ] Link cancellation/refund exceptions from Operational Support to the actionable cancellation workspace.
- [ ] Verify breadcrumbs, back-navigation, refresh/deep links and API-generated action targets.
- [ ] Verify desktop and mobile navigation, including responsive controls.
- [ ] Verify authorized, forbidden-role, unauthenticated, foreign-account and foreign-operator outcomes.
- [x] Mark production identity-restricted paths as `BLOCKED_IDENTITY` with the required configuration and follow-up action.
- [ ] Resolve every `PENDING` and `FAILED` row before approving the retrospective navigation PR.
- [ ] Summarize the matrix and remaining `BLOCKED_IDENTITY` rows for Product Owner visibility.

## Automated verification

- [x] Domain tests cover allowed and forbidden cancellation transitions.
- [x] Policy tests cover configured windows, cutoff and fee caps.
- [ ] Policy tests cover final production policy boundary timestamps after those values are approved.
- [x] Money tests prove explicit currency, rounding and reconciliation.
- [x] Idempotency tests cover duplicate customer requests, approvals, inventory releases and provider execution.
- [x] Concurrency tests cover stale cancellation request and refund state.
- [x] Integration tests verify account isolation and operator isolation.
- [x] Integration tests verify booking history preservation.
- [x] Integration tests verify append-only payment/refund facts.
- [x] Migration registry and fresh-database tests pass.
- [x] Playwright covers customer request and status tracking on desktop and mobile.
- [x] Playwright covers operator review, stale recovery and permission denial.
- [ ] Playwright proves click-through navigation from My Journey, Operator Overview and Operational Support.
- [x] Accessibility checks cover serious/critical axe findings, target size and reflow.
- [x] VS-16 certification evidence was generated on the exact merged SHA.

## Release controls

- [ ] Provider refund credentials are configured only in an approved environment.
- [ ] Production policy version is explicitly approved.
- [x] Production refund execution remains disabled until security and operational readiness are signed off.
- [x] Rollback preserves cancellation and refund facts and cannot silently recreate released inventory.
- [ ] Operational runbook covers provider delay, duplicate callback, partial refund, inventory-release failure and reconciliation before production execution.
- [x] Product Owner approved the exact certified VS-16 runtime SHA before merge.
- [x] VS-16 merged without production deployment.
