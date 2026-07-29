# NoorPath V2 Domain Map & Capability Ownership

Status: Draft for Product Owner review
Version: 0.1
Step: 6

## Purpose

This document defines **which capability owns each business fact** before entities, database schemas, APIs, or UI flows are designed in detail.

It builds on the Product Charter, Capability Map, MVP Scope, and Business Rules & Invariants.

The key rule is:

> **One authoritative owner per business fact. Other capabilities reference, consume, or project that fact; they do not create competing truth.**

This is a logical domain map. It does not yet freeze physical deployment, database technology details, message-bus choices, or exact aggregate design.

---

## 1. Ownership Principles

### DOM-OWN-001 — One owner per fact
Every authoritative business fact has exactly one owning capability.

### DOM-OWN-002 — References are allowed; shadow truth is not
A capability may retain an identifier, immutable snapshot, or projection of another capability's fact when required for workflow or historical reproducibility, but must not silently become a second source of truth.

### DOM-OWN-003 — Historical snapshots are intentional
Bookings may preserve booked package, price, policy, and traveller facts needed to reproduce the agreement. Such snapshots are historical evidence, not replacements for the current source capability.

### DOM-OWN-004 — Cross-capability access uses explicit contracts
Capabilities consume another capability through explicit application/API contracts or events. Direct access to another capability's persistence model is not a valid domain dependency.

### DOM-OWN-005 — Experience surfaces do not own business truth
Customer Discovery, Checkout, Journey Dashboard, and Admin/Operations are experience/composition surfaces. They may combine data from multiple capabilities but do not own the underlying business state.

### DOM-OWN-006 — Domain-specific configuration stays with its domain
Pricing rules belong to Pricing, document requirements belong to Documents, booking cutoff/cancellation policy belongs to the relevant commercial/booking capability, etc. NoorPath must not create one unrestricted generic configuration store containing every business rule.

### DOM-OWN-007 — Derived status remains derived
Readiness, dashboard status, reporting metrics, and availability labels must be derived from authoritative states and cannot become competing mutable truth.

---

# 2. Core Domain Capability Map

## 2.1 Identity & Access

**Purpose:** authenticate human/system principals and provide the security identity required for authorization.

### Owns
- Account / authenticated principal identity
- Authentication subject identifier
- Credential or external identity-provider linkage
- Session/security posture
- MFA state for privileged identities
- Account recovery/security state
- Platform-level privileged role/permission assignment where appropriate

### Does not own
- Traveller identity
- Operator business identity
- Operator publication eligibility
- Booking ownership
- Operator membership business lifecycle

### Key references
- Operator membership may reference an Identity principal.
- Booking Owner references an Identity principal/account.

### Candidate logical schema
`identity.*`

---

## 2.2 Operators

**Purpose:** represent Umrah operators/agencies, their lifecycle, staff membership, scope, and eligibility to operate/sell on NoorPath.

### Owns
- Operator identity
- Operator business profile
- Operator lifecycle/status
- Platform approval/eligibility to publish
- Operator staff membership
- Operator-scoped staff assignment
- Operator verification facts that NoorPath explicitly records
- Operator suspension/reactivation history

### Does not own
- Authentication credentials
- Packages
- Prices
- Inventory
- Bookings
- Payments

### Key references
- Staff membership references Identity principal IDs.
- Catalogue references Operator ID.

### Candidate logical schema
`operators.*`

---

## 2.3 Catalogue

**Purpose:** own what an operator offers and the dated departure facts required to describe the Umrah product truthfully.

### Owns
- PackageTemplate
- PackageVersion
- DepartureBatch
- Package summary/content
- Makkah stay/accommodation facts
- Madinah stay/accommodation facts
- Itinerary/travel facts required for sale
- Inclusions/exclusions
- Origin and departure/return dates
- Booking-cutoff reference/policy association
- Publication lifecycle
- Pending/confirmed commercial content states
- Public catalogue content projection source

### Accommodation placement
For MVP, accommodation is **inside Catalogue**, because hotel/stay information describes the package being sold. It is not a separate bounded module yet.

A future supplier/content integration may justify extraction, but V1 should not create that complexity prematurely.

### Does not own
- Price calculations or price versions
- Sellable availability/holds
- Bookings
- Operator approval

### Key references
- References Operator ID.
- Pricing and Inventory reference Catalogue identifiers.

### Candidate logical schema
`catalogue.*`

---

## 2.4 Pricing

**Purpose:** own commercial pricing definitions and price-version truth used to produce explainable quotes.

### Owns
- PricePlan
- PriceVersion
- Supported occupancy price definitions
- Currency of a price definition
- Fees/taxes inclusion/separation rules
- Deposit/instalment commercial plan definitions
- Price effective dates/validity rules
- Pricing policy versions
- Quote calculation rules

### Does not own
- Actual settled payments
- Outstanding balance ledger truth
- Inventory
- Booking lifecycle

### Key references
- References package/departure/occupancy identifiers from Catalogue.
- Booking references immutable PriceVersion/quote evidence.
- Payments consumes the agreed booking payment schedule/financial obligation.

### Candidate logical schema
`pricing.*`

---

## 2.5 Inventory

**Purpose:** own saleable capacity, availability, temporary holds, reservations, release, and oversell protection.

### Owns
- InventoryPool
- Sellable capacity
- Available/reserved/held quantities
- InventoryHold
- Hold expiry
- Reservation
- Release
- Manual inventory adjustment history
- Availability state derived from inventory truth

### Does not own
- Package content
- Price
- Booking state

### Key references
- References DepartureBatch and occupancy/product identifiers.
- Booking references relevant Hold/Reservation IDs.

### Candidate logical schema
`inventory.*`

---

## 2.6 Traveller

**Purpose:** represent each person travelling, independently from the account that pays/manages the booking.

### Owns
- Traveller identity/profile
- Name/date of birth and other justified identity facts
- Traveller classification when supported
- Traveller contact linkage where applicable
- Account-to-traveller relationship where one exists
- Traveller profile lifecycle/consent facts when introduced

### Does not own
- Passport/document files
- Visa processing state
- Booking state
- Payment state

### Key references
- Booking references Traveller IDs.
- Documents and Visa reference Traveller IDs.

### Candidate logical schema
`traveller.*`

---

## 2.7 Booking

**Purpose:** own the commercial booking agreement and its lifecycle.

### Owns
- Booking
- Booking reference
- Booking Owner association
- Booking lifecycle/state machine
- Traveller participation in a booking
- Booking-specific room/occupancy assignment
- Selected package/departure references
- Immutable booked commercial snapshot required to reproduce customer agreement
- PriceVersion/quote reference used at commitment
- Inventory hold/reservation reference used by the booking
- Cancellation lifecycle
- Booking-specific policy-version references
- Manual booking override history where allowed

### Does not own
- Current package definition
- Current price definition
- Inventory capacity
- Payment settlement truth
- Traveller's master identity
- Document files
- Visa status

### Important rule
Booking may snapshot external facts for legal/commercial reproducibility. Those snapshots are **booked facts**, not competing current catalogue/pricing truth.

### Candidate logical schema
`booking.*`

---

## 2.8 Payments

**Purpose:** own monetary transaction evidence, payment processing state, refunds, reconciliation, and the financial ledger.

### Owns
- Payment
- PaymentAttempt/provider initiation reference
- Verified provider transaction evidence
- Payment status
- LedgerEntry
- Refund
- Refund status
- Reconciliation state
- Idempotency records required for payment processing
- Derived paid amount / outstanding balance from authoritative financial facts plus agreed booking obligation

### Does not own
- Package price definitions
- Booking lifecycle
- Card PAN/CVV

### Key references
- References Booking ID.
- Consumes the booking's agreed financial obligation/payment schedule.
- Emits trusted financial outcomes for Booking/Journey/Notifications.

### Candidate logical schema
`payments.*`

---

## 2.9 Documents

**Purpose:** own traveller document requirements, submissions, secure document metadata, and review lifecycle.

### Owns
- Document requirement definitions/policy versions
- Traveller document requirement instances
- DocumentSubmission metadata
- Secure object/storage reference
- Upload/validation/review status
- Approve/correction-required/reject/resubmit lifecycle
- File safety validation state
- Document access/review history
- Retention/deletion policy application state

### Does not own
- Traveller core identity
- Visa case state
- Booking state
- Public package media

### Key references
- References Traveller and Booking/journey context where required.
- Visa consumes document-readiness evidence.

### Candidate logical schema
`documents.*`

---

## 2.10 Visa

**Purpose:** own visa-processing case state and the explicit mapping between operational and customer-visible status.

### Owns
- VisaCase
- Traveller-specific visa workflow state
- Visa case history
- Required evidence references
- Internal operational visa status
- Customer-visible status mapping/version
- Manual status update audit linkage

### Does not own
- Document file storage
- Traveller identity
- Booking state
- Official authority systems

### Key references
- References Traveller ID.
- References Booking/journey context where required.
- Consumes Documents readiness/evidence references.

### Candidate logical schema
`visa.*`

---

## 2.11 Notifications

**Purpose:** deliver communications triggered by business events without owning the business outcome itself.

### Owns
- Notification template/version
- Notification request/delivery attempt
- Channel selection outcome
- Provider delivery reference
- Delivery status
- Retry/deduplication state
- Notification audit metadata
- Customer notification preferences when introduced

### Does not own
- Booking/payment/document/visa truth

### Key rule
A failed notification never changes whether the underlying business transition happened.

### Candidate logical schema
`notifications.*`

---

## 2.12 Support

**Purpose:** provide a controlled human-support capability around bookings and operator/customer issues.

### MVP ownership
MVP may remain deliberately thin and own only support routing/contact configuration and support-context linkage required for operations.

### V1.x ownership
- SupportCase
- Case category
- Assignment
- Case status
- Escalation
- Case communication/history

### Does not own
- Booking/payment/document/visa business state

### Candidate logical schema
`support.*`

---

## 2.13 Audit

**Purpose:** preserve append-oriented accountability evidence for consequential actions.

### Owns
- AuditRecord
- Actor/system identity reference
- Action
- Affected capability/resource reference
- Timestamp
- Reason where required
- Before/after evidence where required and safe
- Security/privileged-access audit evidence

### Does not own
- The domain fact being audited

### Important rule
The owning domain capability remains authoritative for its state. Audit preserves evidence that a transition/action occurred.

### Candidate logical schema
`audit.*`

---

## 2.14 Reporting & Analytics

**Purpose:** provide operational/business projections and product analytics without becoming shadow system-of-record state.

### Owns
- Reporting projections
- Aggregated metrics
- Privacy-safe product analytics events/projections
- Materialized read models created specifically for reporting

### Does not own
- Booking truth
- Payment truth
- Inventory truth
- Visa/document truth

### Candidate logical schema
`reporting.*`

---

## 2.15 Platform Configuration

**Purpose:** own only genuinely cross-platform configuration that has no natural domain owner.

### May own
- Global support contacts
- Global feature availability flags under governance
- Platform reference values that are truly cross-domain

### Must not become
A generic JSON/configuration dumping ground for Pricing, Booking, Documents, Visa, Inventory, or Security policy.

Those policies stay with their owning domains and are versioned there.

### Candidate logical schema
`platform_config.*`

---

# 3. Experience/Composition Capabilities — Not Sources of Truth

## Customer Discovery
Composes:
- Operators
- Catalogue
- Pricing
- Inventory
- Support

It owns presentation/search projections only, not package/price/availability truth.

## Checkout
Orchestrates/consumes:
- Identity
- Traveller
- Catalogue
- Pricing
- Inventory
- Booking
- Payments

Checkout is a journey, not an authoritative domain module.

## Journey / Travel Readiness Dashboard
Composes authoritative state from:
- Booking
- Payments
- Traveller
- Documents
- Visa
- Catalogue
- Support

Readiness is derived from those states. It must not become a manually editable competing truth.

## Admin / Operations
Provides role-scoped operational workflows across capabilities.

There is no `Admin` domain database that copies every module's state. Admin commands go to the owning capability.

---

# 4. Authoritative Fact Ownership Matrix

| Business fact | Authoritative owner | Typical consumers |
|---|---|---|
| Authenticated account/principal | Identity | Operators, Booking, Audit |
| Operator identity/status | Operators | Catalogue, Admin, Discovery |
| Operator staff membership | Operators | Authorization/Admin/Audit |
| Package template/version | Catalogue | Discovery, Pricing, Booking |
| Departure facts/publication state | Catalogue | Pricing, Inventory, Discovery, Booking |
| Makkah/Madinah hotel facts | Catalogue | Discovery, Booking snapshot |
| Price plan/version | Pricing | Discovery, Booking, Checkout |
| Inventory capacity/availability | Inventory | Discovery, Booking, Admin |
| Hold/reservation | Inventory | Booking, Admin |
| Traveller identity | Traveller | Booking, Documents, Visa |
| Booking lifecycle/agreement | Booking | Payments, Documents, Visa, Journey, Support |
| Settled payment/refund facts | Payments | Booking, Journey, Admin, Reporting |
| Outstanding balance | Payments, derived from agreed obligation + ledger | Journey, Admin, Notifications |
| Document requirement/submission/review | Documents | Journey, Visa, Admin |
| Visa case state | Visa | Journey, Admin, Notifications |
| Notification delivery state | Notifications | Admin/Support |
| Support case | Support | Customer/Admin when introduced |
| Audit evidence | Audit | Admin/Compliance |
| Reporting metric/projection | Reporting | Admin/Product |

---

# 5. Dependency Direction

Logical dependency flow for MVP:

`Identity`

`Operators → Identity`

`Catalogue → Operators`

`Pricing → Catalogue`

`Inventory → Catalogue`

`Traveller → Identity (optional account linkage)`

`Booking → Catalogue + Pricing + Inventory + Traveller + Identity`

`Payments → Booking`

`Documents → Traveller + Booking`

`Visa → Traveller + Booking + Documents`

`Notifications ← business events from all relevant capabilities`

`Support → scoped read contracts from Booking/Operators/etc.`

`Audit ← consequential events/actions from all capabilities`

`Reporting ← events/projections from authoritative capabilities`

This is **logical ownership/dependency**, not permission for direct database joins across module boundaries.

---

# 6. Cross-Capability Boundary Rules

1. No capability writes another capability's tables/state directly.
2. No capability treats another module's ORM/entity classes as its domain model.
3. Cross-capability references use stable identifiers/contracts.
4. Historical booking snapshots are explicitly named snapshots with source/version context.
5. Business events describe completed facts; they are not commands disguised as events.
6. Notifications, audit, and reporting react to business state; they do not determine that state.
7. Cross-capability consistency that cannot be atomic must define failure/compensation/idempotency during architecture design.
8. A UI composition endpoint may aggregate multiple capability views but must not create a new mutable source of truth.
9. Security/authorization checks occur at every owning capability boundary, not only in the UI/API gateway.
10. Domain ownership must remain valid even if modules are physically extracted later; extraction itself is not an MVP goal.

---

# 7. Candidate Aggregate Roots — Not Yet Frozen

These are working candidates to validate in the next domain-design steps:

- Identity: `Account/Principal`
- Operators: `Operator`, `OperatorMembership`
- Catalogue: `PackageTemplate`, `PackageVersion`, `DepartureBatch`
- Pricing: `PricePlan`, `PriceVersion`
- Inventory: `InventoryPool`, `InventoryHold`, `Reservation`
- Traveller: `Traveller`
- Booking: `Booking`
- Payments: `Payment`, `Refund`, `FinancialLedger/Account`
- Documents: `DocumentRequirementSet`, `DocumentSubmission`
- Visa: `VisaCase`
- Notifications: `NotificationDelivery`
- Support: `SupportCase` (V1.x)
- Audit: append-oriented `AuditRecord`

These names do **not** authorize database/entity implementation yet. Aggregate boundaries must be validated against invariants, transactions, state machines, concurrency, and lifecycle rules first.

---

# 8. Preliminary Business Events

Candidate events that establish coupling points without freezing implementation technology:

- `OperatorApproved`
- `OperatorSuspended`
- `PackageVersionPublished`
- `DeparturePublished`
- `DepartureCancelled`
- `PriceVersionPublished`
- `InventoryHeld`
- `InventoryHoldExpired`
- `InventoryReserved`
- `TravellerAddedToBooking`
- `BookingCreated`
- `BookingConfirmed`
- `BookingCancelled`
- `PaymentSettled`
- `PaymentFailed`
- `RefundSettled`
- `DocumentSubmitted`
- `DocumentCorrectionRequired`
- `DocumentApproved`
- `VisaStatusChanged`

Exact event contracts, delivery semantics, and outbox architecture are deferred to Architecture/Integration design.

---

# 9. Decisions Deliberately Deferred

This domain map does not yet decide:

- Exact aggregate transaction boundaries.
- Whether each logical capability uses a separate PostgreSQL schema from day one.
- Exact DbContext arrangement.
- Synchronous vs asynchronous contract for every module interaction.
- Outbox implementation details.
- Service Bus or external messaging.
- Exact external identity provider.
- Payment provider.
- Blob/document storage provider details.
- Exact search architecture.
- Caching architecture.

Those belong to Architecture, Security, Data Architecture, and ADR work after product/domain semantics are sufficiently stable.

---

# 10. Freeze Rule

A capability may only own a fact listed under another capability after an explicit domain decision with impact analysis.

Any proposed new module must demonstrate one of the following:

- distinct business ownership/lifecycle,
- distinct security/privacy boundary,
- distinct transactional/concurrency boundary,
- or independently evolving business rules substantial enough to justify separation.

Technical convenience alone is not sufficient reason to create a new domain capability.
