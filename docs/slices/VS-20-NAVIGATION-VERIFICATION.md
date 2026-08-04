# VS-20 — Navigation and Identity Verification

Result values: `PASS`, `FAIL`, `BLOCKED_IDENTITY`, `BLOCKED_ENVIRONMENT`, `NOT_APPLICABLE`.

This slice changes no navigation control or page rendering. The matrix records the protected routes whose real-identity verification motivated the persistence repair. A route is not marked `PASS` until the deployed environment and Auth0 session have exercised it.

| Actor | Source | Destination | Expected authorization | Result | Evidence / follow-up |
|---|---|---|---|---|---|
| Customer | `/auth/sign-in?returnUrl=/journeys` | `/journeys` | Authenticated account sees only owned confirmed/cancelled bookings | BLOCKED_ENVIRONMENT | Complete Documents migration repair, deploy exact commit, then sign in with the designated Google account. |
| Customer | `/journeys` | `/bookings/71000000-0000-0000-0000-000000000001/journey` | Account owns `DEMO-LKO-001` | BLOCKED_ENVIRONMENT | Verify after hotfix deployment. |
| Customer | Owned journey | `/bookings/71000000-0000-0000-0000-000000000001/documents` | Owned confirmed booking; requirements may be created lazily | BLOCKED_ENVIRONMENT | Missing Documents persistence is the defect repaired by VS-20. |
| Customer | Owned journey | `/bookings/71000000-0000-0000-0000-000000000001/visa` | Owned confirmed booking | BLOCKED_IDENTITY | Requires interactive Auth0 session after deployment. |
| Customer | Direct foreign ID | Delhi/Mumbai booking detail | Safe not-found without existence disclosure | BLOCKED_IDENTITY | Verify with real customer session after deployment. |
| Operator | `/auth/sign-in?returnUrl=/operator` | `/operator` | Active membership under Approved operator | BLOCKED_IDENTITY | Membership and permissions are provisioned; interactive session remains. |
| Operator | `/operator` | `/operator/documents` | `operator.documents.review` | BLOCKED_ENVIRONMENT | Requires repaired Documents schema. |
| Operator | `/operator` | `/operator/visa` | `operator.visa.process` | BLOCKED_IDENTITY | Verify queue scoping to `demo-noorpath-operator`. |
| Operator | `/operator` | `/operator/cancellations` | `operator.admin.access` or governed cancellation permission contract | BLOCKED_IDENTITY | Verify exact deployed route and queue scoping. |
| Operator | `/operator` | `/operator/support` | `operator.support.manage` or admin access | BLOCKED_ENVIRONMENT | Support endpoint queries DocumentsDbContext and requires repaired schema. |
| Platform administrator | `/auth/sign-in?returnUrl=/admin` | `/admin` | Account appears in PlatformAdministratorAccountIds | BLOCKED_IDENTITY | Managed Render allow-list is configured; interactive session remains. |
| Publication approver | `/platform/publications` | publication detail | Account appears in PlatformPublicationApproverAccountIds | BLOCKED_IDENTITY | Managed Render allow-list is configured; interactive session remains. |
| Anonymous visitor | Protected deep link | Auth0 sign-in | No protected content before authentication; returnUrl remains same-origin | BLOCKED_IDENTITY | Verify through browser session. |

## Non-navigation change statement

- No link, menu, breadcrumb, route, page component or shell is added or modified by VS-20.
- Automated click-through navigation review is not applicable to the source diff.
- Real-identity verification remains mandatory because the slice repairs infrastructure required by existing protected destinations.
