# VS-28 Implementation Checklist

## Specify / Design / Contract
- [x] Slice manifest added.
- [x] Outcome, invariants, exclusions and merge rule documented.
- [x] Navigation contract documented.
- [x] Existing departure detail/manifest contracts and reusable operator patterns confirmed.

## Domain / Persistence
- [x] Add Booking-owned handover state/version record.
- [x] Add append-only handover audit history.
- [x] Enforce blocked-completion, idempotency and stale-version rules.
- [x] Add forward-only EF migration and model snapshot evidence.

## API
- [x] Operator-isolated handover query.
- [x] Complete-handover command with optimistic concurrency.
- [x] No generic blocker bypass; exceptional correction remains a separately governed future workflow.
- [x] Safe 404 for foreign scope and 409 for blocked/stale writes.
- [x] Verify failed handover does not mutate booking/source-module state.

## Web
- [x] Departure/manifest entry point to Final handover.
- [x] Summary, blockers and completion state.
- [x] Final note/reason and confirmation interaction.
- [x] Loading, forbidden, safe-not-found, retry, stale/conflict and completed states.
- [x] Responsive, keyboard accessible, minimum targets and no horizontal overflow coverage.

## Verification
- [x] Domain/policy tests for blocker enforcement, note validation, idempotency and stale versions.
- [x] API integration tests for operator isolation, blocker enforcement, source-state integrity and idempotent replay.
- [x] Rendered desktop/mobile Playwright coverage.
- [x] Navigation reachability implementation matrix.
- [ ] `pnpm slice:validate` on final exact head.
- [ ] migration registry validation on final exact head.
- [ ] CI green on exact head.
- [ ] Rendered Slice Review green on exact head.
- [ ] Navigation Reachability Review green on exact head.
- [ ] Slice Governance green on exact head.
- [ ] Product Owner approval for exact certified SHA.

## Close
- [ ] Mark ready only after exact-head technical certification.
- [ ] Re-run required checks after PO approval/ready-state triggers.
- [ ] Merge only when all current required checks pass.
- [x] Do not deploy without separate authorization.
