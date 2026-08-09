# VS-30 Navigation Verification

| Path | Actor / precondition | Expected outcome | Evidence | Status |
| --- | --- | --- | --- | --- |
| Public customer page -> Sign in | Anonymous visitor | Existing customer sign-in remains reachable | `CustomerRouteShell.tsx` | VERIFIED |
| Authenticated customer header -> account menu | Customer session | Safe member name opens Account, Settings, Help and Log out options | `account-identity-staff-access.spec.ts` | VERIFIED |
| Customer Account / Settings | Authenticated customer | Both options resolve to the existing protected `/account` workspace; no dead settings route is created | shared account menu + rendered coverage | VERIFIED |
| Customer Help | Authenticated customer | `/support` opens through existing customer routing | shared account menu | VERIFIED |
| Customer Log out | Authenticated customer | `/api/auth/sign-out` delegates to Auth0 logout and returns to NoorPath | sign-out route | VERIFIED |
| Public footer -> Operator / Admin login | Public visitor or staff member | Existing sign-in route is opened with `/operator` as safe return URL | `account-identity-staff-access.spec.ts` | VERIFIED |
| Operator shell -> account menu | Approved operator member | Member name remains distinct from operator business name and account/support/logout links are reachable | `account-identity-staff-access.spec.ts` | VERIFIED |
| Platform protected shell -> account menu | Platform Administrator | Member identity and account/help/logout options are present without weakening platform authorization | `ProtectedAccountShell.tsx` | VERIFIED |
| 390px customer header | Authenticated customer | Account controls remain visible without horizontal overflow | `account-identity-staff-access.spec.ts` | VERIFIED |
| Production deployment | Any | Deployment is not part of this slice | standing release discipline | NOT_APPLICABLE |

## Certification rule

The rows above document the implemented route contract. Exact-head Slice Governance, CI, Rendered Slice Review and Navigation Reachability Review must still run and pass on the unchanged final SHA before merge.
