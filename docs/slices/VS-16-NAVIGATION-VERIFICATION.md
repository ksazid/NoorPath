# VS-16 — Navigation Verification Matrix

This matrix is required by `docs/06-engineering/NAVIGATION-VERIFICATION-GATE.md` and must be complete before exact-head certification.

## Customer routes and sections

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Customer | `/journeys` booking card/list item | Existing journey link | `/bookings/{bookingId}/journey` | Authenticated booking owner | PENDING | `e2e/cancellation-refunds.spec.ts` | Verify click-through, not only direct navigation. |
| Customer | `/bookings/{bookingId}/journey` commercial/cancellation panel | `Review cancellation policy` | Cancellation policy and entitlement section for the same booking | Authenticated booking owner with confirmed booking | PENDING | `e2e/cancellation-refunds.spec.ts` | Must show authoritative server calculation and policy version. |
| Customer | Cancellation policy section | `Request whole-booking cancellation` | Confirmation/request section | Authenticated eligible booking owner | PENDING | `e2e/cancellation-refunds.spec.ts` | Must clearly state whole-booking impact before submission. |
| Customer | Cancellation request confirmation | Submit action | Cancellation status section on the same journey | Authenticated eligible booking owner | PENDING | `e2e/cancellation-refunds.spec.ts` | Verify duplicate-safe response and no dead end. |
| Customer | Cancellation/refund status section | `Contact support` or existing support action | Existing support route/mail action with booking reference | Authenticated booking owner | PENDING | `e2e/cancellation-refunds.spec.ts` | Verify target is usable and preserves safe reference only. |
| Customer | Direct deep link | Browser refresh/direct URL | `/bookings/{bookingId}/journey` cancellation section | Authenticated booking owner | PENDING | Playwright direct-load test | Verify refresh and loading/recovery states. |
| Customer | Foreign-account deep link | Direct URL | Safe not-found response | Authenticated non-owner | PENDING | API/integration test | Must not reveal booking existence. |
| Customer | Unauthenticated deep link | Direct URL | Sign-in shell and correct return-to route | Unauthenticated | PENDING | Playwright auth-routing test | Record `BLOCKED_IDENTITY` if production Auth0 cannot be exercised. |

## Operator routes and sections

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Operator | `/operator` workspace | `Cancellation reviews` navigation/card | `/operator/cancellations` | Approved Operator membership with cancellation/support permission | PENDING | `e2e/cancellation-refunds.spec.ts` | New page must be linked from the operator shell. |
| Operator | `/operator/support` | Cancellation/refund exception action | `/operator/cancellations` or exact case target | Approved Operator with Operational Support permission | PENDING | `e2e/cancellation-refunds.spec.ts` | API-generated action target must be clicked and verified. |
| Operator | `/operator/cancellations` queue | `Review case` | Cancellation case detail/section | Approved Operator scoped to booking operator | PENDING | `e2e/cancellation-refunds.spec.ts` | Verify queue filter and case loading. |
| Operator | Cancellation case detail | `Approve cancellation` | Updated approved/cancelled/refund status | Approved Operator with required permission | PENDING | Playwright + integration test | Expected version and reason required. |
| Operator | Cancellation case detail | `Reject cancellation` | Updated rejected state | Approved Operator with required permission | PENDING | Playwright + integration test | Must provide customer-safe reason mapping. |
| Operator | Cancellation case detail | Recovery/refund action when enabled | Updated recovery/refund state | Approved Operator with required permission | PENDING | Playwright + integration test | Production provider execution remains disabled. |
| Operator | Foreign-operator deep link | Direct URL | Safe not-found response | Approved membership for another operator | PENDING | Integration test | Must not reveal another operator’s case. |
| Operator | Authenticated forbidden role | `/operator/cancellations` direct link | Forbidden guidance; Platform Admin uses `/admin` | Platform Administrator without Operator membership, or customer-only account | PENDING | Playwright role-routing test | Preserve VS-02R role boundaries. |
| Operator | Unauthenticated deep link | Direct URL | Sign-in shell and return-to route | Unauthenticated | PENDING | Playwright auth-routing test | Record `BLOCKED_IDENTITY` if production Auth0 cannot be exercised. |

## Responsive and shared navigation

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Customer mobile | Mobile journey navigation | Cancellation entry and back/breadcrumb controls | Correct cancellation section and `/journeys` | Authenticated booking owner | PENDING | Mobile Playwright project | Verify no hidden or unreachable control. |
| Operator mobile | Mobile operator navigation | Cancellation reviews entry and breadcrumbs | `/operator/cancellations` and `/operator` | Approved Operator membership | PENDING | Mobile Playwright project | Verify collapsed/responsive navigation. |
| Shared | New pages | Header logo, breadcrumb and footer links | Existing approved destinations | Applicable authenticated identity | PENDING | Playwright click-through | Ensure generated pages are not isolated dead ends. |

## Identity-restricted verification log

No identity restriction is accepted silently. Add one row per unverified identity path before certification.

| Route/path | Required identity or permission | What was attempted | Result | Follow-up verification |
| --- | --- | --- | --- | --- |
| None recorded yet | — | — | — | — |

## Completion rule

Before `certify`:

- every `PENDING` row is changed to `VERIFIED`, `BLOCKED_IDENTITY` or `NOT_APPLICABLE`;
- no `FAILED` row remains;
- all new pages have a tested clickable entry point;
- direct-route tests are not used as a substitute for link verification;
- every `BLOCKED_IDENTITY` row is summarized in PR #70 for Product Owner visibility.