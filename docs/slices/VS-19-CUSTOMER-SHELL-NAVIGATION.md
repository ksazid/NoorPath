# VS-19 — Customer Shell and Navigation Adoption

## Objective

Adopt the VS-18 customer shell foundation across existing live customer-facing routes so NoorPath behaves as one connected product with consistent headers, footers, navigation, breadcrumbs, support access and responsive behavior.

This slice changes presentation composition and reachability only. Existing package, booking, payment, traveller, document, visa, family, cancellation and support domain behavior remains owned by the existing modules.

## Why this slice is needed

VS-18 created the reusable visual and interaction foundation but intentionally limited adoption to internal showcase routes. Existing live pages still contain duplicated or page-specific shell markup, inconsistent navigation, uneven footer treatment and routes that are only proven through direct URL tests.

The approved improvement sequence places consistent customer headers and footers immediately after the design-system foundation. PR #77 also established the permanent rule that destination-page tests do not replace click-through verification from real controls.

## Actors

- Public visitor browsing Umrah packages.
- Customer beginning or resuming a reservation.
- Authenticated customer viewing account and My Journey routes.
- Authenticated booking owner using journey, document, visa and cancellation destinations.
- Customer using mobile navigation, breadcrumbs, back actions and support entry points.

## Shell variants

### Public customer shell

Use on applicable unauthenticated/public routes, including:

- Landing page.
- Package discovery and results.
- Package Details.
- Public How It Works content.
- Public Talk to Us/support entry.
- Public My Journey entry before authentication.

Required primary navigation:

- Packages.
- How It Works.
- Talk to Us.
- My Journey.

The full approved customer footer is required on applicable public pages.

### Authenticated customer shell

Use on applicable signed-in customer routes, including:

- Customer account and profile.
- Family and traveller management.
- My Journey list.
- Booking journey detail.
- Customer document, visa and cancellation/refund status surfaces.

Required primary navigation:

- Packages.
- My Journey.
- Help.
- Talk to Us.
- Profile.

Staff, operator and platform-administration routes must never appear in the customer shell.

### Transactional customer shell

Use on focused, interruption-sensitive routes, including where present:

- Phone/OTP entry.
- Reservation or booking creation.
- Checkout and payment.
- Confirmation transition.
- Traveller-detail capture.
- Document upload.

The transactional shell must provide:

- NoorPath identity and a safe back/exit path.
- Current step or context when useful.
- Compact legal/support footer.
- No unrelated promotional or staff navigation.

## User flows

### Public package journey

Landing or discovery → package result → Package Details → reservation entry.

Assertions:

- Every source control is a real link or button with a valid destination.
- The active route is communicated visually and semantically.
- Header and footer remain consistent without changing approved page content.
- Mobile navigation remains usable without horizontal overflow.

### My Journey entry

Public My Journey control → authentication boundary when required → exact intended My Journey destination.

The existing identity boundary must not be weakened. Until Auth0 and a real customer identity are configured, production return-to verification remains `BLOCKED_IDENTITY` and must be retested later.

### Authenticated journey navigation

My Journey list → journey detail → document, visa, cancellation/refund and support destinations.

Assertions:

- Booking-owned routes remain account isolated.
- Foreign-account routes retain safe not-found behavior.
- Breadcrumbs and back-navigation preserve context.
- Customer-safe labels do not expose internal operational detail.

### Transactional navigation

Package/reservation entry → authentication/OTP → reservation/payment/traveller/document step → My Journey or confirmation destination.

Assertions:

- Existing selections and return destinations are not discarded by shell composition.
- Back/exit controls are explicit and safe.
- Compact support/legal content is present.
- The shell does not change payment, booking or document state.

## UI and design authority

The following remain authoritative:

1. Approved Landing reference.
2. Approved Package Details reference.
3. `design-system/MASTER.md`.
4. VS-18 tokens, icons, components and shell primitives.
5. Existing approved page-specific content and domain states.

This slice may:

- replace duplicated shell markup with shared components;
- normalize page containers, header/footer composition and responsive navigation;
- add active-route state, breadcrumbs and consistent back-navigation;
- repair missing or invalid customer links;
- adopt existing VS-18 tokens and icon mapping in changed shell code;
- add minimal adapters required to compose existing page content inside shared shells.

This slice must not:

- redesign Landing or Package Details;
- restructure page-specific business content;
- change the fixed Package Details section order;
- create new commercial states or terminology;
- introduce a generic marketplace or dashboard visual language.

## Backend and data changes

No backend, database or migration change is expected.

A backend or API change is permitted only when an existing customer navigation target cannot be represented truthfully without a minimal safe presentation contract. Any such change must remain within the owning module, preserve authorization and be explicitly documented in the PR.

## Security and privacy

- Customer shells must never expose operator or administrator navigation.
- Protected routes remain deny-by-default.
- Return-to handling must validate local destinations and prevent open redirects.
- Customer, booking and traveller identifiers must not be added to global navigation telemetry.
- Foreign-account resources must continue to return safe not-found responses.
- Support links may use safe booking references but must not leak internal IDs or private payloads.
- No Auth0 credential, token or session value may appear in logs, rendered evidence or test artifacts.

## Accessibility and responsive requirements

Changed shells and routes must provide:

- semantic header, navigation, main and footer landmarks;
- keyboard-operable desktop and mobile navigation;
- visible focus and logical focus order;
- appropriate current-page state;
- minimum target size for interactive controls;
- accessible menu names and expanded/collapsed state;
- escape and focus-return behavior for overlays or drawers;
- 200% text scaling without clipping;
- mobile reflow without horizontal scrolling;
- reduced-motion equivalents;
- non-color indication of active and error states.

## Navigation verification

`docs/slices/VS-19-NAVIGATION-VERIFICATION.md` is mandatory.

Every changed page, header item, footer item, breadcrumb, card link, support entry and transactional back/exit control must be represented in the matrix with one result:

- `VERIFIED`.
- `BLOCKED_IDENTITY`.
- `NOT_APPLICABLE`.
- `PENDING` during implementation only.
- `FAILED`, which blocks certification.

Direct-route tests supplement but do not replace click-through tests from the real source control.

## Deliverables

- Shared public customer shell adoption.
- Shared authenticated customer shell adoption.
- Shared transactional customer shell adoption.
- Full public footer and compact transactional footer adoption.
- Mobile navigation behavior and active-route states.
- Consistent breadcrumbs and back-navigation.
- Valid support links.
- Component/unit coverage for shell behavior.
- Desktop Chromium and mobile WebKit click-through tests.
- Accessibility and rendered-regression evidence.
- Exact-head navigation matrix and certification record.

## Acceptance criteria

1. Every applicable live customer route uses the correct shell variant.
2. Public pages expose only the approved public navigation items.
3. Authenticated pages expose only the approved customer navigation items.
4. Transactional pages use the reduced-distraction shell and compact footer.
5. Every visible customer navigation target resolves correctly or is explicitly `BLOCKED_IDENTITY`.
6. Header, footer, breadcrumbs and back-navigation work on desktop and mobile.
7. No changed route exposes staff navigation or weakens authorization.
8. No changed route introduces horizontal overflow, inaccessible menu state or clipped text at 200% scaling.
9. Serious and critical automated accessibility findings are zero.
10. Landing and Package Details retain their approved identity and page-specific structure.
11. Existing domain state, APIs, financial behavior and persistence remain unchanged unless an approved minimal adapter is documented.
12. Exact-head CI, Navigation Reachability Review and Rendered Slice Review pass.
13. Product Owner approves the exact unchanged implementation SHA before merge.
14. Merge does not imply real production deployment.

## Testing

- Slice manifest and navigation-matrix validation.
- Formatting and static analysis.
- Existing frontend unit/component tests.
- Production web build.
- Shell component tests for variants, landmarks, active route and mobile menu state.
- Desktop Chromium click-through tests.
- Mobile WebKit click-through tests.
- Keyboard, focus return, target size and accessible-name checks.
- 200% text scaling and mobile reflow.
- Reduced-motion verification.
- Safe customer-route and foreign-account security regression tests.
- Rendered comparison against approved Landing and Package visual authority.

## Dependencies

- VS-18 design-system foundation: merged.
- PR #77 navigation-reachability governance and VS-16 repair: merged.
- Approved `improvement.md` customer navigation and footer direction: merged.
- Auth0/customer identity configuration: not currently available for real identity smoke tests; relevant rows remain `BLOCKED_IDENTITY`.

## Risks and decisions

- Broad shell adoption can create accidental page-level visual drift. Limit changed markup to shell composition and minimal spacing integration.
- Mobile drawers can introduce focus, scroll-lock and hydration issues. Prefer the smallest maintainable interaction pattern with explicit tests.
- Existing routes may contain inconsistent or placeholder destinations. Do not invent content; use an existing approved destination or mark the route unavailable until separately approved.
- Identity-dependent return-to behavior cannot be certified with real users yet. Preserve the restriction and retain explicit follow-up evidence.

## Branch, PR and deployment boundary

- Branch: `slice/vs19-customer-shell-navigation`.
- PR title: `VS-19: Adopt the NoorPath customer shell across live journeys`.
- PR state: Draft during implementation and evidence collection.
- Preview: isolated Vercel preview may be created after exact branch build is green.
- Merge: Product Owner approval required for the exact certified SHA.
- Real production deployment: separately approved and prohibited by this slice approval or merge.
