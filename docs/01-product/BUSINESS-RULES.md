# NoorPath V2 Business Rules & Invariants

Status: Draft for Product Owner review
Version: 0.1

## Purpose

This document defines the rules NoorPath must not violate. It separates:

- **Invariant** — a rule that must remain true regardless of operator configuration or UI.
- **Policy** — a business choice that may vary by operator, package, market, or release, but must be explicit, validated, versioned, and auditable.
- **Open decision** — a product policy that must be resolved before the affected capability becomes Definition-of-Ready.

The goal is to prevent implementation, design, or AI agents from inventing business behaviour.

---

## 1. Identity, Actors & Ownership

### INV-ID-001 — Account and Traveller are distinct
A NoorPath account represents an authenticated user. A Traveller represents a person travelling. One Booking Owner may manage multiple Travellers, and a Traveller does not need a NoorPath account for MVP.

### INV-ID-002 — Booking ownership is explicit
Every customer booking must have one identifiable Booking Owner responsible for the booking relationship with NoorPath.

### INV-ID-003 — Operator ownership is explicit
Every operator-owned resource must carry an unambiguous Operator identity/scope where operator ownership applies.

### INV-ID-004 — Authorization is contextual
Access decisions must consider permission, operator/tenant scope, and resource ownership. Role names alone are insufficient authorization.

### INV-ID-005 — Cross-operator access is denied by default
Operator staff must not read or mutate another operator's private resources unless an explicitly authorized NoorPath platform workflow permits it.

### INV-ID-006 — Privileged access is auditable
Consequential privileged actions by operator or NoorPath staff must identify the actor and be auditable.

## 2. Operator Lifecycle

### INV-OP-001 — Only eligible operators can sell publicly
A package/departure cannot become publicly bookable unless its owning operator is in a platform-approved state that permits publication.

### INV-OP-002 — Operator status cannot erase commitments
Suspending, deactivating, or otherwise restricting an operator must not delete historical packages, bookings, financial records, documents, or audit evidence.

### INV-OP-003 — Operator changes are scoped
Operator staff membership, permissions, and ownership changes must not silently transfer historical commercial responsibility between operators.

### POL-OP-001 — Suspension operating policy
The precise behaviour for existing active bookings when an operator is suspended is a configurable/governed operating policy and must be defined before production launch.

## 3. Catalogue, Package Versions & Departures

### INV-CAT-001 — Package template and departure are separate concepts
Reusable package content must not be treated as the same entity as a dated departure/batch.

### INV-CAT-002 — Published commercial history is reproducible
A booking must retain enough immutable/versioned package information to reproduce what the customer agreed to at booking time.

### INV-CAT-003 — Publication requires validity
A package/departure may be published only when all MVP-required commercial fields are valid and its operator is eligible to publish.

### INV-CAT-004 — Public catalogue exposes only publishable inventory
Draft, cancelled, archived, invalid, or otherwise non-sellable departures must not appear as bookable public offerings.

### INV-CAT-005 — Material changes cannot silently rewrite commitments
Commercially material package/departure changes after bookings exist must preserve the customer's booked facts and require an explicit, auditable change process where customer commitments are affected.

### INV-CAT-006 — Pending facts are labelled as pending
Unconfirmed flight, hotel, or other material travel facts must never be presented as confirmed.

### POL-CAT-001 — Publication approval policy
Whether MVP requires one-step operator publication, NoorPath approval, or another approval workflow must be explicit per release and operator policy.

## 4. Accommodation

### INV-ACC-001 — Makkah and Madinah accommodation are independently represented
Hotel/accommodation details for Makkah and Madinah cannot be inferred from each other or from package tier alone.

### INV-ACC-002 — Accommodation claims must be factual
Hotel name, classification/star claims, location/distance, room type, and confirmation status must originate from explicit operator/platform data and must not be inferred by the UI.

### INV-ACC-003 — Occupancy offered for sale must be supported
A room/occupancy option cannot be sold unless a valid price and corresponding availability model exist for it.

## 5. Pricing

### INV-PRI-001 — Every quote has an explicit price version
A customer quote/booking must reference the exact PriceVersion used to calculate the commercial offer.

### INV-PRI-002 — Price currency is explicit
Every price, payment, refund, and balance must carry an explicit currency. NoorPath must not assume or silently convert currency.

### INV-PRI-003 — Customer totals are explainable
Before commitment/payment, NoorPath must clearly expose the total price, what that total is based on, the amount due now, the remaining balance, and fees/taxes inclusion or separation.

### INV-PRI-004 — Existing bookings are not silently repriced
Publishing a new price version must not change the agreed price of an existing booking unless an explicit permitted amendment process creates a new commercial agreement.

### INV-PRI-005 — Arithmetic must reconcile
The booking financial summary must reconcile from immutable price facts plus payment/refund ledger entries; UI totals must not be independently invented.

### INV-PRI-006 — Unsupported occupancy cannot be priced
A booking cannot quote an occupancy/traveller combination for which no valid saleable price exists.

### POL-PRI-001 — Deposit and instalment policy
Deposit amount/percentage, number of instalments, due dates, and payment-plan rules may vary, but the selected plan must be fixed/versioned for the booking.

### POL-PRI-002 — Price validity policy
Quote validity/expiry rules must be explicit and must not silently extend after the underlying price or availability is no longer valid.

## 6. Inventory & Availability

### INV-INV-001 — Inventory is separate from catalogue content
Availability is operational state and must not be represented solely as a package/batch display field.

### INV-INV-002 — Holds and reservations are atomic
Inventory hold, confirm/reserve, release, and expiry operations must preserve consistency under concurrent booking attempts.

### INV-INV-003 — NoorPath must prevent known overselling
Confirmed reservations for a controlled inventory pool must not exceed its sellable capacity through normal system operations.

### INV-INV-004 — An expired hold cannot be treated as valid inventory
A booking cannot rely on an expired/released inventory hold without successfully acquiring valid availability again.

### INV-INV-005 — Confirmation converts valid held inventory exactly once
A successful booking confirmation must convert/reconcile its relevant hold to a reservation idempotently.

### INV-INV-006 — Inventory adjustments are auditable
Manual changes to sellable capacity, holds, or reservations must record actor/reason and must not silently invalidate existing confirmed commitments.

### POL-INV-001 — Hold duration
Checkout hold duration is a product/operational policy and must be explicitly configured and surfaced where customer behaviour depends on it.

### POL-INV-002 — Limited availability threshold
Any customer-facing "limited" state must come from an explicit truthful rule rather than artificial urgency.

## 7. Travellers & Family Booking

### INV-TRV-001 — Multiple travellers may belong to one booking
MVP must support one Booking Owner creating a booking for one or more Travellers.

### INV-TRV-002 — Traveller identity is not duplicated per screen
Traveller identity is a domain concept referenced by booking/document/visa workflows, not independent copies of form data maintained by each feature.

### INV-TRV-003 — Required traveller facts must be justified
Only information required for booking, travel, payments, documents, visa, support, legal/compliance, or fulfilment may be collected.

### INV-TRV-004 — Traveller-specific readiness remains traveller-specific
One traveller's document or visa state must not imply readiness for other travellers in the same booking.

### POL-TRV-001 — Traveller age categories
Adult/child/infant classification and age thresholds are business policies that must be defined before those categories are sold.

### POL-TRV-002 — Mahram relationship
Mahram capture is introduced only where there is a validated operational/legal/product requirement and must not be inferred from names or gender.

## 8. Booking

### INV-BKG-001 — A booking originates from a saleable offering
A new booking must reference a valid published departure that is currently allowed to accept bookings.

### INV-BKG-002 — Booking commercial references are fixed
A booking records the package/departure, PriceVersion, selected occupancy, Travellers, and relevant inventory reservation/hold references needed to reproduce the agreement.

### INV-BKG-003 — Booking state is explicit
Booking lifecycle must use a defined state machine. Arbitrary state jumps are not permitted.

### INV-BKG-004 — State transitions are permissioned
Every consequential booking transition must define who may perform it and under what preconditions.

### INV-BKG-005 — State transitions are auditable
Manual overrides, cancellations, exceptional confirmations, and other consequential transitions require audit evidence and reason where applicable.

### INV-BKG-006 — Booking and payment states are distinct
A booking must not use one status field to represent both booking fulfilment and payment settlement.

### INV-BKG-007 — Cancellation preserves history
Cancelling a booking changes lifecycle state; it must not delete the booking, price snapshot, traveller relationship, financial ledger, or audit history.

### POL-BKG-001 — Booking cutoff
The cutoff rule, timezone, and behaviour at/after cutoff must be explicitly defined per saleable departure/policy.

### POL-BKG-002 — Cancellation entitlement
Cancellation eligibility, fees, refund entitlement, timing, and operator/platform authority are explicit versioned policies, not hard-coded assumptions.

## 9. Payments, Instalments & Refunds

### INV-PAY-001 — Payment state is based on trusted evidence
A browser/client success callback alone cannot mark money as settled. Settlement must derive from verified provider evidence or an explicitly trusted reconciliation process.

### INV-PAY-002 — Payment processing is idempotent
Duplicate provider webhooks, retries, callbacks, or reconciliation events must not create duplicate financial facts or duplicate booking confirmation.

### INV-PAY-003 — Financial history is append-only
Settled payment/refund facts are not edited away. Corrections occur through explicit compensating/adjusting records.

### INV-PAY-004 — NoorPath does not store raw card secrets
NoorPath must not store PAN/CVV or equivalent sensitive payment credentials; provider-hosted/tokenized flows are required.

### INV-PAY-005 — Payments are attributable
Every payment/refund must be attributable to the relevant booking and provider transaction/reference.

### INV-PAY-006 — Balance is derived from financial facts
Outstanding balance is calculated from the agreed booking price and authoritative payment/refund ledger; it is not a manually editable amount.

### INV-PAY-007 — Refunds do not erase payments
A refund creates a distinct financial event and preserves the original payment history.

### POL-PAY-001 — Payment schedule
Deposit and instalment due dates/amounts may vary, but the booking's selected schedule must be preserved even if templates later change.

### POL-PAY-002 — Failed/late payment handling
Grace periods, retries, booking impact, and cancellation escalation for failed or overdue instalments must be explicitly defined before that workflow ships.

## 10. Documents & Passport Data

### INV-DOC-001 — Identity documents are highly sensitive
Passport and traveller identity documents must use the strongest NoorPath privacy/security classification and cannot be treated as public/media assets.

### INV-DOC-002 — Document access is purpose- and permission-bound
Only authorized actors with a legitimate workflow need may access traveller documents.

### INV-DOC-003 — Document storage is private
Document objects must not be publicly enumerable or exposed through permanent public URLs.

### INV-DOC-004 — Files are validated before trusted use
Uploaded files must be subjected to required type/size/signature/malware controls before downstream staff or systems treat them as safe.

### INV-DOC-005 — Review history is preserved
Approve, correction-required, reject, and resubmit actions must preserve the review/audit chain.

### INV-DOC-006 — Document state is per traveller and requirement
A booking-level "documents complete" state may only be derived when all relevant traveller requirements satisfy the required readiness rules.

### POL-DOC-001 — Required document rules
Requirements may vary by operator/package/traveller/journey, but must come from explicit versioned policy.

### POL-DOC-002 — Retention and deletion
Retention/deletion periods must be defined from legal, contractual, operational, and privacy requirements before production document storage begins.

## 11. Visa

### INV-VIS-001 — Visa case is separate from document storage
Documents provide evidence; VisaCase owns visa-processing state.

### INV-VIS-002 — Visa state is traveller-specific
Each relevant Traveller has an independent visa readiness/case state.

### INV-VIS-003 — Customer status must not overclaim authority
NoorPath must distinguish operator/platform workflow status from an official visa authority decision. Labels must not imply official approval before authoritative evidence exists.

### INV-VIS-004 — Visa state changes are auditable
Manual status changes require an authorized actor and audit history.

### INV-VIS-005 — Internal and customer-visible states may differ deliberately
Customer-facing visa states may simplify operational detail, but the mapping must be explicit and must never misrepresent risk or outcome.

### POL-VIS-001 — Visa workflow vocabulary
The exact internal/customer states and required evidence for each transition must be frozen before Visa implementation.

## 12. Notifications

### INV-NOT-001 — Notifications do not own business truth
Sending or failing to send a notification must not determine whether a booking, payment, document, or visa transition occurred.

### INV-NOT-002 — Notification processing is retry-safe
Retries must not cause uncontrolled duplicate communications or duplicate business transitions.

### INV-NOT-003 — Sensitive data is minimized in messages
Messages must contain only the minimum sensitive information needed for the communication channel and purpose.

### INV-NOT-004 — Critical notifications are traceable
For required transactional communications, NoorPath must record enough delivery state to support operations and troubleshooting.

## 13. Journey Readiness

### INV-JRN-001 — Readiness is derived, not manually guessed
Booking/travel readiness must be derived from authoritative capability states such as payment, traveller completeness, documents, visa, and operational prerequisites.

### INV-JRN-002 — Partial readiness remains visible
One completed requirement cannot conceal another incomplete or blocked requirement.

### INV-JRN-003 — Customer next action is explicit
Where a customer must act, the product should expose what is required, for whom, and the relevant status/deadline when known.

## 14. Admin, Support & Manual Overrides

### INV-ADM-001 — Admin actions follow the same domain rules
Admin UI must not bypass business invariants simply because the actor is privileged.

### INV-ADM-002 — Overrides are explicit
Where a controlled override is genuinely required, it must be a named action with authorization, reason, timestamp, actor, affected resource, and audit evidence.

### INV-ADM-003 — No destructive history rewriting
Support/admin staff must not rewrite historical payment, booking, publication, document, or visa facts to "fix" current state.

### INV-ADM-004 — Sensitive support access is least-privilege
Support staff receive only the customer/operator data required for the support action.

## 15. Audit & Historical Integrity

### INV-AUD-001 — Consequential actions are attributable
Audit evidence must identify the actor/system, action, affected resource, and time; reason and before/after values are captured where needed for accountability.

### INV-AUD-002 — Audit evidence cannot be casually edited
Audit records for security, finance, privileged access, publication, document review, and visa changes must be append-oriented and protected from ordinary application editing.

### INV-AUD-003 — Automated actions are attributable to a system identity
Background jobs/integrations must not appear as anonymous human actions.

## 16. Security & Privacy Invariants

### INV-SEC-001 — Deny by default
Access not explicitly permitted is denied.

### INV-SEC-002 — Secrets are not application content
Secrets/credentials must not be stored in source code, documents, logs, analytics, or client bundles.

### INV-SEC-003 — Sensitive data is not logged
Passwords, tokens, payment secrets, passport images, document URLs/tokens, and equivalent sensitive values must not be written to ordinary logs.

### INV-SEC-004 — Lower environments do not require production PII
Production personal/sensitive customer data must not be copied into lower environments as a normal testing mechanism.

### INV-SEC-005 — Security-sensitive external events are verified
Payment/document/integration callbacks that can affect business state require authenticity/integrity verification appropriate to the provider.

### INV-SEC-006 — Privileged identity requires stronger protection
Privileged users require stronger authentication controls than ordinary public browsing, including MFA for privileged production access.

## 17. Analytics & Reporting

### INV-ANA-001 — Analytics does not become a shadow PII store
Product/business analytics events must exclude passport/document content, payment secrets, authentication secrets, and unnecessary personal data.

### INV-ANA-002 — Operational metrics derive from authoritative states
Reporting must not create competing business truth for booking/payment/document/visa state.

## 18. Configuration

### INV-CFG-001 — Variable business policy is explicit
Business behaviour that legitimately varies must be represented through validated configuration/policy rather than hidden constants or UI assumptions.

### INV-CFG-002 — Configuration changes are versioned where commitments depend on them
Changing a payment schedule, cancellation policy, document requirement, cutoff, or similar policy must not silently rewrite existing bookings that were agreed under an earlier version.

### INV-CFG-003 — Security/integrity invariants are not operator-configurable
Operators cannot configure away authorization, audit, financial integrity, tenant isolation, or required security controls.

## 19. Cross-Capability Consistency

### INV-X-001 — One authoritative owner per business fact
Each business fact has one authoritative capability/source. Other modules consume/reference it rather than maintaining conflicting copies.

### INV-X-002 — Cross-module failures must not corrupt committed truth
Failure of notifications, analytics, OCR, or other secondary processing must not roll back or corrupt a valid committed booking/payment transaction unless that processing is an explicit transactional prerequisite.

### INV-X-003 — External systems are not assumed reliable
All provider integrations must define timeout, retry, idempotency, failure, and reconciliation behaviour before production use.

### INV-X-004 — Customer-visible state cannot be more certain than source truth
The UI must not convert pending, unknown, stale, or failed external information into a confirmed status.

## 20. Open MVP Policy Decisions

These are intentionally **not invented yet**. Each must be resolved before its dependent capability becomes Definition-of-Ready:

1. Supported MVP currency/currencies.
2. Deposit model: fixed amount, percentage, or operator-configured constrained model.
3. Instalment count, schedule, late-payment/grace behaviour.
4. Checkout inventory-hold duration and expiry UX.
5. Booking cutoff rule and timezone semantics.
6. Cancellation/refund rules, fees, deadlines, and approval authority.
7. Exact operator publication/approval workflow.
8. Behaviour of active bookings when an operator is suspended.
9. Exact occupancy model and whether one booking can contain mixed occupancy/room allocations in MVP.
10. Child/infant age thresholds and pricing — deferred unless brought into MVP.
11. Minimum traveller fields required before payment versus after confirmation.
12. Required-document policy source and versioning.
13. Exact visa internal states and customer-visible status mapping.
14. Sensitive-document retention/deletion schedule.
15. Notification channels required at MVP launch.
16. Support/escalation operating hours and responsibility boundaries.
17. Rules for package/departure material changes after customers have booked.
18. Quote expiry and price-change behaviour during checkout.

## Freeze Rule

An **Invariant** may only change through an explicit product/domain/architecture decision with impact analysis.

A **Policy** may evolve without redesigning the system, provided the change is validated, versioned, auditable, and does not retroactively alter existing commitments unless an explicit amendment process permits it.

An **Open decision** blocks implementation of the affected workflow once that decision becomes necessary for Definition-of-Ready.
