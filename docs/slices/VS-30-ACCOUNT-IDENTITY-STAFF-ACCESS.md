# VS-30 — Account Identity and Staff Access

Status: Implementation

## Outcome

Authenticated NoorPath accounts expose the signed-in member identity and a consistent account menu with reachable account, settings, help and logout actions. Public customer pages expose a temporary staff sign-in entry until a dedicated staff portal hostname is introduced later.

## Traceability

- VS-19 Customer Shell and Navigation Adoption
- VS-21 Auth0 Session Establishment
- VS-29 Customer Package-to-Booking UX
- ADR-002 Identity, Authentication and Authorization
- UX baseline: approved NoorPath Landing and Package visual language

No new product or identity policy is invented by this slice.

## Included

- safe display name on customer/platform/operator access projections;
- shared account identity menu for customer, operator and platform protected chrome;
- account, settings, help and logout destinations using existing routes;
- Auth0 logout entry that returns safely to NoorPath;
- temporary public Operator / Admin login link using the existing sign-in flow and operator return URL;
- desktop/mobile rendered and navigation coverage.

## Excluded

- staff portal subdomain migration;
- new authentication providers or phone OTP activation;
- new persisted account-preference model;
- operator shell redesign beyond account identity controls;
- package/departure/commercial changes;
- production deployment.

## Identity and privacy rules

1. Display name is presentation-only and never becomes an authorization input.
2. Standard identity-provider name claims may be projected after trimming and an 80-character cap.
3. Missing display name falls back to `NoorPath member`; opaque account IDs are not used as customer-facing names.
4. Operator business identity remains separate from the signed-in member identity.
5. Provider tokens/secrets are never exposed to browser code.
6. Existing account, operator-membership and Platform Administrator authorization checks remain unchanged.

## UX rules

The approved NoorPath visual language remains authoritative. The account menu uses the existing token system, compact rounded controls, clear focus, 44px touch targets and a small responsive identity treatment. This is a navigation/account affordance, not a redesign.

`Settings` currently resolves to the existing account workspace because no persisted preferences domain is approved. This avoids a dead route while preserving the option label requested by the Product Owner.

The temporary staff entry is intentionally subtle in the public footer and routes through the existing secure sign-in flow with `/operator` as the return destination. A future staff portal hostname remains a separate slice.

## Failure states

- unauthenticated account access shows Sign in rather than fabricated identity;
- access failure omits account identity without leaking internal data;
- operator forbidden behavior remains fail-closed;
- Auth0-unconfigured logout safely returns home rather than exposing an error or provider detail.

## Verification

- account-access API responses remain authorization protected;
- customer menu exposes the member name and reachable account/settings/help/logout links;
- staff sign-in entry points to the existing secure sign-in flow;
- operator menu distinguishes member name from operator business name;
- 390px mobile customer header has no horizontal overflow;
- exact-head CI, rendered review, navigation verification and slice governance must pass before merge.
