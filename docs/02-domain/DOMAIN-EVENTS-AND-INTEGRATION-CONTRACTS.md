# NoorPath V2 Domain Events & Integration Contracts

Status: Draft baseline
Version: 0.1
Step: 9

## Purpose

This document defines how NoorPath capabilities communicate while preserving module ownership, transaction integrity, idempotency, auditability, privacy and future scalability.

It defines logical contracts and event semantics. It does not yet select a message broker, transport, serializer, queue topology or external integration provider.

Core rule:

> A capability owns its state. Other capabilities request behaviour through explicit contracts and react to committed facts through events; they never mutate another capability's persistence directly.

## 1. Communication Types

- **Command** — asks one owning capability to attempt a named behaviour.
- **Query / Read Contract** — returns purpose-specific facts or projections without changing state.
- **Domain Event** — internal fact raised after a successful state change inside the owning capability.
- **Integration Event** — stable cross-capability representation of a committed fact.

Events are past-tense facts, not disguised commands.

## 2. Global Contract Rules

1. No direct cross-module table writes.
2. No shared ORM/persistence entities as contracts.
3. Every command has one authoritative owner.
4. Every event has one authoritative producer.
5. Consumers cannot strengthen the meaning of producer facts.
6. Events carry only required identifiers/immutable facts, not full aggregates.
7. Sensitive document/payment/authentication secrets never appear in events.
8. Integration contracts are versioned independently of internal domain classes.
9. Consumers must be idempotent under duplicate delivery.
10. Global ordering is never assumed; aggregate/resource versioning is used when order matters.
11. Correlation/causation identifiers are carried for tracing.
12. External callbacks become NoorPath facts only after authenticity and domain validation.
13. Business truth commits before secondary effects.
14. Consumer failure cannot rewrite producer truth.

## 3. Standard Integration Envelope

Logical event metadata:
- EventId
- EventType
- EventVersion
- OccurredAtUtc
- ProducerCapability
- AggregateType
- AggregateId
- AggregateVersion/sequence where required
- CorrelationId
- CausationId where applicable
- OperatorId only where legitimately needed
- purpose-specific payload

Event payloads are never treated as authorization evidence by themselves.

## 4. Identity & Operators

### Read contract: OperatorPublicationEligibility
Producer: Operators
Consumers: Catalogue, Admin composition

Returns only operator identity, eligibility result and relevant version/timestamp.

### Integration events
- OperatorApproved
- OperatorSuspended
- OperatorReactivated

Suspension does not silently cancel bookings or departures; downstream behaviour follows governed policy.

## 5. Catalogue

### Read contract: PublishedDepartureView
Producer: Catalogue
Consumers: Discovery, Pricing, Inventory, Booking

Contains the minimum sale-relevant package/departure facts needed by the consumer.

### Integration events
- PackageVersionPublished
- DeparturePublished
- DeparturePaused
- DepartureResumed
- DepartureClosed
- DepartureCancelled

Departure cancellation communicates Catalogue truth; booking/refund handling remains owned downstream workflow.

## 6. Pricing

### Command/query: CreateQuote
Owner: Pricing

Returns exact PriceVersion evidence, total/currency, due-now, remaining schedule, fee/tax disclosure and validity evidence.

### Query: ValidateQuote
Used by Booking before commitment where required.

### Events
- PriceVersionPublished
- QuoteExpired only if asynchronous expiry is actually required

No event is added merely for architectural ceremony.

## 7. Inventory

### Command: AcquireInventoryHold
Atomic capacity acquisition; returns HoldId and authoritative expiry on success.

### Command: ReserveHeldInventory
Converts a valid hold to Reservation exactly once and is idempotent.

### Commands
- ReleaseInventoryHold
- ReleaseReservation

### Query: AvailabilityProjection
Truthful customer/admin availability projection without unnecessary internal quantities.

### Events
- InventoryHeld
- InventoryHoldExpired
- InventoryHoldReleased
- InventoryReserved
- InventoryReservationReleased

## 8. Traveller

### Commands
- CreateTraveller
- UpdateTraveller

### Query: TravellerSummary
Purpose-minimized projection for Booking, Documents and Visa.

TravellerCreated/TravellerUpdated events are added only when asynchronous consumers actually need them.

## 9. Booking

### Command: CreateBooking
Validates BookingOwner, Travellers, published Departure, PackageVersion, Quote/PriceVersion, occupancy, InventoryHold and relevant policy versions before creating PendingPayment.

### Command: ConfirmBooking
Requires trusted payment outcome plus Inventory reservation under approved orchestration.

### Command: CancelBooking
Changes Booking truth only; refund and inventory release remain independent workflows.

### Query: BookingFinancialObligation
Producer: Booking
Consumer: Payments

Returns the immutable agreed financial obligation/payment schedule.

### Query: BookingSummary
Purpose-specific projection for Journey/Admin/Support.

### Events
- BookingCreated
- BookingAwaitingPayment
- BookingConfirmed
- BookingConfirmationExceptionRaised
- BookingExpired
- BookingCancelled
- BookingCompleted

BookingCancelled does not imply refund settled or inventory release succeeded.

## 10. Payments

### Command: InitiatePayment
Owner: Payments

Amount/currency comes from authoritative Booking financial obligation, not arbitrary client input.

### Provider callback path
External callback -> authenticity verification -> idempotency check -> Payments domain transition.

### Queries
- PaymentSummary
- OutstandingBalance

### Refund commands
- RequestRefund
- AuthorizeRefund
- SubmitRefund

### Events
- PaymentInitiated
- PaymentSettled
- PaymentFailed
- PaymentReconciliationRequired
- RefundRequested
- RefundAuthorized
- RefundSettled
- RefundFailed
- RefundReconciliationRequired

PaymentSettled is trusted financial evidence, not permission to blindly set Booking = Confirmed.

## 11. Documents

### Commands
- EstablishDocumentRequirements
- SubmitDocument
- ApproveDocument
- RequireDocumentCorrection
- RejectDocument

Review actions require current submission/version checks.

### Queries
- DocumentReadiness
- AuthorizedDocumentAccess for legitimate secure-review flows

### Events
- DocumentRequirementEstablished
- DocumentUploaded
- DocumentValidated
- DocumentCorrectionRequired
- DocumentApproved
- DocumentRejected

No passport binaries, permanent URLs or unnecessary identity data appear in generic integration events.

## 12. Visa

### Commands
- CreateVisaCase
- StartVisaReview
- MarkVisaReadyForSubmission
- RecordVisaSubmission
- RequireVisaAction
- RecordVisaOutcome
- CancelVisaCase

### Queries
- VisaCustomerStatus
- VisaOperationalStatus for authorized operations only

### Events
- VisaCaseCreated
- VisaReadyForSubmission
- VisaSubmitted
- VisaProcessing
- VisaActionRequired
- VisaApproved
- VisaRejected
- VisaCaseCancelled

Events distinguish NoorPath workflow state from official authority outcome evidence.

## 13. Notifications

Notifications consumes approved business events and owns only its delivery lifecycle.

### Operational events
- NotificationQueued
- NotificationDelivered
- NotificationFailed

Notification failure never reverses a business transition.

## 14. Audit

Consequential operations provide safe audit evidence containing actor/system identity, action, target, timestamp, reason where required, correlation and safe before/after evidence.

Secrets and full sensitive documents are prohibited from audit payloads.

Whether selected critical actions require atomic audit/outbox persistence is finalized during Security/Architecture design.

## 15. Reporting & Analytics

Reporting consumes approved events and builds rebuildable projections.

It never becomes authoritative domain truth and may not become a shadow PII store.

## 16. Primary MVP Coordination Flows

### Publish offering
Operator eligibility -> Catalogue validation -> published Pricing + required Inventory -> Departure publish -> Discovery/Reporting projection updates.

### Start checkout
Discovery projection -> Pricing Quote -> Inventory Hold -> Traveller selection/creation -> Booking PendingPayment with immutable snapshot -> Payment initiation.

### Payment settlement -> Booking confirmation
1. Payments verifies provider result and commits Settled.
2. PaymentSettled is published reliably.
3. Booking consumes it and requests Inventory reservation.
4. On reservation success, Booking becomes Confirmed.
5. If money settled but reservation cannot be made, Booking becomes ConfirmationException for governed reconciliation.

### Cancellation
Booking commits Cancelled -> Inventory release requested idempotently -> Refund workflow evaluated/started independently -> Notifications/Reporting/Audit react.

### Documents -> Visa readiness
Documents establishes requirements -> secure submission -> validation/review -> approved readiness facts -> Visa evaluates its own prerequisites -> Journey derives customer readiness.

## 17. Delivery Semantics Baseline

Cross-capability asynchronous delivery assumes **at-least-once**, not exactly-once.

Therefore:
- producer publication must be reliable after commit
- consumers are idempotent
- duplicates are normal
- retry/backoff is explicit
- dead-letter/poison processing is visible
- replay is safe where supported

For the MVP modular monolith, the preferred default is PostgreSQL transactional Outbox + worker processing, without requiring an external broker on day one.

Exact Outbox implementation is deferred to Architecture/Data design.

## 18. Transaction Boundary Principle

Atomicity belongs inside the owner of the fact.

Examples:
- Payment settlement + Payment outbox record commit atomically.
- Booking confirmation + Booking outbox record commit atomically.

NoorPath must not depend on one cross-module database transaction merely because modules share a process/database.

Cross-module consistency uses explicit contracts, idempotency, state machines, reliable events, retries, compensation/reconciliation and observable exception states.

## 19. Contract Versioning

- explicit version semantics
- additive compatible changes preferred
- field meaning never changes silently
- removals/renames require deprecation/migration
- internal domain changes need not leak into stable integration contracts
- contract tests validate producer/consumer compatibility

## 20. Security & Privacy Rules

- authorization enforced at owning boundaries
- OperatorId in a message does not grant authorization
- purpose-minimized sensitive payloads
- no generic event transport for document binaries
- no payment/provider/authentication secrets in events
- logs/traces redact sensitive fields
- message retention follows classification/privacy policy
- consequential external callbacks require authenticity and replay controls

## 21. Observability

Every cross-capability interaction must eventually expose correlation, producer/consumer, operation/event name, success/failure, retries, latency and dead-letter/reconciliation state where applicable.

Expected business rejection is distinguished from technical failure.

## 22. Testing Requirements

Definition-of-Ready requires owner/consumer, success/rejection contract, authorization, idempotency, duplicates, ordering, retry/timeout, privacy review and compatibility tests to be defined.

Critical MVP tests include:
- duplicate PaymentSettled
- settled payment after hold expiry
- duplicate reservation
- repeated cancellation
- failed release after cancellation
- duplicate/out-of-order visa update
- obsolete document approval attempt
- Notifications/Reporting outage without corruption
- event publication retry after owner transaction commit

## 23. Deferred Technology Decisions

Not chosen yet:
- Azure Service Bus
- RabbitMQ/Kafka
- Redis
- exact Outbox library
- worker topology
- internal synchronous transport implementation
- serialization/schema registry
- provider SDKs

Preferred MVP direction remains modular monolith + explicit application contracts + PostgreSQL transactional Outbox, adding external messaging only when a real scale/integration requirement appears.

## 24. Freeze Rule

A new cross-capability dependency must have one authoritative producer, a legitimate consumer need, a purpose-minimized contract, defined failure/idempotency behaviour, understood security/privacy impact and no shared persistence ownership.

Direct database coupling, implicit state propagation and events used as disguised remote commands are prohibited unless an explicit ADR documents a justified exception.
