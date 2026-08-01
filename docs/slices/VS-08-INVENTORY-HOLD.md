# VS-08 — Inventory Hold

Status: Specification / Definition of Ready

## Outcome

An authenticated customer with a valid VS-07 authoritative quote can atomically secure the exact selected occupancy for a short, explicit period without overselling inventory, silently extending the quote, creating a booking, or initiating payment.

VS-08 owns the first temporary inventory claim. VS-09 will consume a valid quote and active hold to create a booking and attempt payment.

## Product intent

VS-08 turns the VS-07 statement "availability is not reserved yet" into a trustworthy, reversible commitment:

**Review the authoritative quote → secure availability briefly → continue to booking and payment in the next slice.**

The customer must be able to answer:

- What exactly is being held?
- Until what authoritative time is it held?
- Is the quoted commercial basis still unchanged?
- What happens when the hold expires?
- Can changing room/traveller choices release the hold safely?
- Has a booking or payment been created? (No.)

The experience must communicate certainty without scarcity pressure, artificial urgency or countdown-driven persuasion.

## Traceability

- MVP Vertical Slice Map: VS-08 Inventory Hold.
- Domain Map: Inventory owns capacity, availability, holds, expiry, release and oversell protection.
- Core Domain Model: `InventoryPool` is the concurrency hotspot and `InventoryHold` owns temporary allocation lifecycle.
- State Machines: `Active -> Released|Expired`; `Reserved` remains a later transition.
- VS-03 Pricing & Inventory: existing occupancy pools and configured capacity.
- VS-07 Travellers & Authoritative Quote: authenticated, immutable, 30-minute Pricing-owned quote.

No new requirement identifier is invented for this slice.

## Dependencies

- VS-07 is merged and its quote is immutable, customer-owned and explicitly expiring.
- The quote contains `QuoteId`, `AccountId`, `DepartureId`, `OperatorId`, `PriceVersionId`, occupancy, traveller count and `ExpiresAtUtc`.
- Inventory has one configuration per departure and one pool per supported occupancy.
- Public saleability remains composed from Operator eligibility, Catalogue publication, Pricing truth, booking cutoff and Inventory availability.
- Authentication resolves the current NoorPath `AccountId` server-side.
- PostgreSQL and the existing migration-validation path remain authoritative for Inventory persistence.

## Actor and authentication

Actor: authenticated customer / prospective Booking Owner.

- unauthenticated hold commands: `401 not_authenticated`;
- quote or hold owned by another Account: fail closed as `404`;
- no client-supplied Account, departure, operator, occupancy, quantity, price or traveller facts are accepted;
- the hold command derives its complete business input from the authenticated Account and the exact stored quote.

## Capacity unit decision

For the existing MVP `double`, `triple` and `quad` pools, `InventoryPool.Capacity` means the number of saleable **one-room allocations** for that occupancy on the departure.

A VS-07 quote represents exactly one room-sharing selection, so VS-08 acquires `quantity = 1` from the matching occupancy pool:

- one double-sharing quote holds one double room allocation;
- one triple-sharing quote holds one triple room allocation;
- one quad-sharing quote holds one quad room allocation.

Traveller count remains part of the quote and future booking evidence, but it is not subtracted directly from pool capacity.

This is not airline-seat inventory, hotel supplier stock or a multi-room allocation model. Multi-room quantities and supplier-specific inventory are explicitly deferred.

## Authoritative availability

Inventory never persists a customer-display `available` boolean.

For one pool at authoritative time `now`:

```text
available quantity =
  configured capacity
  - effective active hold quantity
  - active reservation quantity
```

For VS-08, reservation quantity is zero because reservation conversion belongs to a later slice.

An effective active hold is a hold whose stored state is `Active` and whose `ExpiresAtUtc` is strictly greater than `now`.

Invariant:

```text
available quantity >= 0
```

All customer discovery, package-detail, quote-creation and hold-acquisition availability reads must use this derived rule. The current VS-07 `Capacity > 0` guard is no longer sufficient after holds exist.

## Hold lifetime policy

Inventory owns hold-duration policy.

MVP launch value: **15 minutes**.

The hold cannot extend Pricing's quote validity:

```text
holdExpiresAtUtc = min(holdCreatedAtUtc + 15 minutes, quote.ExpiresAtUtc)
```

Rules:

- quote expiry is never changed or silently extended;
- a quote must still be valid when the pool lock is acquired;
- `now >= ExpiresAtUtc` means expired;
- all times are UTC and come from the injected authoritative `TimeProvider`;
- the response always returns the exact server expiry timestamp;
- the client countdown is display-only and never determines hold validity.

If little quote time remains, the resulting hold may be shorter than 15 minutes. The UI must show the actual server deadline rather than promising a fixed duration.

The launch value is Inventory-owned configuration, not operator-editable configuration and not a generic platform setting.

## Hold lifecycle

### States implemented by VS-08

- `Active` — one unit is withheld from derived availability.
- `Released` — the customer explicitly returned the unit before expiry.
- `Expired` — authoritative time elapsed and the unit no longer affects availability.

`Released` and `Expired` are terminal for that hold.

### Future state

- `Reserved` — conversion to a Booking-linked Inventory Reservation is not implemented by VS-08.

No VS-08 endpoint can create a reservation, booking or payment.

## Expiry processing

Availability must become correct immediately at the expiry boundary even if no background process has updated the stored status yet.

Therefore:

1. every availability calculation excludes `Active` rows with `ExpiresAtUtc <= now`;
2. hold acquire/get/release operations materialize relevant elapsed `Active` rows to `Expired` using an idempotent set-based update;
3. materialization records the terminal timestamp and safe lifecycle evidence;
4. a dedicated distributed scheduler or queue is not required for VS-08;
5. a later maintenance worker may be added only against an operational requirement.

This provides immediate capacity release without introducing speculative infrastructure.

## Hold creation guards

A hold can be created only when all remain true at the authoritative transaction time:

1. the quote exists and belongs to the authenticated Account;
2. the quote is not expired;
3. the quote still references the same published departure, operator and immutable PriceVersion evidence;
4. the departure remains saleable for a new checkout action;
5. the exact Inventory configuration belongs to the quote departure/operator;
6. the matching occupancy pool exists;
7. configured capacity is positive;
8. derived availability is at least one after expired holds are materialized;
9. the same Account does not already have an effective Active hold for the same departure/occupancy;
10. the same quote does not already have a different effective Active hold;
11. the idempotency key is valid and is not bound to a different request identity.

A PriceVersion becoming superseded does not silently reprice an unexpired quote. Paused, closed, cancelled or otherwise non-saleable departure/operator state blocks a new hold.

## Idempotency contract

Hold creation is retry-sensitive and requires the HTTP `Idempotency-Key` header.

MVP rules:

- trimmed ASCII value, 8–100 characters;
- scoped to authenticated Account;
- never logged in plaintext;
- stored with a request fingerprint containing the exact Account and Quote identity;
- same Account + same key + same fingerprint returns the original hold result;
- same Account + same key + different fingerprint returns `409 idempotency_conflict`;
- concurrent requests using the same key produce one hold row and one logical outcome;
- replaying a key after the hold became terminal returns that original terminal hold, never creates a new one;
- a genuinely new attempt requires a new key and must still satisfy all current guards.

One effective Active hold is allowed per Account + departure + occupancy. A second quote or key cannot make the same customer withhold duplicate capacity for the same selection.

## Concurrency strategy

The approved MVP strategy uses a short PostgreSQL transaction and a lock on the exact target `InventoryPool` row.

Hold acquisition sequence:

1. load the customer-safe quote contract;
2. begin Inventory transaction;
3. lock the matching pool row using PostgreSQL row-level locking;
4. obtain authoritative `now` and re-check quote expiry;
5. materialize relevant expired holds;
6. resolve idempotency replay/conflict;
7. enforce one-active-hold uniqueness;
8. calculate effective committed quantity inside the transaction;
9. reject when `capacity - commitments < 1`;
10. insert the Active hold and lifecycle/outbox evidence;
11. commit;
12. return the stored authoritative result.

All competing acquisitions for the same pool serialize on the same row. Different pools do not require a global lock.

Transient deadlock/serialization failures may use a small bounded server-side retry. Exhausted retries return a safe retryable problem response and do not create an ambiguous second hold.

Database uniqueness remains a second line of defence for idempotency and active-hold constraints; application checks alone are insufficient.

## Capacity-adjustment protection

After VS-08, operator capacity updates must account for active commitments.

An operator cannot reduce a pool's capacity below:

```text
unexpired active holds + active reservations
```

The capacity write must lock the same pool row and return `409 capacity_below_commitments` rather than allowing negative availability.

Existing operator optimistic-concurrency version checks remain applicable. This guard does not permit operator access to customer identity, quote money or traveller data.

## Persistence ownership

Inventory continues to own PostgreSQL schema `inventory`.

### Existing records

- `InventoryConfigurationRecord`
- `InventoryPoolRecord`
- capacity-adjustment audit history

### New hold persistence

`inventory_holds` stores only the Inventory-owned lifecycle and stable external references needed for the workflow:

- `InventoryHoldId`
- `InventoryPoolId`
- `DepartureId`
- `OperatorId`
- `QuoteId`
- owning `AccountId`
- occupancy
- quantity (`1` in VS-08)
- state
- idempotency key or irreversible safe representation
- request fingerprint
- `CreatedAtUtc`
- `ExpiresAtUtc`
- terminal timestamp when released/expired
- safe correlation/audit metadata where required

Inventory does not copy:

- traveller names or dates of birth;
- Traveller records;
- quote monetary amounts;
- payment schedule;
- package/hotel content;
- payment data.

External identifiers are stable references only. No database foreign key crosses module schemas.

### Required indexes/constraints

- unique Account + idempotency key;
- index by pool + state + expiry for effective commitment calculation;
- index by Account + departure + occupancy + state;
- index by quote + state;
- quantity greater than zero;
- expiry after creation;
- valid state allowlist;
- module-local foreign key from hold to pool.

Where partial Active-state uniqueness is used, expired rows must be materialized under the pool lock before a new insert.

## Cross-capability contracts

Inventory must not query or mutate Pricing, Catalogue, Operators, Traveller or Booking persistence directly.

The API/application composition obtains minimum safe contracts such as:

### Pricing: `QuoteForInventoryHold`

- Quote ID
- Account ID
- Departure ID
- Operator ID
- PriceVersion ID
- occupancy
- traveller count
- quote expiry
- validity result

No money or payment schedule is required by Inventory.

### Catalogue/Operators: saleability result

- departure/operator identity match;
- current new-checkout saleability;
- no package content.

### Inventory result

- hold ID
- quote ID
- departure ID
- occupancy
- quantity
- lifecycle state
- creation/expiry/terminal timestamps

Experience composition does not become a source of truth.

## API

### `POST /api/v1/quotes/{quoteId}/holds`

Authentication required.

Header:

```text
Idempotency-Key: <client-generated value>
```

No request body is required. Account, departure, occupancy and quantity are derived from the stored quote.

First successful acquisition returns `201`:

```json
{
  "holdId": "uuid",
  "quoteId": "uuid",
  "departureId": "uuid",
  "occupancy": "double",
  "quantity": 1,
  "status": "active",
  "createdAtUtc": "2026-08-01T01:00:00Z",
  "expiresAtUtc": "2026-08-01T01:15:00Z",
  "availabilityReserved": true
}
```

An idempotent replay returns the same resource and authoritative state without creating another capacity claim.

The response does not expose exact remaining pool capacity.

### `GET /api/v1/inventory-holds/{holdId}`

Authentication required; owner-only, otherwise `404`.

Materializes expiry when applicable and returns the current authoritative hold state.

### `POST /api/v1/inventory-holds/{holdId}/release`

Authentication required; owner-only, otherwise `404`.

- Active and unexpired -> Released;
- effectively expired -> Expired;
- already Released/Expired -> returns the current terminal result;
- no duplicate capacity return is possible.

This endpoint is naturally idempotent by Hold ID and does not require a separate idempotency key.

## Failure contract

- `401 not_authenticated` — sign-in required.
- `400 idempotency_key_required` — header absent.
- `400 invalid_idempotency_key` — header does not satisfy the allowlist/length rule.
- `404` — unknown/private quote or hold.
- `409 hold_unavailable` — capacity or current saleability no longer permits acquisition.
- `409 active_hold_exists` — this Account already owns an effective Active hold for the same departure/occupancy under another request.
- `409 idempotency_conflict` — key was previously used for a different request identity.
- `409 capacity_below_commitments` — operator capacity reduction would violate active commitments.
- `410 quote_expired` — quote expired before acquisition.
- unexpected failures use Problem Details with safe correlation ID.

A simple GET of a hold returns its terminal state rather than using `410`.

## Customer UX

Existing route: `/packages/{departureId}/plan`.

VS-08 extends the authoritative quote step; it does not create a separate checkout visual identity.

### Primary progression

After a valid quote is shown, the primary action becomes:

**Secure availability**

Supporting copy explains that NoorPath will hold one selected room allocation only until the exact server deadline, with no booking or payment yet.

### Active hold presentation

Show:

- selected occupancy and traveller count from the quote;
- clear successful hold state using text and icon, not colour alone;
- exact localised expiry time;
- accessible countdown using tabular figures where displayed;
- explicit "Booking and payment have not started" boundary;
- future progression placeholder for VS-09 without a fake payment action.

### Required interaction rules

- disable duplicate submission while acquisition is pending;
- reuse the same idempotency key after uncertain network outcomes;
- after reconnect/refocus, reload the hold from the server;
- never treat the browser countdown as authority;
- changing room or travellers requires explicit release, then a new quote;
- do not rely on `beforeunload`, browser close or best-effort beacon for correctness;
- browser close simply allows authoritative expiry to return capacity;
- no scarcity messages such as "Hurry", "Only seconds left" or animated urgency.

### Required states

- sign-in required;
- acquiring;
- active hold;
- idempotent recovery after unknown response;
- capacity unavailable;
- quote expired before hold;
- hold expired;
- explicitly released;
- network/server error with safe retry;
- active hold already exists;
- 390px, 360px and desktop layouts;
- keyboard, visible focus, 44px targets, screen-reader updates, 200% text and reduced motion.

Visual authority remains the approved Landing, Package page and `design-system/MASTER.md`. UI UX Pro Max may improve accessibility, interaction and responsive quality but may not replace NoorPath's established identity.

## Accessibility details

- the hold status change is announced through an appropriate polite live region;
- countdown announcements are not emitted every second;
- an accessible static expiry sentence remains available;
- focus moves only when needed for an error/confirmation and never unexpectedly during countdown updates;
- expiry/release is conveyed by text and semantics, not only green/amber/red;
- controls remain operable at 200% text without horizontal scrolling;
- reduced motion disables non-essential transitions.

## Security and privacy

- Account ownership is derived from the authenticated principal.
- Cross-account resources fail closed as `404`.
- Idempotency keys and request fingerprints are not logged in plaintext.
- Inventory records contain no Traveller PII or quote money.
- Public/customer responses do not expose operator adjustment reasons or exact remaining capacity.
- The command is rate-controlled using existing platform mechanisms.
- All state transitions are server-authoritative and protected from client timestamp manipulation.
- No payment, passport, document or provider secret enters this slice.

## Telemetry and evidence

Safe structured logs/metrics:

- operation and outcome;
- hold ID where created/resolved;
- quote ID;
- departure ID;
- pool ID;
- occupancy;
- quantity;
- lifecycle transition;
- idempotent replay boolean;
- lock/contention/retry duration;
- correlation ID.

Do not log:

- idempotency key;
- request fingerprint;
- Account identity unless explicitly required by existing protected audit policy;
- Traveller IDs or PII;
- quote amounts or instalment schedule;
- free text.

Required metrics include acquisition success/unavailable/conflict, active effective holds, release, expiry materialization and lock/retry latency.

Completed lifecycle facts use the existing transactional evidence/outbox foundation where applicable:

- `InventoryHeld`
- `InventoryHoldReleased`
- `InventoryHoldExpired`

No external broker or consumer is introduced solely for this slice.

## Test matrix

### Domain/time

1. hold expiry is `min(now + 15 minutes, quote expiry)`;
2. `now == expiresAtUtc` is expired;
3. release and expiry are terminal/idempotent;
4. no hold extends quote validity;
5. no hold creates reservation/booking/payment state.

### Authorization/contracts

1. authenticated owner can create/get/release its hold;
2. unauthenticated calls receive `401`;
3. Account A cannot access Account B quote/hold;
4. client cannot choose Account, departure, occupancy or quantity;
5. Inventory contract receives no quote monetary or Traveller PII.

### Idempotency

1. same key and same quote returns one hold;
2. concurrent same-key requests produce one row/outcome;
3. same key with different quote returns `409 idempotency_conflict`;
4. replay after terminal state returns the original terminal hold;
5. missing/invalid key is rejected before capacity mutation.

### Concurrency/oversell

1. capacity 1 with many simultaneous Accounts creates exactly one Active hold;
2. capacity N creates at most N effective Active holds;
3. competing pools do not block each other globally;
4. release racing with acquisition never produces negative availability;
5. expiry racing with acquisition safely returns capacity once;
6. quote expiring while waiting for the pool lock creates no hold;
7. capacity reduction below commitments is rejected;
8. database uniqueness protects against duplicate active/idempotent rows;
9. transaction failure creates neither hold nor lifecycle evidence.

### Availability integration

1. discovery/package detail/quote guards subtract effective Active holds;
2. elapsed holds stop affecting availability at the exact time boundary;
3. capacity remains unchanged by hold creation/release/expiry;
4. available quantity is derived, never persisted;
5. public responses remain truthful without exposing exact internal commitments where not approved.

### Persistence/migrations

1. clean PostgreSQL database applies Inventory migration;
2. model snapshot matches;
3. required indexes/check constraints exist;
4. no cross-schema foreign key is introduced;
5. migration validation script remains green.

### Web/rendered

1. valid quote can acquire one hold;
2. active state renders exact expiry and non-booking boundary;
3. retry after uncertain response uses the same key;
4. unavailable/expired/released/error states are understandable;
5. room/traveller changes require explicit release/new quote;
6. keyboard/focus/live-region/target-size/reduced-motion/200%-text tests pass;
7. 390px, 360px and desktop have no horizontal overflow;
8. Landing, Package and completed VS-07 regressions do not reappear.

## Acceptance criteria

1. A valid customer-owned quote can atomically acquire one matching room-allocation hold.
2. Concurrent requests cannot produce effective commitments above configured capacity.
3. Availability subtracts effective Active holds everywhere it is presented or guarded.
4. Hold lifetime is Inventory-owned, 15 minutes maximum, and never exceeds quote expiry.
5. Hold creation is exactly-once for an Account-scoped idempotency key.
6. One Account cannot hold duplicate capacity for the same departure/occupancy concurrently.
7. Release and expiry return capacity exactly once and are safe under retries/races.
8. Operator capacity cannot be reduced below active commitments.
9. Inventory owns hold truth and copies no quote money or Traveller PII.
10. The customer sees active/expired/released/unavailable states without false urgency.
11. No Reservation, Booking or Payment is created.
12. Domain, integration, migration, concurrency, authorization, rendered accessibility and regression gates pass.
13. Exact tested head is reviewed by the Product Owner before merge.

## Explicit exclusions

Inventory reservation, Booking creation, payment initiation/settlement, hold-to-reservation conversion, quote acceptance, multi-room booking, groups larger than four, airline-seat inventory, supplier hotel inventory, waitlist, automatic capacity replenishment, operator hold management UI, manual customer-hold override, distributed lock service, Redis lock, message broker, payment countdown, promotions and dynamic pricing.

## Merge gate

**DO NOT MERGE VS-08 until specification/design, contracts, implementation, migration, oversell/idempotency tests, normal CI, rendered responsive/accessibility verification, regression evidence and Product Owner acceptance are all complete.**
