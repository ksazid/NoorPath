# VS-36 — Package Details Design Correction Checklist

## Design governance
- [x] Read `AGENTS.md` and `design-system/MASTER.md`.
- [x] Read Design Taste Frontend, UI UX Pro Max, Impeccable, Emil Design Engineering and Ponytail project skills.
- [x] Preserve approved NoorPath Package identity and existing VS-35 product behaviour.
- [x] Use the supplied guest-selector screenshot as interaction evidence, not as brand styling.

## Product completeness
- [x] Same-origin Available Travel Dates remain visible and horizontally scrollable.
- [x] Guests exposes Adults, Child with Bed, Child without Bed and Infant categories.
- [x] Child/infant controls remain disabled until authoritative operator pricing exists.
- [x] Adult guest count remains derived from supported current occupancy.
- [x] Room Sharing remains a separate decision.
- [x] Pay Full, Milestone and Pay Later are visible before booking and each maps to an authoritative quote commitment.
- [x] Operator package icon vocabulary is reused for customer package content.
- [x] Journey remains based only on persisted stay/travel facts.
- [x] Customer footer and sticky booking action are preserved.

## Certification
- [ ] Formatting and static analysis.
- [ ] Unit/integration/build checks.
- [ ] Package-details E2E.
- [ ] Keyboard/focus/44px target checks.
- [ ] Axe accessibility checks.
- [ ] 390px mobile and desktop rendered review.
- [ ] 200% text reflow and no viewport horizontal overflow.
- [ ] Reduced-motion check.
- [ ] Navigation reachability.
- [ ] Product Owner screenshot acceptance on exact final SHA.

## Merge and deploy
- [ ] Exact-head CI successful.
- [ ] Exact-head Rendered Slice Review successful.
- [ ] No unresolved review thread.
- [ ] Exact-head Product Owner approval recorded.
- [ ] NoorPath Merge Gate successful.
- [ ] No deployment without separate authorization.
