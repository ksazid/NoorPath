# VS-17 — Navigation Verification Matrix

This matrix is required by `docs/06-engineering/NAVIGATION-VERIFICATION-GATE.md` and must be completed against the exact Production Readiness release candidate.

Every release-scope page, section, menu, card, breadcrumb, deep link, redirect and API-generated target must be represented either directly below or in a linked release-scope matrix generated during VS-17 implementation.

## Public and customer journeys

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Public | Landing page | Package discovery/search controls | Discovery results and package details | Public | PENDING | `e2e/production-readiness.spec.ts` | Verify desktop/mobile click-through and browser refresh. |
| Customer | Package details | Quote/booking controls | Quote, hold, booking and payment sequence | Authenticated customer | PENDING | Production-readiness Playwright and API evidence | Verify return-to after sign-in and no dead end. |
| Customer | Account/customer shell | `Family travellers` | `/account/family` | Authenticated customer | PENDING | Production-readiness Playwright | Verify responsive navigation. |
| Customer | `/journeys` | Journey card | Journey detail | Authenticated booking owner | PENDING | Production-readiness Playwright | Verify real click source. |
| Customer | Journey detail | Documents, visa, cancellation and support links | Exact booking-owned destinations | Authenticated booking owner | PENDING | Production-readiness Playwright | Verify every section/action target. |
| Customer | Foreign-account deep links | Direct URL/API | Safe not-found | Authenticated non-owner | PENDING | Integration evidence | No resource-existence leakage. |
| Customer | Protected deep link | Sign-in and return-to flow | Exact originally requested route | Unauthenticated | PENDING | Demo Auth0 identity smoke evidence | Do not weaken identity restrictions. |

## Operator and platform journeys

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Operator | `/operator` overview and sidebar | Packages | `/operator/packages` | Approved Operator membership | PENDING | Production-readiness Playwright | Verify desktop/mobile and current-page state. |
| Operator | `/operator` overview and sidebar | Bookings | `/operator/bookings` | Approved Operator membership | PENDING | Production-readiness Playwright | Verify booking-management workspace is reachable from the real shell. |
| Operator | `/operator/packages` | Create/continue/preview/approval actions | Package draft, preview and review routes | Approved Operator membership with catalogue permission | PENDING | VS-23 + Production-readiness Playwright | No dead package-management actions. |
| Operator | `/operator/bookings` | Departure, document, visa, support and cancellation actions | Exact module-owned workspaces | Approved Operator membership | PENDING | Production-readiness Playwright | Verify all contextual booking actions. |
| Operator | `/operator` overview and sidebar | Document, visa, support and cancellation links | Exact operator workspaces | Approved Operator membership with required permission | PENDING | Production-readiness Playwright | Verify desktop/mobile and current-page state. |
| Operator | Operational Support | API-generated recovery/navigation actions | Exact module-owned workspace/action | Authorized Operational Support membership | PENDING | Production-readiness Playwright | No inert action target. |
| Operator | Foreign-operator resources | Direct URL/API | Safe not-found | Approved membership for another operator | PENDING | Integration evidence | No cross-operator disclosure. |
| Platform Admin | `/operator` and nested operator routes | Role guidance | `/admin` | Platform Administrator without Operator membership | PENDING | Role-routing Playwright and demo identity smoke | Preserve VS-02R boundary. |
| Platform Admin | `/admin` | Publication approvals | Platform publication queue/detail | Configured Platform Administrator | PENDING | Production-readiness Playwright | Verify independent approval path and no implicit Operator permission. |
| Platform Admin | Publication queue | Review/publish action | Final publication review and published state | Configured Platform Administrator | PENDING | Publication approval + Production-readiness Playwright | Preserve exact-version and no-self-approval rules. |
| Operator/Admin | Protected deep link | Sign-in and return-to flow | Exact authorized route or role guidance | Demo Auth0 identities | PENDING | Demo identity smoke evidence | Verify customer, Operator and Platform Administrator role boundaries. |

## Release, health and recovery routes

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Release operator | Manual deployment workflow | Exact-SHA deployment action | Approved environment deployment | Authorized release operator | NOT_APPLICABLE | Separate deployment approval | This recertification does not deploy. |
| Operations | Service endpoints | `/health/live` and `/health/ready` | Truthful health results | Approved observer | PENDING | Normal/degraded/recovery evidence | Verify dependencies and recovery. |
| Operations | Monitoring alert/runbook link | Alert action | Correct dashboard and runbook | Authorized operational staff | PENDING | Alert exercise evidence | No dead alert links. |
| Release operator | Failed release workflow/runbook | Rollback action | Prior known-good version and verification steps | Authorized release/rollback operator | NOT_APPLICABLE | Separate deployment/rollback exercise | No deployment is authorized by this slice run. |

## Responsive and shared navigation

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Customer mobile | All critical customer sources | Header/menu/cards/breadcrumbs/section links | All release-scope customer destinations | Applicable customer identity | PENDING | Mobile WebKit evidence | Verify visibility, target size, reflow and no dead end. |
| Operator mobile | Operator overview/sidebar/workspaces | Responsive navigation and breadcrumbs | Packages, Bookings and all release-scope operator destinations | Applicable Operator identity | PENDING | Mobile WebKit evidence | Verify permission-scoped entries. |
| Platform mobile | Admin/publication workspaces | Navigation and review controls | Approved platform destinations | Platform Administrator | PENDING | Mobile WebKit evidence | Verify approval flow remains usable without gaining Operator permission. |
| Shared | Header, footer, brand, breadcrumbs and support entry | All shared links | Approved destinations | Applicable identity | PENDING | Cross-browser click-through | Direct route tests are insufficient. |

## Identity verification log

The retained safe demo identities are now part of this recertification boundary. Certification must exercise one authenticated customer/booking-owner path, one approved Operator membership and one Platform Administrator path. Any provider/environment failure must be recorded as `BLOCKED_IDENTITY`; it must not be reported as passed.

| Route/path | Required identity/configuration | Required verification | Result | Follow-up verification |
| --- | --- | --- | --- | --- |
| `/bookings/{bookingId}/journey` protected deep link | Configured Auth0 and demo booking-owner identity | Sign in from the protected deep link and return to the exact owned journey | PENDING | Record exact identity smoke evidence or blocker. |
| `/operator/packages` and `/operator/bookings` protected deep links | Configured Auth0 and approved demo Operator membership | Sign in and reach both workspaces with intended operator scope | PENDING | Record exact identity smoke evidence or blocker. |
| `/operator/cancellations` protected deep link | Configured Auth0 and approved Operator permission | Sign in and return to the cancellation queue | PENDING | Record exact identity smoke evidence or blocker. |
| `/operator` to `/admin` role guidance | Configured Platform Administrator without implicit Operator membership | Verify role guidance to `/admin` and denial of Operator-only capability | PENDING | Record exact identity smoke evidence or blocker. |
| Platform publication approval | Configured Platform Administrator | Open queue/detail and verify final approval path remains independently authorized | PENDING | Record exact identity smoke evidence or blocker. |

## Completion rule

Before VS-17 receives `certify`:

- expand this matrix to cover every release-scope navigation path;
- change every `PENDING` row to `VERIFIED`, `BLOCKED_IDENTITY` or `NOT_APPLICABLE`;
- retain no `FAILED` row;
- prove desktop and mobile click-through from real source controls;
- exercise the retained demo customer, Operator and Platform Administrator identities where environment configuration permits;
- record identity/environment restrictions explicitly and never treat them as passed;
- summarize all `BLOCKED_IDENTITY` rows in the VS-17 PR and release decision;
- ensure evidence belongs to the exact unchanged release-candidate SHA.