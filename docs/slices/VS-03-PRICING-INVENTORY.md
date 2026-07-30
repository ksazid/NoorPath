# VS-03 — Pricing & Inventory

Status: Implementation

## Outcome

An authorized operator staff principal can configure explicit occupancy pricing and controlled capacity for a private draft departure, with Pricing and Inventory remaining separate authoritative capabilities.

## Traceability

- `INV-ID-003`–`INV-ID-006`
- `INV-ACC-003`
- `INV-PRI-001`–`INV-PRI-006` where applicable before quote creation
- `INV-INV-001`, `INV-INV-003`, `INV-INV-006`
- VS-01 Operator Access
- VS-02 Package & Departure Authoring
- MVP Vertical Slice Map VS-03

No stable approved PRD requirement ID is invented here.

## Actor and access

The actor is an authenticated principal with an active membership for an `Approved` operator and explicit `operator.admin.access` permission. Operator scope is derived server-side from VS-01 access context.

Pricing and inventory configuration is available only for a `Draft` departure owned by the current operator. Cross-operator and unknown departure access returns `404` so private resource existence is not disclosed.

## MVP occupancy decision

VS-03 supports exactly these adult room-sharing occupancy keys:

- `double`
- `triple`
- `quad`

This is a slice-level MVP decision. Child/infant pricing, single occupancy, custom occupancy keys and traveller-age pricing are excluded until explicitly approved in a later slice/policy change.

An occupancy is commercially ready only when both of these exist for the same departure and occupancy key:

1. a valid Pricing amount with explicit currency; and
2. an Inventory pool with capacity greater than zero.

No module stores a synthetic `available` boolean. Availability is derived from authoritative inventory facts.

## Included

### Pricing

- one editable `PricePlan` per operator-owned draft departure;
- explicit ISO-style three-letter currency code, normalized to uppercase;
- one positive amount per configured occupancy;
- duplicate occupancy keys rejected;
- integer optimistic concurrency version;
- create/update audit evidence with actor, correlation ID, version and timestamp.

VS-03 does not publish an immutable `PriceVersion`. VS-04 owns review/publication and may issue the first immutable public price version after completeness checks.

### Inventory

- one inventory configuration per operator-owned draft departure;
- one `InventoryPool` per configured occupancy key;
- non-negative sellable capacity;
- derived available quantity equals configured capacity in VS-03 because holds/reservations do not exist until later slices;
- integer optimistic concurrency version;
- every capacity write records actor, reason, correlation ID, version and timestamp.

VS-03 has no holds or reservations. Future hold/reservation slices must subtract authoritative commitments from availability and prevent oversell under concurrency.

### Operator UX

- pricing and inventory sections extend the existing VS-02 departure composer;
- Double / Triple / Quad rows only;
- explicit currency field;
- occupancy price and capacity inputs;
- pricing and inventory save-in-progress/saved states;
- no-change state;
- field validation;
- stale-version conflict with reload path;
- unauthenticated/forbidden/hidden-departure handling;
- clear readiness indication based on both price and capacity without implying publication.

## Excluded

Price publication, customer quote generation, quote expiry, deposit/instalment rules, fees/taxes, promotions, child/infant pricing, custom occupancy keys, waitlist, supplier inventory sync, holds, reservations, booking, customer-facing price/availability, review and publish.

## API

### `GET /api/v1/operator/departures/{departureId}/commercial`

Returns the current operator's combined private configuration projection:

- departure ID;
- pricing configuration or `null`;
- inventory configuration or `null`;
- per-occupancy readiness derived from both capabilities.

### `PUT /api/v1/operator/departures/{departureId}/pricing`

Upserts the Pricing-owned draft plan.

Request:

- `expectedVersion` — `0` for first configuration;
- `currency` — exactly three ASCII letters;
- `occupancies[]` — unique `double|triple|quad` keys with positive decimal `amount`.

Returns the current pricing version and normalized values. Stale writes return `409` Problem Details with code `stale_pricing_version`.

### `PUT /api/v1/operator/departures/{departureId}/inventory`

Upserts the Inventory-owned configuration.

Request:

- `expectedVersion` — `0` for first configuration;
- `adjustmentReason` — required, trimmed, maximum 240 characters;
- `pools[]` — unique `double|triple|quad` keys with non-negative integer `capacity`.

Returns the current inventory version and derived available quantity. Stale writes return `409` Problem Details with code `stale_inventory_version`.

Validation returns `422` Problem Details with field errors. Authentication/authorization uses the established VS-01 `401`/`403` contract.

## Persistence ownership

- Pricing owns PostgreSQL schema `pricing` and does not query Catalogue or Inventory tables.
- Inventory owns PostgreSQL schema `inventory` and does not query Catalogue or Pricing tables.
- Catalogue continues to own the draft departure and does not gain price/capacity columns.
- API composition verifies the current operator owns a draft departure, then delegates reads/writes to the owning capability contexts.

## Concurrency

Pricing and Inventory have independent optimistic concurrency versions because they are separate sources of truth and may be edited independently.

There is deliberately no cross-module all-or-nothing save transaction in VS-03. The operator UX reports each capability's save result independently and commercial readiness is derived only after both valid configurations exist.

## Telemetry

Pricing and Inventory writes log only outcome, operator ID, departure ID, capability version and correlation ID. Price amounts, free-text adjustment reasons and other commercial payloads are not logged.

## Test matrix

- pricing validation: currency, amount, occupancy allowlist, duplicates;
- inventory validation: capacity, occupancy allowlist, duplicates, reason;
- authenticated authorized create/load/update for both capabilities;
- unauthenticated/forbidden denial;
- cross-operator/unknown departure hidden as `404`;
- non-draft departure rejected once later states exist;
- stale pricing and inventory writes return capability-specific `409`;
- PostgreSQL persistence and audit history;
- module migration model parity and clean database application;
- architecture boundaries prevent Pricing/Inventory cross-module implementation dependencies;
- operator UI validation, loading, saved, conflict, error and readiness states.

## Acceptance criteria

1. Approved VS-01 operator access can configure pricing and inventory for its own VS-02 draft departure without supplying OperatorId.
2. Pricing stores explicit currency and positive amounts only for Double, Triple and Quad occupancies.
3. Inventory stores non-negative capacity only for Double, Triple and Quad occupancies and derives availability rather than persisting a display boolean.
4. Pricing, Inventory and Catalogue remain separate owners/schemas.
5. An occupancy is reported ready only when both valid price and capacity greater than zero exist.
6. Operator A cannot read or mutate Operator B's commercial configuration.
7. Stale Pricing and Inventory saves cannot overwrite newer versions.
8. Inventory capacity writes preserve actor/reason/correlation/version/timestamp audit evidence; Pricing writes preserve actor/correlation/version/timestamp evidence.
9. No price publication, quote, payment-plan, hold, reservation, booking or customer commercial projection is introduced.
10. Applicable build, format, unit, PostgreSQL integration, migration, authorization, accessibility and CI gates pass.
11. UI implementation extends NoorPath's approved visual language and receives Product Owner visual acceptance before VS-03 closes.
