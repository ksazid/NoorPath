# VS-21 — Navigation and Identity Verification

Result values: `PASS`, `FAIL`, `BLOCKED_IDENTITY`, `BLOCKED_ENVIRONMENT`, `NOT_APPLICABLE`.

The automated evidence in this slice verifies the NoorPath sign-in entry and safe relative return destinations without using real Auth0 credentials. Interactive callback, session persistence, role authorization and logout checks remain blocked until the exact certified head is deployed to the Auth0-enabled release environment.

| Actor | Source | Destination | Expected authorization / behavior | Result | Evidence / follow-up |
|---|---|---|---|---|---|
| Anonymous visitor | `/auth/sign-in?returnUrl=/account` | Auth0 login link | Sign-in page renders and preserves the relative `/account` destination | PASS | `apps/web/e2e/auth0-session-establishment.spec.ts` verifies the rendered sign-in entry, accessibility, target size, reflow and encoded return destination. |
| Anonymous visitor | External `returnUrl` value | Auth0 login link | Reject open redirect and fall back to `/account` | PASS | `apps/web/e2e/auth0-session-establishment.spec.ts` verifies an external URL is not propagated. |
| Anonymous visitor | `/account` | `/auth/login` | Begin Auth0 transaction and return to `/account` | BLOCKED_ENVIRONMENT | Requires deployment of the exact VS-21 head with the Auth0 environment variables and tenant callback configuration. |
| Authenticated customer | Auth0 callback | `/account` | Persist the encrypted session cookie and render My NoorPath | BLOCKED_IDENTITY | Requires interactive Google sign-in after exact-head deployment. |
| Authenticated customer | `/journeys` | `/bookings/71000000-0000-0000-0000-000000000001/journey` | Render owned `DEMO-LKO-001` only | BLOCKED_IDENTITY | Test identity and booking ownership are provisioned; execute after session establishment succeeds. |
| Authenticated customer | Owned journey | documents, visa, payment and cancellation pages | Render only customer-safe state for the owned booking | BLOCKED_IDENTITY | Requires the retained real Auth0 session. |
| Authenticated customer | Foreign Delhi/Mumbai booking deep link | Safe not found | Do not reveal foreign booking existence | BLOCKED_IDENTITY | Delhi and Mumbai fixtures remain available for isolation verification. |
| Approved operator member | `/operator` | operator queues | Allow active membership with explicit permissions | BLOCKED_IDENTITY | Membership and all four permissions are provisioned; execute after session establishment succeeds. |
| Platform administrator | `/admin` | administrator shell | Allow account configured in `PlatformAdministratorAccountIds` | BLOCKED_IDENTITY | Render allow-list is configured; interactive session remains required. |
| Publication approver | `/platform/publications` | publication detail | Allow account configured in publication approver allow-list | BLOCKED_IDENTITY | Interactive session remains required. |
| Authenticated user | `/auth/logout` | sign-in boundary | Remove session and deny subsequent protected requests | BLOCKED_ENVIRONMENT | Verify against the exact deployed VS-21 head after successful callback/session evidence. |

No identity-dependent route is considered passed until it is executed against the exact deployed VS-21 head with the retained Auth0 test identity. The two `PASS` outcomes cover only the deterministic rendered sign-in and return-destination contracts exercised in CI.
