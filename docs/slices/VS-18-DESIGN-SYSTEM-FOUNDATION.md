# VS-18 — Design System Foundation

## Objective

Establish one reusable and enforceable NoorPath visual and interaction foundation for customer, operator and administrator web surfaces without redesigning approved screens or changing product behaviour.

## Why this slice is needed

NoorPath now has multiple customer and staff surfaces created across several vertical slices. Header, footer, navigation, spacing, card treatment, status presentation and interaction patterns have started to diverge. Future booking, package-authoring and My Journey improvements would amplify that inconsistency unless the foundation is consolidated first.

This slice makes the approved NoorPath Landing and Package references executable as shared tokens, components, layout contracts and QA rules.

## Governing visual authority

1. `design-references/noorpath-landing-reference.png`
2. `design-references/noorpath-package-reference.png`
3. Approved slice-specific Figma or prototype evidence where present
4. `design-system/MASTER.md`
5. Existing approved components

Installed skills may improve usability, accessibility, responsive behaviour, consistency and purposeful feedback, but may not replace NoorPath's visual identity.

## Scope

### Design-token consolidation

- Reconcile current token JSON, CSS and application-level variables.
- Define semantic roles for brand, surface, text, border, focus, status, disabled, skeleton and inverse states.
- Define approved spacing, radius, elevation, motion and z-index scales.
- Preserve reduced-motion behaviour.
- Avoid raw values in new foundation components unless documented as reference-derived exceptions.

### Typography

- Preserve the approved editorial serif and compact interface sans relationship.
- Define reusable text roles only where supported by repository references and current production patterns.
- Preserve minimum mobile text size, wrapping, readable measure and tabular figures for prices and operational data.

### Icons

- Confirm and adopt one SVG icon library.
- Define fixed NoorPath mappings for standard package and navigation concepts.
- Provide small avatar-group icons for double, triple and quad occupancy.
- Prohibit emoji icons and operator-selected icon substitutions.
- Document how custom Umrah-specific icons must match the standard stroke, grid and optical size.

### Shared components

Foundation components include:

- primary, secondary, tertiary and destructive actions;
- text fields, selects, radio cards, checkboxes and toggles;
- content cards and metric cards;
- package feature tiles;
- occupancy cards and avatar groups;
- status badges with text and icon, never colour alone;
- support actions for WhatsApp and Request a Callback;
- timelines and progress states;
- loading, empty, error, offline, unavailable, retry and success states;
- dialog, drawer and sheet primitives where an existing dependency or platform primitive supports them;
- consistent focus, pressed, disabled and pending states.

### Customer shell

Create a canonical responsive customer shell with:

- approved NoorPath logo treatment;
- pre-login and authenticated navigation slots;
- `Talk to Us` support access;
- `My Journey` entry;
- full public footer and compact transactional footer variants;
- reduced checkout header variant;
- mobile menu, safe-area spacing and sticky-action accommodation.

This slice establishes the shell and contracts. It does not implement the new reservation or authentication behaviour.

### Staff shell

Create a canonical responsive staff shell with:

- NoorPath Portal brand treatment;
- compact top bar;
- global or scoped search slot;
- notification and profile slots;
- sidebar headings: Overview, Content, Operations and Administration;
- role-aware item rendering;
- responsive drawer behaviour;
- no public marketing footer.

This slice does not change permissions or add missing staff features.

### Package Details layout contract

The repository-approved Package Details design remains unchanged and defines the fixed order:

1. Hero gallery
2. Verified operator
3. Package summary
4. Hotels
5. Room occupancy and pricing
6. Journey payment summary
7. Reserve action
8. Itinerary
9. Package inclusions
10. Travel kit
11. Umrah kit
12. Journey Payment Schedule
13. Confirmed and pending services
14. Cancellation policy
15. Help and support
16. Sticky reservation action where applicable

The slice introduces reusable section wrappers and contracts so later features can be inserted without redesigning or reordering the page.

### Design-system documentation

- Reconcile historical statements in `design-system/MASTER.md` with current repository truth.
- Add component, icon, layout and motion documentation.
- Document terminology and operator presentation restrictions.
- Link the design system to `PRODUCT_PRINCIPLES.md` and `PR_UX_CHECKLIST.md`.

### Rendered evidence

Provide a development or test-only component showcase using production components and include:

- desktop and mobile examples;
- long-content and text-expansion examples;
- keyboard and focus coverage;
- loading, empty, error and success states;
- reduced-motion coverage;
- automated accessibility checks.

## User flows affected

- Public customer navigation and footer presentation
- Authenticated customer shell presentation
- Package Details presentation foundation
- Operator and administrator workspace shell presentation

No domain workflow or commercial behaviour changes in this slice.

## Backend and data changes

None expected. Any data used by a component showcase must be static, synthetic and non-production.

## Security and permissions

- Existing authorization remains authoritative.
- Staff navigation is rendered from server-authorized role/permission information.
- Hidden navigation is not a substitute for API authorization.
- No sensitive customer, payment, passport, document or visa data may appear in examples or screenshots.

## Explicit exclusions

- New reservation, payment, package-authoring, departure or My Journey behaviour
- Customer authentication changes
- Operator or agent approval implementation
- New package terminology in live flows beyond shared constants needed by the foundation
- Dark mode
- Production deployment

## Acceptance criteria

- One semantic token source is used by changed shared components.
- One icon system and mapping are documented and enforced.
- Customer and staff shells are distinct but recognisably NoorPath.
- Changed shared components meet WCAG 2.2 AA and keyboard requirements.
- Touch targets meet the approved minimum size.
- Mobile and desktop layouts reflow without horizontal scrolling.
- Reduced-motion users receive equivalent state feedback.
- Package Details order and repository design authority are preserved.
- Existing routes and behaviours continue to pass regression tests.
- Rendered screenshot evidence is produced at approved mobile and desktop viewports.
- Product Owner approves the exact implementation commit before merge.

## Branch and PR

- Branch: `slice/design-system-foundation`
- Planning/runtime PR title: `VS-18: Establish NoorPath design system foundation`

## Deployment boundary

Merging VS-18 does not authorize production deployment. Any deployment requires a separate explicit Product Owner instruction for the exact merged SHA.
