# VS-37 — Package Details Booking Decision & OTP Design

## Outcome

A prospective customer can evaluate a published departure in one continuous Package Details dossier, choose among same-origin dates without hash navigation, review a complete commercial price breakdown, default to Pay Full, and open a design-first phone OTP/traveller flow before any booking commitment.

## Product rules

- Pay Full is the default payment choice.
- Available Travel Dates replaces the single Travel date selector inside the booking card.
- Previous/next date controls scroll in place and never write a hash fragment to the address bar.
- Itinerary and operator-authored package content sit directly below the image/operator profile, not below the booking card row.
- Price Breakdown is visible before Book now and includes route, unit price, pre-discount total, discount row, after-discount total, service provider, NoorPath support attribution, tax disclosure, final total, pay-today and remaining values.
- Until discount/tax rules are implemented, the UI must not invent a discount; it displays zero discount and explicitly states configuration is pending.
- Book now opens a design preview of mobile OTP. No SMS is sent and no account is authenticated in this slice.
- The design preview exposes the post-login traveller-name step with + Add traveller up to the selected adult guest count.
- Child/infant pricing remains out of scope.

## Visual authority

Approved NoorPath Package reference → design-system/MASTER.md → Design Taste Frontend for the package surface → UI UX Pro Max → Impeccable bounded refinement → Emil purposeful feedback → Ponytail minimum implementation.

## Exclusions

- Real phone OTP/Auth0 configuration or authentication persistence.
- Traveller persistence from the preview.
- Discount/tax operator configuration.
- Airline data integration; airline is intentionally shown as pending until operator flight authoring is implemented.
- Hotel/airport/airline operator authoring; that follows as VS-38.
- Deployment.
