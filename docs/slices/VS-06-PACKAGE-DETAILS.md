# VS-06 — Package Details

Status: Specification

## Outcome

A public customer can open a currently saleable published Umrah departure and understand its operator, dates, accommodation, travel facts, inclusions/exclusions, published occupancy prices and current availability sufficiently to decide whether to continue toward booking.

## Actor

Public customer. Authentication is not required for package details.

## Traceability

- `INV-OP-001`
- `INV-CAT-002`–`INV-CAT-006`
- `INV-ACC-001`–`INV-ACC-003`
- `INV-PRI-002`, `INV-PRI-003`, `INV-PRI-006`
- `INV-INV-001`, `INV-INV-003`
- `POL-INV-002`
- `POL-BKG-001`, `POL-BKG-002` remain unresolved and must not be invented
- UX baseline: Public Customer / Package detail
- MVP Vertical Slice Map VS-06
- VS-05 Customer Discovery

No stable approved PRD requirement ID is invented here.

## Dependencies

- VS-05 public discovery is implemented.
- VS-04 publication produces a published Catalogue departure and immutable `PublishedPriceVersionId`.
- Pricing owns published occupancy amounts and explicit currency.
- Inventory owns current occupancy capacity/availability.
- Operator publication eligibility is re-evaluated at read time.

## Included

- unauthenticated public detail query for one departure;
- only a currently saleable `Published` departure whose package version is also `Published`;
- current operator eligibility check before exposure;
- package title, summary, origin, departure/return dates and duration;
- independent Makkah and Madinah accommodation facts and confirmation state;
- operator-entered travel route summary, travel details and confirmation state;
- complete ordered inclusions and exclusions from the published package version;
- immutable published occupancy prices with explicit ISO currency;
- current Inventory availability matched to each published occupancy;
- clear unavailable state for a published occupancy that currently has zero availability;
- existing approved Package page composition migrated from static preview content to authoritative public data;
- loading, not-found/unavailable, error/retry and populated detail states;
- responsive/accessibility verification at desktop, 390 px and 360 px.

## Excluded

- traveller-count pricing and authoritative quote generation (VS-07);
- occupancy selection that creates commercial commitment;
- inventory holds (VS-08);
- booking, authentication, payment or instalment schedule (VS-07+);
- operator credentials such as IATA/ISO/years-in-business unless later backed by an approved public operator contract;
- invented day-by-day itinerary facts not represented by Catalogue;
- cancellation/refund entitlement, booking cutoff, deposit or instalment rules while their policies remain unresolved;
- advanced recommendations, comparison or dynamic supplier data;
- redesign of the approved Package page.

## Public saleability rules

The detail endpoint returns a package only when all of the following remain true at query time:

1. Catalogue departure status is `Published`.
2. Its PackageVersion status is `Published`.
3. The owning operator is currently approved for publication.
4. `PublishedPriceVersionId` resolves to the immutable Pricing snapshot for the same departure/operator.
5. Published occupancy prices are positive.
6. Inventory configuration belongs to the same departure/operator.
7. At least one published occupancy has matching current positive Inventory availability.

Unknown, inconsistent, private or no-longer-saleable departures return the same public not-found result. Internal state is not disclosed.

## Pricing and availability rules

- The response currency comes only from the immutable published `PriceVersion`.
- Each published positive occupancy price is returned with the current matching Inventory quantity.
- A priced occupancy with no positive current inventory is displayed as unavailable, not silently removed from the price table.
- The package itself is not publicly returned when no occupancy is currently saleable.
- The UI never derives, edits, converts or combines authoritative money values.
- No limited/scarcity wording is used because `POL-INV-002` has no approved threshold.

## API

### `GET /api/v1/departures/{departureId}`

Authentication: none.

Successful response:

```json
{
  "departureId": "uuid",
  "operator": {
    "id": "operator-id",
    "displayName": "Operator name"
  },
  "packageName": "Package name",
  "summary": "Package summary",
  "origin": "Delhi (DEL)",
  "departureDate": "2026-09-18",
  "returnDate": "2026-09-30",
  "durationNights": 12,
  "makkah": {
    "hotelName": "Hotel name",
    "classification": "5 star",
    "distanceDisclosure": "Approximately 300 m from Masjid al-Haram",
    "nights": 7,
    "confirmationState": "confirmed"
  },
  "madinah": {
    "hotelName": "Hotel name",
    "classification": "5 star",
    "distanceDisclosure": "Approximately 250 m from Al-Masjid an-Nabawi",
    "nights": 5,
    "confirmationState": "confirmed"
  },
  "travel": {
    "routeSummary": "Delhi → Jeddah → Makkah → Madinah → Delhi",
    "details": "Operator-entered travel details",
    "confirmationState": "pending"
  },
  "inclusions": ["Return air travel", "Visa support"],
  "exclusions": ["Personal expenses"],
  "pricing": {
    "currency": "INR",
    "occupancies": [
      {
        "occupancy": "double",
        "amount": 110000.00,
        "availableQuantity": 10,
        "status": "available"
      },
      {
        "occupancy": "triple",
        "amount": 100000.00,
        "availableQuantity": 0,
        "status": "unavailable"
      }
    ]
  }
}
```

The API exposes no price-version IDs, package-template/version IDs, staff identities, review/audit information, inventory adjustment reasons or unpublished data.

## Failure contract

- `200` for a currently saleable published departure.
- `404` with the same public response for unknown, unpublished, operator-ineligible, commercially inconsistent or no-longer-saleable departures.
- `5xx` with the existing safe error behavior for unexpected server failure.
- Correlation ID remains available through the existing response header for retry/support UX.

## UX / design boundary

The approved NoorPath Package reference is the visual authority. VS-06 is a truth migration, not a redesign.

Required content changes:

- replace static preview/operator claims with authoritative operator and package facts;
- replace fixed Quad Sharing text with factual accommodation data and separate occupancy pricing/availability;
- replace fabricated day-by-day itinerary copy with the published travel route/details plus factual dates and stay durations;
- replace placeholder payment amounts with published occupancy prices; no deposit, instalment or due-now amount is invented before VS-07;
- render full ordered inclusions and exclusions;
- keep unresolved cancellation/refund/payment-plan policy wording explicitly pending before commitment rather than inventing terms;
- preserve header, gallery, panel hierarchy, footer/sticky-action treatment and overall responsive visual language unless a minimum accessibility/state adjustment is required.

Required states:

- loading: preserve the detail-page footprint and announce loading;
- populated: all authoritative facts are visible with confirmed/pending labels where relevant;
- not found/unavailable: calm explanation with route back to published packages;
- error/offline: explain that the package could not be loaded and offer retry;
- mobile: operator, price, availability and next-action context remain visible at 390/360 px without horizontal scrolling.

UI UX Pro Max governs state quality, accessibility and responsive behavior. Impeccable and Emil are bounded refinement only. NoorPath references and `design-system/MASTER.md` remain visually authoritative. Ponytail governs implementation simplicity after those constraints.

## Security / privacy

- Endpoint is intentionally public and read-only.
- Re-check operator eligibility and saleability before returning details.
- Fail closed for cross-module inconsistencies.
- Do not expose internal identifiers beyond the already-public departure/operator IDs required by the public contract.
- Do not expose staff/account IDs, audit records, mutable draft pricing, internal versions or adjustment reasons.
- Log only safe outcome, duration and correlation context; do not log package free text or commercial payloads.

## Telemetry

Record safe detail-query outcome (`success` / `not_found`), duration and correlation ID. Avoid logging price payloads, accommodation/travel free text, staff identities or inventory adjustment reasons.

## Test matrix

### API / integration

1. Anonymous caller can load a valid published departure detail.
2. Unknown, draft and submitted departures return public 404.
3. Published departure becomes 404 when operator is no longer eligible.
4. Immutable published PriceVersion supplies occupancy prices and currency.
5. Mutable draft pricing changes do not rewrite published detail pricing.
6. Current inventory is matched per occupancy and zero quantity is represented as unavailable.
7. Departure becomes 404 when no occupancy is saleable.
8. Makkah, Madinah, travel, ordered inclusions and ordered exclusions serialize from Catalogue.
9. No private/admin fields appear in the public DTO.

### Web

1. Package route no longer depends on `public-package-preview` for commercial/package truth.
2. Loading state is announced and preserves usable page structure.
3. Populated state shows operator, dates, both stays, travel, inclusions/exclusions, every published occupancy price and availability.
4. Pending accommodation/travel facts are labelled pending rather than implied confirmed.
5. Unknown/non-saleable package shows a calm not-found/unavailable state with path back to packages.
6. Network/error state offers retry and may expose safe correlation reference.
7. No fabricated IATA/ISO/experience, day-by-day itinerary, deposit/instalment or cancellation entitlement claims remain.
8. Keyboard, visible focus, 44 px targets, reduced motion, 200% text zoom and 390/360 px reflow pass.
9. Approved Package page visual composition remains recognizably unchanged except where static placeholder content is replaced by authoritative facts/states.

## Acceptance criteria

1. A customer can open a discovery card and receive authoritative detail for that exact currently saleable departure.
2. Unpublished, ineligible or no-longer-saleable departures cannot leak through the detail route.
3. Accommodation and travel facts preserve explicit confirmation state.
4. All published supported occupancy prices show explicit currency and current availability.
5. The UI does not invent commercial, operator-credential, itinerary or policy claims.
6. The approved Package visual language remains the source of truth.
7. Loading, not-found/unavailable, error/retry and mobile states are accessible.
8. Applicable integration, frontend, E2E, accessibility, formatting/build and CI gates pass.
9. Product Owner reviews rendered desktop/mobile evidence before merge.

## Rollback / migration notes

VS-06 should not require a database migration. It is a read-only composition over Catalogue, Operators, Pricing and Inventory state already created by VS-02 through VS-05. If implementation discovers a need for new persisted package-detail facts, stop and resolve the owning domain/policy before adding schema.
