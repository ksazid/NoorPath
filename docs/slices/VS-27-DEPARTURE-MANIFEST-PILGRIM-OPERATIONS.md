# VS-27 — Departure Manifest & Pilgrim Operations

## Outcome
Approved operator staff can open a departure-level pilgrim manifest, review each traveller's operational readiness and blockers across booking, payment, documents, visa and accommodation, record governed operational notes/actions, and prepare a reliable departure handoff without mutating authoritative source-module state.

## Scope
- departure-level manifest for confirmed bookings;
- traveller readiness projection;
- explicit blockers for payment, documents, visa and accommodation;
- search/filter by traveller, booking reference and readiness;
- departure summary counts;
- operator note/readiness acknowledgement with optimistic concurrency;
- append-only readiness audit history;
- operator isolation and safe not-found behavior;
- responsive, accessible operator UI;
- integration, rendered and navigation certification.

## Invariants
1. Source modules remain authoritative. VS-27 reads their state and must not mutate their tables.
2. Hard blockers cannot be manually overridden by an operator readiness action.
3. Foreign operator departure or booking identifiers return safe not-found responses.
4. Governed writes require current version, actor, operator, reason/note and correlation id.
5. Stale writes return conflict with no partial mutation.
6. Audit records are append-only.
7. No price, payment, document, visa, accommodation or cancellation state changes occur from manifest operations.

## Readiness model
A traveller can be operationally ready only when all required indicators are complete:
- booking is confirmed;
- required payment readiness is satisfied by the existing authoritative payment projection;
- required documents are approved/ready;
- visa is approved/ready;
- accommodation assignments required for the departure are complete.

The manifest must show each blocking category explicitly. Colour may reinforce state but never be the sole carrier of meaning.

## UX flow
`Departures → Open departure → Pilgrim manifest → Review blockers → Open traveller → Record note/acknowledgement → Re-evaluate readiness`

The screen reuses the established operator shell and design tokens. It must include loading, empty, permission, error/retry, stale/conflict and success feedback. On mobile, traveller information may stack, but operational blockers and the primary action must remain directly discoverable without horizontal scrolling.

## Explicit exclusions
- payment capture, adjustment or refund execution;
- document review/state mutation;
- visa transitions;
- room assignment changes;
- booking amendment/cancellation;
- flight ticketing or supplier inventory;
- customer manifest access;
- production deployment.

## Merge rule
Keep the PR Draft until implementation is complete and `certify` is applied. Every required exact-head gate must execute and pass. Skipped/unavailable/cancelled/failed gates are not passes. Product Owner approval is valid only for the unchanged certified SHA. Production deployment remains separately authorized.
