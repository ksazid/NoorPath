# VS-35 — Package Details Conversion UX

## Status
Specification prepared for implementation on `agent/vs35-package-details-conversion-ux`. The PR must remain Draft until exact-head certification and Product Owner acceptance.

## Outcome
A prospective customer can understand and configure an adult-only published Umrah departure directly on Package Details: compare same-origin travel dates, choose a currently saleable room/guest combination, inspect operator-authored journey facts and package content, understand total/minimum-today/remaining commitment and the published payment schedule, and continue to booking without NoorPath inventing prices, availability, itinerary, discounts, or cancellation terms.

## Governing rules
- `INV-ACC-003` — only supported occupancy can be offered for sale.
- `INV-PRI-003` — total, pricing basis, due-now and remaining amount are explainable before commitment.
- `INV-PRI-005` — customer UI must not invent or recalculate authoritative commercial amounts.
- `INV-PRI-006` — unsupported occupancy cannot be priced.
- `POL-PRI-001` — published deposit/payment-plan policy remains versioned and fixed.
- `POL-TRV-001` — child/infant categories remain unavailable until explicit eligibility and pricing policy exists.
- Approved Landing + Package references and `design-system/MASTER.md` remain visual authority.

## Dependencies
- VS-07 — adult-only authoritative pricing and payment schedule.
- VS-29 — package-to-booking occupancy handoff.
- VS-32 — governed operator package inclusion/exclusion catalogue.

## Modules and ownership
- Catalogue owns published package, stay, route, inclusion and exclusion facts.
- Pricing owns immutable published occupancy prices and the snapshotted payment plan.
- Inventory owns current room availability and sold-out state.
- Operators owns public operator eligibility and display identity.
- Web renders server-owned facts and preserves NoorPath's established customer visual language.

## Customer journey
1. `/packages/{departureId}` loads the public published departure.
2. Available Travel Dates shows other published departures with the same operator, origin and package name; current date is highlighted and sold-out dates remain visible but disabled.
3. Customer sees verified operator, hotel/stay facts, route, duration and current room availability.
4. Adult guest count and room sharing remain constrained to current adult-only rules: double=2, triple=3, quad=4.
5. Journey is rendered only from persisted departure/stay/travel/return facts; no fabricated itinerary is introduced.
6. Package, Travel Kit, Umrah Kit and exclusion items use NoorPath semantic icon mapping.
7. Payment Summary renders server-authoritative per-adult price, total, minimum due today, remaining balance and future schedule.
8. Customer can view the governed remaining balance as either the detailed Milestone schedule or a Pay Later summary to the same final published deadline. This is a presentation choice in VS-35 and does not introduce a second pricing policy.
9. `Book now` continues to the existing planner with selected `occupancy` and `paymentMode` preserved. Existing quote, hold and payment boundaries remain unchanged.

## UX contract
- Use the supplied reference for information hierarchy and conversion clarity, not for branding.
- Keep NoorPath's ivory/black/gold/green visual language, typography hierarchy, header and customer interaction conventions.
- Payment facts remain visible before the primary booking action.
- Do not show fake airline logos, discounts, certifications, cancellation percentages or urgency.
- Confirmed/pending facts are text-labelled, not colour-only.
- Same-origin travel dates scroll horizontally within their own region without causing viewport overflow.
- Mobile sticky action keeps total and minimum today visible with Book now.
- Minimum 44px interactive targets, visible focus, keyboard operation, 200% text reflow and reduced-motion support are required.

## Failure and stale-state behaviour
- Current package remains unavailable when it is not publicly saleable or has no saleable occupancy.
- A sibling departure can remain visible as `Sold out` without becoming navigable.
- Public payment preview is informational; the existing authenticated flow revalidates quote and availability before reservation.
- Public API failures keep correlation-safe retry behaviour.

## Acceptance criteria
- [ ] Same-origin published sibling dates appear chronologically with current and sold-out states.
- [ ] No sibling from a different operator, origin or package is leaked into the date strip.
- [ ] Adult guests and room sharing expose only currently supported adult combinations.
- [ ] Total, minimum today, remaining and payment breakdown are calculated server-side from immutable published pricing/payment-plan state.
- [ ] Milestone view displays authoritative future due dates/amounts; Pay Later summary uses the same due-now amount and final published deadline.
- [ ] Journey uses only persisted operator facts and does not fabricate itinerary steps.
- [ ] Standard inclusion/exclusion items use NoorPath semantic icons and are grouped into Package, Travel Kit, Umrah Kit and Not Included.
- [ ] Confirmed/pending and truthful cancellation/payment disclosures appear before booking.
- [ ] Book now preserves occupancy and payment-mode selection into the existing planner route.
- [ ] Loading, unavailable, retry, desktop and mobile states remain usable.
- [ ] Keyboard, focus, axe, 44px targets, 200% text, reduced motion and no viewport horizontal overflow pass.
- [ ] Existing commercial/public package tests remain green and no module ownership boundary is weakened.

## Explicit exclusions
- Child/infant guest eligibility, pricing or operator configuration.
- Multi-room family allocation beyond existing double/triple/quad adult rules.
- New Pricing persistence, migrations or a new Pay Later contractual domain model.
- Changing authenticated quote, hold, booking or payment execution semantics.
- Moving customer authentication later in the funnel; this slice stops at the existing planner handoff.
- Detailed operator itinerary-step persistence that does not exist today.
- Arbitrary custom icon persistence beyond the existing standard content vocabulary.
- Airline/supplier integrations, promotions/discount engine or invented cancellation percentages.
- Customer phone OTP provider activation, Knowledge Pack work or production deployment.

## Merge rule
The slice is mergeable only when exact-head CI and Rendered Slice Review pass, navigation evidence is current, no unresolved review thread remains, and Product Owner acceptance is recorded for the final SHA. Production deployment remains separately authorized.
