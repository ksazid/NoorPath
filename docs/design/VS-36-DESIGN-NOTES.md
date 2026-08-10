# VS-36 — Design Notes

## Design read
Package Details should feel like a calm travel dossier rather than a dense dashboard: the journey and operator establish trust, package facts remain scannable, and the booking card answers date → guests → room → money → Book now in that order.

## Applied project skills
- Design Taste Frontend: preserve NoorPath Package art direction and avoid marketplace/template drift.
- UI UX Pro Max: mobile-first structure, 44px targets, explicit labels, focus, state clarity and no horizontal viewport overflow.
- Impeccable: remove VS-35's excessive micro-type/density, strengthen grouping and reading rhythm, avoid unnecessary nested container treatment.
- Emil Design Engineering: short ease-out state transitions, responsive press feedback and reduced-motion equivalents; no decorative motion.
- Ponytail full: keep VS-35 API/payment/date semantics intact and change the minimum frontend surface required.

## Guest-selector decision
The supplied BookMyUmrahTrip screenshot is used only as interaction evidence. NoorPath exposes Adults, Children 2–11 With Bed, Children 2–4 Without Bed and Infants 0–2 Without Bed in one expandable guest control. Only Adults are currently enabled because NoorPath does not yet have authoritative child/infant operator pricing; disabled rows state that fact instead of implying zero/free pricing.

## Content-icon decision
Package Details now uses the same SVG icon vocabulary as the operator package inclusion editor for standard inclusion/exclusion categories. This corrects the prior visual mismatch without changing stored catalogue values.

## Responsive thesis
Desktop retains the approved package-reference three-part overview: imagery, operator/stay facts and booking summary. Mobile stacks those in that reading order, keeps same-origin dates horizontally scrollable within their own region, and keeps total/pay-today/Book now in the safe-area-aware sticky action.
