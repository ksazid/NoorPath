# VS-18 Implementation Checklist

## Governance and source inspection

- [x] Branch from the latest `main` after PR #75 merge.
- [x] Read `AGENTS.md`.
- [x] Verify and read `.agents/skills/ui-ux-pro-max/SKILL.md`.
- [ ] Read `.agents/skills/impeccable/SKILL.md`.
- [ ] Read `.agents/skills/emil-design-eng/SKILL.md`.
- [ ] Read `.agents/skills/ponytail/SKILL.md` in full mode.
- [ ] Read `.agents/skills/design-taste-frontend/SKILL.md` for approved public/package surfaces only.
- [ ] Inspect both approved images under `design-references/` at their native dimensions.
- [ ] Inspect `design-system/MASTER.md`, token exports and current app-level variables.
- [ ] Inventory current customer, operator and administrator headers, footers and shells.
- [ ] Inventory icon dependencies and ad-hoc SVG/emoji usage.
- [ ] Inventory duplicated button, card, field, badge, state and layout implementations.

## Design decisions

- [ ] Confirm the existing approved icon dependency or approve Lucide as the single icon system.
- [ ] Record fixed icon mappings for package inclusions, occupancy, support, payment and status.
- [ ] Confirm customer pre-login header contract.
- [ ] Confirm customer authenticated header contract.
- [ ] Confirm full and compact customer footer contracts.
- [ ] Confirm staff sidebar groups and existing role-aware item mapping.
- [ ] Confirm component showcase visibility and environment boundary.
- [ ] Record unresolved reference details rather than inventing values.

## Tokens

- [ ] Reconcile token JSON, token CSS and application aliases.
- [ ] Add missing semantic colour tokens.
- [ ] Add border, focus, disabled and skeleton tokens.
- [ ] Consolidate spacing scale.
- [ ] Consolidate radius scale.
- [ ] Consolidate restrained elevation scale.
- [ ] Consolidate motion durations and easing.
- [ ] Add z-index/layering scale.
- [ ] Preserve reduced-motion handling.
- [ ] Add automated raw-value or token-consistency checks where practical.

## Shared components

- [ ] Actions and icon buttons.
- [ ] Form fields and selection controls.
- [ ] Content, metric and support cards.
- [ ] Status badge.
- [ ] Feature tile.
- [ ] Occupancy avatar group and occupancy card.
- [ ] Timeline/progress item.
- [ ] Loading and skeleton state.
- [ ] Empty state.
- [ ] Error/offline/unavailable state with retry.
- [ ] Responsive dialog/drawer/sheet only where already justified.
- [ ] Component tests for variants and accessible names.

## Layouts

- [ ] Canonical public customer shell.
- [ ] Canonical authenticated customer shell slots.
- [ ] Reduced transactional/checkout header.
- [ ] Full public footer.
- [ ] Compact transactional footer.
- [ ] Canonical staff shell with grouped navigation.
- [ ] Responsive staff navigation drawer.
- [ ] Package Details fixed-order section contract.
- [ ] Sticky mobile CTA safe-area contract.

## Documentation

- [ ] Reconcile `design-system/MASTER.md` historical/current sections.
- [ ] Add `design-system/COMPONENTS.md`.
- [ ] Add `design-system/ICONS.md`.
- [ ] Add `design-system/LAYOUTS.md`.
- [ ] Add `design-system/MOTION.md`.
- [ ] Link `PRODUCT_PRINCIPLES.md` and `PR_UX_CHECKLIST.md`.
- [ ] Document operator content-only restrictions.

## Rendered and accessibility validation

- [ ] Add production-component showcase/test route using synthetic data.
- [ ] Desktop Chromium evidence.
- [ ] Mobile WebKit evidence.
- [ ] Keyboard-only completion.
- [ ] Visible focus and logical focus order.
- [ ] 44px minimum customer touch targets and appropriate staff targets.
- [ ] 200% zoom and narrow reflow.
- [ ] Long-content and text-expansion coverage.
- [ ] Reduced-motion coverage.
- [ ] Automated serious/critical accessibility checks.
- [ ] Same-viewport comparison against approved references for affected patterns.

## Regression and delivery

- [ ] Existing web unit/component tests pass.
- [ ] Existing E2E tests pass.
- [ ] Production web build passes.
- [ ] Formatting, lint and type checking pass.
- [ ] Slice registry validation passes.
- [ ] No domain, API, database or permission behaviour changed.
- [ ] No deployment performed.
- [ ] PR includes interaction, accessibility, mobile and desktop evidence.
- [ ] Product Owner approves the exact final SHA before merge.
