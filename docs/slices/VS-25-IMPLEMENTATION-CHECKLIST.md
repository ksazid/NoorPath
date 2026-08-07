# VS-25 Implementation Checklist

## Specify and contract

- [x] Register VS-25 in `delivery/slices`.
- [x] Define module ownership, exclusions and lifecycle policy.
- [x] Define preview-then-confirm commercial amendment contract.
- [ ] Add Booking amendment domain policy and contracts.
- [ ] Add Pricing-owned authoritative amendment pricing contract.

## Booking persistence

- [ ] Add booking optimistic concurrency version.
- [ ] Add append-only booking amendment record/audit model.
- [ ] Add migration, model snapshot and migration registry evidence.
- [ ] Ensure before/after commercial and traveller evidence is immutable JSON/snapshot data.

## API/application

- [ ] Add operator-owned amendment preview endpoint.
- [ ] Add operator-owned amendment confirmation endpoint.
- [ ] Reject foreign-operator booking ids without tenancy leakage.
- [ ] Reject lifecycle-ineligible bookings before write.
- [ ] Revalidate preview fingerprint and booking version on confirmation.
- [ ] Preserve Payments/Documents/Visa/Cancellation write ownership.
- [ ] Add correlation-safe telemetry without sensitive traveller payloads.

## Web

- [ ] Add `Amend booking` action from VS-24 booking detail.
- [ ] Add `/operator/bookings/{bookingId}/amend` route in existing operator shell.
- [ ] Edit booking traveller snapshot and occupancy with accessible controls.
- [ ] Show server-authoritative current/proposed financial snapshots and delta.
- [ ] Require explicit confirmation for a price-changing amendment.
- [ ] Cover loading, validation, not-found/forbidden, ineligible, stale, error/retry and success states.
- [ ] Preserve NoorPath operator visual identity and mobile reflow.

## Verification

- [ ] Domain tests for allowed/disallowed states and occupancy/traveller invariants.
- [ ] Integration tests for operator isolation and safe not-found.
- [ ] Integration tests for stale-version conflict and atomic no-partial-write behavior.
- [ ] Financial-integrity tests proving payment history is untouched.
- [ ] Audit tests proving confirmed amendments append rather than overwrite history.
- [ ] Desktop Chromium rendered flow passes.
- [ ] Mobile WebKit/390 flow passes.
- [ ] Accessibility, target-size and horizontal-overflow checks pass.
- [ ] Navigation matrix has no `PENDING` or `FAILED` outcome at certification.
- [ ] Full exact-head `certify` workflows run and pass.
- [ ] Product Owner approves unchanged certified SHA.

## Release

- [ ] Merge only after all required exact-head gates are green.
- [ ] Do not deploy as part of VS-25 unless separately authorized.
