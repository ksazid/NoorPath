# VS-24 — Implementation Checklist

## Planning and traceability

- [x] Register VS-24 in `delivery/slices`.
- [x] Define explicit scope, exclusions and module ownership.
- [x] Bind implementation PR to VS-24 and keep it Draft until certification.

## API and authorization

- [x] Add operator booking-detail GET endpoint.
- [x] Require authenticated active operator membership.
- [x] Enforce operator ownership and safe not-found semantics.
- [x] Reuse existing Booking, Catalogue, Payments, Documents and Visa data.
- [x] Do not add cross-module mutation behavior.
- [x] Add authorization/integration coverage for owned, foreign and missing bookings.

## Web

- [x] Add `/operator/bookings/[bookingId]` route.
- [x] Add real clickable booking-detail action from `/operator/bookings`.
- [x] Present booking/departure facts and lifecycle status.
- [x] Present traveller roster and occupancy.
- [x] Present financial totals and instalment timeline.
- [x] Present document and visa readiness.
- [x] Link to existing departure, document, visa, support and cancellation workflows.
- [x] Cover loading, forbidden, not-found and retry/error states.
- [x] Preserve current staff shell and design system.

## Quality

- [x] Add desktop Chromium and mobile WebKit rendered tests.
- [x] Verify click-through navigation from booking list to booking detail in the registered rendered test contract.
- [x] Include keyboard/focus, minimum-target, no-overflow and serious/critical accessibility assertions in rendered certification.
- [ ] Run format, static analysis, tests and build on the exact implementation head.
- [ ] Run .NET tests and registered migration validation on the exact implementation head.
- [ ] Apply `certify` and require Slice Governance, CI, Navigation Reachability and Rendered Slice Review to pass on the exact head SHA.
- [ ] Record Product Owner approval for the exact certified SHA before merge.
- [x] Do not deploy without separate explicit approval.
