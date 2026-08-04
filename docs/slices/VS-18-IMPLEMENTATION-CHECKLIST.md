# VS-18 Implementation Checklist

## Governance and source inspection

- [x] Branch from the latest `main` after PR #75 merge.
- [x] Read `AGENTS.md`.
- [x] Verify and read `.agents/skills/ui-ux-pro-max/SKILL.md`.
- [x] Read `.agents/skills/impeccable/SKILL.md`.
- [x] Read `.agents/skills/emil-design-eng/SKILL.md`.
- [x] Read `.agents/skills/ponytail/SKILL.md` in full mode.
- [x] Read `.agents/skills/design-taste-frontend/SKILL.md` for approved public/package surfaces only.
- [ ] Inspect both approved images under `design-references/` at their native dimensions during rendered visual review.
- [x] Inspect `design-system/MASTER.md`, token exports and current app-level variables.
- [x] Inventory current customer, operator and administrator headers, footers and shells.
- [x] Inventory icon dependencies and ad-hoc SVG/emoji usage.
- [x] Inventory duplicated button, card, field, badge, state and layout implementations.

## Design decisions

- [x] Use one typed internal NoorPath SVG registry because no application icon dependency is installed; avoid dependency and lockfile churn.
- [x] Record fixed icon mappings for package inclusions, occupancy, support, payment and status.
- [x] Confirm customer pre-login header contract.
- [x] Confirm customer authenticated header contract.
- [x] Confirm full and compact customer footer contracts.
- [x] Confirm staff sidebar groups and role-aware navigation input contract.
- [x] Restrict showcase routes to non-production unless `NOORPATH_ENABLE_DESIGN_SYSTEM_SHOWCASE=true` is explicitly set.
- [x] Record unresolved reference details rather than inventing values.

## Tokens

- [x] Reconcile token JSON and CSS exports for new foundation code; existing page aliases remain an incremental adoption boundary.
- [x] Add missing semantic colour tokens.
- [x] Add border, focus, disabled and skeleton tokens.
- [x] Consolidate spacing scale.
- [x] Consolidate radius scale.
- [x] Consolidate restrained elevation scale.
- [x] Consolidate motion durations and easing.
- [x] Add z-index/layering scale.
- [x] Preserve reduced-motion handling.
- [x] Add executable semantic-export and raw-colour consistency checks for the shared foundation styles.

## Shared components

- [x] Actions and icon buttons.
- [x] Text, select, checkbox, radio-card and switch controls.
- [x] Content, metric and support cards.
- [x] Status badge.
- [x] Feature tile.
- [x] Occupancy avatar group and occupancy card.
- [x] Timeline/progress item.
- [x] Loading and skeleton state.
- [x] Empty state.
- [x] Error/offline/unavailable state with retry slot.
- [x] Use native responsive disclosure for shell navigation; no new dialog/drawer dependency is justified.
- [x] Component export, fixed-order and rendered accessible-name coverage added.

## Layouts

- [x] Canonical public customer shell.
- [x] Canonical authenticated customer shell slots.
- [x] Reduced transactional/checkout header.
- [x] Full public footer.
- [x] Compact transactional footer.
- [x] Canonical staff shell with grouped navigation, search and action slots.
- [x] Responsive staff navigation disclosure.
- [x] Package Details fixed-order section contract.
- [x] Sticky mobile CTA safe-area contract.

## Documentation

- [x] Confirm `design-system/MASTER.md` already identifies historical inventory as superseded and current references as authoritative.
- [x] Add `design-system/COMPONENTS.md`.
- [x] Add `design-system/ICONS.md`.
- [x] Add `design-system/LAYOUTS.md`.
- [x] Add `design-system/MOTION.md`.
- [x] Link `PRODUCT_PRINCIPLES.md` and `PR_UX_CHECKLIST.md` from the foundation documentation.
- [x] Document operator content-only restrictions.

## Rendered and accessibility validation

- [x] Add production-component showcase/test routes using synthetic data and a fail-closed production boundary.
- [ ] Desktop Chromium evidence passes on the exact head.
- [ ] Mobile WebKit evidence passes on the exact head.
- [ ] Keyboard-only completion passes on the exact head.
- [ ] Visible focus and logical focus order pass on the exact head.
- [ ] 44px minimum customer touch targets and appropriate staff targets pass on the exact head.
- [ ] 200% zoom and narrow reflow pass on the exact head.
- [ ] Long-content and text-expansion coverage passes on the exact head.
- [ ] Reduced-motion coverage passes on the exact head.
- [ ] Automated serious/critical accessibility checks pass on the exact head.
- [ ] Same-viewport comparison against approved references is recorded for affected patterns.

## Regression and delivery

- [ ] Existing web unit/component tests pass on the exact head.
- [ ] Existing E2E tests pass on the exact head.
- [ ] Production web build passes on the exact head.
- [ ] Formatting, lint and type checking pass on the exact head.
- [x] Slice registry validation passes.
- [x] Diff contains no domain, API, database, migration or permission behaviour changes.
- [x] No deployment performed.
- [ ] PR includes completed interaction, accessibility, mobile and desktop certification evidence.
- [ ] Product Owner approves the exact final SHA before merge.
