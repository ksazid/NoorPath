# VS-22 — Auth0 API Token Handoff Repair Implementation Checklist

## Historical implementation
- [x] Server-side session token resolver implemented.
- [x] Customer API proxy uses the shared token resolver.
- [x] Operator API proxy uses the shared token resolver.
- [x] Platform API proxy uses the shared token resolver.
- [x] Missing token remains deny-by-default.
- [x] No authorization-policy change was introduced.
- [x] Implementation merged through PR #82.

## Reconciliation certification
- [ ] Slice manifest validates with the current registry.
- [ ] Full CI passes on the exact reconciliation head when `certify` is applied.
- [ ] Customer protected-route navigation is verified with the retained demo identity.
- [ ] Operator protected-route navigation is verified with the retained approved-operator identity.
- [ ] Platform protected-route navigation is verified with the retained Platform Administrator identity.
- [ ] Deep-link sign-in and exact return destination are verified where the Auth0 environment is available.
- [ ] No token is exposed in browser-visible payloads, URLs or logs.
- [ ] No unresolved review thread or known regression remains.
- [ ] Product Owner approval is bound to the exact certified reconciliation SHA before merge.
