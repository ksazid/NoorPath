# VS-16 — Navigation Verification Matrix

This matrix is required by `docs/06-engineering/NAVIGATION-VERIFICATION-GATE.md`.

## Customer routes and sections

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Customer | `/journeys` booking card | `View journey` | `/bookings/{bookingId}/journey` | Authenticated booking owner | PENDING | `apps/web/e2e/cancellation-refunds.spec.ts` | Must be verified by click-through, not direct navigation only. |
| Customer | Journey overview | `Review cancellation options` | `#cancellation` section | Authenticated booking owner | PENDING | `apps/web/e2e/cancellation-refunds.spec.ts` | New explicit section link. |
| Customer | Cancellation section | `Request cancellation review` | Updated cancellation status section | Authenticated eligible booking owner | PENDING | `apps/web/e2e/cancellation-refunds.spec.ts` | Must remain duplicate-safe. |
| Customer | Journey breadcrumb | `My Journey` | `/journeys` | Authenticated booking owner | PENDING | Playwright click-through | Verify back-navigation. |
| Customer | Foreign-account deep link | Direct URL | Safe not-found response | Authenticated non-owner | PENDING | Integration test | Must not reveal booking existence. |
| Customer | Unauthenticated deep link | Direct URL | Sign-in shell with correct return URL | Unauthenticated | BLOCKED_IDENTITY | Existing test-mode routing only | Production Auth0 values are unavailable; verify after identity configuration without weakening access. |

## Operator routes and sections

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Operator | `/operator` work areas | `Cancellations & refunds` | `/operator/cancellations` | Approved Operator membership | PENDING | `apps/web/e2e/cancellation-refunds.spec.ts` | Must click from overview. |
| Operator | Operator sidebar | `Cancellations` | `/operator/cancellations` | Approved Operator membership | PENDING | Playwright click-through | Verify current-page state. |
| Operator | `/operator/support` cancellation exception | `Open cancellation review` | `/operator/cancellations` | Operational Support or Admin permission | PENDING | `apps/web/e2e/cancellation-refunds.spec.ts` | API-generated target must render as a real link. |
| Operator | `/operator/cancellations` queue | `Review case` | Case detail section | Approved Operator in booking scope | PENDING | `apps/web/e2e/cancellation-refunds.spec.ts` | Verify queue-to-detail flow. |
| Operator | Cancellation case breadcrumb | `Operator` | `/operator` | Approved Operator membership | PENDING | Playwright click-through | Verify back-navigation. |
| Operator | Foreign-operator deep link/API | Direct request | Safe not-found response | Approved membership for another operator | PENDING | Integration test | Must not reveal another operator’s case. |
| Operator | Authenticated forbidden role | `/operator/cancellations` | Platform Administrator guidance to `/admin`, or forbidden customer state | Platform Administrator without Operator membership/customer-only account | PENDING | Existing role-routing Playwright + route-specific check | Preserve VS-02R boundary. |
| Operator | Unauthenticated deep link | Direct URL | Sign-in shell with correct return URL | Unauthenticated | BLOCKED_IDENTITY | Existing test-mode routing only | Production Auth0 values are unavailable; verify after identity configuration. |

## Responsive and shared navigation

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Customer mobile | `/journeys` and journey page | Journey and cancellation links | Journey route and `#cancellation` | Authenticated booking owner | PENDING | Mobile Playwright project | Verify target size, visibility and no overflow. |
| Operator mobile | Operator overview/sidebar/support | Cancellation links | `/operator/cancellations` | Approved Operator membership | PENDING | Mobile Playwright project | Verify responsive controls. |
| Shared | New/changed pages | Brand, breadcrumb and support links | Existing approved destinations | Applicable identity | PENDING | Playwright click-through | No dead-end pages. |

## Completion rule

Before this retrospective verification PR is approved:

- every `PENDING` row becomes `VERIFIED`, `BLOCKED_IDENTITY` or `NOT_APPLICABLE`;
- no `FAILED` row remains;
- Playwright proves the real click sources rather than only calling `page.goto()` on destinations;
- all remaining `BLOCKED_IDENTITY` rows are disclosed to the Product Owner with the required identity configuration and follow-up step.