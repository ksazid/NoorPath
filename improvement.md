# NoorPath Booking and Operator Experience Improvement Plan

## Product goal

- Let customers reserve an Umrah package in under two minutes.
- Let operators create or clone a standard package in a few guided steps.
- Keep every package visually and linguistically consistent regardless of operator.
- Preserve the approved Package Details design already present in the repository.

## Customer booking flow

- Package listing → Package Details → traveller count → room occupancy → exact price → Reserve Your Seats → mobile OTP → reservation payment → My Journey.
- Package Details is the primary booking surface; do not introduce an unnecessary separate configurator.
- Ask for full traveller and passport information only after reservation.
- Preserve every selection through authentication.

## Customer authentication

- Mobile number and OTP only.
- No password and no Google login in the customer flow.
- Email may be collected later as an optional communication channel.

## Occupancy and pricing

Use fixed NoorPath terminology and small approved avatar icons:

- 2-person room — Double sharing
- 3-person room — Triple sharing
- 4-person room — Quad sharing

Always show separately:

- Price per traveller
- Total journey price
- Reserve today amount
- Remaining journey balance

Recommend valid room combinations automatically for larger groups.

## Customer payment structures

Use the common heading **Journey Payment Options**. A package may offer:

- Full Payment at Booking
- Reservation + One Remaining Payment
- Reservation + Journey Payment Schedule

Each scheduled stage is a **Payment Milestone**. Avoid EMI, installment, loan, and finance terminology.

Default milestone suggestions:

1. Seat Reservation
2. Documents Completion
3. Visa Processing
4. Flight Confirmation
5. Final Payment Before Departure

Operators may change approved milestone labels, amounts, percentages, dates, and trigger rules. Validation must ensure the complete schedule equals the package price and no payment is due after departure.

## My Journey

My Journey is the customer source of truth after reservation. It should show:

- Next required action
- Journey progress timeline
- Important updates
- Traveller readiness
- Payment status and receipts
- Document upload and review status
- Visa progress
- Flight and hotel confirmation
- Departure countdown
- Available downloads
- WhatsApp Support and Request a Callback

Recommended downloads include booking confirmation, invoices, receipts, visa, e-ticket, flight itinerary, hotel confirmation, final itinerary, Ziyarah schedule, packing checklist, and emergency contact sheet. Do not show empty download placeholders.

## Customer navigation

Before login:

- Packages
- How It Works
- Talk to Us
- My Journey

Talk to Us contains:

- WhatsApp Support
- Request a Callback

After login:

- Packages
- My Journey
- Help
- Talk to Us
- Profile

Use the full approved footer on public pages and a compact legal/support footer during OTP, checkout, payment, and document upload.

## Portal separation

- Customer experience uses the current deployed customer domain during development and later `noorpath.in`.
- Administrators, operators, and future agents use the current staff domain during development and later `portal.noorpath.in`.
- Domain values must be configurable rather than hard-coded.
- Operators and agents require administrator approval before access.

## Staff navigation

Use one sidebar with non-clickable section headings:

### Overview

- Dashboard

### Content

- Packages

### Operations

- Bookings
- Departures
- Customers
- Documents
- Visa Cases
- Payments
- Support

### Administration

- Team
- Reports
- Audit Log
- Settings

Hide modules the signed-in role cannot access.

## Domain model

- Package: the commercial product offered for sale.
- Departure: the operational group travelling on a particular date.
- Booking: the customer reservation.
- Traveller: an individual pilgrim.

## Package workspace

When staff open a package, provide tabs or equivalent in-context navigation:

- Overview
- Departures
- Customers
- Payments
- Documents
- Visa
- Package Content
- Activity

The Customers view should show booking reference, traveller count, occupancy, total price, paid amount, remaining balance, booking source, assigned agent where applicable, and status.

## Departures

A departures view is required so operators can manage actual travel batches. Show:

- Package name
- Departure city and date
- Duration
- Capacity, reserved, and available seats
- Flight and hotel readiness
- Document, visa, and payment readiness
- Assigned group leader
- Status

Provide filters for upcoming, departing soon, full, attention required, completed, and cancelled departures.

## Fast operator package creation

Entry choices:

- Start from NoorPath template
- Clone an existing package
- Continue a saved draft

Recommended guided flow:

1. Basic information and dates
2. Hotels, flights, and transport
3. Occupancy, pricing, and capacity
4. Standard inclusions and itinerary
5. Journey Payment Options
6. Preview and submit for review

Use autosave, smart defaults, progressive disclosure, structured selectors, and inline validation.

## Automatic duration

Operators enter departure and return dates. NoorPath calculates:

- Nights = return date minus departure date
- Days = nights + 1

Use the calculated duration in headings and summaries. Makkah, Madinah, and transit nights must reconcile with total nights. Block submission when they conflict.

## Future packages

Operators may publish packages at least six months before departure. A future package may contain a controlled mix of confirmed and pending details.

Details that may remain pending include final flight schedule, exact room allocation, final Ziyarah timings, and final transport timing. Pending information must be clearly labelled and never presented as confirmed.

A publishable future package must still disclose departure date or window, duration, origin, stay plan, occupancy prices, total price, reservation amount, payment structure, inclusions, and cancellation terms.

## Standard inclusion catalogue

NoorPath owns the wording, icon, description style, and display order. Operators select applicability and enter only package-specific details.

Initial catalogue:

1. Return Flights
2. Umrah Visa Included
3. Makkah Hotel
4. Madinah Hotel
5. Airport Transfers
6. Intercity Transport
7. Daily Meals Included
8. Guided Ziyarah
9. Group Leader and Support
10. Travel Kit
11. Umrah Kit

Intercity Transport details may be Bus, High-Speed Train, Private Coach, or Mixed.

Daily Meals Included normally means breakfast, lunch, and dinner; deviations must be shown explicitly.

Each catalogue item has one state: Included, Excluded, or Not specified. Selecting Included removes it from Excluded and vice versa. Contradictions must be impossible.

## Design-system governance

- The approved landing and Package Details designs in the repository are mandatory visual sources of truth.
- Use one approved icon family and centrally mapped custom Umrah icons where needed.
- Operators cannot alter icons, colours, typography, spacing, cards, buttons, section order, or standard labels.
- Package preview must render the real customer-facing desktop and mobile components.
- Administrator review verifies commercial completeness and design-system compliance.

## Fixed Package Details order

1. Hero and package heading
2. Verified operator
3. Hotels
4. Occupancy and pricing
5. Journey payment summary
6. Reserve Your Seats
7. Itinerary
8. Package inclusions
9. Travel Kit
10. Umrah Kit
11. Journey Payment Options or Schedule
12. Confirmed and pending services
13. Cancellation summary
14. WhatsApp Support and Request a Callback
15. Sticky mobile reservation action

## Package review workflow

Statuses:

- Draft
- Submitted for Review
- Under Review
- Changes Requested
- Approved
- Scheduled
- Published
- Sold Out
- Departed
- Archived

Before submission, tell operators the expected review SLA. Initial target: **within 4 business hours**. Show submission time, current status, expected review window, reviewer feedback, and notifications for every transition.

## Administrator visibility

Administrators can view all customers, bookings, packages, departures, booking sources, agents/operators, prices, paid amounts, remaining balances, refunds, documents, visa progress, and audit history.

Agent-created bookings remain owned by the customer while recording who created and manages them.

## Deferred ideas

Special-assistance and Journey Preferences are future scope, including wheelchair assistance, elderly traveller support, room near elevator, reduced mobility, dietary requests, adjacent rooms, first-time Umrah guidance, and language preferences.

## Delivery direction

Implement foundations before feature expansion:

1. Product principles and design-system enforcement
2. Consistent customer header and footer
3. Staff portal shell and role-aware navigation
4. Standard terminology and icon catalogue
5. Operator package builder and automatic duration
6. Clone, preview, approval, and review SLA
7. Package Details reservation controls
8. Customer mobile OTP
9. Reservation checkout and approved payment structures
10. My Journey progress, updates, documents, payments, and downloads
11. Departures and package customer views
12. Staff approval, agent booking, reporting, and audit
