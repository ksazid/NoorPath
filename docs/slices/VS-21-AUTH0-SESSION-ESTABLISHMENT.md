# VS-21 — Auth0 Session Establishment Repair

## Objective

Remove the post-login sign-in loop and prove that Auth0 establishes a persistent NoorPath session before customer, operator and platform authorization runs.

## Root cause

The Auth0 SDK was invoked both from the global Next.js middleware and from an App Router catch-all route. The route handler could receive `NextResponse.next()` from `auth0.middleware`, which is invalid in an App Route Handler and prevented callback/session completion.

## Scope

- remove the conflicting Auth0 App Route;
- use the Next.js 16 `proxy.ts` authentication boundary;
- preserve safe relative return destinations;
- test login, callback, session, logout and protected-route behavior;
- audit all customer, operator and platform protected pages with the retained smoke-test identity.

## Page impact

No page layout, copy, style or domain behavior changes. Every authenticated page is functionally affected through the shared session boundary.

## Security

Authorization remains deny-by-default. Operator membership, permissions, platform allow-lists and foreign-account isolation remain unchanged. Secrets and session cookies are never committed.

## Deployment boundary

Release/test environment only. Merge and deployment require exact-head certification and Product Owner approval.
