# VS-19 — Customer Shell and Navigation Adoption Checklist

## Planning and authority

- [x] Register VS-19 with a unique slice identifier.
- [x] Base the slice on merged PR #77 and VS-18.
- [x] Confirm approved Landing and Package Details references remain visual authority.
- [x] Confirm `design-system/MASTER.md` and VS-18 components are implementation authority.
- [x] Confirm approved public navigation: Packages, How It Works, Talk to Us, My Journey.
- [x] Confirm approved authenticated navigation: Packages, My Journey, Help, Talk to Us, Profile.
- [x] Confirm transactional routes require reduced-distraction shell and compact footer.
- [x] Confirm no backend, database or domain-state change is expected.
- [x] Record Auth0/real-identity checks as deferred `BLOCKED_IDENTITY`, not waived.
- [ ] Confirm all target routes and current shell implementations through repository inspection.
- [ ] Record any route that has no approved destination before implementation.

## Route inventory and classification

- [ ] Inventory all public customer routes.
- [ ] Inventory all authenticated customer routes.
- [ ] Inventory all transactional routes.
- [ ] Inventory duplicated headers, footers, breadcrumbs and back controls.
- [ ] Identify routes already using VS-18 primitives.
- [ ] Identify page-specific CSS likely to conflict with shared shells.
- [ ] Classify each route as public, authenticated, transactional or excluded.
- [ ] Record every route in the navigation matrix.
- [ ] Confirm staff/operator/admin routes are excluded from customer shell adoption.

## Shared public shell

- [ ] Adopt canonical public header on Landing without redesign.
- [ ] Adopt canonical public header on discovery/results.
- [ ] Adopt canonical public header on Package Details without changing fixed section order.
- [ ] Adopt canonical public shell on How It Works destination.
- [ ] Adopt canonical public shell on Talk to Us destination.
- [ ] Adopt canonical public My Journey entry.
- [ ] Use approved full footer on applicable public pages.
- [ ] Implement active-route indication.
- [ ] Verify brand/home link destination.
- [ ] Verify all header and footer links are valid.

## Shared authenticated shell

- [ ] Adopt canonical customer shell on account/profile routes.
- [ ] Adopt canonical customer shell on family/traveller routes.
- [ ] Adopt canonical customer shell on My Journey list.
- [ ] Adopt canonical customer shell on journey detail.
- [ ] Adopt canonical customer shell on customer document status routes.
- [ ] Adopt canonical customer shell on customer visa status routes.
- [ ] Adopt canonical customer shell on cancellation/refund status routes.
- [ ] Ensure staff/operator/admin navigation is never rendered.
- [ ] Preserve account and booking ownership checks.
- [ ] Preserve safe not-found behavior for foreign resources.

## Transactional shell

- [ ] Identify all OTP/authentication entry routes.
- [ ] Identify all reservation/booking routes.
- [ ] Identify all payment and confirmation routes.
- [ ] Identify traveller-detail capture routes.
- [ ] Identify document-upload routes.
- [ ] Adopt reduced-distraction transactional header.
- [ ] Adopt compact legal/support footer.
- [ ] Provide safe back/exit controls.
- [ ] Preserve current selection and return destination through shell composition.
- [ ] Ensure no unrelated promotional or staff navigation appears.

## Navigation and breadcrumbs

- [ ] Add consistent active-page state.
- [ ] Add or normalize breadcrumbs where applicable.
- [ ] Add or normalize mobile back-navigation.
- [ ] Ensure breadcrumbs use real links, not inert text controls.
- [ ] Verify My Journey list → journey detail click-through.
- [ ] Verify journey detail → documents click-through.
- [ ] Verify journey detail → visa click-through.
- [ ] Verify journey detail → cancellation/refund click-through.
- [ ] Verify journey detail → support click-through.
- [ ] Verify footer support/legal destinations.
- [ ] Verify API-generated navigation targets remain real links.

## Mobile navigation

- [ ] Implement or adopt the smallest approved mobile navigation pattern.
- [ ] Provide accessible menu name and expanded/collapsed state.
- [ ] Keep menu controls at least the approved target size.
- [ ] Trap focus only when using a modal/drawer pattern.
- [ ] Return focus to the menu trigger on close.
- [ ] Support Escape where an overlay/drawer is used.
- [ ] Prevent background interaction only when required by the interaction model.
- [ ] Verify no horizontal overflow at supported widths.
- [ ] Verify long labels and 200% text scaling.
- [ ] Verify orientation and viewport changes do not strand the menu open.

## Styling and design-system adoption

- [ ] Use VS-18 semantic tokens in changed shell code.
- [ ] Use the approved NoorPath icon mapping; no emoji icons.
- [ ] Reuse VS-18 shell primitives before creating new primitives.
- [ ] Keep changed class names bounded and maintainable.
- [ ] Preserve Landing typography, spacing and imagery.
- [ ] Preserve Package Details structure and visual hierarchy.
- [ ] Preserve full footer design on applicable public pages.
- [ ] Avoid global CSS changes that alter unrelated staff or design-system routes.
- [ ] Verify reduced-motion behavior.

## Security and privacy

- [ ] Verify customer shell never exposes staff routes.
- [ ] Verify protected pages remain deny-by-default.
- [ ] Verify local return-to validation prevents open redirects.
- [ ] Verify foreign-account resources remain safe not-found.
- [ ] Verify global shell telemetry excludes customer and booking identifiers.
- [ ] Verify support links expose only approved safe references.
- [ ] Verify no token, session value or Auth0 secret appears in rendered evidence.
- [ ] Keep identity-dependent real-user checks `BLOCKED_IDENTITY` until configuration exists.

## Component and unit tests

- [ ] Test public shell navigation items and landmarks.
- [ ] Test authenticated shell navigation items and absence of staff routes.
- [ ] Test transactional shell reduced navigation and compact footer.
- [ ] Test active-route state.
- [ ] Test mobile menu expanded/collapsed state.
- [ ] Test keyboard close and focus return where applicable.
- [ ] Test breadcrumbs and back controls.
- [ ] Test invalid/unsupported destination handling.
- [ ] Test shell composition does not alter page-owned content props.

## Navigation reachability

- [ ] Complete every row in `docs/slices/VS-19-NAVIGATION-VERIFICATION.md`.
- [ ] Replace every `PENDING` row before certification.
- [ ] Retain no `FAILED` row.
- [ ] Verify desktop Chromium click-through from real source controls.
- [ ] Verify mobile WebKit click-through from real source controls.
- [ ] Verify public header and footer links.
- [ ] Verify authenticated customer navigation using synthetic/test identity fixtures.
- [ ] Verify transactional back/exit paths.
- [ ] Verify foreign-account safe not-found behavior.
- [ ] Record Auth0 real-user return-to routes as `BLOCKED_IDENTITY` with exact follow-up.

## Accessibility and responsive testing

- [ ] Verify semantic header, nav, main and footer landmarks.
- [ ] Verify accessible names and current-page state.
- [ ] Verify keyboard-only operation.
- [ ] Verify visible focus and logical order.
- [ ] Verify minimum target sizes.
- [ ] Verify serious and critical automated accessibility findings are zero.
- [ ] Verify 200% text scaling.
- [ ] Verify mobile reflow and no clipped content.
- [ ] Verify reduced motion.
- [ ] Verify screen-reader status for menu state where applicable.

## Regression and build verification

- [ ] Run slice manifest validation.
- [ ] Run navigation registration validation.
- [ ] Run formatting checks.
- [ ] Run static analysis.
- [ ] Run frontend unit/component tests.
- [ ] Run production web build.
- [ ] Run relevant .NET/integration tests when changed or impacted.
- [ ] Run secret scanning.
- [ ] Run desktop and mobile rendered review.
- [ ] Compare Landing and Package Details with approved references.
- [ ] Verify design-system showcase remains internal/fail-closed as configured.

## Pull request and evidence

- [ ] Keep PR Draft during implementation.
- [ ] Record all changed routes and shell classifications in the PR.
- [ ] Record any minimal adapter/API change explicitly.
- [ ] Upload rendered evidence for desktop and mobile.
- [ ] Record exact implementation SHA.
- [ ] Apply `certify` only after implementation is complete.
- [ ] Require exact-head Slice Governance, Navigation Reachability Review, Rendered Slice Review and CI success.
- [ ] Confirm zero unresolved review threads.
- [ ] Obtain Product Owner approval for exact unchanged SHA.
- [ ] Merge without real production deployment.

## Deferred identity follow-up

- [ ] Configure Auth0 customer identity environment.
- [ ] Create/identify a real booking-owner test identity.
- [ ] Verify unauthenticated customer deep-link sign-in and return to exact journey route.
- [ ] Verify real customer post-login navigation and profile route.
- [ ] Retain evidence for the real production-readiness decision.

These identity tasks are not required to implement the shared shell but remain mandatory before NoorPath's real production release.
