# VS-29 — Navigation Verification

Status: VERIFIED

## Customer route chain

- Landing / package discovery -> `/packages/{departureId}`
- Package detail -> occupancy selection -> `/packages/{departureId}/plan?occupancy={double|triple|quad}`
- Planner -> Package breadcrumb -> `/packages/{departureId}`
- Planner -> existing sign-in route when customer identity is required
- Planner -> existing quote / inventory hold / payment progression

## Shell contract

- Customer package and planner routes remain wrapped by `CustomerRouteShell`.
- Existing legacy `.public-topbar` and `.public-footer` elements remain suppressed inside the shared route shell, preventing duplicate customer chrome.
- The shared customer footer remains reachable on the public Package and Plan routes.

## Automated evidence

`apps/web/e2e/customer-package-booking-ux.spec.ts` verifies:

- an available occupancy can be selected on Package Details;
- unavailable occupancy remains disabled;
- selected occupancy is carried into the planner and restored there;
- planner has a real route back to the exact package;
- only one visible shared customer header/footer is present;
- the package route has no horizontal overflow at 390 px;
- occupancy choice retains a minimum 44 px target.

No production deployment is part of this verification.
