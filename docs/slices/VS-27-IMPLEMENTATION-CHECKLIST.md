# VS-27 Implementation Checklist

## Specify / Design / Contract
- [x] Slice manifest added.
- [x] Outcome, invariants, exclusions and merge rule documented.
- [x] Navigation contract documented.
- [x] Confirm existing departure/operator routes and source-module projection contracts.

## Domain / Persistence
- [x] Add minimal booking-owned operational readiness record/version only if existing structures cannot hold governed notes/actions.
- [x] Append-only readiness audit history with actor, operator, reason, correlation id and before/after state.
- [x] Add migration and registry entry if persistence changes are required.

## API
- [x] Operator-isolated departure manifest query.
- [x] Consolidated traveller readiness projection across authoritative module contracts/projections.
- [x] Search/filter support.
- [x] Governed note/readiness action endpoint with optimistic concurrency.
- [x] Safe 404 for foreign scope and 409 for stale writes.
- [x] No cross-module table mutations.

## Web
- [x] Departure detail entry point to Pilgrim manifest.
- [x] Departure summary counts and explicit blockers.
- [x] Search/filter controls.
- [x] Traveller readiness detail/action surface.
- [x] Loading, empty, permission, error/retry, conflict and success states.
- [x] Responsive/no-horizontal-overflow behavior.
- [x] WCAG 2.2 AA semantics, keyboard navigation and target sizing.

## Verification
- [x] Unit/domain readiness tests.
- [x] API integration tests: isolation, blocker calculation, source-state integrity, stale writes, append-only audit.
- [x] Rendered desktop/mobile Playwright coverage.
- [x] Navigation reachability verification.
- [x] `pnpm slice:validate`.
- [x] migration registry / registered migration validation if applicable.
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
