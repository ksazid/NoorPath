# VS-37 — Package Booking Composition Navigation Verification

## Required path

`Public package discovery -> /packages/{departureId} -> choose an in-place same-origin travel date -> choose Guests / Room Sharing / payment option -> review Price Breakdown -> Book now -> /packages/{departureId}/plan?occupancy={value}&paymentMode={value} -> phone OTP boundary when unauthenticated -> Add traveller when authenticated`

## Verification matrix

| From | Action | Expected destination/state | Outcome |
| --- | --- | --- | --- |
| Package Details | Select another available same-origin date | matching `/packages/{siblingDepartureId}` opens; browser back restores the original departure | VERIFIED |
| Package Details | Use Previous / Next travel-date controls | in-place Available Travel Dates rail moves without document navigation | VERIFIED |
| Package Details | Inspect a sold-out date | visible `Sold out`, not navigable | VERIFIED |
| Package Details | Open/close Guests and change supported adult count | guest categories remain in place and room sharing stays synchronized | VERIFIED |
| Package Details | Choose Pay Full without changing the default | Pay Full remains selected and the full total is shown as due today | VERIFIED |
| Package Details | Choose Milestone | governed milestone schedule is shown before Book now | VERIFIED |
| Package Details | Choose Pay Later | governed minimum due today and final remaining balance are shown before Book now | VERIFIED |
| Package Details | Review Price Breakdown | airline/route disclosure, unit pricing, discount placeholder, provider, pay-today/remaining and TOTAL render before Book now | VERIFIED |
| Package Details | Activate Book now with default selection | `/packages/{departureId}/plan?occupancy={value}&paymentMode=pay-full` opens | VERIFIED |
| Planner unauthenticated | Enter mobile number and activate Send Code | phone OTP preview appears and explicitly states that no code was sent while SMS is unconfigured | VERIFIED |
| Planner authenticated | Activate `Add traveller` | existing traveller names remain visible and the name + DOB adult-eligibility form is revealed progressively | VERIFIED |

## Rendered checks

- Desktop uses a continuous story column: image/operator facts -> itinerary -> package content -> status/terms/about, with booking controls in the supporting rail.
- Available Travel Dates occupies the booking rail travel-date position and retains horizontal scrolling plus 44px Previous/Next controls.
- Mobile stacks hero -> booking controls -> itinerary/content without document-level horizontal overflow.
- Price Breakdown is visible before Book now and never invents discount, airline, tax or supplier facts.
- Phone OTP preview has labelled fields and an explicit non-sending state until the provider is configured.
- Authenticated traveller entry is progressive: saved cards prioritize names and `Add traveller` reveals the eligibility form.
- Keyboard focus, target-size, axe accessibility, 200% root-text reflow and reduced-motion checks remain required.

## Evidence

- `apps/web/e2e/package-details.spec.ts` — `available same-origin dates navigate and browser back returns safely` covers sibling-date navigation and safe browser back.
- `apps/web/e2e/package-details.spec.ts` — `Book now preserves Pay Full by default and shows the phone OTP design boundary` covers Pay Full default, Previous/Next date controls, planner query preservation, phone field, Send Code preview, explicit no-code-sent status and accessibility.
- `apps/web/e2e/package-details.spec.ts` package conversion coverage verifies the three payment choices and pre-booking Price Breakdown rows.
- `apps/web/e2e/plan-journey.spec.ts` — `authenticated traveller step shows names first and adds travellers progressively` covers saved-name presentation and progressive Add traveller disclosure.
