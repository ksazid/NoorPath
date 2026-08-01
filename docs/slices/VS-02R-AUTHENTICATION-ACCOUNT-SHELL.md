# VS-02R — Authentication and protected account shell

Status: Implementation; Product Owner acceptance pending

## Outcome

Customers can start phone OTP or Google authentication through a configured
hosted identity service. Authenticated customers, operator users, and NoorPath
platform administrators receive distinct protected shells. API authorization
remains provider-neutral and denies access by default.

## Traceability

- `INV-ID-003`–`INV-ID-006`
- `INV-OP-001`–`INV-OP-003`
- ADR-002 — Identity, Authentication and Authorization Architecture

No stable approved PRD requirement ID was found; none is invented here.

## Included

- a provider-neutral hosted sign-in hand-off for phone OTP and Google;
- same-origin return URL validation;
- customer, operator, and platform-administrator protected shells;
- explicit unauthenticated, forbidden, loading, retryable-error, and authorized states;
- authenticated-customer and explicitly allow-listed platform access APIs;
- responsive, keyboard-accessible, reduced-motion-aware account UI.

## Security boundary

NoorPath does not collect a phone number, OTP, password, provider token, or
identity-provider secret in the web application. `NOORPATH_AUTH_SIGN_IN_URL`
selects the hosted identity adapter. The adapter owns authentication and must
return a secure browser session accepted by the configured API authentication
boundary. Application authorization continues to use normalized `AccountId`.

Platform shell access uses
`Authorization:PlatformAdministratorAccountIds`; it is separate from operator
membership and publication approval. Production provisioning must enforce MFA
for every privileged account.

## Excluded

Provider tenant provisioning, production credentials, account recovery policy,
role administration, platform mutations, deployment, and production data are
excluded. The slice must not merge or deploy without Product Owner approval.
