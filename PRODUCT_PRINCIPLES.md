# NoorPath Product Principles

These rules apply to every customer, operator, agent, and administrator flow.

## 1. Few clicks first

- Every common task must use the minimum practical number of interactions.
- Routine actions should normally take no more than three interactions after entering the correct section.
- Complex flows may take longer only when each step collects required information or makes a necessary decision.
- Every slice and PR must state the interaction count for its primary flow.

### Interaction targets

- Open a package from discovery: 3 interactions or fewer.
- Contact WhatsApp support: 1 interaction.
- Request a callback: 2 interactions or fewer.
- Make the next journey payment: 3 interactions or fewer.
- Download an available document: 2 interactions or fewer.
- Resume the next required My Journey action: 2 interactions or fewer.
- Clone a package before editing: 3 interactions or fewer.
- Open all customers for a package or departure: 2 interactions or fewer.

## 2. One screen, one primary decision

- Every screen has one visually dominant primary action.
- Secondary actions must not compete with it.
- Use outcome-based labels such as `Reserve Your Seats`, `Pay and Reserve`, `Upload Passport`, and `Submit Package for Review`.
- Avoid generic labels such as `Continue`, `Next`, `Proceed`, and `Submit` where a clearer outcome is available.

## 3. Progressive disclosure

- Ask only for information required at the current stage.
- Customers reserve first and complete administrative requirements later through My Journey.
- Advanced operator options appear only when relevant.

## 4. No duplicate entry

- Users must never enter the same information twice.
- Preserve package, traveller, occupancy, and pricing selections through OTP authentication.
- Reuse existing customer, traveller, hotel, payment, and document data.
- Cloning copies reusable content but not invalid dates or stale confirmations.

## 5. Smart defaults and reduced typing

Prefer templates, cloning, cards, toggles, steppers, date pickers, searchable selectors, and approved content libraries over free text.

NoorPath should automatically:

- Calculate days and nights from departure and return dates.
- Recommend room combinations for larger groups.
- Preselect standard inclusions and approved icons.
- Suggest a default Journey Payment Schedule.
- Reuse approved cancellation, itinerary, and package templates.

## 6. Customer navigates by journey

- Customer navigation reflects the pilgrimage journey, not internal system modules.
- Payments, documents, visa, updates, travel details, and downloads live under My Journey.
- My Journey prioritises:
  1. Next required action
  2. Journey progress
  3. Important updates
  4. Payments and documents
  5. Downloads and travel details

## 7. Staff navigates by task

Use one staff portal with section headers:

- Overview
- Content
- Operations
- Administration

Permissions hide inaccessible modules instead of showing disabled clutter.

Domain definitions:

- Package: commercial product.
- Departure: operational travel batch.
- Booking: customer reservation.
- Traveller: individual pilgrim.

## 8. Operators manage content, not presentation

Operators may change dates, hotels, flights, transport, prices, capacity, itinerary, payment structures, and package-specific descriptions.

Operators may not change typography, colours, icon mapping, card layouts, section order, standard terminology, or customer header and footer structure.

## 9. NoorPath owns terminology and icons

All operators use the same centrally managed labels, descriptions, icons, and display order.

Standard terms include:

- Return Flights
- Umrah Visa Included
- Makkah Hotel
- Madinah Hotel
- Airport Transfers
- Intercity Transport
- Daily Meals Included
- Guided Ziyarah
- Group Leader and Support
- Journey Payment Schedule
- Remaining Journey Balance

## 10. One design system

- The approved landing and Package Details designs in the repository are the visual source of truth.
- Use one icon family, spacing scale, typography scale, radius system, button system, and reusable component library.
- Package preview must render the actual customer-facing component tree on desktop and mobile.
- Do not build a separate mock preview.

## 11. One source of truth

- Customers use My Journey for status, actions, updates, payments, documents, travel details, and downloads.
- Operators use package and departure workspaces for readiness and execution.
- Administrators use the global portal for governance, reporting, approvals, and audit.
- The same state must not be maintained independently in multiple modules.

## 12. Predictable, fast, and recoverable

- The same action must look and behave consistently everywhere.
- Status labels, colours, and icons retain the same meaning.
- Animations must never delay task completion.
- Use autosave and draft recovery for long workflows.
- Show immediate feedback after consequential actions.
- Prevent duplicate submissions and repeated payments.

## 13. Mobile first for customers

- Design customer flows for phones first.
- Keep reservation information and the main CTA visible.
- Avoid dense tables and horizontal scrolling.
- Keep WhatsApp Support and Request a Callback easy to reach.

## 14. Clear state and next step

Every screen must make these obvious:

- Where am I?
- What is the current status?
- What should I do next?
- What happens after I do it?

## 15. Transparent commercial information

Before reservation, show:

- Price per traveller
- Total journey price
- Reservation amount
- Remaining journey balance
- Payment structure and due dates
- Cancellation terms

Never make customers calculate these manually.

## 16. Approved payment structures

A package may support:

- Full Payment at Booking
- Reservation + One Remaining Payment
- Reservation + Journey Payment Schedule

Do not use customer-facing terms such as EMI, loan, finance, or installment.

## 17. Governance and auditability

- Operators and agents require administrator approval.
- Package publication follows an approval workflow.
- Operators see the expected review time before submission.
- Every privileged action records actor, timestamp, and outcome.
- Package preview and approval must verify both commercial completeness and design-system compliance.

## Definition of simplicity

A flow is simple when the user knows what to do without training, enters required information only once and only when needed, receives predictable defaults, can resume after interruption, and can complete the task without contacting support while support remains one tap away.
