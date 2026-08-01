# VS-08 Inventory Hold — Implementation Checklist

Status: Specification complete / implementation not started

## Baseline and governance

- [x] Verify repository default branch is `main`; no `master` branch exists.
- [x] Verify exact latest `main` commit before starting VS-08: `786f2d2346aab74b904546aa358d520125034a12`.
- [x] Create `agent/vs08-inventory-hold` from that exact commit.
- [x] Read VS-03, VS-07, Domain Map, Core Domain Model and State Machines.
- [x] Read `.agents/skills/ui-ux-pro-max/SKILL.md` and `design-system/MASTER.md`.
- [x] Define product/domain/API/UX scope before implementation.
- [ ] Keep the PR Draft until every required gate below is complete.
- [ ] Confirm Product Owner acceptance of the explicit VS-08 policy decisions before implementation merge.

## Frozen slice decisions

- [x] Inventory owns hold state, expiry, release and oversell protection.
- [x] Existing occupancy capacity is defined as saleable one-room allocations.
- [x] One VS-07 quote acquires quantity `1` from its matching occupancy pool.
- [x] MVP hold lifetime is 15 minutes, capped by quote expiry.
- [x] `now >= expiresAtUtc` means expired.
- [x] Hold creation requires an Account-scoped idempotency key.
- [x] One effective Active hold is allowed per Account + departure + occupancy.
- [x] Available quantity remains derived; no mutable `available` field is introduced.
- [x] Booking, payment and reservation conversion remain excluded.

## Contract and architecture

- [ ] Define a minimum `QuoteForInventoryHold` Pricing/application contract.
- [ ] Define a minimum current-saleability contract for Catalogue/Operators composition.
- [ ] Define Inventory hold command/query/result contracts.
- [ ] Ensure Inventory does not query or mutate Pricing, Catalogue, Operators, Traveller or Booking persistence.
- [ ] Ensure API composition does not become a new source of truth.
- [ ] Add architecture tests for prohibited module dependencies.
- [ ] Define safe Problem Details codes and OpenAPI contract.
- [ ] Confirm no quote money, payment schedule or Traveller PII crosses into Inventory persistence/logging.

## Inventory domain

- [ ] Add `InventoryHoldState` with VS-08 transitions `Active -> Released|Expired`.
- [ ] Add hold-lifetime policy through Inventory-owned configuration and injected `TimeProvider`.
- [ ] Implement exact expiry calculation capped by quote expiry.
- [ ] Implement effective-active predicate using state and authoritative time.
- [ ] Implement idempotent release and expiry behaviour.
- [ ] Prevent duplicate active holds for the same quote.
- [ ] Prevent duplicate active holds for the same Account/departure/occupancy.
- [ ] Prove VS-08 cannot create Reservation state.
- [ ] Add domain tests for expiry boundaries and terminal transitions.

## Persistence and migration

- [ ] Add Inventory-owned hold persistence under schema `inventory`.
- [ ] Add module-local hold-to-pool foreign key.
- [ ] Add unique Account + idempotency-key constraint.
- [ ] Add pool/state/expiry index for commitment calculation.
- [ ] Add Account/departure/occupancy/state index.
- [ ] Add quote/state index.
- [ ] Add quantity, timestamp and state constraints.
- [ ] Add lifecycle/audit/outbox evidence using existing foundation where applicable.
- [ ] Add deterministic forward-only EF Core migration.
- [ ] Update `InventoryDbContextModelSnapshot`.
- [ ] Keep external Account/Quote/Departure/Operator identifiers as references without cross-schema foreign keys.
- [ ] Update and pass `scripts/validate-inventory-migrations.sh` where required.
- [ ] Prove clean-database migration and model parity.

## Atomic acquisition and oversell protection

- [ ] Acquire a PostgreSQL row lock on the exact target Inventory pool.
- [ ] Re-check quote expiry after lock acquisition using authoritative time.
- [ ] Materialize relevant elapsed Active holds inside the transaction.
- [ ] Resolve idempotency replay/conflict transactionally.
- [ ] Calculate effective commitments inside the transaction.
- [ ] Insert hold only when derived availability is at least one.
- [ ] Commit hold and lifecycle/outbox evidence atomically.
- [ ] Add bounded retry for supported transient concurrency failures.
- [ ] Return a safe retryable failure when retry is exhausted.
- [ ] Ensure competing pools do not require a global lock.
- [ ] Prove transaction rollback leaves no hold or transition evidence.

## Idempotency

- [ ] Require `Idempotency-Key` for hold creation.
- [ ] Validate trimmed ASCII length 8–100.
- [ ] Never log the plaintext key.
- [ ] Store safe request fingerprint for exact request identity.
- [ ] Same key + same quote returns the original hold.
- [ ] Same key + different quote returns `409 idempotency_conflict`.
- [ ] Concurrent same-key requests produce one logical outcome.
- [ ] Replay after terminal state returns the original terminal hold.
- [ ] Add unit/integration/concurrency tests for all idempotency paths.

## Availability integration

- [ ] Replace VS-07 `Capacity > 0` quote guard with derived current availability.
- [ ] Update customer-discovery availability projection to subtract effective Active holds.
- [ ] Update package-detail availability projection to subtract effective Active holds.
- [ ] Update any shared Inventory availability contract rather than duplicating calculations.
- [ ] Ensure elapsed holds stop consuming capacity at the exact expiry boundary.
- [ ] Ensure hold creation/release/expiry does not mutate configured capacity.
- [ ] Ensure customer responses do not expose unapproved exact remaining capacity.
- [ ] Add regression tests for discovery, package details and quote creation.

## Capacity adjustment protection

- [ ] Lock the same pool row during operator capacity writes.
- [ ] Calculate active hold/reservation commitments before reducing capacity.
- [ ] Reject capacity below commitments with `409 capacity_below_commitments`.
- [ ] Preserve existing optimistic-concurrency version behaviour.
- [ ] Keep customer identity and quote details out of operator responses.
- [ ] Add concurrent hold-vs-capacity-adjustment tests.

## API

- [ ] Implement `POST /api/v1/quotes/{quoteId}/holds`.
- [ ] Derive all hold business input from authenticated Account and stored quote.
- [ ] Return `201` for first acquisition and the original resource for idempotent replay.
- [ ] Implement owner-only `GET /api/v1/inventory-holds/{holdId}`.
- [ ] Implement owner-only idempotent `POST /api/v1/inventory-holds/{holdId}/release`.
- [ ] Materialize elapsed hold state before GET/release response.
- [ ] Implement `401`, `400`, `404`, `409` and `410` failure contracts from the specification.
- [ ] Include safe correlation IDs in unexpected Problem Details.
- [ ] Add API authorization, contract and integration tests.

## Customer UX

- [ ] Extend `/packages/{departureId}/plan`; do not create a new visual identity.
- [ ] Add one clear `Secure availability` primary action after a valid quote.
- [ ] Explain exactly what is held and that booking/payment have not started.
- [ ] Render expiry only from server `ExpiresAtUtc`.
- [ ] Use a display-only accessible countdown with static expiry text.
- [ ] Avoid urgency, scarcity pressure and second-by-second live-region announcements.
- [ ] Reuse the same idempotency key after uncertain network outcomes.
- [ ] Reload authoritative hold state after reconnect/refocus.
- [ ] Require explicit release before changing room/traveller choices.
- [ ] Do not rely on browser close, unload or beacon for correctness.
- [ ] Implement acquiring, active, unavailable, quote-expired, hold-expired, released, recovery and server-error states.
- [ ] Preserve approved Landing/Package/VS-07 visual language and shared footer/header patterns.
- [ ] Apply UI UX Pro Max only for accessibility, interaction and responsive-quality control.

## Accessibility and responsive verification

- [ ] Hold status change uses an appropriate polite live region.
- [ ] Countdown does not announce every second.
- [ ] Keyboard path and focus order remain logical.
- [ ] All actions have visible focus and minimum 44px targets.
- [ ] State is not conveyed by colour alone.
- [ ] 200% text reflows without clipping or horizontal overflow.
- [ ] 390px, 360px and representative desktop viewports pass.
- [ ] Reduced-motion preference is respected.
- [ ] Automated axe/accessibility checks pass.
- [ ] Rendered screenshots/traces are captured for active, unavailable and expired states.

## Telemetry and security

- [ ] Add safe structured hold operation/transition telemetry.
- [ ] Add acquisition success/unavailable/conflict metrics.
- [ ] Add release/expiry and contention/retry metrics.
- [ ] Do not log idempotency keys, fingerprints, quote money, Traveller IDs/PII or free text.
- [ ] Apply existing authentication, rate-control and correlation mechanisms.
- [ ] Verify client timestamps cannot affect validity.
- [ ] Secret scan remains green.
- [ ] Review threat-model delta for duplicate requests, capacity exhaustion and resource ownership.

## Concurrency test matrix

- [ ] Capacity 1 + many simultaneous Accounts creates exactly one Active hold.
- [ ] Capacity N creates at most N effective Active holds.
- [ ] Concurrent same-key requests create one row/outcome.
- [ ] Different key for same Account/departure/occupancy cannot duplicate capacity claim.
- [ ] Quote expiry while waiting for pool lock creates no hold.
- [ ] Release racing with acquisition does not produce negative availability.
- [ ] Expiry racing with acquisition returns capacity exactly once.
- [ ] Capacity reduction racing with acquisition preserves the invariant.
- [ ] Deadlock/transient retry remains bounded and observable.
- [ ] Available quantity never becomes negative in persisted/integration assertions.

## Quality and delivery

- [ ] Pre-commit formatting automation operates on the branch.
- [ ] Node format/check/test/build pass.
- [ ] .NET format/build/tests pass.
- [ ] Inventory domain/API/PostgreSQL integration tests pass.
- [ ] Inventory migration validation passes.
- [ ] Architecture boundaries pass.
- [ ] Secret scanning passes.
- [ ] Latest branch CI is fully green.
- [ ] Rendered VS-08 workflow is green on the exact tested head.
- [ ] Landing, Package, Discovery, Package Details and VS-07 regressions do not reappear.
- [ ] Deploy exact tested product head to an isolated preview when required for Product Owner review.
- [ ] Product Owner reviews the complete hold journey and all material states.
- [ ] Record acceptance evidence and final tested commit SHA.

## Merge gate

**DO NOT MERGE until every required VS-08 domain, contract, migration, oversell, idempotency, authorization, availability-integration, rendered accessibility, regression, CI and Product Owner acceptance gate is complete.**
