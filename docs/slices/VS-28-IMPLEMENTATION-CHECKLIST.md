# VS-28 Implementation Checklist

## Specify / Design / Contract
- [x] Slice manifest added.
- [x] Outcome, invariants, exclusions and merge rule documented.
- [x] Navigation contract documented.
- [ ] Confirm existing departure detail/manifest contracts and reusable operator patterns.

## Domain / Persistence
- [ ] Add Booking-owned handover state/version record.
- [ ] Add append-only handover audit history.
- [ ] Enforce blocked-completion, idempotency and stale-version rules.
- [ ] Add forward-only EF migration and migration registry evidence.

## API
- [ ] Operator-isolated handover query.
- [ ] Complete-handover command with optimistic concurrency.
- [ ] Explicit exception policy only if required; never generic blocker bypass.
- [ ] Safe 404 for foreign scope and 409 for stale writes.
- [ ] Verify no source-module mutations.

## Web
- [ ] Departure/manifest entry point to Final handover.
- [ ] Summary, blockers and completion state.
- [ ] Final note/reason and confirmation interaction.
- [ ] Loading, empty, forbidden, safe-not-found, retry, stale and completed states.
- [ ] Responsive, keyboard accessible, minimum targets and no horizontal overflow.

## Verification
- [ ] Domain/policy tests.
- [ ] API integration tests for isolation, blocker enforcement, source-state integrity, idempotency and stale writes.
- [ ] Rendered desktop/mobile Playwright coverage.
- [ ] Navigation reachability verification.
- [ ] `pnpm slice:validate`.
- [ ] migration registry validation.
- [ ] CI green on exact head.
- [ ] Rendered Slice Review green on exact head.
- [ ] Navigation Reachability Review green on exact head.
- [ ] Slice Governance green on exact head.
- [ ] Product Owner approval for exact certified SHA.

## Close
- [ ] Mark ready only after exact-head technical certification.
- [ ] Re-run required checks after PO approval/ready-state triggers.
- [ ] Merge only when all current required checks pass.
- [ ] Do not deploy without separate authorization.
