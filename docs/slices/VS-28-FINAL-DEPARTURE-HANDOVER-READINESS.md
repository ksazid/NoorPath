# VS-28 — Final Departure Handover & Readiness

## Outcome
Approved operator staff can complete the final operational handover for a departure after authoritative readiness has been reviewed, with explicit blockers, governed exceptions, optimistic concurrency and append-only audit evidence.

## Primary flow
Departures → Departure → Pilgrim manifest → Final handover → Review blockers/exceptions → Complete handover.

## Invariants
- Operator isolation is deny-by-default and foreign departure identifiers return safe not-found behavior.
- VS-27 remains the authoritative readiness projection; VS-28 consumes readiness and does not duplicate source-module business rules.
- Final handover is blocked while unresolved payment, document, visa or accommodation blockers remain.
- No silent override is permitted.
- Any explicitly supported exceptional approval requires a non-empty reason, actor identity, timestamp and correlation id.
- Completion uses optimistic concurrency and is idempotent.
- Completed handover becomes operationally immutable; any later correction must be a separately governed audited action.
- Payment, Documents, Visa, Accommodation and booking commercial snapshots are never mutated by this slice.

## Operator experience
The handover workspace must show departure facts, total travellers, ready/blocked counts, unresolved blocker categories, acknowledgement state, final operational note, completion status and audit history. It must provide loading, empty, forbidden, safe-not-found, recoverable error, stale-conflict, success and completed/read-only states.

## API contract
- `GET /api/v1/operator/departures/{departureId}/handover`
- `POST /api/v1/operator/departures/{departureId}/handover/complete`
- Optional correction/exception endpoint only if required by the implemented policy; it must never become a generic blocker bypass.

## Persistence
Use Booking-owned operational records only for handover state and append-only audit evidence. Store operator, actor, expected/resulting version, reason/note, correlation id and UTC timestamps. Add forward-only EF migration and registry evidence if persistence changes.

## Verification
- domain/policy tests for blocker enforcement, completion, idempotency and stale versions;
- API integration tests for isolation, source-state integrity, blocked completion, successful completion and concurrency;
- rendered desktop/mobile Playwright coverage;
- WCAG 2.2 AA semantics, keyboard operation, minimum targets and no horizontal overflow;
- verified navigation from existing departure operations;
- exact-head CI, Slice Governance, Rendered Slice Review and Navigation Reachability Review.

## Merge rule
Keep the PR Draft through technical certification. `certify` must be active and every required exact-head workflow must actually run and pass. A skipped gate is not a pass. Product Owner approval applies only to the unchanged certified SHA. Any head change invalidates that approval. After PO approval/ready-state changes, all retriggered required gates must pass before merge. Production deployment is separately authorized and is not part of this slice.
