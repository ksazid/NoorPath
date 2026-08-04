# VS-20 — Implementation Checklist

## Governance

- [x] Exact merged VS-19 baseline identified.
- [x] Neon rollback branch created before identity/data changes.
- [x] Slice specification and exclusions recorded.
- [ ] Draft PR opened.
- [ ] Exact-head certification completed.
- [ ] Product Owner approves exact unchanged SHA.

## Identity provisioning

- [x] Real Auth0 Google identity identified without committing secrets.
- [x] Normalized NoorPath AccountId derived through the existing identity contract.
- [x] Active membership added to approved demo operator.
- [x] Admin, document-review, visa-processing and operational-support permissions granted.
- [x] Platform administrator and publication-approver allow-lists updated in managed configuration.
- [x] `DEMO-LKO-001` assigned to the real account.
- [x] Delhi and Mumbai demo journeys retained as foreign-account fixtures.

## Persistence repair

- [ ] Documents module added to `delivery/modules.json`.
- [ ] Existing undiscoverable migration replaced through `dotnet ef migrations add`.
- [ ] Generated migration designer committed.
- [ ] Generated model snapshot committed.
- [ ] Migration count and designer count match.
- [ ] Pending-model-change validation passes.
- [ ] Fresh Documents test database migration passes.
- [ ] Neon documents schema and tables exist after deployment.

## Runtime verification

- [x] Render API deployed from merged VS-19 main.
- [x] Render API readiness and migration startup confirmed.
- [x] Auth0 sign-in endpoint on `noorpath-release` redirects to Google Universal Login.
- [ ] Customer account and owned My Journey access verified interactively.
- [ ] Customer documents and visa access verified.
- [ ] Foreign booking safe-not-found verified.
- [ ] Operator root and account access verified.
- [ ] Operator packages and departures verified.
- [ ] Operator documents verified.
- [ ] Operator visa verified.
- [ ] Operator cancellations verified.
- [ ] Operator support verified.
- [ ] Platform administrator and publication routes verified.
- [ ] Logout/session-expiry behavior verified.

## Quality

- [ ] Slice Governance passes.
- [ ] Formatting passes.
- [ ] Static analysis passes.
- [ ] Unit and integration tests pass.
- [ ] Migration registry/model parity passes.
- [ ] Security and authorization tests pass.
- [ ] Navigation matrix contains no untruthful PASS result.
- [ ] No UI files changed.
- [ ] No secret or real document data committed.

## Deployment boundary

- [ ] Hotfix merged only after Product Owner approval.
- [ ] Release/test API deployed from exact merged commit.
- [ ] Auth0-enabled release frontend tested.
- [ ] Rollback branch retained until test completion.
- [ ] No real customer production launch performed.
