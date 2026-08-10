# VS-35 — Package Details Conversion UX Navigation Verification

## Required path

`Public package discovery -> /packages/{departureId} -> choose same-origin date (optional) -> choose adult guests/room sharing -> choose payment view -> Book now -> /packages/{departureId}/plan?occupancy={value}&paymentMode={value}`

## Verification matrix

| From | Action | Expected destination/state |
| --- | --- | --- |
| Package discovery | Open a published package | `/packages/{departureId}` loads authoritative public facts |
| Package Details | Select another available same-origin date | matching `/packages/{siblingDepartureId}` |
| Package Details | Inspect sold-out sibling | visible `Sold out`, not navigable |
| Package Details | Change Adults | corresponding supported available occupancy is selected |
| Package Details | Change Room sharing | guests/payment summary update from server response |
| Package Details | Select Milestone plan | authoritative future due dates and amounts shown |
| Package Details | Select Pay later | same due-now plus remaining balance at final published deadline shown |
| Package Details | Book now | existing planner route with `occupancy` and `paymentMode` query values |
| Package Details | Browser back | returns without a dead-end route |

## Rendered checks
- Desktop date strip scrolls horizontally when dates exceed available width.
- Mobile date strip scrolls locally and does not create document-level horizontal overflow.
- Sticky booking action does not obscure focused controls or content.
- Keyboard focus reaches available date links, guest selector, room radios, payment radios and Book now in logical order.
- Sold-out dates are announced with unavailable text rather than colour alone.
