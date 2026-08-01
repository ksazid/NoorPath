# VS-09 — Booking & Payment Implementation Checklist

## Development mode
- [ ] Keep the PR Draft while implementation is changing.
- [ ] Do not apply `certify` until the complete feature and all required states are implemented.
- [ ] Remove `certify` before further development after a failed or superseded certification.

## Contract and ownership
- [ ] Booking and Payments boundaries are explicit and architecture-tested.
- [ ] Booking copies the authoritative quote snapshot without recomputing money.
- [ ] Inventory hold validity and consumption remain Inventory-owned.
- [ ] Provider webhook and return contracts are versioned and documented.

## Product completeness
- [ ] Valid quote and active hold create exactly one booking and reference.
- [ ] Immutable booking snapshot includes departure, travellers, occupancy and all commercial facts.
- [ ] Booking create replay returns the original result.
- [ ] Conflicting idempotency reuse returns a deterministic conflict.
- [ ] Expired, released, foreign and consumed holds are rejected.
- [ ] Payment initiation is provider-hosted/tokenised and contains no raw card handling.
- [ ] Authenticated webhooks are replay-safe and terminal payment states cannot regress.
- [ ] Pending, succeeded, failed, cancelled, requires-action and retry states are complete.
- [ ] Plan, booking review, payment and return routes are interlinked.

## Persistence and failure safety
- [ ] Booking and Payments migrations are deterministic with constraints and indexes.
- [ ] Clean database apply and model snapshot parity pass.
- [ ] Transaction failure cannot leave a booking without its required initial payment evidence.
- [ ] Duplicate request and webhook storms produce no duplicate financial effects.
- [ ] Out-of-order provider events are reconciled safely.

## Certification gates
- [ ] Formatting passes.
- [ ] Type checking, linting and static analysis pass.
- [ ] Unit tests pass.
- [ ] Integration, PostgreSQL and provider-contract tests pass.
- [ ] Architecture and module-ownership tests pass.
- [ ] Migration validation passes.
- [ ] Authentication, authorization, privacy and secret scanning pass.
- [ ] Keyboard, focus, semantics, target-size and axe checks pass.
- [ ] Desktop, 390px, 360px, 200% text and reduced-motion evidence passes.
- [ ] Route graph and customer journey linking pass.
- [ ] Logs, traces, metrics and reconciliation evidence are verified.
- [ ] Product Owner accepts the exact certified SHA.

## Final merge gate
- [ ] Full CI passed on the exact final SHA.
- [ ] Rendered Slice Review passed on the exact final SHA.
- [ ] Evidence artifact and certification comment reference the exact final SHA.
- [ ] No unresolved review thread or known regression remains.
- [ ] `po-approved` is present only after Product Owner review.
- [ ] NoorPath Merge Gate is successful.
