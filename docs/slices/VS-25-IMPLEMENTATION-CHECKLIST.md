# VS-25 Implementation Checklist

## Specify and contract

- [x] Register VS-25 in `delivery/slices`.
- [x] Define module ownership, exclusions and lifecycle policy.
- [x] Define preview-then-confirm commercial amendment contract.
- [x] Add Booking amendment domain policy and contracts.
- [x] Add Pricing-owned authoritative amendment pricing contract.

## Booking persistence

- [x] Add booking optimistic concurrency version.
- [x] Add append-only booking amendment record/audit model.
- [x] Add migration, model snapshot and migration registry evidence.
- [x] Ensure before/after commercial and traveller evidence is immutable JSON/snapshot data.

## API/application

- [x] Add operator-owned amendment preview endpoint.
- [x] Add operator-owned amendment confirmation endpoint.
- [x] Reject foreign-operator booking ids without tenancy leakage.
- [x] Reject lifecycle-ineligible bookings before write.
- [x] Revalidate preview fingerprint and booking version on confirmation.
- [x] Preserve Payments/Documents/Visa/Cancellation write ownership.
- [x] Add correlation-safe telemetry/audit evidence without logging sensitive traveller payloads.

## Web

- [x] Add `Amend booking` action from VS-24 booking detail.
- [x] Add `/operator/bookings/{bookingId}/amend` route in existing operator shell.
- [x] Edit booking traveller snapshot and occupancy with accessible controls.
- [x] Show server-authoritative current/proposed financial snapshots and delta.
- [x] Require explicit confirmation for a price-changing amendment.
- [x] Cover loading, validation, not-found/forbidden, ineligible, stale, error/retry and success states.
- [x] Preserve NoorPath operator visual identity and mobile reflow.

## Verification

- [x] Domain tests for allowed/disallowed states and occupancy/traveller invariants.
- [x] Integration tests for operator isolation and safe not-found.
- [x] Integration tests for stale/replay conflict and atomic no-partial-write behavior.
- [x] Financial-integrity tests proving payment history is untouched.
- [x] Audit tests proving confirmed amendments append rather than overwrite history.
- [ ] Desktop Chromium rendered flow passes.
- [ ] Mobile WebKit/390 flow passes.
- [ ] Accessibility, target-size and horizontal-overflow checks pass.
- [x] Navigation matrix has no `PENDING` or `FAILED` outcome at certification input.
- [ ] Full exact-head `certify` workflows run and pass.
- [ ] Product Owner approves unchanged certified SHA.

## Release

- [ ] Merge only after all required exact-head gates are green.
- [x] Do not deploy as part of VS-25 unless separately authorized.
