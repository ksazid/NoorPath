# VS-15 — Family Booking & Mahram Linking Implementation Checklist

## Slice registration

- [x] Slice identifier, outcome, dependencies and exclusions are registered.
- [x] Product Owner approved the documented slice boundary.
- [ ] Runtime PR is created from the merged documentation SHA.

## Domain and policy

- [ ] Family party aggregate and lifecycle are implemented.
- [ ] Membership uniqueness and archive rules are enforced.
- [ ] Mahram relationship type vocabulary is configurable.
- [ ] Self, duplicate, cross-account, archived-traveller and non-member links are rejected.
- [ ] Validation policy is versioned and returns stable issue codes.
- [ ] Relationship or membership changes invalidate prior validation.
- [ ] Unit tests cover valid and invalid party combinations.

## Persistence

- [ ] Family Booking owns a dedicated PostgreSQL schema and DbContext.
- [ ] Deterministic forward-only migration and model snapshot are added.
- [ ] Unique constraints protect active membership and active relationship links.
- [ ] Optimistic concurrency tokens protect parties and relationships.
- [ ] Migration registry and clean-database validation are updated.

## API and authorization

- [ ] Account-scoped list, detail, create and update endpoints are implemented.
- [ ] Member add/remove endpoints are implemented.
- [ ] Mahram link create/revoke endpoints are implemented.
- [ ] Party validation endpoint is implemented.
- [ ] Every endpoint enforces account ownership server-side.
- [ ] Foreign resources return safe not-found responses.
- [ ] Stale versions return recoverable conflict responses.
- [ ] Privacy-safe audit and telemetry are emitted.

## Quote, booking and journey integration

- [ ] Quote accepts a validated family party and rechecks ownership/version.
- [ ] Booking copies an immutable traveller and relationship snapshot.
- [ ] Booked snapshots retain the policy version used at checkout.
- [ ] Later family edits cannot mutate a booking snapshot.
- [ ] My Journey exposes a customer-safe family and Mahram summary.
- [ ] Documents and Visa consume traveller identifiers without cross-module writes.

## Customer experience

- [ ] Family party list and empty state are complete.
- [ ] Traveller selection and party composition flow are complete.
- [ ] Mahram linking flow includes clear direction and relationship labels.
- [ ] Validation issues provide actionable customer-safe guidance.
- [ ] Religious/legal disclaimer is visible at relevant decision points.
- [ ] Loading, stale, denied, not-found and service-error states are complete.
- [ ] UI extends the approved NoorPath visual system and shared footer.

## Automated validation

- [ ] Formatting and static analysis pass.
- [ ] Domain and policy unit tests pass.
- [ ] PostgreSQL integration tests pass.
- [ ] Account-isolation and safe-not-found tests pass.
- [ ] Concurrency and duplicate-link race tests pass.
- [ ] Architecture ownership tests pass.
- [ ] Migration/model parity passes.
- [ ] Rendered Chromium and WebKit journeys pass.
- [ ] Mobile, keyboard, 200% text, reduced-motion and axe checks pass.
- [ ] Exact-head Slice Governance passes.
- [ ] Exact-head CI passes.
- [ ] Exact-head Rendered Slice Review passes.
- [ ] Product Owner approval is bound to the unchanged certified SHA.

## Merge and deployment

- [ ] No unresolved review thread remains.
- [ ] Runtime PR is marked ready only after certification.
- [ ] Certified runtime SHA is merged to `main`.
- [ ] No production deployment occurs unless separately requested and approved.
