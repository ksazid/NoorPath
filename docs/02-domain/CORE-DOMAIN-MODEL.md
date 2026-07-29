# NoorPath V2 Core Domain Model

Status: Draft baseline  
Version: 0.1  
Step: 7

## Purpose

This document turns the approved capability ownership and business invariants into a logical core domain model before persistence design.

It defines domain objects, aggregate candidates, relationships, mutability, transaction/concurrency concerns, snapshot rules and anti-patterns. It does **not** authorize EF Core entities, database tables, message-bus choices or microservice boundaries yet.

---

# 1. Model Principles

1. Account, Booking Owner and Traveller are distinct concepts.
2. Operator is the commercial/tenant owner of operator-scoped resources.
3. PackageTemplate is reusable authoring; PackageVersion is historical commercial content; DepartureBatch is the dated departure.
4. Pricing owns price versions; Inventory owns capacity; Booking owns the commercial agreement.
5. Payment settlement is independent from Booking lifecycle.
6. Documents own sensitive traveller evidence; Visa owns visa case state.
7. Journey Readiness is derived, not independently mutable.
8. Notifications, Audit and Reporting observe/consume business outcomes and never become alternative sources of truth.
9. Cross-module relationships are identifiers/contracts, not direct ORM object graphs.
10. Simple V1 implementations are preferred, but boundaries must support additive V1.x/V2 evolution.

---

# 2. Core Aggregate Candidates

## 2.1 Identity — Account

### Aggregate root
`Account`

### Responsibility
Represents an authenticated principal and security lifecycle.

### Owns
- authentication subject/provider linkage
- account status
- security/recovery state
- privileged MFA posture where applicable

### References
- OperatorMemberships reference Account/Principal ID
- Booking references BookingOwner Account ID
- Traveller may optionally link to Account

### Boundary rule
Account does not contain Traveller, Operator, Booking or permissions from other domains as owned child objects.

---

## 2.2 Operators — Operator

### Aggregate root
`Operator`

### Responsibility
Represents an Umrah operator/agency, business profile, operating lifecycle and platform eligibility.

### Candidate children/value objects
- BusinessProfile
- PublicProfile
- Approval/Eligibility state
- OperatingStatus

### Important behaviour
- approve for publication
- suspend/reactivate
- update governed business/public profile

### Invariants
- historical commercial ownership survives suspension/deactivation
- operator approval/eligibility is authoritative here

## OperatorMembership

Treat as a distinct aggregate or independently controlled entity if permission/membership changes need their own concurrency/audit lifecycle.

### Owns
- OperatorId
- Account/PrincipalId
- scoped role/permission assignment
- membership status

### Boundary rule
Identity authenticates the principal; Operators decides whether that principal belongs to the operator and with what business scope.

---

## 2.3 Catalogue — PackageTemplate

### Aggregate root
`PackageTemplate`

### Responsibility
Reusable authoring container for a package concept.

### Owns
- OperatorId
- draft package identity/name
- authoring lifecycle
- relationship to generated PackageVersions

### Important behaviour
- edit draft
- produce/version commercial content

### Boundary rule
A mutable template is never the sole evidence for an existing customer's booked package.

## PackageVersion

### Aggregate root or immutable version entity
`PackageVersion`

### Responsibility
Frozen/versioned commercial package content.

### Candidate contained value objects
- PackageSummary
- StayPlan
- MakkahAccommodation
- MadinahAccommodation
- TravelFacts
- Inclusions
- Exclusions
- ConfirmationStates

### Important behaviour
- validate completeness
- mark/version as publishable under Catalogue workflow

### Invariants
- Makkah and Madinah accommodation are independent
- pending facts remain pending
- a committed/published version is not silently rewritten

## DepartureBatch

### Aggregate root
`DepartureBatch`

### Responsibility
Dated departure/publication lifecycle for a PackageVersion.

### Owns
- OperatorId
- PackageVersionId
- origin
- departure/return dates
- booking cutoff policy reference
- lifecycle/publication state

### Important behaviour
- submit/review/publish according to frozen workflow
- pause/close/cancel

### Boundary rule
DepartureBatch does not own price or inventory quantities.

---

## 2.4 Pricing — PricePlan

### Aggregate root
`PricePlan`

### Responsibility
Editable commercial pricing definition for a Catalogue offering.

### Owns
- Catalogue references
- supported occupancy pricing definition
- fee/tax treatment
- payment-plan definition
- version history

### Important behaviour
- edit draft commercial rules
- publish/create PriceVersion

## PriceVersion

### Aggregate root or immutable version entity
`PriceVersion`

### Responsibility
Exact immutable commercial price definition used for quotes/bookings.

### Candidate contained value objects
- Currency
- OccupancyPrice(s)
- FeeTaxDisclosure
- PaymentPlanDefinition
- EffectivePeriod
- PricingPolicyReferences

### Invariants
- currency always explicit
- unsupported occupancy cannot be quoted
- new version never silently reprices existing Booking

## Quote

### Logical object
`Quote`

### Responsibility
Calculated customer offer for a specific input set and PriceVersion.

### Ownership
Pricing owns quote generation/validity. Booking preserves committed quote evidence.

### Candidate facts
- QuoteId if persisted
- DepartureBatchId
- PriceVersionId
- occupancy/traveller basis
- total/due-now/remaining
- fee/tax disclosure
- validity/expiry

### Important note
Persistence/aggregate treatment remains an architecture decision. The semantic object is required even if an MVP implementation calculates it synchronously and only persists the committed evidence in Booking.

---

## 2.5 Inventory — InventoryPool

### Aggregate root
`InventoryPool`

### Responsibility
Authoritative sellable capacity for a defined departure/unit/occupancy.

### Owns
- capacity
- saleable unit/occupancy key
- adjustments
- derived availability

### Concurrency hotspot
All operations affecting capacity must preserve oversell invariants under concurrent checkout.

### Important behaviour
- adjust capacity with audit
- acquire hold atomically
- validate availability

## InventoryHold

### Aggregate root or concurrency-controlled lifecycle object
`InventoryHold`

### Responsibility
Temporary allocation during checkout.

### Owns
- pool reference
- quantity/unit
- created/expiry times
- hold lifecycle
- booking/checkout correlation

### Important behaviour
- expire
- release
- convert/reconcile exactly once to reservation

## Reservation

### Aggregate root or inventory-owned allocation
`Reservation`

### Responsibility
Confirmed allocation of inventory to Booking.

### Important behaviour
- create from valid hold or governed reservation flow
- release/cancel according to business transition

### Invariants
- no normal overselling
- expired holds cannot be consumed
- hold-to-reservation conversion idempotent

---

## 2.6 Traveller — Traveller

### Aggregate root
`Traveller`

### Responsibility
Represents a person travelling independently of the Account managing the booking.

### Owns
- justified identity/profile facts
- optional account linkage
- traveller category when introduced

### Does not own
- passport/document file
- visa case
- booking participation lifecycle

### Boundary rule
Passport/document-derived facts remain Documents-owned unless a specific justified fact is deliberately promoted into Traveller through a governed design.

---

## 2.7 Booking — Booking

### Aggregate root
`Booking`

### Responsibility
Authoritative commercial agreement and booking lifecycle.

### Candidate children/value objects
- BookingOwnerRef
- BookingTraveller(s)
- OccupancySelection / RoomAllocation
- BookedCommercialSnapshot
- PolicyVersionReferences
- InventoryAllocationReference

### References
- OperatorId
- DepartureBatchId
- PackageVersionId
- PriceVersionId / Quote evidence
- TravellerIds
- InventoryHold/Reservation IDs

### Important behaviour
- create from saleable offer
- attach selected traveller participation
- capture committed commercial snapshot
- move through controlled booking state machine
- confirm only when required preconditions are satisfied
- cancel without deleting history
- perform named manual override only where policy explicitly permits it

### Invariants
- one identifiable Booking Owner
- at least one Traveller for a customer booking
- fixed commercial/version references at commitment
- Booking state separate from payment/document/visa state
- historical snapshots immutable

### Transaction boundary
Booking should protect its own lifecycle consistency, but must not assume a distributed transaction can update Pricing, Inventory and Payments tables directly. Cross-capability coordination is handled explicitly in architecture/state-machine design.

---

## 2.8 Payments — Payment / Financial Ledger

## Payment

### Aggregate root
`Payment`

### Responsibility
Track provider payment process and authoritative settlement outcome.

### Owns
- BookingId
- amount/currency
- provider references
- payment processing status
- settlement evidence
- attempts

### Important behaviour
- initiate provider payment
- accept verified provider outcome idempotently
- reconcile ambiguous/pending state

### Invariants
- browser callback alone cannot settle money
- duplicate provider messages cannot duplicate settlement
- no PAN/CVV storage

## Financial Ledger

### Logical append-only model
`LedgerEntry`

### Responsibility
Preserve monetary facts used to derive paid/refunded/outstanding amounts.

### Characteristics
- append-only
- compensating entries instead of editing history
- attributable to source Payment/Refund/Adjustment
- currency explicit

### Aggregate decision
Exact ledger aggregate/storage grouping is deferred to Data Architecture, but append-only semantics are mandatory.

## Refund

### Aggregate root or Payments-owned lifecycle object
`Refund`

### Responsibility
Return funds without destroying original payment evidence.

### Important behaviour
- request/authorize according to policy
- process provider refund
- settle/refuse/fail
- emit ledger evidence

---

## 2.9 Documents — DocumentRequirementSet / DocumentSubmission

## DocumentRequirementSet

### Aggregate root
`DocumentRequirementSet`

### Responsibility
Versioned definition of required evidence for applicable traveller/journey contexts.

### Owns
- requirement version
- applicability
- requirement definitions
- effective period

### Important rule
Existing journeys reference the applicable version; later requirement changes do not silently rewrite historical obligations.

## TravellerDocumentRequirement

### Lifecycle object
Represents one required item for one Traveller in a Booking/journey context.

### Responsibility
Connect requirement policy to fulfilment/readiness.

## DocumentSubmission

### Aggregate root
`DocumentSubmission`

### Responsibility
Secure submitted evidence and review lifecycle.

### Candidate children/value objects
- private storage reference
- safe file metadata
- validation result
- review history/status

### Important behaviour
- upload/submit
- validate file safety
- approve
- require correction
- reject
- resubmit while preserving prior history

### Security boundary
Highly Sensitive document content must not leak into logs, analytics, public URLs or unrelated projections.

---

## 2.10 Visa — VisaCase

### Aggregate root
`VisaCase`

### Responsibility
Traveller-specific visa workflow and customer-facing status projection.

### Owns
- TravellerId
- Booking/journey context
- internal workflow state
- customer-visible state mapping
- evidence references
- case history

### Important behaviour
- transition internal state through approved workflow
- map to customer-facing state
- record manual changes/audit

### Invariants
- separate from Documents
- traveller-specific
- customer status cannot overclaim official authority outcome

---

## 2.11 Notifications — NotificationRequest / Delivery

### Aggregate/lifecycle object
`NotificationRequest`

### Responsibility
Track communication requested from authoritative business outcomes.

### Owns
- use case/event reference
- recipient/channel
- template version
- deduplication key
- delivery lifecycle

### Child/history
`NotificationDeliveryAttempt`

### Invariants
- sending success/failure never determines business truth
- retries are deduplicated/retry-safe
- message data minimized

---

## 2.12 Support

### MVP
No heavy Support aggregate is required if MVP only exposes governed support routes and scoped context access.

### V1.x candidate aggregate
`SupportCase`

### Responsibility
Structured case lifecycle, assignment, escalation and communication history.

### Boundary rule
Support never edits Booking/Payment/Document/Visa state directly; it invokes authorized commands on owning capabilities.

---

## 2.13 Audit — AuditRecord

### Append-oriented object
`AuditRecord`

### Responsibility
Preserve accountability evidence for consequential actions.

### Facts
- actor/system identity
- action
- resource/capability
- timestamp
- reason where required
- safe before/after evidence
- correlation/context

### Rule
Audit is evidence about domain activity, not a mutable backup copy of domain state.

---

# 3. Primary Domain Relationships

```text
Account
  ├─ may link to Traveller
  ├─ may be OperatorMembership principal
  └─ owns/manages Booking as BookingOwner

Operator
  ├─ OperatorMembership*
  └─ PackageTemplate*
        └─ PackageVersion*
              └─ DepartureBatch*
                    ├─ PriceVersion*   [Pricing-owned]
                    ├─ InventoryPool*  [Inventory-owned]
                    └─ Booking*        [Booking-owned]

Booking
  ├─ BookingTraveller* → Traveller
  ├─ BookedCommercialSnapshot
  ├─ Inventory reservation reference
  ├─ Payment* → Payments
  ├─ TravellerDocumentRequirement* → Documents
  │      └─ DocumentSubmission*
  └─ VisaCase* → Visa
```

The diagram shows business relationships, not permission for direct cross-module database foreign-key navigation or ORM graphs.

---

# 4. Historical Snapshot Model

NoorPath requires two kinds of truth:

## Current truth
Examples:
- current Operator status
- latest package version
- current price version available for new quotes
- current Inventory availability
- current Traveller profile

## Commitment truth
Examples captured by Booking:
- PackageVersion customer selected
- departure facts relevant to agreement
- PriceVersion / Quote used
- fee/tax disclosure
- payment schedule
- selected occupancy
- policy version references
- material operator/package facts required to reproduce the sale

### Rule
A current source changing later never silently rewrites commitment truth.

### Rule
Snapshots must contain only what is necessary for reproducibility. They are not excuses to duplicate entire external aggregates.

---

# 5. Mutability Model

## Mutable authoring/state
- Account security/profile state
- Operator business profile/status
- PackageTemplate draft
- PricePlan draft
- Departure lifecycle
- Inventory capacity through controlled adjustments
- Traveller profile facts where legally/operationally permissible
- processing lifecycle states

## Versioned
- PackageVersion
- PriceVersion
- PaymentPlanDefinition
- DocumentRequirementSet
- policy definitions affecting commitments
- notification templates

## Snapshot/immutable after commitment
- BookedCommercialSnapshot
- booking references to PackageVersion/PriceVersion/policies
- committed payment schedule
- agreed occupancy selection unless explicit amendment is implemented

## Append-only
- settled financial ledger entries
- protected audit evidence
- material review/history records where required

## Derived
- availability quantity/state
- outstanding balance
- journey readiness
- reporting projections
- customer-facing aggregated dashboard states

---

# 6. Concurrency & Transaction Hotspots

These must receive explicit technical designs later.

## Inventory acquisition
Two customers may attempt to buy the last availability concurrently. Hold/reservation must be atomic and oversell-safe.

## Hold expiry vs payment confirmation
A payment result can race with hold expiry. The state machine must define exactly what evidence permits confirmation and how exceptional reconciliation works.

## Duplicate payment webhooks
Repeated/out-of-order provider messages must be idempotent.

## Publication/version updates
A customer may be viewing an older package/price while a newer version is published. Quote/booking must either remain valid under explicit rules or force re-confirmation; no silent substitution.

## Booking transition concurrency
Admin/operator/customer actions must not perform incompatible state transitions concurrently without detection.

## Document review/resubmission
A reviewer must not approve an obsolete submission after a newer resubmission becomes authoritative without explicit detection.

---

# 7. Domain Boundary Contracts

Cross-capability interactions must exchange stable contracts such as:

- `OperatorPublicationEligibility`
- `PublishedDepartureView`
- `PriceQuote`
- `InventoryHoldResult`
- `InventoryReservationResult`
- `TravellerSummary`
- `BookingFinancialObligation`
- `PaymentOutcome`
- `DocumentReadiness`
- `VisaCustomerStatus`

These are conceptual contract names, not frozen API DTOs yet.

### Rules
1. Contracts expose only required facts.
2. Consumer modules do not import another module's persistence entity.
3. A returned projection is not writable business truth.
4. Sensitive data is minimized by contract purpose.
5. Events describe completed facts, while commands request behaviour.

---

# 8. Candidate Domain Events

Working events for later state-machine/integration design:

- OperatorApproved
- OperatorSuspended
- PackageVersionPublished
- DeparturePublished
- DeparturePaused
- DepartureCancelled
- PriceVersionPublished
- InventoryHeld
- InventoryHoldExpired
- InventoryReserved
- InventoryReleased
- TravellerAddedToBooking
- BookingCreated
- BookingAwaitingPayment
- BookingConfirmed
- BookingCancelled
- PaymentInitiated
- PaymentSettled
- PaymentFailed
- RefundSettled
- DocumentSubmitted
- DocumentCorrectionRequired
- DocumentApproved
- VisaStatusChanged

Exact event payloads, transaction/outbox semantics and delivery technology are not frozen in Step 7.

---

# 9. Explicit Anti-Patterns

The V2 model rejects these shortcuts:

- one `User` entity representing customer, traveller, operator staff and admin
- one giant `Package` entity containing package content, departure, price, capacity and booking state
- `DepartureBatch.Capacity` as the complete inventory model
- a mutable `Price` field on Booking with no price-version evidence
- one universal `Status` enum reused across unrelated domains
- a mutable `OutstandingBalance` database field as financial truth
- storing payment settlement based only on frontend success callback
- passport files stored as public media URLs
- Visa status stored directly on Traveller or Booking
- Journey readiness stored as a manually editable flag
- Admin database tables that copy/override every capability
- direct cross-module DbContext/table access as integration
- generic configuration JSON containing all business/security rules
- deleting/correcting historical financial or audit facts in place

---

# 10. Decisions Still Open After Step 7

The logical model is stable enough to proceed, while these decisions remain intentionally unresolved until their dedicated steps:

1. Exact state machines for Operator, Catalogue, Booking, Payment, Document and Visa.
2. Exact aggregate persistence boundaries/DbContext arrangement.
3. Whether logical modules get separate PostgreSQL schemas immediately.
4. Quote persistence strategy and expiry policy.
5. Inventory pool granularity for MVP and mixed-room allocation rules.
6. Payment/deposit/instalment rules.
7. Cancellation/refund workflow.
8. Minimum pre-payment traveller fields.
9. Document requirement/retention policy details.
10. Visa status vocabulary/customer mapping.
11. Sync vs async cross-module operations.
12. Outbox implementation and worker topology.
13. Identity, payment, notification and storage providers.

These are tracked decisions, not implementation gaps to be guessed.

---

# 11. Step 7 Freeze Criteria

Step 7 may be treated as ready for the next domain-design step when:

- each core fact has one owner
- Account/Traveller/BookingOwner separation is accepted
- PackageTemplate/PackageVersion/DepartureBatch separation is accepted
- PriceVersion is separate from Payment truth
- Inventory is separate from Catalogue
- Booking owns commitment snapshots, not current external state
- Documents and Visa are independent domains
- financial history is append-only and balance is derived
- readiness remains derived
- unresolved policy values remain explicitly unresolved

Changes to these structural decisions after baseline freeze require impact analysis across Product, UX, Architecture, Security, Data and migration compatibility.
