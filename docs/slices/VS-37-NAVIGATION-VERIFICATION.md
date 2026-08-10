# VS-37 — Package Details Booking Decision & OTP Design Navigation Verification

## Required path

`Public package discovery -> /packages/{departureId} -> browse same-origin dates -> choose Guests / Room Sharing / Payment -> Book now -> OTP design preview -> Add travellers`

## Verification matrix

| From | Action | Expected destination/state | Outcome |
| --- | --- | --- | --- |
| Package discovery | Open a published package | `/packages/{departureId}` loads Package Details | VERIFIED |
| Package Details | Activate Previous/Next travel-date control | horizontal date rail moves in place and URL remains hash-free | VERIFIED |
| Package Details | Select another available same-origin date | `/packages/{siblingDepartureId}` becomes selected without a hash fragment | VERIFIED |
| Sibling Package Details | Browser Back | returns to original package without a hash fragment or dead end | VERIFIED |
| Package Details | Open/close Guests | guest categories expand/collapse without route/hash mutation | VERIFIED |
| Package Details | Change supported Room Sharing | adult guest count and authoritative price preview remain synchronized without route/hash mutation | VERIFIED |
| Package Details | Select Pay Full / Milestone / Pay Later | local payment presentation changes without route/hash mutation | VERIFIED |
| Package Details | Book now | opens the phone OTP design-preview dialog without route/hash mutation | VERIFIED |
| OTP design preview | Enter mobile number and continue | shows six-digit OTP design state; no SMS/authentication is claimed | VERIFIED |
| OTP design preview | Enter six-digit preview code | shows Add travellers state | VERIFIED |
| Add travellers | Activate `+ Add traveller` | adds name-only traveller row up to selected adult count with no persistence | VERIFIED |

## Rendered checks

- Desktop keeps image/operator facts followed immediately by itinerary and operator-authored package content in one continuous primary story.
- Available Travel Dates lives inside the booking card and exposes minimum-44px previous/next controls.
- No changed Package Details local interaction writes a `#` fragment into the address bar.
- Pay Full is selected by default; Milestone and Pay Later remain explicit alternatives.
- Price Breakdown is visible before Book now and truthfully shows zero discount until governed discount configuration exists.
- Book now opens a modal on desktop and bottom-sheet treatment on 390 × 844 mobile without document-level horizontal overflow.
- Keyboard focus reaches date controls/date links, guest disclosure/stepper, room radios, payment radios, Book now and OTP/traveller controls in logical order.
- 200% text scaling reflows without viewport horizontal overflow.
- Reduced-motion removes optional transition effects.

## Evidence

- `apps/web/e2e/package-details.spec.ts` covers Package Details reachability, same-origin sibling navigation/back, date-control hash-free behaviour, guest categories, room sharing, Pay Full default, all three payment presentations, full commercial breakdown, Book now OTP design preview, traveller-name addition, mobile reflow, 200% text, target size and axe accessibility.
- VS-35/VS-36 API and payment integration coverage remains authoritative for the unchanged published package/financial-preview contract.
