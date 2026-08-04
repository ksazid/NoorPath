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
| Customer | Protected deep link | Sign-in and return-to flow | Exact originally requested route | Unauthenticated | PENDING | Production identity smoke evidence or `BLOCKED_IDENTITY` record | Do not weaken identity restrictions. |

## Operator and platform journeys

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Operator | `/operator` overview and sidebar | Authoring, document, visa, support and cancellation links | Exact operator workspaces | Approved Operator membership with required permission | PENDING | Production-readiness Playwright | Verify desktop/mobile and current-page state. |
| Operator | Operational Support | API-generated recovery/navigation actions | Exact module-owned workspace/action | Authorized Operational Support membership | PENDING | Production-readiness Playwright | No inert action target. |
| Operator | Foreign-operator resources | Direct URL/API | Safe not-found | Approved membership for another operator | PENDING | Integration evidence | No cross-operator disclosure. |
| Platform Admin | `/operator` and nested operator routes | Role guidance | `/admin` | Platform Administrator without Operator membership | PENDING | Role-routing Playwright and production identity smoke | Preserve VS-02R boundary. |
| Platform Admin | `/admin` | Administration navigation | Approved admin destinations | Configured Platform Administrator | PENDING | Production-readiness Playwright | Verify no implicit Operator permission. |
| Operator/Admin | Protected deep link | Sign-in and return-to flow | Exact authorized route or role guidance | Unauthenticated/forbidden identity | PENDING | Production identity smoke evidence or `BLOCKED_IDENTITY` record | Record unavailable identities explicitly. |

## Release, health and recovery routes

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Release operator | Manual deployment workflow | Exact-SHA deployment action | Approved environment deployment | Authorized release operator | PENDING | Workflow run and deployment evidence | Production deployment still requires separate approval. |
| Operations | Service endpoints | `/health/live` and `/health/ready` | Truthful health results | Approved observer | PENDING | Normal/degraded/recovery evidence | Verify dependencies and recovery. |
| Operations | Monitoring alert/runbook link | Alert action | Correct dashboard and runbook | Authorized operational staff | PENDING | Alert exercise evidence | No dead alert links. |
| Release operator | Failed release workflow/runbook | Rollback action | Prior known-good version and verification steps | Authorized release/rollback operator | PENDING | Rollback exercise evidence | Verify health and critical navigation after rollback. |

## Responsive and shared navigation

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Customer mobile | All critical customer sources | Header/menu/cards/breadcrumbs/section links | All release-scope customer destinations | Applicable customer identity | PENDING | Mobile WebKit evidence | Verify visibility, target size, reflow and no dead end. |
| Operator mobile | Operator overview/sidebar/workspaces | Responsive navigation and breadcrumbs | All release-scope operator destinations | Applicable Operator identity | PENDING | Mobile WebKit evidence | Verify permission-scoped entries. |
| Shared | Header, footer, brand, breadcrumbs and support entry | All shared links | Approved destinations | Applicable identity | PENDING | Cross-browser click-through | Direct route tests are insufficient. |

## Identity-restricted verification log

Add one row for every route that cannot be verified because an identity, membership, permission, tenant, provider or environment configuration is unavailable.

| Route/path | Required identity/configuration | What was attempted | Result | Follow-up verification |
| --- | --- | --- | --- | --- |
| None recorded yet | — | — | PENDING | Replace this row with exact evidence or `NOT_APPLICABLE` before certification. |

## Completion rule

Before VS-17 receives `certify`:

- expand this matrix to cover every release-scope navigation path;
- change every `PENDING` row to `VERIFIED`, `BLOCKED_IDENTITY` or `NOT_APPLICABLE`;
- retain no `FAILED` row;
- prove desktop and mobile click-through from real source controls;
- record production identity restrictions explicitly;
- summarize all `BLOCKED_IDENTITY` rows in the VS-17 PR and release decision;
- ensure evidence belongs to the exact unchanged release-candidate SHA.