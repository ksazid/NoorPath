# VS-16 — Navigation Verification Matrix

This matrix is required by `docs/06-engineering/NAVIGATION-VERIFICATION-GATE.md`.

Verified browser evidence:

- Navigation Reachability Review run `30890719264` on SHA `fc20e4f41fdd247319a41fbcc89b2b5016d0d609`.
- Desktop Chromium and mobile WebKit acceptance passed.
- Evidence artifact `8884970317` (`vs16-navigation-reachability`).
- Standard CI run `30890718422` passed frontend checks/build, .NET build, all registered migrations and the complete solution test suite.

## Customer routes and sections

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Customer | `/journeys` booking card | `View journey` | `/bookings/{bookingId}/journey` | Authenticated booking owner | VERIFIED | `apps/web/e2e/cancellation-refunds.spec.ts`; run `30890719264` | Clicked from the real journey list on desktop and mobile. |
| Customer | Journey overview | `Review cancellation options` | `#cancellation` section | Authenticated booking owner | VERIFIED | `apps/web/e2e/cancellation-refunds.spec.ts`; artifact `8884970317` | Explicit 44px section link scrolls to the authoritative cancellation panel. |
| Customer | Cancellation section | `Request cancellation review` | Updated cancellation status section | Authenticated eligible booking owner | VERIFIED | `apps/web/e2e/cancellation-refunds.spec.ts`; run `30890719264` | Request submission and under-review status passed without browser-derived money. |
| Customer | Journey breadcrumb | `My Journey` | `/journeys` | Authenticated booking owner | VERIFIED | Desktop/mobile Playwright; run `30890719264` | Mobile breadcrumb was restored and click-back passed. |
| Customer | Foreign-account deep link | Direct URL/API | Safe not-found response | Authenticated non-owner | VERIFIED | `CancellationRefundApiTests`; CI run `30890718422` | Booking existence remains hidden from another account. |
| Customer | Unauthenticated production deep link | Direct URL | Sign-in shell with correct return URL | Unauthenticated | BLOCKED_IDENTITY | Test-mode routing only | Production Auth0 values are unavailable. Verify the real redirect and return URL after `AUTH0_DOMAIN`, `AUTH0_CLIENT_ID`, `AUTH0_CLIENT_SECRET` and `AUTH0_SECRET` are restored; do not weaken access for testing. |

## Operator routes and sections

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Operator | `/operator` work areas | `Cancellations & refunds` | `/operator/cancellations` | Approved Operator membership | VERIFIED | `apps/web/e2e/cancellation-refunds.spec.ts`; run `30890719264` | Clicked from the Operator Overview on desktop and mobile. |
| Operator | Operator sidebar | `Cancellations` | `/operator/cancellations` | Approved Operator membership | VERIFIED | Desktop/mobile Playwright; artifact `8884970317` | Shared protected shell exposes and resolves the link. |
| Operator | `/operator/support` cancellation exception | `Open cancellation review` | `/operator/cancellations` | Operational Support or Admin permission | VERIFIED | `apps/web/e2e/cancellation-refunds.spec.ts`; run `30890719264` | API-generated target renders as a real link and reaches the workspace. |
| Operator | `/operator/cancellations` queue | `Review case` | Case detail section | Approved Operator in booking scope | VERIFIED | `apps/web/e2e/cancellation-refunds.spec.ts`; run `30890719264` | Queue-to-detail, stale recovery and disabled-provider states passed. |
| Operator | Cancellation case breadcrumb | `Operator` | `/operator` | Approved Operator membership | VERIFIED | Desktop/mobile Playwright; run `30890719264` | Back-navigation passed. |
| Operator | Foreign-operator deep link/API | Direct request | Safe not-found response | Approved membership for another operator | VERIFIED | `CancellationRefundApiTests`; CI run `30890718422` | A valid foreign Operator with AdminAccess receives 404. |
| Operator | Authenticated forbidden role | `/operator/cancellations` | Platform Administrator guidance to `/admin`, or forbidden customer state | Platform Administrator without Operator membership/customer-only account | VERIFIED | Nested route Playwright; run `30890719264` | Shared protected shell displays `Use NoorPath administration` and the `/admin` handoff without changing permissions. |
| Operator | Unauthenticated production deep link | Direct URL | Sign-in shell with correct return URL | Unauthenticated | BLOCKED_IDENTITY | Test-mode routing only | Production Auth0 values are unavailable. Verify the live sign-in/return flow after identity configuration is restored. |

## Responsive and shared navigation

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Customer mobile | `/journeys` and journey page | Journey, cancellation and breadcrumb links | Journey route, `#cancellation` and `/journeys` | Authenticated booking owner | VERIFIED | Mobile WebKit project; run `30890719264` | Visibility, minimum target size, breadcrumb and no-overflow checks passed. |
| Operator mobile | Operator overview/sidebar/support | Cancellation links | `/operator/cancellations` | Approved Operator membership | VERIFIED | Mobile WebKit project; run `30890719264` | Overview, sidebar, support and nested protected-shell navigation passed. |
| Shared | New/changed pages | Brand, breadcrumb and support links | Existing approved destinations | Applicable identity | VERIFIED | Cross-browser Playwright and accessibility helpers; run `30890719264` | No navigation dead end or serious/critical accessibility violation remained. |

## Identity-restricted verification log

| Route/path | Required identity or configuration | What was verified | Result | Follow-up verification |
| --- | --- | --- | --- | --- |
| Customer production sign-in return to `/bookings/{bookingId}/journey` | Configured Auth0 Regular Web Application and a real booking-owner Google identity | Test-mode authorization, route isolation and customer click paths | BLOCKED_IDENTITY | After Auth0 environment values are restored, sign out, open the deep link, authenticate with the booking owner and confirm return to the exact journey URL. |
| Operator production sign-in return to `/operator/cancellations` | Configured Auth0 application plus approved Operator membership | Test-mode Operator access, foreign-operator isolation and Platform Administrator `/admin` handoff | BLOCKED_IDENTITY | After Auth0 restoration, verify one approved Operator and one Platform Administrator on the live route without modifying memberships. |

## Completion result

- No `PENDING` or `FAILED` navigation result remains.
- All available customer/operator click sources were exercised on desktop and mobile.
- The two remaining restrictions are production identity-environment gaps, not unrecorded navigation assumptions.
- Product Owner approval must be informed that those two rows remain `BLOCKED_IDENTITY` until Auth0 production configuration is restored.