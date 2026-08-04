# VS-19 — Customer Shell and Navigation Adoption Checklist

## Planning and authority

- [x] Register VS-19 with a unique slice identifier.
- [x] Base the slice on merged PR #77 and VS-18.
- [x] Keep the approved Landing and Package Details references as visual authority.
- [x] Keep `design-system/MASTER.md` and VS-18 components as implementation authority.
- [x] Use approved public navigation: Packages, How It Works, Talk to Us, My Journey.
- [x] Use approved authenticated navigation: Packages, My Journey, Help, Talk to Us, Profile.
- [x] Use reduced-distraction chrome and compact footer on transactional routes.
- [x] Confirm no backend, API, database, migration, payment-state or authorization change is required.
- [x] Record Auth0/real-identity checks as deferred `BLOCKED_IDENTITY`, not waived.

## Route inventory and classification

- [x] Inspect existing customer pages, `PublicHeader`, `PublicFooter`, `ProtectedAccountShell` and VS-18 shell primitives.
- [x] Classify public routes: `/`, `/packages/*`, `/support`, `/privacy`, `/terms`.
- [x] Classify authenticated routes: `/journeys`, `/account*`, customer journey and visa routes.
- [x] Classify transactional routes: `/auth/sign-in`, `/inventory-holds/*`, payment, confirmation and document-upload routes.
- [x] Exclude `/operator*`, `/admin*`, `/platform*`, `/design-system*` and `/api*`.
- [x] Inventory duplicate legacy headers and footers and suppress them only inside the adopted customer route shell.
- [x] Keep existing page-owned `<main>`, breadcrumbs, forms, state and commands intact.
- [x] Record every changed navigation surface in `VS-19-NAVIGATION-VERIFICATION.md`.

## Shared public shell

- [x] Adopt the canonical public shell on Landing and discovery without redesigning their content.
- [x] Adopt the canonical public shell on Package Details without changing its fixed section order.
- [x] Retain full public shell on the package-planning route.
- [x] Add valid `/support`, `/privacy` and `/terms` destinations.
- [x] Add approved full footer to applicable public pages.
- [x] Implement route-aware active-page indication.
- [x] Verify brand/home, header and footer destinations through source-control clicks.

## Shared authenticated shell

- [x] Adopt the canonical customer shell on account/profile routes.
- [x] Adopt it on family/traveller routes.
- [x] Adopt it on My Journey list and journey detail.
- [x] Adopt the correct shell mode on documents, visa and cancellation/refund customer routes.
- [x] Ensure customer navigation never renders staff/operator/admin entries.
- [x] Preserve existing account and booking ownership checks.
- [x] Preserve existing safe-not-found behavior for foreign resources.

## Transactional shell

- [x] Adopt the reduced-distraction header on sign-in/auth entry.
- [x] Adopt it on inventory-hold/booking, payment, confirmation and document-upload routes.
- [x] Add compact Support, Privacy and Terms footer.
- [x] Retain page-owned breadcrumb, back, exit and continue controls.
- [x] Preserve return URL, traveller selection, quote, hold, booking and payment state because the adapter changes presentation only.
- [x] Ensure no promotional, operator or administrator navigation is rendered.

## Navigation and breadcrumbs

- [x] Add consistent current-page state to shared navigation.
- [x] Preserve existing real breadcrumb links rather than replacing them with inert controls.
- [x] Verify Landing header and footer click paths.
- [x] Verify discovery result → Package Details → plan click-through.
- [x] Verify My Journey list → journey detail.
- [x] Verify journey detail → documents.
- [x] Verify journey detail → visa.
- [x] Verify journey detail → cancellation/refund anchor.
- [x] Verify journey breadcrumb → My Journey.
- [x] Verify authenticated Packages, Help, Talk to Us and Profile click-through.
- [x] Preserve safe support references and existing API-generated links.

## Mobile navigation

- [x] Use the smallest approved non-modal pattern: native `<details>` and `<summary>`.
- [x] Provide an accessible Menu name and native expanded/collapsed state.
- [x] Keep menu controls and links at approved target size.
- [x] Verify open and close behavior.
- [x] Verify public and authenticated menu items.
- [x] Verify staff/admin links are absent.
- [x] Verify no horizontal overflow at supported widths.
- [x] Verify 200% text scaling and reduced motion.
- [x] Confirm focus trapping, background blocking and custom Escape handling are not applicable to the non-modal native details pattern.

## Styling and design-system adoption

- [x] Use VS-18 semantic tokens and `np-*` shell classes.
- [x] Reuse the approved NoorPath wordmark and existing icon system; add no emoji icons.
- [x] Keep new classes bounded to `CustomerRouteShell` and `customer-route-shell.css`.
- [x] Preserve Landing typography, spacing and imagery.
- [x] Preserve Package Details structure and visual hierarchy.
- [x] Avoid staff, administrator and design-system route changes.
- [x] Respect reduced-motion behavior.

## Security, privacy, migration and telemetry

- [x] Verify customer shell never exposes staff routes.
- [x] Preserve deny-by-default protected pages.
- [x] Preserve local return-to validation that rejects external/open redirects.
- [x] Preserve foreign-account safe-not-found behavior.
- [x] Add no global-shell telemetry containing customer, booking or traveller identifiers.
- [x] Keep support content limited to approved safe references and warn against sending passport/payment details.
- [x] Add no token, session value, Auth0 secret or sensitive payload to rendered evidence.
- [x] Add no database model or migration; migration validation remains a no-drift gate.
- [x] Keep real identity-dependent checks `BLOCKED_IDENTITY`.

## Automated verification implemented

- [x] Test public shell navigation and landmarks.
- [x] Test authenticated navigation and absence of staff routes.
- [x] Test transactional reduced navigation and compact footer.
- [x] Test route-aware current-page state.
- [x] Test mobile menu expanded/collapsed state.
- [x] Test source-to-destination breadcrumbs and card links.
- [x] Test staff routes remain outside the adapter.
- [x] Reuse existing domain tests for discovery, package, plan, journey, document, visa, cancellation, payment and confirmation behavior.
- [x] Complete every navigation matrix row without a failed result.
- [x] Retain Auth0 real-user paths as `BLOCKED_IDENTITY` with exact follow-up.

## Accessibility and responsive evidence

- [x] Preserve semantic header, nav, page-owned main and footer landmarks.
- [x] Verify accessible names and current-page state.
- [x] Verify visible focus on shared links/menu controls through existing design-system rules.
- [x] Verify minimum target sizes.
- [x] Verify serious and critical automated accessibility findings through the rendered suite.
- [x] Verify mobile reflow, 200% text scaling and reduced motion.
- [x] Verify no clipped content or horizontal overflow.

## Certification and pull request

- [x] Keep PR #78 Draft during implementation.
- [x] Record the route adapter, new destinations, classifications and exclusions in the PR artifacts.
- [x] Record that no adapter/API/database/domain change was made beyond presentation composition.
- [x] Prepare desktop and mobile rendered evidence tests.
- [ ] Run exact-head Slice Governance.
- [ ] Run exact-head Navigation Reachability Review.
- [ ] Run exact-head Rendered Slice Review.
- [ ] Run exact-head CI: formatting, static analysis, tests, build, migrations and secret scanning.
- [ ] Record the final unchanged implementation SHA and evidence artifact.
- [ ] Confirm zero unresolved review threads.
- [ ] Obtain Product Owner approval for the exact certified SHA.
- [ ] Merge without real production deployment.

## Deferred identity follow-up — required before real production

- [ ] Configure the Auth0 customer identity environment.
- [ ] Create or identify a real booking-owner test identity.
- [ ] Verify unauthenticated customer deep-link sign-in and return to the exact requested journey route.
- [ ] Verify real customer session, post-login navigation, logout and profile route.
- [ ] Retain this evidence in the real production-readiness decision.

These identity tasks do not block implementation or synthetic certification of VS-19, but they remain mandatory before NoorPath's real production release.
