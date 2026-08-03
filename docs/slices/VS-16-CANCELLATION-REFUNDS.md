# VS-16 — Cancellation & Refunds

## Outcome

An authenticated NoorPath booking owner can request cancellation of an eligible confirmed booking, understand the versioned policy and estimated refund entitlement before committing, and follow the governed cancellation and refund outcome without NoorPath erasing or rewriting booking, payment, traveller, inventory or audit history.

Authorized operator staff can review cases that require human approval or recovery, but they must use explicit Booking- and Payments-owned commands rather than editing state directly.

## Requirement and invariant traceability

This slice implements the pilot requirement for cancellation and refund rules and is governed by:

- `INV-BKG-003` — booking state is explicit;
- `INV-BKG-004` — consequential booking transitions are permissioned;
- `INV-BKG-005` — cancellation and overrides are auditable;
- `INV-BKG-007` — cancellation preserves history;
- `POL-BKG-002` — cancellation entitlement is an explicit versioned policy;
- `INV-PAY-002` — refund processing is idempotent;
- `INV-PAY-003` — financial history is append-only;
- `INV-PAY-005` — refunds are attributable to a booking and provider reference;
- `INV-PAY-006` — balance derives from authoritative financial facts;
- `INV-PAY-007` — refunds do not erase payments;
- `INV-INV-005` and `INV-INV-006` — inventory commitment/release remains exact and auditable;
- `INV-ADM-001` to `INV-ADM-003` — privileged actions do not bypass domain rules or rewrite history.

## Product boundary

VS-16 supports **whole-booking cancellation only**. It does not introduce partial traveller cancellation, traveller substitution, occupancy changes, vouchers, loyalty credit, chargeback handling or supplier-refund integrations.

The cancellation policy is not a UI constant. It is a validated, versioned commercial policy attached to the booking snapshot or an immutable referenced policy version. The server is the sole authority for eligibility, fees and refundable amount.

No production refund execution may be enabled until the Product Owner approves the policy values and the configured payment provider is verified for refund operations.

## Domain ownership

- **Booking** owns:
  - cancellation request lifecycle;
  - eligibility against booking state and fulfilment milestones;
  - the immutable cancellation-policy version used;
  - approved booking cancellation transition;
  - rejection and withdrawal where allowed.
- **Payments** owns:
  - settled-payment facts;
  - refundable financial basis;
  - refund authorization and execution lifecycle;
  - provider refund references;
  - append-only refund ledger and reconciliation.
- **Inventory** owns:
  - releasing committed inventory exactly once after an approved booking cancellation;
  - audit evidence for release or recovery-required outcomes.
- **My Journey** consumes a customer-safe projection from Booking and Payments.
- **Operational Support** composes exceptions but invokes only module-owned commands.
- **Accounts / Operator Access** own authentication, booking-owner isolation, operator membership, permissions and operator scope.

No module may query or mutate another module's tables directly.

## Core model

### CancellationPolicyVersion

Minimum immutable facts:

- `PolicyVersionId`
- operator/departure/package applicability as approved
- governing timezone
- effective-from timestamp
- ordered cancellation windows
- fee calculation rule for each window
- refundable/non-refundable component rules
- approval mode for each window
- fulfilment milestone restrictions
- customer-facing summary
- published actor and timestamp

A policy version referenced by a booking cannot be edited. Corrections create a new version and do not silently change existing commitments.

### CancellationRequest

- `CancellationRequestId`
- `BookingId`
- `AccountId`
- `OperatorId`
- `PolicyVersionId`
- `BookingVersionAtRequest`
- customer-selected reason code and optional bounded note
- `RequestedAtUtc`
- status
- optimistic concurrency `Version`
- idempotency key
- latest customer-safe outcome code

Initial lifecycle:

- `Requested`
- `UnderReview`
- `Approved`
- `Rejected`
- `Withdrawn`
- `CancellationCommitted`
- `RecoveryRequired`

`Approved` is not equivalent to a settled refund. Booking cancellation, inventory release and refund execution remain explicit downstream facts.

### CancellationEntitlement

An immutable calculation snapshot containing:

- booking and policy version identifiers;
- calculation timestamp;
- governing timezone and departure-relative window;
- explicit currency;
- agreed booking total;
- settled amount eligible for refund consideration;
- fee components and reason codes;
- non-refundable components and reason codes;
- maximum refundable amount;
- approval mode;
- expiry or recalculation condition where applicable.

The entitlement must reconcile to authoritative booking and payment facts. The web client only renders the server result.

### RefundInstruction / RefundRecord

Payments-owned facts:

- `RefundInstructionId`
- `BookingId`
- `CancellationRequestId`
- explicit currency and amount
- authorization actor/time or automated-policy evidence
- provider and provider refund reference where applicable
- idempotency key
- state
- timestamps
- failure/recovery code
- append-only ledger reference
- optimistic concurrency version where mutable operational state exists

Initial lifecycle:

- `Authorized`
- `Submitted`
- `ProviderPending`
- `PartiallySettled`
- `Settled`
- `Failed`
- `RecoveryRequired`

A refund failure does not revert or erase the original settled payment.

## Required flows

### View cancellation policy and estimate

From My Journey, the booking owner can open a cancellation view. The server verifies account ownership and returns:

- current booking eligibility;
- policy version and customer-facing summary;
- exact governing departure time/timezone;
- explainable estimated entitlement;
- whether human review is required;
- material milestones that affect eligibility;
- a warning that the estimate may require recalculation if authoritative state changes before submission.

### Submit cancellation request

The customer confirms the whole-booking request and provides an approved reason code. The command requires:

- account-owned booking;
- eligible booking state;
- expected booking version;
- current entitlement/policy reference;
- idempotency key.

Duplicate retries return the existing result. A second active request is rejected safely.

### Review and decide

Where policy requires review, authorized operator staff see an operator-scoped queue and case detail. The decision command requires:

- permission and operator scope;
- expected cancellation-request version;
- current entitlement revalidation;
- approved decision reason;
- actor and correlation context.

The operator cannot type an arbitrary refund amount. Any permitted adjustment must be an explicit future policy/override capability with separate authorization and audit; it is excluded from VS-16.

### Commit booking cancellation

After approval, Booking performs one idempotent lifecycle transition to cancelled and records the policy/entitlement snapshot. It preserves the original booking, commercial snapshot, travellers, documents, visa cases and history.

### Release inventory

Booking invokes the Inventory-owned release command after cancellation is committed. Inventory releases the reservation exactly once or records a recoverable exception. Inventory failure does not cause direct table edits or duplicate release attempts.

### Authorize and execute refund

Payments revalidates the authoritative settled amount and creates an append-only refund instruction from the approved entitlement. Provider submission and callbacks are signature-verified and idempotent. Provider-delayed and failed outcomes remain visible to operations and customers using safe language.

### My Journey projection

My Journey exposes:

- cancellation eligibility before request;
- request and review state;
- approved/rejected reason summary where safe;
- booking cancellation completion;
- refund amount/currency after authorization;
- refund pending, partial, settled or recovery-required state;
- support entry using the booking reference.

It must not expose provider payloads, internal fraud markers, staff-only notes or private financial metadata.

### Operational support

VS-14 may surface cancellation/refund exceptions and retry-safe recovery commands. It must not gain direct write access to Booking, Inventory or Payments storage.

## Minimum API contract

Account-scoped customer endpoints:

- `GET /api/v1/bookings/{bookingId}/cancellation`
- `POST /api/v1/bookings/{bookingId}/cancellation-requests`
- `GET /api/v1/bookings/{bookingId}/cancellation-requests/{requestId}`
- `POST /api/v1/bookings/{bookingId}/cancellation-requests/{requestId}/withdraw` only if the approved policy permits withdrawal before decision

Operator-scoped endpoints:

- `GET /api/v1/operator/cancellations`
- `GET /api/v1/operator/cancellations/{requestId}`
- `POST /api/v1/operator/cancellations/{requestId}/approve`
- `POST /api/v1/operator/cancellations/{requestId}/reject`
- explicit recovery commands only where owned by Booking, Inventory or Payments

Foreign-account and foreign-operator resources return safe not-found responses. Mutations require expected versions and idempotency keys where retries could duplicate effects.

## Security, privacy and audit

- Booking ownership is enforced server-side for every customer read and mutation.
- Operator permission and operator scope are enforced server-side for every queue, case and decision.
- Customer reason notes are bounded, private and excluded from ordinary telemetry.
- Logs use opaque booking, request and refund identifiers only.
- Payment provider payloads, access tokens, traveller data, passport/document data and secrets are never logged.
- Every consequential action records actor/system identity, action, resource, timestamp, reason/outcome, correlation identifier and policy version.
- Privileged production access remains MFA-protected.

## Accessibility and experience

Customer and operator flows must extend the approved NoorPath visual language. Required states include:

- loading;
- eligible with estimate;
- ineligible with truthful reason;
- policy unavailable;
- validation error;
- stale booking or request version;
- duplicate request recovery;
- under review;
- approved;
- rejected;
- cancellation committed;
- refund pending;
- partial refund;
- refunded;
- provider delayed;
- recovery required;
- permission denied;
- offline/service error with safe retry.

Changed surfaces must pass keyboard operation, focus visibility, target size, 200% text, mobile reflow, reduced motion and serious/critical accessibility checks.

## Policy decisions required before runtime implementation

The Product Owner must approve these values before the slice becomes Definition-of-Ready for runtime code:

1. Cancellation windows relative to departure date/time and the governing timezone.
2. Fee calculation for each window.
3. Refundable and non-refundable components, including provider fees and already-incurred supplier costs.
4. Whether customer requests always require operator approval or may auto-approve in a defined safe window.
5. The cutoff and treatment after ticketing, visa submission, visa decision or other material fulfilment milestones.
6. The customer-facing refund processing expectation and escalation threshold.
7. Operator-initiated cancellation policy and whether it follows a distinct entitlement rule.
8. Whether policy varies per operator, departure or package and who may publish a policy version.

Until these are approved, implementation may establish contracts and test fixtures only; it must not invent monetary values or production behaviour.

## Explicit exclusions

- Partial cancellation of selected travellers.
- Traveller substitution or occupancy amendment.
- Chargebacks, disputes, fraud investigations or card-network arbitration.
- Arbitrary operator-entered refund amounts.
- Supplier, airline, hotel or insurance refund integrations.
- Vouchers, credits, loyalty points, referrals or goodwill compensation.
- Cancellation of quotes, expired holds or abandoned checkout attempts.
- Production provider refunds before credentials, policy approval and production security review.

## Merge rule

The planning PR remains Draft until the slice boundary, policy decision list, dependencies, ownership and acceptance criteria are accepted by the Product Owner. Runtime implementation must use a separate Draft PR and remains unmerged until exact-head CI, migration, financial reconciliation, idempotency, concurrency, authorization, rendered accessibility and Product Owner gates pass.
