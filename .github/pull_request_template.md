## Slice and requirements

- Slice manifest: <!-- delivery/slices/VS-XX.json, or Not applicable -->
- Requirement IDs: <!-- approved IDs, or Not applicable — engineering foundation -->
- Specification: <!-- docs/slices/... -->
- Implementation checklist: <!-- docs/slices/... -->

## Outcome

<!-- Describe the smallest complete user/business outcome. -->

## Explicit exclusions

<!-- State what this PR deliberately does not implement. -->

## Domain and data ownership

<!-- Identify module owners, state transitions, migrations and cross-module contracts. -->

## Customer journey and route linking

<!-- List incoming/outgoing page transitions and confirm no orphaned route. -->

## Validation completed before certification

- [ ] Complete success, loading, empty, error, conflict and recovery states are implemented.
- [ ] Targeted/local checks pass.
- [ ] Migration, concurrency, idempotency and rollback risks are covered where applicable.
- [ ] Security, privacy, accessibility, telemetry and failure paths are implemented.
- [ ] Documentation, manifest and checklist match the current implementation.

## Certification workflow

- Keep this PR **Draft** during active development.
- Apply `certify` only when the feature is complete and ready for full exact-head certification.
- Remove `certify` before further development after a failed or superseded certification.
- Apply `po-approved` only after Product Owner review of the exact certified SHA.

## Merge rule

**DO NOT MERGE** until full CI, migrations, rendered/accessibility regression evidence, journey linking, unresolved-thread review and Product Owner acceptance all pass on the exact final SHA and `NoorPath / Merge Gate` is successful.

## Manual actions or external blockers

<!-- Include provider credentials, repository settings, preview limits or approvals. External infrastructure failures must not be presented as product success. -->
