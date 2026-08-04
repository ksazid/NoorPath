# NoorPath Layout Contracts

## Customer shell

### Public header

Approved information architecture:

- NoorPath logo
- Packages
- How It Works
- Talk to Us
- My Journey

`Talk to Us` exposes WhatsApp Support and Request a Callback. The header must preserve a single visually dominant task when a page has a primary commercial action.

### Authenticated customer header

Approved slots:

- NoorPath logo
- Packages
- My Journey
- Help
- Talk to Us
- Profile

Authentication state determines available links; hiding navigation is never a substitute for server authorization.

### Transactional header

Reservation and authentication surfaces may use a reduced header containing:

- NoorPath logo
- Secure Reservation context
- support access

It must preserve the selected package and reservation context through authentication.

### Customer footer

The full public footer groups links under:

- NoorPath
- Explore
- Support
- Legal

A compact transactional footer may show only essential trust, support and legal links. Customer-facing pages retain the approved footer character from the Landing and Package references.

## Staff shell

The staff portal is visually related to NoorPath but is an operational application, not a marketing page. It uses no public marketing footer.

Approved sidebar groups:

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

Items render only when the authorized role has a relevant capability. API authorization remains authoritative.

On small screens, the sidebar becomes a dismissible navigation drawer with a visible trigger, focus management and escape route. Main content must not be trapped in a nested horizontal scroll region.

## Package Details fixed order

The following order is canonical and represented in code by `PACKAGE_DETAIL_SECTION_ORDER`:

1. Hero gallery
2. Verified operator
3. Package summary
4. Hotels
5. Room occupancy and pricing
6. Journey payment summary
7. Reserve action
8. Itinerary
9. Package inclusions
10. Travel kit
11. Umrah kit
12. Journey Payment Schedule
13. Confirmed and pending services
14. Cancellation policy
15. Help and support
16. Sticky reservation action where applicable

Operators edit approved content and operational status. They cannot reorder sections or change presentation tokens.

## Responsive rules

- Mobile content order preserves trust, price and next action before secondary detail.
- Minimum supported width is 320 pixels without horizontal page scrolling.
- Verify representative mobile and desktop viewports, 200% zoom and text expansion.
- Fixed or sticky bars reserve content space and respect device safe areas.
- Customer controls target at least 44 × 44 pixels.
- Use `min-height: 100dvh` rather than fixed mobile viewport assumptions.
