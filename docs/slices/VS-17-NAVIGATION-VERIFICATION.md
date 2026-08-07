# VS-17 — Navigation Verification Matrix

This matrix is required by `docs/06-engineering/NAVIGATION-VERIFICATION-GATE.md` and is bound to the exact Production Readiness release-candidate head. `VERIFIED` rows are backed by the integrated VS-17 rendered/navigation suite or exact-head CI. Live-provider-only checks that cannot run inside GitHub certification are explicitly `BLOCKED_IDENTITY`; they are not treated as passed.

## Public and customer journeys

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Public | Landing page | Package discovery/search controls | Discovery results and package details | Public | VERIFIED | Integrated customer-shell/rendered suite | Desktop Chromium and mobile WebKit exercise real source controls. |
| Customer | Package details | Quote/booking controls | Quote, hold, booking and payment sequence | Authenticated test customer | VERIFIED | Integrated customer-shell plus existing booking/payment rendered contracts | Current-head rendered suite supplements domain/integration coverage. |
| Customer | Account/customer shell | `Family travellers` | `/account/family` | Authenticated test customer | VERIFIED | `family-booking-mahram` + customer-shell tests | Responsive navigation included. |
| Customer | `/journeys` | Journey card | Journey detail | Authenticated booking-owner test identity | VERIFIED | customer-shell/My Journey navigation coverage | Real click source retained. |
| Customer | Journey detail | Documents, visa, cancellation and support links | Exact booking-owned destinations | Authenticated booking-owner test identity | VERIFIED | customer-shell, documents, visa, cancellation and operational-support suites | Module-owned destinations remain linked. |
| Customer | Foreign-account deep links | Direct URL/API | Safe not-found | Authenticated non-owner fixture | VERIFIED | Exact-head integration/authorization CI | No resource-existence leakage. |
| Customer | Protected deep link | Auth0 sign-in and return-to | Exact originally requested route | Real Auth0 demo booking owner | BLOCKED_IDENTITY | Auth0 boundary tests pass; live provider session is outside GitHub browser certification | Demo account exists, but an interactive provider session is still required for the live smoke check. |

## Operator and platform journeys

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Operator | `/operator` sidebar | Packages | `/operator/packages` | Approved Operator test membership | VERIFIED | VS-17 production-readiness navigation test | Current-page state and responsive shell verified. |
| Operator | `/operator` sidebar | Bookings | `/operator/bookings` | Approved Operator test membership | VERIFIED | VS-17 production-readiness navigation test | Booking-management workspace is reached from the real shell. |
| Operator | `/operator/packages` | Create/continue/preview/approval actions | Package draft, preview and review routes | Approved Operator test membership with catalogue permission | VERIFIED | VS-23, operator-package-management and integrated VS-17 suite | No inert package-management action. |
| Operator | `/operator/bookings` | Departure, document, visa, support and cancellation actions | Exact module-owned workspaces | Approved Operator test membership | VERIFIED | operator-booking-management plus integrated operational suites | Contextual actions retain module ownership. |
| Operator | `/operator` sidebar | Document, visa, support and cancellation links | Exact operator workspaces | Approved permissioned Operator test identity | VERIFIED | documents, visa, operational-support and cancellation suites | Desktop/mobile source navigation retained. |
| Operator | Operational Support | API-generated recovery/navigation actions | Exact module-owned workspace/action | Authorized Operational Support test membership | VERIFIED | operational-support and cancellation suites | No inert action target. |
| Operator | Foreign-operator resources | Direct URL/API | Safe not-found | Approved foreign-operator fixture | VERIFIED | Exact-head integration/authorization CI | No cross-operator disclosure. |
| Platform Admin | Nested operator route | Role guidance | `/admin` | Role-preserving Platform Administrator test identity without Operator membership | VERIFIED | VS-17 platform-navigation test | Synthetic role boundary preserves VS-02R behavior. |
| Platform Admin | `/admin` | `Open publication reviews` | `/platform/publications` | Configured Platform Administrator test identity | VERIFIED | VS-17 platform-navigation + publication-approval suites | Independent approval path remains reachable. |
| Platform Admin | Publication queue | Review/publish action | Final publication review and published state | Configured Platform Administrator test identity | VERIFIED | platform-publication-approval suite | Exact-version and no-self-approval rules are unchanged. |
| Operator/Admin | Protected deep link | Auth0 sign-in and return-to | Exact authorized route or role guidance | Real Auth0 demo identity | BLOCKED_IDENTITY | Auth0 session/return-to contracts are automated; live provider session is not available to CI | Positive all-access smoke account cannot substitute for least-privilege live role-separation evidence. |

## Release, health and recovery routes

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Release operator | Manual deployment workflow | Exact-SHA deployment action | Approved environment deployment | Authorized release operator | NOT_APPLICABLE | Separate deployment approval | This recertification does not deploy. |
| Operations | Service endpoints | `/health/live` and `/health/ready` | Truthful health results | Approved observer | VERIFIED | `production-readiness.spec.ts` plus exact-head CI | Normal release candidate health contract is rechecked. |
| Operations | Monitoring/runbook references | Diagnostic/runbook action | Correct operational procedure | Authorized operational staff | VERIFIED | Existing VS-17 runbook/release validation remains unchanged and is included in exact-head CI | No runtime/runbook behavior changed by the recertification slice. |
| Release operator | Failed release workflow/runbook | Rollback action | Prior known-good version and verification steps | Authorized release/rollback operator | NOT_APPLICABLE | Separate deployment/rollback exercise | No deployment is authorized by this slice run. |

## Responsive and shared navigation

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Customer mobile | Critical customer sources | Header/menu/cards/breadcrumbs/section links | Release-scope customer destinations | Test customer identities | VERIFIED | Integrated suite on mobile WebKit | Visibility, target size, reflow and navigation are rechecked. |
| Operator mobile | Operator sidebar/workspaces | Responsive navigation and breadcrumbs | Packages, Bookings and operational destinations | Approved Operator test identity | VERIFIED | Integrated suite on mobile WebKit | Permission-scoped entries remain reachable. |
| Platform mobile | Admin/publication workspaces | Navigation and review controls | Approved platform destinations | Platform Administrator test identity | VERIFIED | Integrated suite on mobile WebKit | Approval flow remains usable without changing authorization. |
| Shared | Header, footer, brand, breadcrumbs and support entry | Shared links | Approved destinations | Applicable test identity | VERIFIED | Customer-shell and integrated navigation suites | Direct-route checks do not replace source-control clicks. |

## Live identity verification log

The safe real Auth0 smoke-test account provisioned in VS-20 remains an authenticated customer owning `DEMO-LKO-001`, an approved member of `demo-noorpath-operator`, and an allow-listed Platform Administrator/publication approver. It is useful for positive-access smoke testing, but because it is deliberately all-access it cannot prove least-privilege role separation by itself. GitHub certification also cannot establish an interactive Google provider session. Those limitations are recorded below rather than waived.

| Route/path | Required identity/configuration | Automated/current-head evidence | Result | Follow-up verification |
| --- | --- | --- | --- | --- |
| `/bookings/{bookingId}/journey` protected deep link | Real Auth0 demo booking-owner session | Auth0 return-to boundary, customer navigation and ownership isolation are automated | BLOCKED_IDENTITY | In the existing release environment, sign in with the provisioned smoke account from the protected deep link and verify return to `DEMO-LKO-001`. |
| `/operator/packages` and `/operator/bookings` protected deep links | Real Auth0 demo Operator session | Operator shell, Packages and Bookings click-through are verified on the exact head | BLOCKED_IDENTITY | In the existing release environment, sign in through Auth0 and verify both workspaces under `demo-noorpath-operator`. |
| `/operator/cancellations` protected deep link | Real Auth0 demo Operator session with permission | Cancellation source/destination and authorization contracts are automated | BLOCKED_IDENTITY | Verify live provider return-to without changing permissions. |
| `/operator` to `/admin` role guidance | A real Platform Administrator identity without Operator membership | Least-privilege behavior is verified with role-preserving automated identity | BLOCKED_IDENTITY | The current all-access smoke account cannot prove this negative boundary; use a dedicated Platform-only live identity when available. |
| Platform publication approval | Real Auth0 publication approver session | Admin-to-queue and publication review contracts are verified on exact head | BLOCKED_IDENTITY | Use the existing real smoke account to verify positive live access to the queue/detail in the release environment. |

## Completion rule

For merge certification:

- there must be no `PENDING` or `FAILED` row;
- every `VERIFIED` row must be exercised by exact-head automated evidence or unchanged exact-head CI coverage;
- every `BLOCKED_IDENTITY` row must name the missing live-provider condition and follow-up step;
- desktop Chromium and mobile WebKit must pass the integrated release-scope suite;
- no production deployment is implied or authorized by this recertification;
- production release remains blocked until any live identity checks required by the final go/no-go decision are completed.