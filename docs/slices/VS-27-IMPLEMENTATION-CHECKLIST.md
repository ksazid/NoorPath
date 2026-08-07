# VS-27 Implementation Checklist

## Specify / Design / Contract
- [x] Slice manifest added.
- [x] Outcome, invariants, exclusions and merge rule documented.
- [x] Navigation contract documented.
- [ ] Confirm existing departure/operator routes and source-module projection contracts.

## Domain / Persistence
- [ ] Add minimal booking-owned operational readiness record/version only if existing structures cannot hold governed notes/actions.
- [ ] Append-only readiness audit history with actor, operator, reason, correlation id and before/after state.
- [ ] Add migration and registry entry if persistence changes are required.

## API
- [ ] Operator-isolated departure manifest query.
- [ ] Consolidated traveller readiness projection across authoritative module contracts/projections.
- [ ] Search/filter support.
- [ ] Governed note/readiness action endpoint with optimistic concurrency.
- [ ] Safe 404 for foreign scope and 409 for stale writes.
- [ ] No cross-module table mutations.

## Web
- [ ] Departure detail entry point to Pilgrim manifest.
- [ ] Departure summary counts and explicit blockers.
- [ ] Search/filter controls.
- [ ] Traveller readiness detail/action surface.
- [ ] Loading, empty, permission, error/retry, conflict and success states.
- [ ] Responsive/no-horizontal-overflow behavior.
- [ ] WCAG 2.2 AA semantics, keyboard navigation and target sizing.

## Verification
- [ ] Unit/domain readiness tests.
- [ ] API integration tests: isolation, blocker calculation, source-state integrity, stale writes, append-only audit.
- [ ] Rendered desktop/mobile Playwright coverage.
- [ ] Navigation reachability verification.
- [ ] `pnpm slice:validate`.
- [ ] migration registry / registered migration validation if applicable.
- [ ] CI green on exact head.
- [ ] Rendered Slice Review green on exact head.
- [ ] Navigation Reachability Review green on exact head.
- [ ] Slice Governance green on exact head.
- [ ] Product Owner approval for exact certified SHA.

## Close
- [ ] Mark ready for review only after exact-head technical certification.
- [ ] Re-run all required checks after PO approval/ready-state triggers.
- [ ] Merge only when all current required checks pass.
- [ ] Do not deploy without separate authorization.
