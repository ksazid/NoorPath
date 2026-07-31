# VS-06 Package Details — Implementation Checklist

Status: Complete — technical validation, rendered validation and Product Owner acceptance recorded 2026-07-31.

## Contract and scope

- [x] VS-06 specification committed before merge.
- [x] Package Details remains public and read-only.
- [x] VS-07 quote/traveller behavior is excluded.
- [x] VS-08 inventory holds are excluded.
- [x] Unresolved cancellation, refund, booking-cutoff and instalment policies are not invented.
- [x] Approved Package reference remains the visual authority.

## API

- [x] Add `GET /api/v1/departures/{departureId}`.
- [x] Require published Catalogue departure and package version.
- [x] Re-check current operator publication eligibility.
- [x] Validate immutable published PriceVersion belongs to the same departure/operator.
- [x] Return all positive published occupancy prices with explicit currency.
- [x] Join current Inventory by occupancy.
- [x] Represent priced zero-capacity occupancy as unavailable.
- [x] Fail closed when no occupancy is currently saleable.
- [x] Return complete ordered inclusions and exclusions.
- [x] Return Makkah, Madinah and travel confirmation states.
- [x] Keep staff IDs, internal versions, audit fields and adjustment reasons private.
- [x] Use safe outcome/duration/correlation telemetry only.
- [x] No database migration introduced.

## Customer UI

- [x] Remove package-detail dependency on static preview data.
- [x] Bind route to authoritative public detail API by `departureId`.
- [x] Preserve approved Package page composition rather than redesigning it.
- [x] Remove unsupported IATA/ISO/experience claims.
- [x] Remove fixed Quad Sharing accommodation assumption.
- [x] Replace fabricated day-by-day itinerary with authoritative dates/travel facts.
- [x] Render complete inclusions and exclusions.
- [x] Render every published occupancy price and current availability state.
- [x] Do not label occupancy amounts as per-person without an authoritative pricing basis.
- [x] Do not invent deposit, due-now, remaining balance or instalment schedule.
- [x] Add loading state.
- [x] Add public unavailable/not-found state.
- [x] Add error/retry state with safe correlation reference.
- [x] Preserve human-support path.
- [x] Add 390 px and 360 px responsive intent.
- [x] Preserve reduced-motion behavior and minimum press feedback only.

## Automated verification

- [x] Integration coverage for successful published package detail.
- [x] Integration coverage for private-field exclusion.
- [x] Integration coverage for unknown/unpublished 404.
- [x] Integration coverage for operator ineligibility.
- [x] Integration coverage for zero-availability occupancy.
- [x] Integration coverage for no-saleable-inventory fail closed.
- [x] Browser coverage for populated authoritative facts.
- [x] Browser coverage for unavailable/not-found state.
- [x] Browser coverage for error/retry state.
- [x] Browser coverage for 390 px and 360 px reflow.
- [x] Browser coverage for keyboard focus, target size, accessibility scan and 200% text reflow.
- [x] VS-06 rendered-review CI passed on the acceptance branch before Product Owner review.

## Rendered / Product Owner gate

- [x] Capture populated desktop implementation at the approved comparison viewport.
- [x] Compare populated desktop against `noorpath-package-reference.png`.
- [x] Capture 390 px populated implementation.
- [x] Capture 360 px populated implementation.
- [x] Capture pending-fact state.
- [x] Capture unavailable/not-found state.
- [x] Capture error/retry state.
- [x] Verify keyboard path and visible focus in a real browser.
- [x] Verify 200% zoom/text reflow in a real browser.
- [x] Verify reduced-motion behavior in a real browser.
- [x] Product Owner accepts VS-06 rendered customer experience.

## Acceptance closure

The Product Owner accepted the deployed VS-06 customer experience on 2026-07-31 after reviewing the package detail experience on mobile. Netlify-only deterministic fixtures used for acceptance verification are not part of the production product contract and must not be merged into `main`.

The follow-up acceptance merge preserves the current approved Landing/Footer implementation and carries only the safe VS-06 accessibility closure required after rendered review.
