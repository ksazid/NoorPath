# VS-22 — Navigation Verification

Current-main reconciliation status: **PENDING CERTIFICATION**.

| Source | Identity | Destination / outcome | Status |
| --- | --- | --- | --- |
| Protected customer route | Demo customer | Auth0 session -> customer API -> owned route | PENDING |
| Protected operator route | Approved demo operator | Auth0 session -> operator API -> operator workspace | PENDING |
| Protected platform route | Demo Platform Administrator | Auth0 session -> platform API -> admin workspace | PENDING |
| Protected deep link | Signed-out demo identity | Sign in -> exact safe relative return destination | PENDING |
| Missing/expired session | No valid session | Sign-in boundary; no bearer forwarding | PENDING |
| Operator route with platform-only identity | Platform Administrator without operator membership | Role-preserving guidance to admin; no operator authorization bypass | PENDING |

A row may only move to `VERIFIED` when the exact current-main/reconciliation SHA has executable evidence. A skipped workflow is not verification.
