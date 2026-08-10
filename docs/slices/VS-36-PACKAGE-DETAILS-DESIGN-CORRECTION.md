# VS-36 — Package Details Design Correction

## Status
UI-only corrective slice following Product Owner rejection of the VS-35 visual result. Keep the PR Draft until rendered visual review and exact-head Product Owner acceptance.

## Outcome
Package Details keeps all certified VS-35 travel-date, pricing, payment and booking behaviour, but presents it using NoorPath's approved Package visual language with readable hierarchy, calm density and an explicit guest selector showing Adults, Children with Bed, Children without Bed and Infants before booking.

## Governing visual workflow
The following installed project skills are mandatory for this UI slice, in the order required by `AGENTS.md`:
1. `.agents/skills/design-taste-frontend/SKILL.md` — Package surface identity and anti-template direction.
2. `.agents/skills/ui-ux-pro-max/SKILL.md` — responsive structure, accessibility, touch targets, guest interaction and state clarity.
3. `.agents/skills/impeccable/SKILL.md` — bounded hierarchy, typography, spacing and density correction.
4. `.agents/skills/emil-design-eng/SKILL.md` — purposeful feedback, press states and short reduced-motion-safe transitions.
5. `.agents/skills/ponytail/SKILL.md` — minimum-change implementation after design decisions.

Approved NoorPath Landing/Package references and `design-system/MASTER.md` remain above every skill recommendation.

## Scope
- Recompose Package Details without changing VS-35 API or financial behaviour.
- Keep Available Travel Dates as a horizontally scrollable same-origin rail.
- Add an expandable Guests control modelled on the supplied interaction reference.
- Show Adults, Children (2–11 years) With Bed, Children (2–4 years) Without Bed, and Infants (0–2 years) Without Bed.
- Adults remain the only authoritative priced/booking-enabled guest category in this slice.
- Child/infant rows are visible but disabled with explicit copy until operator pricing/configuration exists; no free or fabricated child pricing is allowed.
- Keep Room Sharing separate from guest categories.
- Keep Milestone and Pay Later presentations with total, pay-today and remaining commitment before Book now.
- Use the operator inclusion editor's icon vocabulary for customer Package/Travel Kit/Umrah Kit/Not Included presentation.
- Preserve truthful journey/stay/travel facts; do not invent itinerary steps.
- Restore the customer footer and keep the sticky booking action clear on mobile.

## Acceptance criteria
- [ ] Package Details reads as a calm NoorPath travel/package dossier rather than a dense dashboard.
- [ ] No body/form control is rendered at the extremely small VS-35 density; mobile primary form/control labels remain readable at normal text scaling.
- [ ] Guest panel exposes all four requested guest categories with 44px minimum stepper targets.
- [ ] Adult count stays synchronized with the currently selected supported occupancy.
- [ ] Child and infant add/remove controls are visibly disabled until authoritative operator pricing exists.
- [ ] Same-origin travel-date navigation, sold-out state, browser back and selected state remain unchanged.
- [ ] Package content uses the same visual icon vocabulary as operator package configuration.
- [ ] Milestone/Pay Later values and booking href remain unchanged from VS-35 semantics.
- [ ] Desktop, 390px mobile, 200% text, keyboard, focus, reduced-motion, axe and horizontal-overflow checks pass.
- [ ] Visual screenshots are reviewed by the Product Owner before merge.

## Explicit exclusions
- Child/infant pricing, eligibility rules, inventory consumption or operator configuration.
- API, database, Pricing, Inventory, Booking or payment-domain changes.
- New airline/supplier integrations or invented logos.
- New itinerary persistence.
- Deployment before Product Owner visual approval and merge authorization.

## Merge rule
Do not merge until exact-head CI, Rendered Slice Review and Navigation Reachability pass, no unresolved review thread remains, and Product Owner acceptance is recorded for the final SHA. Deployment remains separately authorized.
