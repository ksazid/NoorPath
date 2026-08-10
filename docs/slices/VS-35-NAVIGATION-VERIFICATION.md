# VS-35 — Package Details Conversion UX Navigation Verification

## Required path

`Public package discovery -> /packages/{departureId} -> choose same-origin date (optional) -> choose adult guests/room sharing -> choose payment view -> Book now -> /packages/{departureId}/plan?occupancy={value}&paymentMode={value}`

## Verification matrix

| From | Action | Expected destination/state | Outcome |
| --- | --- | --- | --- |
| Package discovery | Open a published package | `/packages/{departureId}` loads authoritative public facts | VERIFIED |
| Package Details | Select another available same-origin date | matching `/packages/{siblingDepartureId}` becomes the selected departure | VERIFIED |
| Package Details | Inspect sold-out sibling | visible `Sold out`, not navigable | VERIFIED |
| Package Details | Change Adults | corresponding supported available occupancy is selected | VERIFIED |
| Package Details | Change Room sharing | guests/payment summary update from server-authored values | VERIFIED |
| Package Details | Select Milestone plan | authoritative future due dates and amounts shown | VERIFIED |
| Package Details | Select Pay later | same due-now plus remaining balance at final published deadline shown | VERIFIED |
| Package Details | Book now | existing planner route with `occupancy` and `paymentMode` query values | VERIFIED |
| Sibling Package Details | Browser back | returns to the original package without a dead-end route | VERIFIED |

## Rendered checks

- Desktop date strip scrolls horizontally when dates exceed available width.
- Mobile date strip scrolls locally and does not create document-level horizontal overflow.
- Sticky booking action does not obscure focused controls or content.
- Keyboard focus reaches available date links, guest selector, room radios, payment radios and Book now in logical order.
- Sold-out dates are announced with unavailable text rather than colour alone.

## Evidence

- `apps/web/e2e/package-details.spec.ts` covers published-package reachability, available sibling date click-through, browser back, sold-out presentation, adult/room changes, Milestone/Pay Later states, Book now query preservation, mobile reflow and accessibility.
- `tests/NoorPath.Commercial.Integration.Tests/PackageDetailsConversionApiTests.cs` verifies the public API emits chronological same-package/same-origin sibling states and server-authoritative payment preview values.
