# VS-23 — Navigation Verification

Result values: `PASS`, `FAIL`, `BLOCKED_IDENTITY`, `BLOCKED_ENVIRONMENT`, `NOT_APPLICABLE`.

| Actor | Source | Destination | Expected behavior | Result | Evidence / follow-up |
|---|---|---|---|---|---|
| Approved operator | `/operator/packages` | `/operator/packages/new` | Primary action opens the package-first quick start | PASS | `apps/web/e2e/operator-draft-package-builder.spec.ts` verifies the route, heading and few-click controls. |
| Approved operator | Package card | `/operator/packages/new?cloneFrom={departureId}` | Clone creates a new private draft and leaves the source unchanged | PASS | Clone component creates through the existing operator-scoped API and redirects to the new departure draft. |
| Approved operator | Package quick start | Saved departure composer | Dates calculate duration/title; standard inclusions are preselected; bus/train is explicit | PASS | Rendered test verifies duration, title, terminology and intercity controls. |
| Approved operator | Saved draft | commercial and payment-plan editors | Continue to price, occupancy and payment milestones without rebuilding the package | PASS | Existing `CommercialEditor` and `PaymentPlanEditor` remain linked to the saved departure ID. |
| Approved operator | Saved draft | customer-style preview | Preview matches approved package-details contract | BLOCKED_ENVIRONMENT | Final preview route and rendered evidence are completed later in this Draft PR. |
| Approved operator | Publication review | platform approval queue | Operator submits; platform remains the approval authority | PASS | Existing publication review boundary remains unchanged. |
| Unauthenticated user | `/operator/packages/new` | sign-in boundary | Protected operator route does not disclose draft data | BLOCKED_IDENTITY | Requires the deployed Auth0-enabled environment for interactive verification. |
| Foreign operator member | Clone or edit another operator draft | Safe not found / forbidden | Operator isolation is preserved | BLOCKED_IDENTITY | Execute with retained cross-operator identities after exact-head deployment. |

No identity-dependent route is considered passed solely from rendered CI. `PASS` outcomes above cover deterministic route and component contracts or retained governed API boundaries.
