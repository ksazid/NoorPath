# VS-02R — Authentication and protected account shell

Status: Implementation; Product Owner acceptance pending

## Outcome

Customers can start Google authentication through Auth0 Universal Login. Phone
OTP remains unavailable until an SMS provider is configured. Authenticated customers, operator users, and NoorPath
platform administrators receive distinct protected shells. API authorization
remains provider-neutral and denies access by default.

## Traceability

- `INV-ID-003`–`INV-ID-006`
- `INV-OP-001`–`INV-OP-003`
- ADR-002 — Identity, Authentication and Authorization Architecture

No stable approved PRD requirement ID was found; none is invented here.

## Included

- Auth0 Universal Login with Google sign-in;
- same-origin return URL validation;
- customer, operator, and platform-administrator protected shells;
- explicit unauthenticated, forbidden, loading, retryable-error, and authorized states;
- authenticated-customer and explicitly allow-listed platform access APIs;
- responsive, keyboard-accessible, reduced-motion-aware account UI.

## Security boundary

NoorPath does not collect a password, provider token, or identity-provider
secret in browser code. Auth0 owns Google authentication and returns an
encrypted server-side web session. The Next.js BFF attaches the API access token
server-side; the browser never receives it. Application authorization continues to use normalized `AccountId`.

Platform shell access uses
`Authorization:PlatformAdministratorAccountIds`; it is separate from operator
membership and publication approval. Production provisioning must enforce MFA
for every privileged account.

## Excluded

Provider tenant provisioning, production credentials, account recovery policy,
role administration, platform mutations, deployment, and production data are
excluded. The slice must not merge or deploy without Product Owner approval.
