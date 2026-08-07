# VS-22 — Auth0 API Token Handoff Repair

## Status
Implementation was merged in PR #82 on 2026-08-04. This specification is registered retrospectively because the implementation used the VS-22 identifier without adding a slice manifest. Current-main regression certification is required by the reconciliation plan before NoorPath treats the integrated protected-route surface as release-ready.

## Outcome
An authenticated NoorPath server session forwards its issued Auth0 API access token to protected customer, operator and platform API routes without exposing the token to the browser.

## Actor
Authenticated NoorPath customers, approved operator members and platform administrators.

## Dependency
VS-21 Auth0 Session Establishment Repair.

## Scope
- resolve the access token from the established Auth0 server session;
- use refresh only as a server-side fallback;
- forward bearer authorization from customer, operator and platform server proxies;
- retain deny-by-default behavior when no valid server session/token exists;
- keep authorization and resource-isolation policies unchanged.

## Security boundary
Tokens remain server-side. They must not be emitted to browser JavaScript, URLs, client payloads or logs. Operator membership, customer ownership and platform allow-list authorization remain owned by their existing APIs.

## Historical implementation evidence
PR #82 — `VS-22: Restore Auth0 API token handoff` — changed only the web Auth0 token resolver and customer/operator/platform proxy routes. It did not introduce a new role model, page redesign or domain behavior.

## Current certification rule
Historical implementation evidence is not a substitute for current-main regression evidence after later protected-navigation changes. The integrated demo identities must be exercised again during the reconciliation certification pass.
