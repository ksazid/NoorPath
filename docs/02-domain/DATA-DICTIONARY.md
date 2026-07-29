# NoorPath V2 Data Dictionary

Status: Draft baseline  
Version: 0.1  
Step: 7

## Purpose

This dictionary defines the semantic meaning, authoritative owner, relationships, mutability, and sensitivity of NoorPath's core business data before physical database design.

It is a logical contract. It does **not** define PostgreSQL column types, EF entities, indexes, storage accounts, or API payloads yet.

## Classification

### Mutability
- **Mutable** — may change while preserving business rules and audit where required.
- **Versioned** — changes create a new version; earlier committed versions remain reproducible.
- **Append-only** — facts are added/compensated, not rewritten or deleted as ordinary operations.
- **Derived** — calculated from authoritative facts and never independently edited.
- **Snapshot** — immutable historical evidence captured at a commitment point.

### Sensitivity
- **Public** — intentionally suitable for public display.
- **Internal** — operational data not intended for public exposure.
- **Confidential** — personal/commercial data requiring scoped access.
- **Highly Sensitive** — identity documents, authentication/payment secrets, or equivalent data requiring strongest controls.

---

# 1. Shared Semantic Types

## Entity Identifier
**Meaning:** stable opaque identifier for a domain object.  
**Rule:** identifiers must not encode mutable business meaning or tenant secrets.  
**Mutability:** immutable.

## Operator Scope
**Meaning:** operator/tenant ownership context applied to operator-owned resources.  
**Rule:** cross-operator access is denied by default.  
**Mutability:** immutable for historical commercial ownership unless an explicit governed transfer process exists.

## Money
**Meaning:** monetary amount plus explicit currency.  
**Rule:** an amount without currency is invalid business data. Silent conversion is prohibited.  
**Mutability:** depends on owning fact; settled financial facts are append-only.

## Version Reference
**Meaning:** identifier of the exact package, price, policy, or requirement version used by another domain.  
**Rule:** historical commitments reference the exact version, not only the mutable parent object.

## Actor Reference
**Meaning:** human or system identity responsible for an action.  
**Rule:** consequential manual/system actions must be attributable.

## Effective Period
**Meaning:** explicit time window in which a version/policy is valid.  
**Rule:** timezone semantics must be explicit whenever cutoff/expiry behaviour depends on time.

---

# 2. Identity & Access

## Account
**Owner:** Identity  
**Meaning:** authenticated NoorPath user/principal. It is not the same as Traveller.  
**Key facts:** AccountId, authentication subject/provider linkage, account status, security/recovery state, privileged MFA state where applicable.  
**Relationships:** may own/manage Bookings; may link to a Traveller; may participate in OperatorMembership.  
**Mutability:** mutable security/profile state; identity linkage changes are security-sensitive.  
**Sensitivity:** Confidential; credentials/tokens themselves are Highly Sensitive and must not be stored as ordinary domain fields.

## AuthenticationSubject
**Owner:** Identity  
**Meaning:** stable identifier from the selected identity provider representing the authenticated principal.  
**Mutability:** immutable per provider relationship.  
**Sensitivity:** Confidential.

## AccountStatus
**Owner:** Identity  
**Meaning:** whether the principal may authenticate/use relevant NoorPath account capabilities.  
**Rule:** account status alone does not grant operator/resource authorization.  
**Mutability:** mutable, auditable for privileged/security-relevant changes.

---

# 3. Operators

## Operator
**Owner:** Operators  
**Meaning:** Umrah operator/agency represented on NoorPath.  
**Key facts:** OperatorId, business/legal identity, public display identity, contact/support facts, operating status, platform publication eligibility/approval state.  
**Relationships:** owns PackageTemplates, PackageVersions, DepartureBatches and operator-scoped staff memberships.  
**Mutability:** business profile mutable; lifecycle changes auditable; historical ownership retained.  
**Sensitivity:** mix of Public and Confidential depending on field.

## OperatorDisplayName
**Owner:** Operators  
**Meaning:** operator name approved for customer-facing display.  
**Mutability:** mutable with history where changes could affect historical customer evidence.  
**Sensitivity:** Public.

## OperatorApprovalState
**Owner:** Operators  
**Meaning:** NoorPath platform decision governing whether the operator may publish/sell.  
**Rule:** Catalogue cannot make an ineligible operator publicly bookable.  
**Mutability:** mutable through governed workflow; auditable.  
**Sensitivity:** Internal.

## OperatorOperatingStatus
**Owner:** Operators  
**Meaning:** current active/suspended/deactivated operating state.  
**Rule:** status changes never delete existing commitments or historical records.  
**Mutability:** mutable and auditable.  
**Sensitivity:** Internal; customer-facing projection only when intentionally disclosed.

## OperatorMembership
**Owner:** Operators  
**Meaning:** relationship between an Identity principal and an Operator.  
**Key facts:** OperatorMembershipId, OperatorId, Account/PrincipalId, role/permission assignment, membership status.  
**Mutability:** mutable; permission changes auditable.  
**Sensitivity:** Internal/Confidential.

---

# 4. Catalogue

## PackageTemplate
**Owner:** Catalogue  
**Meaning:** reusable operator-owned definition of an Umrah product independent of a specific dated departure.  
**Key facts:** PackageTemplateId, OperatorId, working name/identity, draft content source.  
**Relationships:** parent of PackageVersions. DepartureBatches sell a specific PackageVersion.  
**Mutability:** mutable while being authored; published customer commitments reference versions, not mutable template state.  
**Sensitivity:** Internal until published projection exists.

## PackageVersion
**Owner:** Catalogue  
**Meaning:** immutable/versioned commercial content describing a package at a point in time.  
**Key facts:** PackageVersionId, PackageTemplateId, version identifier, package name/title, summary, duration/stay structure, inclusions, exclusions, travel/transport facts, Makkah accommodation facts, Madinah accommodation facts, content confirmation states.  
**Relationships:** used by DepartureBatch and snapshotted/referenced by Booking.  
**Mutability:** versioned; a committed/published version is not silently rewritten.  
**Sensitivity:** Public when published; otherwise Internal.

## AccommodationStay
**Owner:** Catalogue  
**Meaning:** package accommodation facts for one destination/stay segment. Makkah and Madinah stays are independent.  
**Key facts:** city, hotel/display name, supported classification claim, distance disclosure where factual, stay dates/nights/sequence, accommodation confirmation state.  
**Rule:** claims cannot be inferred from package tier or another city.  
**Mutability:** part of PackageVersion or Departure-specific facts; material changes require version/history preservation.  
**Sensitivity:** Public when published.

## TravelFact
**Owner:** Catalogue  
**Meaning:** sale-relevant flight/transport/route fact.  
**Key facts:** route/origin/destination, description/reference, confirmed/pending state.  
**Rule:** pending facts are never shown as confirmed.  
**Mutability:** versioned or departure-specific with history for material changes.  
**Sensitivity:** Public when published.

## DepartureBatch
**Owner:** Catalogue  
**Meaning:** dated saleable departure associated with an operator and package version.  
**Key facts:** DepartureBatchId, OperatorId, PackageVersionId, origin, departure date/time, return date/time, booking cutoff reference, lifecycle/publication state.  
**Relationships:** referenced by Pricing, Inventory and Booking.  
**Mutability:** lifecycle/facts may change under governed rules; booked history is preserved.  
**Sensitivity:** Public when published; otherwise Internal.

## PublicationState
**Owner:** Catalogue  
**Meaning:** whether a package/departure is draft/reviewable/published/paused/closed/cancelled/archived according to the frozen state machine.  
**Rule:** only valid sellable states appear bookable publicly.  
**Mutability:** controlled state transitions; auditable.

---

# 5. Pricing

## PricePlan
**Owner:** Pricing  
**Meaning:** editable commercial pricing definition for a package/departure before immutable price versions are issued.  
**Key facts:** PricePlanId, relevant Catalogue references, supported occupancy categories, commercial rule set.  
**Mutability:** mutable draft definition.  
**Sensitivity:** Internal until published via a PriceVersion.

## PriceVersion
**Owner:** Pricing  
**Meaning:** exact immutable/versioned price definition usable for quoting and booking.  
**Key facts:** PriceVersionId, PricePlanId, currency, occupancy prices, fee/tax treatment, deposit/instalment plan definition, effective period, policy version references.  
**Rule:** existing bookings are never silently repriced by publishing another version.  
**Mutability:** versioned/immutable after publication or use in a committed quote/booking.  
**Sensitivity:** Public when used for public offer; otherwise Internal.

## OccupancyPrice
**Owner:** Pricing  
**Meaning:** price applicable to one supported room/occupancy basis.  
**Key facts:** occupancy key (for example double/triple/quad where offered), amount, currency, traveller/pricing basis.  
**Rule:** only occupancies supported by both Pricing and Inventory may be sold.  
**Mutability:** part of PriceVersion.

## PaymentPlanDefinition
**Owner:** Pricing  
**Meaning:** commercial schedule template describing due-now and future amounts/dates/rules.  
**Rule:** exact amounts/rules are an open MVP policy until approved; the booking's selected schedule must later be snapshotted.  
**Mutability:** versioned.

## Quote
**Owner:** Pricing until commitment; Booking preserves committed evidence.  
**Meaning:** calculated offer for a specific departure, occupancy/traveller basis and PriceVersion at a point in time.  
**Key facts:** QuoteId where persisted, PriceVersionId, calculation inputs, total, due-now, remaining amount, fee/tax disclosure, validity/expiry where applicable.  
**Rule:** customer-visible arithmetic must be explainable and reconcile with the referenced PriceVersion.  
**Mutability:** ephemeral or immutable evidence; never silently modified after customer commitment.  
**Sensitivity:** Confidential once associated with a customer/booking.

---

# 6. Inventory

## InventoryPool
**Owner:** Inventory  
**Meaning:** controlled saleable capacity for a defined unit of a DepartureBatch.  
**Key facts:** InventoryPoolId, DepartureBatchId, saleable unit/occupancy key, capacity, adjustment history.  
**Rule:** availability is operational truth, not Catalogue content.  
**Mutability:** mutable under concurrency controls; manual adjustments auditable.  
**Sensitivity:** Internal; derived availability projection may be Public.

## AvailableQuantity
**Owner:** Inventory  
**Meaning:** capacity currently available after authoritative holds/reservations/adjustments.  
**Mutability:** Derived. Never directly edited as an independent business fact.

## InventoryHold
**Owner:** Inventory  
**Meaning:** temporary claim on inventory during booking/checkout.  
**Key facts:** InventoryHoldId, InventoryPoolId, requested quantity/unit, creation time, expiry, state, booking/checkout correlation reference.  
**Rule:** expired/released holds cannot be used as valid availability; conversion to reservation is idempotent.  
**Mutability:** controlled lifecycle; expiry/release preserved for audit/operations.

## Reservation
**Owner:** Inventory  
**Meaning:** confirmed allocation of controlled inventory to a booking.  
**Key facts:** ReservationId, InventoryPoolId, BookingId reference, quantity/unit, confirmation state/time.  
**Rule:** normal operations must not create confirmed reservations beyond sellable capacity.  
**Mutability:** controlled lifecycle; release/cancellation is explicit, not deletion.

---

# 7. Traveller

## Traveller
**Owner:** Traveller  
**Meaning:** a person travelling under one or more NoorPath workflows; not equivalent to Account.  
**Key facts:** TravellerId, justified identity facts such as legal/travel name, date of birth, traveller category when supported, contact linkage where required.  
**Relationships:** referenced by Booking, Documents and Visa; may optionally link to an Account.  
**Mutability:** mutable with appropriate history/validation when used in active commitments.  
**Sensitivity:** Confidential.

## BookingOwnerLink
**Owner:** Booking for a specific booking; Identity provides Account identity.  
**Meaning:** identifies the Account responsible for the customer booking relationship.  
**Rule:** every customer booking has one identifiable Booking Owner.  
**Sensitivity:** Confidential.

## Passport/Identity Document Facts
**Owner:** Documents unless explicitly promoted to Traveller for a justified operational need.  
**Rule:** passport number/image/details are not generic Traveller profile fields by default. Document-derived identity data receives Highly Sensitive handling.  
**Sensitivity:** Highly Sensitive.

---

# 8. Booking

## Booking
**Owner:** Booking  
**Meaning:** commercial agreement/lifecycle linking Booking Owner, Travellers, booked offering, price evidence and inventory reservation.  
**Key facts:** BookingId, BookingReference, BookingOwnerAccountId, OperatorId reference, DepartureBatchId, PackageVersionId/booked package evidence, PriceVersionId/Quote evidence, lifecycle state, creation/commitment timestamps, policy version references.  
**Relationships:** contains booking participants/room allocation; references Inventory reservation; Payments/Documents/Visa reference BookingId.  
**Mutability:** controlled state machine; historical commitment facts are immutable/snapshotted.  
**Sensitivity:** Confidential.

## BookingReference
**Owner:** Booking  
**Meaning:** customer/operations-friendly unique reference for a booking.  
**Rule:** not a security credential and must not grant access by possession alone.  
**Mutability:** immutable.

## BookingTraveller
**Owner:** Booking  
**Meaning:** participation of a Traveller in a specific Booking.  
**Key facts:** BookingId, TravellerId, booking-specific role/category/room assignment references where needed.  
**Rule:** removing/replacing travellers after commitment requires an explicit amendment policy; MVP amendment is deferred.  
**Mutability:** controlled by booking lifecycle/policy.

## RoomAllocation / OccupancySelection
**Owner:** Booking for the committed allocation; Catalogue/Pricing/Inventory define what is sellable.  
**Meaning:** booking-specific selection mapping traveller(s) to the sold occupancy/room basis.  
**Rule:** exact mixed-room behaviour remains an open MVP decision.  
**Mutability:** Snapshot/controlled after commitment.

## BookedCommercialSnapshot
**Owner:** Booking  
**Meaning:** immutable evidence needed to reproduce what the customer agreed to, while retaining source version references.  
**Includes:** relevant package/departure display facts, pricing summary, fee/tax disclosure, payment schedule, policy/version references, selected occupancy and other material agreed facts.  
**Rule:** snapshot is historical evidence, not current Catalogue/Pricing truth.  
**Mutability:** Snapshot.  
**Sensitivity:** Confidential.

## BookingState
**Owner:** Booking  
**Meaning:** booking lifecycle only; payment/document/visa states remain separate.  
**Mutability:** controlled state machine, auditable.

---

# 9. Payments & Refunds

## Payment
**Owner:** Payments  
**Meaning:** payment transaction/process associated with a Booking financial obligation.  
**Key facts:** PaymentId, BookingId, amount/currency, provider, provider transaction reference, authoritative status, settled time where applicable.  
**Rule:** browser success alone cannot settle Payment; provider evidence/reconciliation is required.  
**Mutability:** processing state mutable; settled financial facts preserved.  
**Sensitivity:** Confidential. Raw PAN/CVV are never stored.

## PaymentAttempt
**Owner:** Payments  
**Meaning:** one provider initiation/attempt for a payment obligation.  
**Key facts:** attempt ID, PaymentId/BookingId, provider reference, created time, outcome/error category.  
**Mutability:** append-oriented history.  
**Sensitivity:** Confidential; provider secrets/tokens excluded from ordinary storage/logging.

## LedgerEntry
**Owner:** Payments  
**Meaning:** append-only financial fact representing money settled, refunded, adjusted or otherwise recognized by NoorPath's financial model.  
**Key facts:** LedgerEntryId, BookingId, entry type, signed/business amount, currency, source Payment/Refund reference, effective/recorded time.  
**Mutability:** Append-only. Corrections use compensating entries.  
**Sensitivity:** Confidential.

## Refund
**Owner:** Payments  
**Meaning:** explicit return of funds related to a prior payment/booking.  
**Key facts:** RefundId, BookingId, source PaymentId where applicable, amount/currency, reason/policy reference, provider reference, status.  
**Rule:** refund never erases original payment history.  
**Mutability:** processing state mutable; settled result append-oriented.  
**Sensitivity:** Confidential.

## OutstandingBalance
**Owner:** Payments as a derived financial projection from Booking's agreed obligation plus authoritative ledger.  
**Mutability:** Derived. Never manually edited.

---

# 10. Documents

## DocumentRequirementSet
**Owner:** Documents  
**Meaning:** versioned policy defining required document types/evidence for a relevant journey context.  
**Key facts:** RequirementSetId/version, applicability conditions, requirement definitions, effective period.  
**Mutability:** Versioned.

## TravellerDocumentRequirement
**Owner:** Documents  
**Meaning:** required document/evidence instance for one Traveller in one relevant Booking/journey context.  
**Key facts:** TravellerDocumentRequirementId, TravellerId, BookingId/context, source RequirementSetVersion, requirement type, readiness status.  
**Mutability:** controlled lifecycle; completion derived from valid submission/review state.  
**Sensitivity:** Confidential.

## DocumentSubmission
**Owner:** Documents  
**Meaning:** submitted file/evidence against a traveller document requirement.  
**Key facts:** DocumentSubmissionId, requirement/traveller references, private storage reference, safe file metadata, validation state, review state, submission/review timestamps.  
**Rule:** permanent public URLs are prohibited.  
**Mutability:** lifecycle mutable; prior submissions/reviews preserved.  
**Sensitivity:** Highly Sensitive for passports/identity documents.

## SecureStorageReference
**Owner:** Documents  
**Meaning:** opaque reference to the private object, not a permanent public URL.  
**Mutability:** infrastructure reference may rotate/migrate while logical document identity remains stable.  
**Sensitivity:** Highly Sensitive/Internal.

## DocumentReviewState
**Owner:** Documents  
**Meaning:** submitted/validating/under-review/approved/correction-required/rejected or equivalent frozen state machine.  
**Mutability:** controlled and auditable.

---

# 11. Visa

## VisaCase
**Owner:** Visa  
**Meaning:** traveller-specific visa-processing case for the journey.  
**Key facts:** VisaCaseId, TravellerId, BookingId/context, internal workflow state, customer-visible state projection, evidence references, history.  
**Rule:** NoorPath status must not imply official authority approval without authoritative evidence.  
**Mutability:** controlled state machine; changes auditable.  
**Sensitivity:** Confidential; evidence may be Highly Sensitive.

## VisaInternalState
**Owner:** Visa  
**Meaning:** operational workflow state used by authorized staff.  
**Rule:** exact vocabulary remains an open policy decision until frozen.  
**Mutability:** controlled.

## VisaCustomerState
**Owner:** Visa as an explicit mapping/projection from internal/authoritative state.  
**Meaning:** simplified customer-facing state that cannot overclaim certainty.  
**Mutability:** Derived/versioned mapping.

---

# 12. Notifications

## NotificationTemplateVersion
**Owner:** Notifications  
**Meaning:** versioned transactional message template for a channel/use case.  
**Mutability:** Versioned.  
**Sensitivity:** Internal; template content must minimize sensitive data.

## NotificationRequest
**Owner:** Notifications  
**Meaning:** request to communicate because a business event/operational rule requires it.  
**Key facts:** NotificationRequestId, event/use-case reference, recipient/channel reference, template version, deduplication key, lifecycle.  
**Mutability:** controlled lifecycle.  
**Sensitivity:** Confidential depending on recipient/context.

## NotificationDeliveryAttempt
**Owner:** Notifications  
**Meaning:** one provider delivery attempt.  
**Key facts:** attempt ID, request ID, provider reference, outcome, timestamp, retry metadata.  
**Mutability:** append-oriented.

---

# 13. Support

## SupportRoute
**Owner:** Support / Platform Configuration depending on scope  
**Meaning:** approved human support contact/path available to customers/operators.  
**Mutability:** Mutable, governed.  
**Sensitivity:** Public/Internal depending on route.

## SupportContextLink
**Owner:** Support  
**Meaning:** scoped linkage allowing authorized support staff to locate relevant booking/operator context without copying domain truth.  
**Mutability:** Mutable.  
**Sensitivity:** Confidential.

## SupportCase
**Owner:** Support  
**Release:** V1.x unless MVP scope changes.  
**Meaning:** structured support workflow with category, assignment, status, escalation and history.  
**Mutability:** controlled lifecycle.

---

# 14. Audit

## AuditRecord
**Owner:** Audit  
**Meaning:** protected accountability evidence for consequential manual/system actions.  
**Key facts:** AuditRecordId, actor/system identity reference, action, target capability/resource ID, timestamp, reason where required, safe before/after evidence, correlation/context.  
**Rule:** audit does not become a second copy of business truth and must not store secrets/document contents unnecessarily.  
**Mutability:** Append-only/protected.  
**Sensitivity:** Internal/Confidential.

---

# 15. Reporting & Analytics

## ReportingProjection
**Owner:** Reporting  
**Meaning:** derived/read-optimized representation of authoritative domain facts for operations/product analysis.  
**Mutability:** Derived/rebuildable.  
**Rule:** cannot be used as an independent write source for domain state.

## ProductAnalyticsEvent
**Owner:** Reporting/Analytics  
**Meaning:** privacy-safe behavioural/business event used to validate the product.  
**Rule:** no passport/document content, payment secrets, authentication secrets or unnecessary personal data.  
**Mutability:** append-oriented analytics record.

---

# 16. Platform Configuration

## GlobalSupportContact
**Owner:** Platform Configuration  
**Meaning:** platform-wide support contact where no natural capability-specific owner exists.  
**Mutability:** Mutable, governed.

## FeatureAvailabilityFlag
**Owner:** Platform Configuration  
**Meaning:** controlled rollout/availability switch, not a replacement for authorization or business invariants.  
**Mutability:** Mutable, audited for production-sensitive changes.

---

# 17. Derived Experience Concepts — Not Authoritative Data

## PublicPackageCard
Derived from Operators + Catalogue + Pricing + Inventory. It must not have independently editable operator, package, price or availability truth.

## CheckoutSummary
Derived from Catalogue + Pricing + Inventory + Traveller context. The committed result becomes Booking evidence; checkout itself is not a domain owner.

## JourneyReadiness
Derived from Booking + Payments + Traveller + Documents + Visa + operational prerequisites. It is never manually edited as a competing status.

## AdminDashboardState
Derived/composed from owning capabilities. Admin commands route back to the owner of each business fact.

---

# 18. Core Relationship Model

- Account may manage many Bookings.
- Account may optionally link to one or more Traveller relationships according to future profile policy; Traveller never requires an account in MVP.
- Operator has many OperatorMemberships.
- Operator has many PackageTemplates.
- PackageTemplate has many PackageVersions.
- PackageVersion may be used by many DepartureBatches.
- DepartureBatch may have one or more PriceVersions through Pricing definitions over time.
- DepartureBatch has InventoryPools for supported saleable units/occupancies.
- Booking references one saleable DepartureBatch and the exact PackageVersion/PriceVersion evidence used for commitment.
- Booking contains one or more BookingTravellers referencing Traveller records.
- Booking references InventoryHold/Reservation evidence needed for sold capacity.
- Payments references Booking; ledger facts do not live inside Booking.
- Documents references Traveller and Booking/journey context.
- VisaCase references Traveller and Booking/journey context and consumes Documents evidence.
- Notifications, Audit and Reporting consume domain outcomes but do not own those outcomes.

---

# 19. Explicitly Unresolved Data Decisions

The dictionary deliberately does not invent values for:

1. MVP currency/currencies.
2. Deposit formula and instalment schedule.
3. Hold duration.
4. Booking cutoff timezone/rule.
5. Cancellation/refund policy.
6. Exact operator approval/publication workflow.
7. Mixed occupancy/room-allocation rules.
8. Minimum traveller facts before payment versus post-booking.
9. Child/infant categories and thresholds.
10. Exact document requirement source/version model details.
11. Exact visa state vocabulary/mapping.
12. Document retention/deletion periods.
13. Notification channels required for launch.
14. Exact support responsibility/operating hours.
15. Quote expiry/price-change behaviour during checkout.

These remain blockers for the affected workflow's Definition of Ready, not excuses for implementation to invent defaults.

---

# 20. Data Dictionary Governance

1. Every authoritative fact must have one owning capability.
2. New fields require a business purpose and sensitivity classification.
3. Highly Sensitive fields require explicit security/privacy design before implementation.
4. Historical commercial and financial evidence is never overwritten for convenience.
5. Derived fields are not independently writable.
6. Cross-capability references use stable IDs/contracts rather than foreign ORM entity graphs.
7. Physical schema design must trace each persisted field back to a dictionary concept or an explicitly documented technical concern.
8. Any conflict with Business Rules & Invariants is resolved in favour of the invariant until a governed decision changes it.
