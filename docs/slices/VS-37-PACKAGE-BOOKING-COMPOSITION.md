# VS-37 — Package Booking Composition & OTP Preview

## Status
Corrective customer-flow slice after Product Owner review of VS-36. Keep unmerged until rendered visual review and exact-head Product Owner acceptance.

## Outcome
Package Details becomes one continuous purchase-decision dossier: image and operator facts lead, itinerary and operator-authored package content follow directly below, while one compact booking rail contains in-place travel dates, guests, room sharing, three payment choices, transparent price breakdown and Book now. Book now leads to a truthful phone-OTP preview until the SMS provider is configured, then authenticated customers can reveal Add traveller and enter traveller details progressively.

## Design workflow used
- Design Taste Frontend for the Package surface composition.
- UI UX Pro Max for reading order, date navigation, form semantics, touch targets and responsive reflow.
- Impeccable layout/context + craft-floor for grouping, rhythm and drift control.
- Emil Design Engineering for short press/state feedback only.
- Ponytail full mode for minimum-change implementation.

Approved NoorPath Landing/Package references and `design-system/MASTER.md` remain visual authority.

## Scope
- Remove the detached top travel-date region and replace the booking card's static Travel date row with Available Travel Dates.
- Add previous/next controls plus the existing swipe/scroll rail, keeping same-origin, selected and sold-out semantics.
- Recompose desktop into a story column plus booking rail with Your itinerary and Package includes immediately below the hero/operator block.
- Make Pay Full the default while preserving explicit Milestone and Pay Later selection.
- Show a detailed pre-booking price breakdown including airline/route disclosure, unit price, before-discount total, discount placeholder, after-discount total, service provider, NoorPath support, pay-today/remaining commitment, total and statutory-charge note.
- Never invent discounts or airline facts: unpublished discount is zero and airline/return routing remain explicitly to be confirmed.
- On unauthenticated Book now flow show the intended phone OTP form without transmitting the phone number or pretending a code was sent before provider configuration.
- After authenticated traveller load, show traveller names and reveal the existing authoritative name + DOB form only after Add traveller is pressed.

## Acceptance criteria
- [ ] No detached empty gap remains between the page lead and booking content.
- [ ] Available Travel Dates occupies the booking rail's travel-date position and has 44px previous/next controls.
- [ ] Same-origin sibling navigation, sold-out state and browser back still work.
- [ ] On desktop, itinerary and package content appear directly below image/operator facts while booking controls remain in the right rail.
- [ ] Pay Full is selected when no payment mode was explicitly supplied and its Book now link carries `paymentMode=pay-full`.
- [ ] Milestone and Pay Later continue to show the governed payment commitment.
- [ ] Price Breakdown is visible before Book now and never fabricates discounts, airline or taxes.
- [ ] Unauthenticated Book now shows phone OTP design, and the preview explicitly states that no code was sent while SMS is unconfigured.
- [ ] Authenticated traveller cards prioritize names; Add traveller progressively reveals the existing eligibility fields.
- [ ] Desktop, mobile, keyboard, focus, target-size, axe, 200-percent-text, reduced-motion and no-horizontal-overflow checks pass.
- [ ] Product Owner visually accepts exact-head screenshots before merge.

## Explicit exclusions
- Real SMS/OTP delivery or provider configuration.
- Discount/promotion persistence or calculation.
- Airline/supplier integration.
- Child/infant pricing or booking.
- Traveller schema relaxation or removal of adult eligibility validation.
- Database migrations.
- Deployment before explicit authorization.

## Merge rule
Do not merge until exact-head CI, Rendered Slice Review and Navigation Reachability pass, no unresolved review thread remains, and Product Owner acceptance is recorded for the final SHA. Deployment remains separately authorized.
