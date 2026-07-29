# NoorPath V2 UX / Information Architecture & Journey Baseline

Status: Draft baseline
Version: 0.1
Step: 14

## Purpose

Define how customers, operator staff and NoorPath staff move through the MVP before detailed Figma screen design begins.

The UX baseline must extend the approved NoorPath Landing and Package visual language rather than redesign the product. The core experience principle remains:

> Sacred calm + operational confidence.

## 1. UX Principles

- Trust before conversion.
- One clear primary action per decision point.
- Reveal complexity progressively rather than displaying the full operational model to customers.
- Preserve factual certainty: pending, unknown and blocked states remain visible as such.
- Booking Owner and Traveller are distinct in the UX where necessary, but the user should not need to understand domain terminology.
- Customer experience prioritises reassurance, transparency, next action and journey progress.
- Operator/Admin experience prioritises state, exceptions, accuracy, validation, safe actions and auditability.
- Mobile-first customer usability; admin responsive behaviour follows task suitability rather than blind stacking.
- WCAG 2.2 AA is a release requirement.
- Loading, empty, error, offline, conflict and permission states are designed, not left to implementation guesswork.

## 2. Primary Experience Areas

### Public Customer
- Landing
- Package discovery/list
- Package details
- Trust/operator information
- Price and payment explanation
- Sign in / continue to booking entry

### Checkout / Booking
- Booking context summary
- Room/occupancy selection
- Traveller details
- Review/quote
- Inventory hold state
- Payment handoff
- Payment result / pending state
- Booking confirmation

### Customer Journey
- Booking overview
- Payment progress
- Traveller readiness
- Document requirements/upload/review
- Visa status
- Departure/package information
- Support route

### Operator Operations
- Operational home / work queue
- Packages
- Departure batches
- Pricing
- Inventory
- Bookings
- Travellers/document context
- Visa workflow
- Payment visibility
- Exceptions requiring action

### NoorPath Administration
- Operator approval/status
- Cross-platform operational exceptions
- Privileged workflow review
- Audit/support/finance views according to permission

## 3. MVP Customer Information Architecture

Suggested top-level customer navigation:

- Home
- Packages
- My Journey (authenticated)
- Help / Support
- Account

Public browsing remains available without forcing authentication. Authentication is introduced when the user must create/manage a booking or access private journey information.

### Landing page role
The approved Landing page remains the primary entry and visual anchor. It should establish trust, explain NoorPath, surface relevant packages and drive discovery without becoming a dense travel portal.

### Package listing
Package cards should expose enough information to support comparison without duplicating the full detail page:
- package title
- operator identity
- origin
- departure/return timing
- duration
- key Makkah/Madinah accommodation summary
- starting/applicable price state
- availability state
- clear detail CTA

### Package detail
The approved Package page remains the source-of-truth pattern and should progressively answer:
1. Is this package right for me?
2. Who operates it?
3. When and from where does it travel?
4. Where will I stay in Makkah and Madinah?
5. What is included/excluded?
6. What will I pay and when?
7. Is it available?
8. What happens next?
9. Who can I contact?

## 4. MVP Booking Journey

Primary flow:

Discover -> Package Detail -> Select Departure/Occupancy -> Sign in / Identify Booking Owner -> Add Travellers -> Review Quote -> Acquire Hold -> Pay -> Confirm -> Journey Dashboard

### UX rules
- Package/departure context remains visible throughout checkout.
- Total, due now, remaining balance and payment-plan information remain clear before payment.
- Occupancy that cannot be sold is unavailable rather than failing after payment.
- Inventory hold/expiry is surfaced truthfully where it affects the customer.
- Price/availability changes require explicit reconfirmation; never silently substitute.
- A payment browser success page is not presented as final booking confirmation until trusted backend state confirms it.
- ConfirmationException/reconciliation scenarios receive calm, explicit messaging and human support escalation rather than generic failure.

## 5. Traveller UX

- Booking Owner may add one or more Travellers.
- Traveller forms collect only data required at that stage.
- Do not require passport/document data in general traveller profile fields unless the approved workflow requires it.
- Traveller-specific document/visa progress must remain distinct.
- Family booking should feel like managing people within one journey, not creating separate bookings.
- Future child/infant/Mahram complexity remains additive and must not clutter MVP where not in scope.

## 6. Payment UX

Before leaving NoorPath for provider payment, show:
- booking/package reference context
- total booking amount
- amount due now
- remaining amount / schedule where applicable
- payment provider handoff expectation
- what happens after payment

Post-payment states:
- Confirmed payment + confirmed booking
- Payment processing / awaiting verification
- Payment failed
- Payment settled but booking confirmation exception requiring support/reconciliation

Never show final success solely from a frontend/provider redirect parameter.

## 7. Journey Dashboard

The customer dashboard is composed from authoritative states rather than a manually editable journey status.

Core structure:
- booking summary
- overall next action
- payment progress
- Traveller cards
- document checklist/progress per Traveller
- visa progress per Traveller
- departure/package information
- support contact

Recommended derived journey labels:
- Action Required
- In Progress
- Ready
- Blocked
- Completed

The UI should explain the reason beneath the label rather than relying on colour alone.

## 8. Document UX

Document workflow per requirement:
- requirement explanation
- who the document is for
- upload
- validating
- under review
- approved
- correction required
- rejected/invalid where applicable
- resubmit

Rules:
- communicate privacy/security expectations simply
- do not expose permanent document URLs
- file validation failures explain allowed corrective action
- previous rejected/corrected submission history need not overwhelm customer UI but must remain available operationally

## 9. Visa UX

Customer-facing states remain simpler than internal operations:
- Preparing
- Submitted
- Processing
- Action Required
- Approved
- Not Approved
- Cancelled

Rules:
- never imply official approval without authoritative evidence
- show next action where one exists
- distinguish NoorPath/operator preparation from authority processing
- traveller-specific visa state remains visible in family bookings

## 10. Support UX

MVP support remains intentionally thin:
- contextual Help/Support entry points
- booking reference carried into support context where safe
- clear human contact path for payment/document/visa exceptions
- emergency wording reserved only for genuine operational need

Structured ticketing remains V1.x unless promoted by MVP operations requirements.

## 11. Operator/Admin Information Architecture

Admin is a workflow system, not a marketing dashboard.

Suggested primary navigation:
- Work Queue / Overview
- Packages
- Departures
- Pricing
- Inventory
- Bookings
- Documents / Readiness
- Visa
- Payments
- Operators (NoorPath roles only where applicable)
- Audit / Reports according to role

### Work Queue
Prioritise exceptions/actions, for example:
- packages awaiting review
- departures blocked from publication
- low/zero inventory
- booking confirmation exceptions
- payment reconciliation cases
- document corrections/reviews
- visa action-required cases

### Detail page pattern
Operational detail pages should consistently provide:
- identity/context header
- authoritative state
- current blockers
- relevant timeline/history
- domain-specific facts
- allowed actions only
- reason capture where consequential
- related business context without duplicating ownership

## 12. Publication UX

Package/departure publishing should be a guided workflow rather than one giant undifferentiated form.

Recommended structure:
- Package content
- Makkah stay
- Madinah stay
- Travel details
- Inclusions/exclusions
- Pricing
- Inventory
- Review / validation
- Publish

MVP may simplify the number of screens, but validation categories and source ownership remain clear.

## 13. Error and Exception UX

Every critical journey must explicitly design:
- validation error
- authentication expired
- permission denied
- resource unavailable
- price changed
- inventory no longer available
- hold expired
- payment failed
- payment pending
- confirmation exception
- document unsafe/invalid
- document correction required
- visa action required
- provider unavailable
- network/offline interruption
- stale/concurrent admin edit

Rules:
- preserve user-entered safe data where possible
- explain what happened, what was preserved and the next action
- avoid generic "Something went wrong" for recoverable business states
- operationally important exceptions expose correlation/reference information safe for support

## 14. Responsive Baseline

Customer journeys are designed for:
- 390px
- 360px
- 320px where required
- tablet
- desktop

Rules:
- no horizontal scrolling for core customer workflows
- primary CTA remains reachable and contextually clear
- dense content is progressively disclosed on small screens
- package price/availability/trust information must not disappear on mobile

Admin:
- desktop-first for dense operational workflows
- tablet support for practical workflows
- mobile support only for actions that remain safe/useful; complex tables need alternate responsive patterns rather than shrinking columns blindly

## 15. Accessibility Baseline

- WCAG 2.2 AA
- semantic headings/landmarks
- keyboard operability
- visible focus
- correct form labels/instructions
- accessible error association
- status changes announced appropriately
- sufficient contrast
- colour never sole state indicator
- 44px target guidance for important touch controls
- text zoom/reflow support
- reduced-motion preference honoured
- dialogs/focus management implemented correctly
- tables use appropriate semantics and responsive alternatives

## 16. Content and Trust Baseline

Customer content should be:
- factual
- calm
- explicit
- culturally respectful
- free of false urgency
- clear about pending versus confirmed information

Trust elements include:
- operator identity
- price transparency
- availability truth
- package inclusions/exclusions
- accommodation/travel confirmation states
- policies
- booking/payment status
- human support

Religious imagery/language does not substitute for commercial evidence.

## 17. Design Authority and Workflow

Design authority chain:

Approved Landing + Package references -> MASTER -> Figma -> tokens/components -> production

Workflow per feature:
- product requirement
- business/state rules
- UI UX Pro Max analysis
- Figma flow/screens/components
- Impeccable visual refinement
- Emil-style interaction/motion review where useful
- Product Owner approval
- token/MASTER update where needed
- implementation
- Playwright + accessibility + visual verification

Ponytail is used only after design approval to keep implementation simple; it has no visual/product authority.

## 18. Figma Structure

- 00 Foundations
- 01 Brand
- 02 Tokens
- 03 Primitives
- 04 Customer Components
- 05 Admin Components
- 06 Patterns
- 07 Customer Journeys
- 08 Operator Journeys
- 09 Edge States
- 10 Accessibility
- 11 Approved Screens
- 12 Explorations / Archive

Only Approved Screens become implementation authority.

## 19. Definition of Ready for Detailed Design

A feature may enter detailed Figma design when:
- persona/actor known
- user outcome defined
- domain owner known
- state machine relevant states known
- business rules known or explicitly flagged
- permission rules known
- sensitive-data constraints known
- happy path known
- important edge/error states known
- responsive intent known
- content/trust requirements known
- acceptance outcome known

## 20. Explicit MVP UX Non-Goals

- advanced package comparison
- complex loyalty/rewards
- rich support ticketing
- OCR-driven auto-fill
- AI trip assistant
- real-time chat
- dynamic airline/hotel supplier inventory
- sophisticated amendment flows
- decorative motion-heavy interfaces
- generic admin analytics dashboards unrelated to action

## 21. Step 14 Freeze Rule

Detailed visual design may refine layout, hierarchy, components and interaction, but may not silently redefine domain state, pricing truth, inventory behaviour, payment confirmation, document privacy, visa certainty or authorization rules.

Any UX proposal requiring a new business rule or state transition returns to Product/Domain for an explicit decision.
