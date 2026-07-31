# VS-07 — Travellers & Authoritative Quote

Status: Specification / Definition of Ready

## Outcome

An authenticated customer can select one currently saleable occupancy for a published departure, create the minimum adult Traveller profiles required for that room, and receive an authoritative, expiring Pricing-owned quote that clearly shows total price, due now, remaining balance and the exact payment schedule that would apply if the customer proceeds.

VS-07 does **not** reserve inventory. VS-08 owns the first temporary inventory claim.

## Product intent

VS-07 makes NoorPath's approved proposition concrete:

**Plan early → understand the full commitment → pay gradually when the published operator payment plan allows it → prepare confidently.**

The customer must be able to answer before commitment:

- Who is travelling?
- Which room-sharing option am I choosing?
- What is the authoritative total?
- What is due now?
- What remains?
- On which dates would future instalments be due?
- How long is this quote valid?
- Is availability reserved yet? (No — not until VS-08.)

## Dependencies

- VS-06 Package Details is complete.
- The plan-ahead Product Design exploration is merged into `main`.
- Published departures carry an immutable `PublishedPriceVersionId` from Pricing.
- Public saleability continues to depend on Catalogue publication, current Operator eligibility, immutable published pricing and current Inventory.
- The foundation authentication boundary already resolves an authenticated principal to NoorPath `AccountId` and supports deterministic local/CI identities.

## Actor and authentication

Actor: authenticated customer / prospective Booking Owner.

Public Package Details remains browseable without authentication. Creating or reusing Traveller profiles and creating/retrieving a customer quote require authentication because Traveller name/date of birth and quote ownership are customer-scoped data.

- unauthenticated protected calls: `401`
- a quote/traveller owned by another account: fail closed as `404`
- no client-supplied `AccountId` is accepted

Real identity-provider UI/provider smoke verification remains deployment work; automated tests use the accepted deterministic test-auth boundary.

## Traveller boundary

### MVP traveller facts

VS-07 introduces the minimum Traveller capability required by the existing MVP scope:

- `TravellerId`
- owning/manager `AccountId` for pre-booking customer scope
- full name
- date of birth
- created/updated timestamps

Only these facts are collected. Passport, OCR, gender, nationality, contact details, Mahram relationship, visa data and document evidence are excluded.

### Adult-only slice policy

Existing published occupancy pricing is adult room-sharing pricing (`double`, `triple`, `quad`) and no child/infant pricing policy exists yet.

For VS-07, a Traveller must be **18 years or older on the published departure date** to be used in a quote. This is a narrow MVP slice policy, not a future child/infant policy. Child/infant categories remain deferred until their thresholds and pricing are explicitly defined.

### Room-sharing rule

VS-07 quotes exactly one currently published room-sharing option:

- `double` → exactly 2 Travellers
- `triple` → exactly 3 Travellers
- `quad` → exactly 4 Travellers

Multi-room allocation and groups larger than four are deferred. A later slice may introduce explicit room allocations without changing Traveller identity or Quote ownership.

## Pricing and payment-plan authority

Pricing owns the Quote until commitment. Booking will later preserve immutable commercial evidence.

### Payment plan definition

A mutable operator `PricePlan` may optionally define an instalment policy. The policy is versioned together with Pricing and is snapshotted into the immutable published `PriceVersion`.

MVP payment-plan fields:

- `depositPercent` — percentage of quote total due initially; greater than 0 and at most 100, max two decimal places
- `instalmentDayOfMonth` — integer 1–28
- `finalPaymentDueDaysBeforeDeparture` — integer 0–180

The plan is optional. When no payment plan is published, the full quote total is due now.

This model deliberately avoids hard-coded platform-wide instalment amounts or counts. It allows the operator's published commercial policy and the amount of time remaining before departure to determine the customer's schedule.

### Authoritative schedule calculation

At quote creation:

1. Pricing reads the departure's immutable published occupancy amount.
2. `total = published per-traveller amount × Traveller count`.
3. When no published payment plan applies: `dueNow = total`, `remaining = 0`, no future instalments.
4. When a published payment plan applies and its final-payment deadline is still in the future:
   - `dueNow` is the plan's deposit percentage of total, rounded to currency precision (2 decimals for MVP);
   - the final payment deadline is `departureDate - finalPaymentDueDaysBeforeDeparture`;
   - future due dates are generated from the configured day-of-month after quote creation through the final-payment deadline;
   - the final-payment deadline is always the last due date if it is not already represented by the monthly cadence;
   - remaining amount is divided across those future due dates; the final instalment absorbs rounding so all amounts reconcile exactly.
5. If the final-payment deadline is on/before quote creation, the full total is due now and no future schedule is offered.

The UI never calculates or changes authoritative quote arithmetic.

### Quote validity policy

VS-07 quote lifetime is **30 minutes** from successful creation.

- `createdAtUtc` and `expiresAtUtc` are authoritative server facts.
- expiry is never silently extended.
- an expired quote cannot be used by VS-08/VS-09; a new quote must be created.
- quote expiry does not release inventory because VS-07 never acquires inventory.

## Quote creation guards

A quote is created only when all remain true at request time:

1. departure and package are `Published`;
2. owning operator is currently eligible for public sale;
3. the departure's `PublishedPriceVersionId` resolves to the same departure/operator;
4. selected occupancy has a positive published immutable price;
5. selected occupancy currently has positive Inventory availability;
6. Traveller IDs belong to the authenticated Account;
7. Traveller count exactly matches the selected room-sharing option;
8. all selected Travellers satisfy the VS-07 adult-only policy on departure date;
9. no duplicate Traveller ID is supplied.

Quote creation does **not** promise availability. The response must say that availability is checked now but a place is not reserved until the next step (VS-08).

## Persistence ownership

### Traveller

New `traveller` schema and `TravellerDbContext` own Traveller profile records.

Pricing stores only Traveller stable identifiers as quote calculation inputs. Pricing must not copy names or dates of birth into quote persistence.

### Pricing

Pricing extends its existing schema with:

- optional payment-plan definition on mutable `PricePlan`;
- immutable payment-plan snapshot fields on `PriceVersion`;
- immutable/append-only Quote records;
- Quote Traveller references;
- Quote future instalment rows.

A Quote is not edited. Recalculation creates a new Quote.

## API

### `GET /api/v1/travellers`

Authentication required.

Returns only Traveller profiles managed by the current Account.

### `POST /api/v1/travellers`

Authentication required.

Request:

```json
{
  "fullName": "Amina Khan",
  "dateOfBirth": "1994-05-17"
}
```

Validation:

- full name trimmed, 2–120 characters;
- date of birth is a valid past calendar date;
- no adult classification is permanently stored; eligibility for this quote is evaluated against the departure date.

Returns `201` with opaque `travellerId` and normalized customer-safe facts.

### `POST /api/v1/departures/{departureId}/quotes`

Authentication required.

Request:

```json
{
  "occupancy": "double",
  "travellerIds": ["uuid", "uuid"]
}
```

Returns `201`:

```json
{
  "quoteId": "uuid",
  "departureId": "uuid",
  "priceVersionId": "uuid",
  "occupancy": "double",
  "travellerCount": 2,
  "currency": "INR",
  "unitPrice": 90000.00,
  "total": 180000.00,
  "dueNow": 36000.00,
  "remaining": 144000.00,
  "instalments": [
    { "sequence": 1, "dueDate": "2026-09-05", "amount": 24000.00 }
  ],
  "createdAtUtc": "...",
  "expiresAtUtc": "...",
  "availabilityReserved": false
}
```

The example is shape-only; runtime values come exclusively from authoritative state.

### `GET /api/v1/quotes/{quoteId}`

Authentication required; owner-only, otherwise `404`.

Returns the stored immutable quote and current `expired` boolean derived from `expiresAtUtc`.

## Failure contract

- `401 not_authenticated` — customer must sign in.
- `404` — unknown/private traveller or quote; departure no longer saleable; cross-account ownership hidden.
- `409 quote_unavailable` — selected occupancy no longer has current positive availability or commercial state changed during calculation.
- `410 quote_expired` is reserved for later commands that attempt to consume an expired quote; simple GET returns the quote with `expired: true`.
- `422` — invalid occupancy, traveller count, duplicate travellers, traveller not adult for this departure, invalid traveller input.
- unexpected failures use Problem Details + correlation ID.

## Customer UX

Route: `/packages/{departureId}/plan`

Package Details gains one primary progression action to begin planning without turning the public detail page into checkout.

The plan flow preserves package context and uses progressive disclosure:

1. **Room & travellers** — choose a currently available room-sharing option; add/select the exact number of adult travellers required.
2. **Your quote** — authoritative total, due now, remaining balance, exact future dates/amounts, and quote expiry.
3. **Next step boundary** — explain that the quote does not reserve inventory; the future primary action is to secure availability in VS-08.

Required states:

- unauthenticated/sign-in-required boundary;
- loading;
- traveller validation errors adjacent to fields;
- no/insufficient Traveller profiles;
- quote creation in progress;
- authoritative quote loaded;
- quote expired;
- package/occupancy no longer saleable;
- network/server error with retry where safe;
- responsive 390/360 px and desktop;
- keyboard/focus/200% text/reduced-motion verification.

Visual authority remains approved Landing + Package + `design-system/MASTER.md`. UI UX Pro Max is used only for accessibility, interaction and responsive-quality checks.

## Security / privacy

- Traveller endpoints require authenticated Account scope.
- Quote ownership derives server-side from authenticated Account.
- Names/DOB never enter logs or Pricing quote rows.
- Public package endpoints remain PII-free.
- Quote endpoints return only commercial values and Traveller IDs required by the current Account's flow.
- No passport/document/payment secrets are introduced.
- Rate/abuse control uses existing platform mechanisms; no speculative infrastructure.

## Telemetry

Safe structured telemetry only:

- operation/outcome;
- departure ID;
- quote ID where applicable;
- occupancy key;
- traveller count;
- pricing/price-version identifiers;
- duration;
- correlation ID.

Do not log Traveller names, DOB, quote monetary payloads, payment-plan amounts or free text.

## Test matrix

### Traveller

1. authenticated Account can create/list its own Travellers;
2. unauthenticated calls receive `401`;
3. invalid name/date receives `422`;
4. Account A cannot read/select Account B's Traveller;
5. PII is not emitted in logs.

### Quote

1. valid published departure + exact Traveller count creates a quote;
2. total equals immutable published unit price × Traveller count;
3. mutable draft price changes do not alter the quote basis;
4. optional payment plan is snapshotted at publication and drives quote schedule;
5. no published payment plan produces full amount due now;
6. schedule arithmetic reconciles exactly;
7. late quote after final-payment deadline produces full amount due now;
8. quote expires exactly 30 minutes after creation;
9. unsupported occupancy / mismatched Traveller count / duplicates / under-18 traveller receives `422`;
10. unavailable occupancy/commercial race fails without creating a usable quote;
11. quote is immutable after creation;
12. cross-account quote lookup is hidden as `404`;
13. quote does not create Inventory hold/reservation state.

### Web

1. Package Details leads clearly into planning;
2. package context remains visible through room/traveller/quote steps;
3. payment schedule is rendered only from quote response;
4. quote expiry and non-reservation language is clear without urgency pressure;
5. loading/error/validation/auth states work;
6. keyboard/focus/accessibility/reduced-motion/390/360 px reflow pass.

## Acceptance criteria

1. Authenticated customer can manage the minimum Traveller profiles needed for VS-07.
2. Only supported adult Traveller/occupancy combinations can be quoted.
3. Quote uses the departure's immutable published PriceVersion and current availability guard.
4. Total/due-now/remaining/instalment arithmetic is server authoritative and reconciles exactly.
5. Operator payment-plan policy is versioned and snapshotted with published pricing; UI invents no commercial amounts.
6. Quote has explicit 30-minute expiry and cannot be silently extended.
7. Quote creation does not reserve inventory.
8. Customer flow extends approved NoorPath visual language and meets responsive/accessibility states.
9. Pricing, Traveller, Catalogue and Inventory ownership boundaries remain explicit; no cross-module table mutation.
10. Migration, build, tests, CI, rendered Netlify review and Product Owner acceptance pass before merge.

## Explicit exclusions

Inventory hold/reservation, booking creation, payment provider, payment settlement, reusable family profiles beyond the minimal Traveller records, child/infant pricing, multi-room allocation, passport/OCR, Mahram, documents, visa workflows, promotions/discounts, dynamic pricing and automatic operator price optimization.

## Merge gate

**Do not merge VS-07 until specification/design, contract, implementation, migrations, CI, rendered Netlify verification and Product Owner acceptance are all complete.**
