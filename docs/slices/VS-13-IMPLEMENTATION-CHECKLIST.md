# VS-13 Implementation Checklist

## Domain and persistence

- [ ] Add Visa domain and infrastructure projects only when runtime work begins.
- [ ] Model explicit allowed transitions and required reasons.
- [ ] Add VisaDbContext, visa schema, generated migration and model snapshot.
- [ ] Add optimistic concurrency and append-only transition history.
- [ ] Register migration ownership and clean-database validation.

## API and authorization

- [ ] Customer account-scoped visa projection endpoint.
- [ ] Operator-scoped actionable queue and case endpoint.
- [ ] Permission-gated transition endpoint with stale-version rejection.
- [ ] Safe not-found and forbidden behavior verified.
- [ ] No document content or internal notes in customer responses.

## Experience

- [ ] Link per-traveller visa status from My Journey.
- [ ] Add responsive customer status/action view.
- [ ] Add operator queue, case history and guarded transition form.
- [ ] Complete loading, empty, delayed, denied, stale, rejected, action-required, approved and retry states.
- [ ] Preserve approved NoorPath visual language and shared deferred-load pattern.

## Verification

- [ ] Domain transition unit tests.
- [ ] PostgreSQL persistence, concurrency, tenant and account isolation tests.
- [ ] API authorization and projection tests.
- [ ] Migration registry and pending-model validation.
- [ ] Chromium/WebKit rendered accessibility and responsive review.
- [ ] Safe telemetry assertions.
- [ ] Full certification once after feature completion.
- [ ] Exact-head Product Owner approval before merge.
- [ ] No deployment without separate Product Owner approval.
