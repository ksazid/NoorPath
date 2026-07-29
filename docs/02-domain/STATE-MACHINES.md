# NoorPath V2 State Machines

Status: Draft baseline
Version: 0.1
Step: 8

## Purpose

This document defines the lifecycle states, valid transitions, guards, ownership, audit expectations and exceptional paths for NoorPath's core MVP domains.

A state machine is authoritative only for the capability that owns it. Booking state does not replace Payment, Document or Visa state; Journey Readiness is derived from those independent machines.

## Global Rules

1. State transitions occur only through named domain actions; direct status-field mutation is invalid.
2. Every consequential transition defines actor/system authority, guard conditions and audit/event consequences.
3. Invalid transitions fail explicitly; no silent state correction.
4. Duplicate/retried commands and provider events must be idempotent where applicable.
5. Historical/terminal states are never deleted to recreate a cleaner history.
6. Customer-facing labels may simplify internal states, but must never overstate certainty.
7. Timed transitions such as expiry or cutoff use an explicit timezone/time source.
8. Cross-capability transitions are coordinated through contracts/events, not cross-table mutation.
9. Exception states exist only where normal distributed workflows can genuinely fail after a consequential fact has already occurred.
10. Policy values such as hold duration, cancellation eligibility or payment grace periods remain governed configuration/decisions rather than hard-coded state-machine assumptions.

---

# 1. Operator Lifecycle

Owner: Operators

## States

- `Draft` — operator record is being created/configured and cannot sell.
- `PendingApproval` — required onboarding facts are ready for NoorPath review.
- `Approved` — operator is eligible to participate in publication/sale subject to package/departure rules.
- `Rejected` — approval was declined; no public sale is permitted.
- `Suspended` — selling/operating privileges are temporarily restricted while historical commitments remain intact.
- `Deactivated` — operator is no longer active for new commercial activity; history is retained.

## Principal Transitions

- `Draft -> PendingApproval` via `SubmitOperatorForApproval`
  - Guard: required MVP operator facts complete.
- `PendingApproval -> Approved` via `ApproveOperator`
  - Actor: authorized NoorPath platform role.
  - Effect: operator becomes publication-eligible.
- `PendingApproval -> Rejected` via `RejectOperator`
  - Requires reason/audit.
- `Rejected -> Draft` via `ReopenOperatorApplication`
  - Allows correction without deleting rejection history.
- `Approved -> Suspended` via `SuspendOperator`
  - Requires reason/audit.
  - Existing bookings are not deleted or silently cancelled.
- `Suspended -> Approved` via `ReactivateOperator`
  - Requires authorized review.
- `Draft|Rejected|Approved|Suspended -> Deactivated` via governed deactivation when allowed.

## Events

`OperatorSubmittedForApproval`, `OperatorApproved`, `OperatorRejected`, `OperatorSuspended`, `OperatorReactivated`, `OperatorDeactivated`

## Open Policy

Behaviour of active bookings and published departures after operator suspension remains a governed launch decision.

---

# 2. Package Version Lifecycle

Owner: Catalogue

## States

- `Draft` — mutable authoring state.
- `ReadyForReview` — author believes required commercial content is complete.
- `Approved` — content has passed the required publication approval policy but is not yet public.
- `Published` — available as an approved commercial package version for eligible departures.
- `Superseded` — replaced by a newer version for new publication/use; historical references remain valid.
- `Archived` — retained for history and not available for new publication.

## Principal Transitions

- `Draft -> ReadyForReview` via `SubmitPackageVersion`
  - Guard: required content present; pending facts explicitly labelled.
- `ReadyForReview -> Draft` via `ReturnPackageForChanges`
  - Requires review feedback/reason.
- `ReadyForReview -> Approved` via `ApprovePackageVersion`
  - Actor depends on the frozen publication policy.
- `Approved -> Published` via `PublishPackageVersion`
  - Guard: owning Operator is `Approved`; required package validation passes.
- `Published -> Superseded` when a newer package version becomes authoritative for new commercial use.
- `Draft|ReadyForReview|Approved|Superseded -> Archived` where no active commitment requires new use.

## Immutability Rule

Once `Published`, material commercial content is not edited in place. A change creates another version.

## Events

`PackageVersionSubmitted`, `PackageVersionApproved`, `PackageVersionPublished`, `PackageVersionSuperseded`, `PackageVersionArchived`

---

# 3. Departure Batch Lifecycle

Owner: Catalogue

## States

- `Draft` — departure is being configured.
- `ReadyForPublication` — required departure facts are complete and references are valid.
- `Published` — publicly discoverable/bookable when Pricing and Inventory also permit sale.
- `Paused` — temporarily not accepting new bookings.
- `Closed` — no longer accepting new bookings due to cutoff, operational closure or completed sales window.
- `Cancelled` — departure will not operate as originally committed; existing bookings require governed handling.
- `Archived` — historical record retained after commercial/operational lifecycle ends.

## Principal Transitions

- `Draft -> ReadyForPublication` via `ValidateDepartureForPublication`
  - Guards: valid PackageVersion, eligible Operator, required dates/origin/cutoff facts.
- `ReadyForPublication -> Published` via `PublishDeparture`
  - Guard: valid public PriceVersion and required Inventory setup exist for saleable occupancies.
- `Published -> Paused` via `PauseDeparture`
  - New bookings blocked; existing commitments remain.
- `Paused -> Published` via `ResumeDeparture`
  - Guards revalidated before sale resumes.
- `Published|Paused -> Closed` via cutoff/operational close.
- `Draft|ReadyForPublication|Published|Paused|Closed -> Cancelled` only through authorized cancellation workflow.
- `Closed|Cancelled -> Archived` when retention/operational conditions permit.

## Important Rule

`Published` does not alone mean purchasable. Public sellability is composed from Operator eligibility + Catalogue publication + valid Pricing + Inventory availability + booking cutoff.

## Events

`DeparturePublished`, `DeparturePaused`, `DepartureResumed`, `DepartureClosed`, `DepartureCancelled`, `DepartureArchived`

---

# 4. Price Version and Quote Lifecycle

Owner: Pricing

## PriceVersion States

- `Draft` — editable PricePlan work before a version is issued.
- `Published` — immutable version available for valid quotes during its effective period.
- `Superseded` — newer version is preferred for new quotes; existing quoted/booked references remain valid according to policy.
- `Retired` — no new quotes may use the version.

### Rule

A published PriceVersion is never edited to change existing customer commitments.

## Quote States

- `Valid` — calculation is currently usable subject to availability and expiry rules.
- `Accepted` — customer committed to the quote and Booking captured immutable evidence.
- `Expired` — validity window ended.
- `Invalidated` — underlying policy permits invalidation before commitment, for example when required sale conditions materially change.

## Quote Transitions

- quote creation -> `Valid`
- `Valid -> Accepted` only when Booking creation/commitment succeeds using the exact quote/version evidence.
- `Valid -> Expired` by authoritative time rule.
- `Valid -> Invalidated` only under explicit price/change policy.

Accepted quotes are never repriced in place.

---

# 5. Inventory Hold and Reservation Lifecycle

Owner: Inventory

## InventoryHold States

- `Active` — capacity is temporarily withheld from available inventory.
- `Reserved` — hold was converted exactly once into confirmed Reservation.
- `Released` — capacity explicitly returned before expiry.
- `Expired` — validity elapsed and capacity returned.

## Principal Transitions

- creation -> `Active`
  - Guard: atomic capacity check/acquisition succeeds.
- `Active -> Reserved` via `ReserveHeldInventory`
  - Guard: hold is still valid, not previously converted, and booking correlation matches.
- `Active -> Released` via `ReleaseInventoryHold`
- `Active -> Expired` via authoritative expiry processing.

`Reserved`, `Released` and `Expired` are terminal for the hold. A new attempt creates a new hold.

## Reservation States

- `Active` — inventory is committed to a Booking.
- `Released` — commitment was explicitly released due to a valid business transition such as cancellation.

Reservation release must be idempotent and auditable.

## Concurrency Rule

Hold creation, expiry/release and conversion to Reservation must prevent oversell under concurrent requests.

## Events

`InventoryHeld`, `InventoryHoldReleased`, `InventoryHoldExpired`, `InventoryReserved`, `InventoryReservationReleased`

---

# 6. Booking Lifecycle

Owner: Booking

## States

- `PendingPayment` — customer has committed to the booking request, commercial evidence is captured and the booking awaits required payment outcome/confirmation prerequisites.
- `Confirmed` — commercial booking is accepted and required inventory has been successfully reserved based on trusted payment/booking rules.
- `ConfirmationException` — a consequential prerequisite such as payment settlement occurred but normal confirmation could not be completed, requiring governed reconciliation.
- `Cancelled` — booking has been cancelled under an authorized policy/workflow.
- `Expired` — an unconfirmed booking opportunity ended without a valid confirmation, for example after hold/quote/payment window expiry where policy allows.
- `Completed` — journey/booking lifecycle has concluded operationally.

## Creation Preconditions

A Booking may be created only from:
- valid published DepartureBatch,
- accepted/current Quote and PriceVersion evidence,
- valid supported occupancy,
- one Booking Owner,
- at least one Traveller,
- valid InventoryHold where the MVP flow requires one.

## Principal Transitions

- creation -> `PendingPayment`
  - Captures immutable commercial snapshot and version references.
- `PendingPayment -> Confirmed` via `ConfirmBooking`
  - Guards: trusted payment rule satisfied, Inventory reservation succeeds, booking is not expired/cancelled, versions/snapshot match the committed offer.
- `PendingPayment -> Expired`
  - Guard: confirmation deadline/hold/payment policy reached and no authoritative settled-payment exception prevents expiry.
- `PendingPayment -> Cancelled` when customer/operator cancellation policy explicitly allows pre-confirmation cancellation.
- `PendingPayment -> ConfirmationException`
  - Used only when a consequential external/financial fact already occurred but confirmation cannot be safely completed, e.g. verified payment settlement after inventory can no longer be reserved.
- `ConfirmationException -> Confirmed` after authorized successful recovery/reconciliation.
- `ConfirmationException -> Cancelled` only with the corresponding financial/operational compensation process.
- `Confirmed -> Cancelled` via governed cancellation workflow.
- `Confirmed -> Completed` after operational journey completion criteria.

## Important Rules

- Payment status, document readiness and visa state are not Booking states.
- `Confirmed` never results from a browser success callback alone.
- Cancellation preserves booked snapshot, traveller relationships, payment history and audit history.
- Arbitrary admin status edits are prohibited.

## Events

`BookingCreated`, `BookingAwaitingPayment`, `BookingConfirmed`, `BookingConfirmationExceptionRaised`, `BookingExpired`, `BookingCancelled`, `BookingCompleted`

---

# 7. Payment Lifecycle

Owner: Payments

## States

- `Created` — payment obligation/process exists but has not yet been submitted to provider.
- `PendingProvider` — provider flow has been initiated and final trusted outcome is not known.
- `Settled` — authoritative provider/reconciliation evidence confirms settlement.
- `Failed` — trusted outcome confirms payment failed.
- `Cancelled` — payment attempt/intent was cancelled before settlement where provider/model permits it.
- `ReconciliationRequired` — provider evidence is ambiguous, conflicting, missing beyond expected timing, or otherwise requires operational resolution.

## Principal Transitions

- `Created -> PendingProvider` via provider initiation.
- `PendingProvider -> Settled` only from verified provider evidence/reconciliation.
- `PendingProvider -> Failed` from trusted failure evidence.
- `PendingProvider -> Cancelled` where valid.
- `PendingProvider -> ReconciliationRequired` for unresolved ambiguity/timeout/conflict.
- `ReconciliationRequired -> Settled|Failed|Cancelled` through trusted reconciliation evidence.

A `Settled` payment is not edited back to `Failed`; subsequent correction/refund uses append-only financial facts.

## Events

`PaymentInitiated`, `PaymentSettled`, `PaymentFailed`, `PaymentCancelled`, `PaymentReconciliationRequired`

---

# 8. Refund Lifecycle

Owner: Payments

## States

- `Requested` — refund request exists under a booking/cancellation/business reason.
- `Authorized` — refund has passed required policy/approval checks.
- `Submitted` — sent to payment provider or trusted refund process.
- `Settled` — provider/reconciliation confirms funds returned.
- `Rejected` — refund request was not authorized/permitted.
- `Failed` — provider/process returned a definitive failure.
- `ReconciliationRequired` — result is ambiguous and requires investigation.

## Principal Transitions

- `Requested -> Authorized|Rejected`
- `Authorized -> Submitted`
- `Submitted -> Settled|Failed|ReconciliationRequired`
- `ReconciliationRequired -> Settled|Failed`

Refund settlement creates append-only ledger evidence and never removes the original payment.

## Events

`RefundRequested`, `RefundAuthorized`, `RefundSubmitted`, `RefundSettled`, `RefundRejected`, `RefundFailed`, `RefundReconciliationRequired`

---

# 9. Document Submission Lifecycle

Owner: Documents

## States

- `Uploaded` — file accepted into private intake but not yet trusted.
- `Validating` — MIME/signature/size/malware and required safety checks are running.
- `UnsafeOrInvalid` — file failed mandatory safety/format validation and cannot enter reviewer workflow.
- `UnderReview` — safe submission is available to an authorized reviewer.
- `Approved` — submission satisfies the relevant requirement.
- `CorrectionRequired` — reviewer requests a replacement/correction.
- `Rejected` — submission cannot satisfy the requirement under current evidence/policy.

## Principal Transitions

- creation -> `Uploaded`
- `Uploaded -> Validating`
- `Validating -> UnderReview` when all mandatory safety checks pass.
- `Validating -> UnsafeOrInvalid` when a mandatory control fails.
- `UnderReview -> Approved|CorrectionRequired|Rejected`

## Resubmission Rule

A correction/rejection does not rewrite the old submission. A new `DocumentSubmission` is created against the same TravellerDocumentRequirement, preserving the previous submission/review chain.

## Concurrency Rule

Review commands must detect when a newer submission has become authoritative so an obsolete file cannot be accidentally approved.

## Events

`DocumentUploaded`, `DocumentValidated`, `DocumentRejectedAsUnsafe`, `DocumentSubmittedForReview`, `DocumentApproved`, `DocumentCorrectionRequired`, `DocumentRejected`

---

# 10. Visa Case Lifecycle

Owner: Visa

The MVP vocabulary below is the baseline workflow. Provider/authority-specific sub-statuses may be added later without changing customer truth semantics.

## Internal States

- `Preparing` — case exists but required evidence/readiness is incomplete.
- `ReadyForReview` — required NoorPath/operator evidence is present for pre-submission review.
- `UnderReview` — authorized operational review is in progress.
- `ReadyForSubmission` — internal review is complete and case is ready to submit to the applicable process/provider.
- `Submitted` — case has been submitted with traceable evidence/reference where available.
- `Processing` — submitted case is awaiting authoritative outcome or next action.
- `ActionRequired` — additional/corrected information or intervention is required.
- `Approved` — authoritative evidence supports an approved visa outcome.
- `Rejected` — authoritative evidence supports a rejected visa outcome.
- `Cancelled` — case was intentionally stopped under a valid workflow.

## Principal Transitions

- creation -> `Preparing`
- `Preparing -> ReadyForReview` when defined prerequisites are satisfied.
- `ReadyForReview -> UnderReview`
- `UnderReview -> ReadyForSubmission|ActionRequired`
- `ActionRequired -> Preparing|ReadyForReview` after required correction/evidence.
- `ReadyForSubmission -> Submitted` only with an authorized submission action and traceable evidence/reference where available.
- `Submitted -> Processing|ActionRequired`
- `Processing -> Approved|Rejected|ActionRequired`
- non-terminal active state -> `Cancelled` only through governed cancellation.

## Customer-Facing Projection

Internal states map to simpler customer states:

- `Preparing`, `ReadyForReview`, `UnderReview`, `ReadyForSubmission` -> `Preparing`
- `Submitted` -> `Submitted`
- `Processing` -> `Processing`
- `ActionRequired` -> `Action Required`
- `Approved` -> `Approved`
- `Rejected` -> `Not Approved`
- `Cancelled` -> `Cancelled`

The customer label `Approved` is permitted only when authoritative evidence justifies it; NoorPath operational progress must never be represented as authority approval.

## Events

`VisaCaseCreated`, `VisaReadyForReview`, `VisaReadyForSubmission`, `VisaSubmitted`, `VisaProcessing`, `VisaActionRequired`, `VisaApproved`, `VisaRejected`, `VisaCaseCancelled`

---

# 11. Journey Readiness — Derived Model, Not a State Machine

Owner: none as independent mutable truth; Journey experience composes authoritative capabilities.

Journey Readiness is derived from at least:

- Booking lifecycle
- payment obligations/balance
- Traveller completeness
- Document requirements/readiness
- VisaCase states where applicable
- operational departure/package prerequisites

Recommended customer projection:

- `Action Required`
- `In Progress`
- `Ready`
- `Blocked`
- `Completed`

These are derived labels. Staff must never manually set a booking to `Ready` to bypass incomplete payment/document/visa truth.

---

# 12. Cross-Machine Coordination Rules

## Payment settlement and Booking confirmation

`PaymentSettled` does not directly mutate Booking persistence. Booking consumes trusted outcome through an explicit contract/event and verifies its own confirmation prerequisites.

## Booking cancellation and Inventory

Booking cancellation requests/requires Inventory reservation release according to the approved cancellation workflow. Failure to release inventory must be operationally visible/retryable without erasing the Booking cancellation fact.

## Booking cancellation and Refund

Cancellation and refund are separate facts. A Booking can be `Cancelled` while Refund is `Requested`, `Submitted`, `Settled`, etc. Customer UX must show both independently.

## Documents and Visa

`DocumentApproved` contributes to document readiness. Visa may consume readiness/evidence but cannot rewrite Document state.

## Notifications

All meaningful transitions may trigger Notification requests. Notification failure never reverses the business transition.

## Audit

Consequential manual transitions emit audit evidence containing actor, action, target, time and reason where required.

---

# 13. Critical Race Conditions to Test

1. Two customers acquiring the final inventory simultaneously.
2. Inventory hold expiring while payment settlement arrives.
3. Duplicate/out-of-order payment webhooks.
4. Cancellation racing with payment settlement.
5. Cancellation racing with inventory confirmation.
6. Departure paused/cancelled while checkout is in progress.
7. Quote expiring while customer initiates booking/payment.
8. Package/PriceVersion superseded while customer is viewing old content.
9. Document resubmission while reviewer has the prior file open.
10. Visa status update duplicated or received out of order.
11. Operator suspension while packages remain published.
12. Manual admin command racing with automated transition.

Each race must eventually have a deterministic test/evidence strategy before its capability reaches Definition of Ready.

---

# 14. Open Policy Decisions Remaining After Step 8

The state structure is defined, but the following values/authority rules remain intentionally unresolved:

1. Exact actor/approval authority for PackageVersion approval/publication.
2. Operator-suspension behaviour for already-published departures and active bookings.
3. Checkout hold duration.
4. Quote validity duration and exact invalidation rules.
5. Booking confirmation/payment deadline rules.
6. Booking cutoff/timezone rules.
7. Cancellation eligibility and cancellation-to-refund policy.
8. Payment late/grace/retry rules for instalments.
9. Refund approval authority and partial-refund scope for MVP.
10. Exact document requirement sets and retention periods.
11. Exact evidence source required for Visa `Approved`/`Rejected`.
12. Completion criteria for Booking `Completed`.

These block the affected implementation when Definition of Ready requires them; they are not defaults for engineers or agents to invent.

---

# 15. State-Machine Implementation Contract for Later Engineering

For each machine, implementation must eventually provide:

- explicit state type owned by its module
- named transition commands/use cases
- guard/precondition validation
- optimistic concurrency or equivalent where races matter
- idempotency for retried external/system actions
- transition audit where consequential
- domain/integration event after successful commit where required
- tests for every allowed transition
- tests for every important prohibited transition
- tests for race/duplicate/out-of-order cases
- mapping from internal states to customer/admin UX labels
- observability for exception/reconciliation states

No controller, UI, SQL script or admin tool may bypass the owning domain transition rules.
