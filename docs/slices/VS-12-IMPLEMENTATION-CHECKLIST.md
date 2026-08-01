# VS-12 — Documents Implementation Checklist

## Definition of Ready — blocking

- [ ] Product Owner approves the versioned required-document policy source and snapshot behavior.
- [ ] Legal/privacy and Product Owner approve the sensitive-document retention and deletion schedule.
- [ ] Architecture/security approve the private object-storage, encryption, upload and short-lived access mechanism in an ADR.
- [ ] Security approves the malware scanner, file constraints, quarantine lifecycle and fail-safe behavior in an ADR/threat-model update.
- [ ] Do not start runtime implementation until all four decisions above are frozen.

## Development mode

- [ ] VS-01 and VS-11 contracts are stable.
- [ ] Keep the implementation PR Draft while behavior is changing.
- [ ] Apply `certify` only after every success, safety, correction and failure state is complete.

## Ownership and security

- [ ] Documents owns requirement, submission, validation, review and append-oriented history state.
- [ ] Customer account ownership and operator scope/permission are enforced server-side on every operation.
- [ ] Cross-customer, cross-operator and identifier-enumeration tests deny access safely.
- [ ] Private encrypted objects are accessible only through short-lived purpose-bound authorization.
- [ ] File size, declared type, content signature and malware status are verified before reviewer access.
- [ ] Unsafe, timed-out and indeterminate scans remain quarantined.
- [ ] Document content, object keys and signed URLs are absent from logs, analytics and events.
- [ ] Access, review, correction and resubmission audit evidence includes actor, reason/purpose and timestamp.
- [ ] Approved retention/deletion behavior and production-to-lower-environment prohibition are tested.

## Domain and persistence

- [ ] Requirements are explicit, versioned and snapshotted without rewriting existing bookings.
- [ ] Submission and review states are separate and transitions reject direct state skipping.
- [ ] Resubmission preserves previous submissions and review history.
- [ ] Readiness remains per traveller and requirement; booking readiness is derived only from all applicable approvals.
- [ ] Documents does not duplicate Traveller identity or own VisaCase state.
- [ ] Forward-only Documents migration and clean PostgreSQL validation pass.

## Customer and operator experience

- [ ] My Journey links to a per-traveller requirement checklist.
- [ ] Upload provides visible labels, constraints, progress and safe validation feedback.
- [ ] Authorized operator review exposes only safe submissions and requires a reason for correction/rejection.
- [ ] Loading, empty, offline, retry, validation, quarantine, permission, stale/conflict, correction and success states are complete.
- [ ] Approved NoorPath customer/operator visual language and shared navigation patterns are preserved.
- [ ] Keyboard order, visible focus, semantic status, 44px targets, 200% text, reflow and reduced motion pass.

## Certification gates

- [ ] Formatting, static analysis and build pass.
- [ ] Unit, integration, contract, architecture and PostgreSQL migration tests pass.
- [ ] Authorization, upload-abuse, malware/quarantine, privacy, audit and leakage tests pass.
- [ ] Desktop/mobile rendered regression, accessibility and route-linking evidence pass.
- [ ] Telemetry diagnoses failures without Highly Sensitive data.
- [ ] Product Owner accepts the exact certified SHA.

## Final merge gate

- [ ] Full CI and Rendered Slice Review passed on the exact final SHA.
- [ ] Evidence and certification comments reference the exact final SHA.
- [ ] No unresolved review thread, known regression or open Definition-of-Ready decision remains.
- [ ] `po-approved` is present only after Product Owner review.
- [ ] NoorPath Merge Gate is successful.
