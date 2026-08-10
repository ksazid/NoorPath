# VS-36 — Package Details Design Correction Navigation Verification

## Required path

`Public package discovery -> /packages/{departureId} -> compare same-origin dates -> inspect Guests -> choose supported Room Sharing -> choose Milestone or Pay Later -> Book now -> /packages/{departureId}/plan?occupancy={value}&paymentMode={value}`

## Verification matrix

| From | Action | Expected destination/state | Outcome |
| --- | --- | --- | --- |
| Package discovery | Open a published package | `/packages/{departureId}` loads Package Details | VERIFIED |
| Package Details | Select another available same-origin date | matching `/packages/{siblingDepartureId}` becomes selected | VERIFIED |
| Package Details | Inspect sold-out sibling | visible `Sold out`, not navigable | VERIFIED |
| Sibling Package Details | Browser back | returns to original package without a dead end | VERIFIED |
| Package Details | Activate Travel date Change | Available Travel Dates anchor is the target | VERIFIED |
| Package Details | Open/close Guests | guest categories expand/collapse without navigation | VERIFIED |
| Package Details | Inspect child/infant rows | rows remain visible and controls disabled until authoritative pricing exists | VERIFIED |
| Package Details | Change supported Room Sharing | adult guest count and server-authored payment preview remain synchronized | VERIFIED |
| Package Details | Select Milestone plan | authoritative scheduled payment presentation remains on Package Details | VERIFIED |
| Package Details | Select Pay later | authoritative final-deadline presentation remains on Package Details | VERIFIED |
| Package Details | Book now | existing planner route preserves `occupancy` and `paymentMode` | VERIFIED |

## Rendered checks

- Desktop preserves the image / operator facts / booking-summary hierarchy.
- Same-origin travel dates scroll within their own horizontal region.
- Mobile 390 × 844 stacks primary content and does not create document-level horizontal overflow.
- Guest +/- controls expose minimum 44px touch targets; unavailable child/infant controls remain labelled.
- Sticky booking action keeps total, pay-today and Book now visible without obscuring focused content.
- Keyboard focus reaches date links, guest disclosure/stepper, room radios, payment radios and Book now in logical order.
- 200% root text scaling reflows without viewport horizontal overflow.
- Reduced-motion removes the guest-panel and control transition effects.

## Evidence

- `apps/web/e2e/package-details.spec.ts` covers Package Details reachability, sibling-date click-through, browser back, sold-out presentation, all four guest-category labels, disabled child/infant controls, room sharing, Milestone/Pay Later, Book now query preservation, mobile reflow, 200% text, target size and axe accessibility.
- VS-35 integration coverage remains authoritative for the unchanged public travel-date and financial-preview API contract.
