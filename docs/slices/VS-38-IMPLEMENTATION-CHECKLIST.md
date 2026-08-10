# VS-38 Implementation Checklist

- [x] Read current `AGENTS.md`, product rules, MASTER and required implementation/UI skills.
- [x] Confirm VS-37 handoff and avoid inventing provider configuration.
- [x] Reuse existing Catalogue and operator workspace boundaries.
- [x] Add structured flight-leg domain validation with explicit pending/confirmed semantics.
- [x] Add versioned package travel-fact persistence and forward-only migration.
- [x] Add tenant-scoped GET/PUT travel-fact API with draft locking and stale-write rejection.
- [x] Add operator travel-fact route reachable from existing departure authoring.
- [x] Add loading, empty, pending, confirmed, saved, conflict, error and locked UI states.
- [x] Preserve existing Makkah/Madinah hotel authoring as the only accommodation authoring path.
- [x] Add domain and rendered accessibility coverage.
- [x] Register slice scope, exclusions and navigation evidence.
- [ ] Exact-head CI.
- [ ] Exact-head Rendered Slice Review.
- [ ] Exact-head Navigation Reachability.
- [ ] Product Owner screenshot acceptance.
- [ ] Merge Gate.
