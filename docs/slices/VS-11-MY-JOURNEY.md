# VS-11 — My Journey

## Status
Specification prepared. Implementation begins only after VS-10 is complete and remains Draft until exact-head certification and Product Owner acceptance.

## Outcome
A confirmed customer has one trusted, accessible place to understand booking, payment and upcoming Umrah journey state without fabricated readiness or operational facts.

## Actor
Authenticated customer viewing bookings and travellers within the customer account scope.

## Dependencies
- VS-10 Confirmation is complete.
- Booking and Payments expose stable customer-safe projections.
- Existing Landing, Discovery, Package Details and Plan visual language remains authoritative.

## Projection ownership
- **Booking** supplies confirmation state, booking reference and immutable commercial facts.
- **Payments** supplies customer-safe payment and instalment status.
- **Catalogue** supplies the referenced published journey facts.
- **Traveller** supplies account-owned traveller identities.
- Document and visa states remain explicit placeholders until VS-12 and VS-13.

## Customer journey
1. Customer opens My Journey from the authenticated customer navigation.
2. Customer sees account-owned confirmed bookings.
3. Customer opens one booking dashboard.
4. Dashboard presents journey facts, travellers, payment schedule and current authoritative status.
5. Customer can reach support with booking context.
6. Future readiness sections state clearly that documents/visa are unavailable until those capabilities exist.

## Required states
- Loading and projection delay.
- No confirmed journeys.
- Confirmed journey.
- Payment action required.
- Recoverable data error.
- Safe not-found/foreign-resource response.
- Explicit future-capability placeholder.

## Acceptance criteria
- [ ] Customer sees only account-owned bookings.
- [ ] Dashboard presents truthful booking, departure, stay, traveller, occupancy and commercial facts.
- [ ] Payment totals and schedule come from authoritative projections.
- [ ] No document, visa or readiness completion is fabricated.
- [ ] Loading, empty, delayed, unavailable and error states are actionable.
- [ ] Approved NoorPath customer header, footer and visual language are preserved.
- [ ] Keyboard, focus, semantics, live status, target size, 200% text and reduced motion pass.
- [ ] Landing -> Discovery -> Package -> Plan -> Booking -> My Journey is fully connected.
- [ ] Support entry carries safe booking context.
- [ ] Telemetry records outcome and projection delay without unnecessary personal data.

## Explicit exclusions
- Document upload/review, visa lifecycle and live readiness.
- Flight tracking, GPS, chat and orientation content.
- Operator exception management.
- Customer editing of booking, payment or confirmation state.

## Merge rule
Do not merge until every authoritative projection, account-scope, accessibility, rendered, route-linking and Product Owner gate passes on the exact final SHA.
