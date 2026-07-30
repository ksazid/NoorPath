# VS-05 — Customer Discovery

Status: Specification

## Outcome

A public customer can browse currently saleable published Umrah departures and see a truthful headline price and availability state without signing in.

## Actor

Public customer. Authentication is not required for discovery.

## Traceability

- `INV-OP-001`
- `INV-CAT-003`–`INV-CAT-006`
- `INV-ACC-001`–`INV-ACC-003`
- `INV-PRI-002`, `INV-PRI-006`
- `INV-INV-001`, `INV-INV-003`
- `POL-INV-002`
- UX baseline: Public Customer / Package listing
- API & Application Contract Baseline
- MVP Vertical Slice Map VS-05
- VS-04 Review & Publish

No stable approved PRD requirement ID is invented here.

## Dependencies

- VS-04 publication is complete.
- A published departure carries the immutable `PublishedPriceVersionId` created by Pricing.
- Catalogue publication requires matching positive Pricing and Inventory occupancy facts.
- Operator publication eligibility is re-evaluated at read time so an operator that is no longer approved is not exposed as a saleable public offer.

## Included

- unauthenticated public departure-list query;
- only `Published` Catalogue departures whose package version is also `Published`;
- only departures owned by an operator currently eligible for public sale;
- published immutable occupancy prices from the departure's `PublishedPriceVersionId`;
- current Inventory availability for the same occupancy keys;
- headline starting price derived from the lowest currently available published occupancy price;
- explicit ISO currency and the occupancy that produced the starting price;
- package title, operator identity, origin, departure/return dates, duration, Makkah/Madinah stay summary, travel confirmation state, inclusion highlights, price and availability needed by the approved Landing package cards;
- deterministic ordering and a bounded result set;
- loading, empty, error and populated customer states;
- responsive and accessibility verification for the existing approved Landing package section.

## Excluded

- package-detail migration to authoritative public data (VS-06);
- advanced search/filtering;
- recommendations;
- package comparison;
- traveller-count pricing;
- quotes;
- inventory holds;
- booking/payment actions;
- limited-availability urgency labels;
- cancellation/archival workflows not yet defined by an approved lifecycle;
- new customer visual identity or redesign of the approved Landing page.

## Public saleability rules

A departure is returned only when all of the following remain true at query time:

1. Catalogue departure status is `Published`.
2. Its PackageVersion status is `Published`.
3. The owning operator is currently approved for publication.
4. `PublishedPriceVersionId` exists and resolves to the immutable Pricing snapshot for that departure/operator.
5. At least one occupancy has a positive published price and a matching current Inventory pool with positive available quantity.

The query fails closed for inconsistent commercial data; invalid or non-saleable rows are not exposed as public offers.

## Headline price rule

The card's headline price is the minimum amount among occupancies that are both:

- present in the immutable published PriceVersion; and
- currently available in Inventory.

The response always includes the explicit currency and occupancy key. The client formats money for presentation and must not derive or alter authoritative amounts.

## Availability rule

For VS-05, Inventory does not yet have holds/reservations. Current available quantity is therefore the authoritative configured capacity for each occupancy pool.

The public contract exposes occupancy availability facts. The customer card uses a calm factual `Available` label when at least one saleable occupancy exists. It does not invent scarcity/urgency wording; any future `Limited` state requires the explicit threshold policy from `POL-INV-002`.

## API

### `GET /api/v1/departures`

Authentication: none.

The endpoint returns a bounded list ordered by `departureDate`, then `departureId`.

Response shape:

```json
{
  "items": [
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
      "travelConfirmationState": "pending",
      "inclusionHighlights": ["Return air travel", "Visa support"],
      "headlinePrice": {
        "amount": 80000.00,
        "currency": "INR",
        "occupancy": "quad"
      },
      "availability": {
        "status": "available",
        "occupancies": [
          {
            "occupancy": "quad",
            "availableQuantity": 20
          }
        ]
      }
    }
  ]
}
```

No internal package-template IDs, audit information, actor IDs, unpublished price-plan data or inventory-adjustment reasons are exposed.

## Failure contract

- `200` with `items: []` when no saleable published departures exist.
- `5xx` with standard Problem Details for unexpected server failure.
- No authentication-specific customer state is introduced for this public endpoint.
- Correlation ID is returned through the existing response header and may be surfaced in the retry/error UI where useful.

## UX / design boundary

The approved NoorPath Landing page remains the primary visual source of truth. VS-05 binds the existing package-card composition to authoritative public data rather than redesigning it.

Required states:

- loading: reserve the package-grid footprint and communicate that published journeys are loading;
- populated: preserve the approved card hierarchy while replacing placeholder commercial text with authoritative operator, price and availability facts;
- empty: calm message that no published journeys are currently available, without fake examples;
- error/offline: explain that journeys could not be loaded and provide a retry action;
- mobile: price, availability and primary `View package` action remain visible and operable at 390/360 px with no horizontal scrolling.

UI UX Pro Max is used for accessibility, responsive behaviour, hierarchy and state-quality review only. It must not replace the established NoorPath visual identity. Any visual refinement remains subordinate to the approved Landing/Package references and `design-system/MASTER.md`.

## Security / privacy

- Endpoint is intentionally public and read-only.
- Expose only public commercial facts required for discovery.
- Re-check current operator eligibility before exposing the offer.
- Do not expose unpublished drafts, internal staff/account identifiers, audit records, adjustment reasons or privileged review state.
- Add reasonable public-query abuse protection only using existing platform mechanisms; do not introduce new infrastructure solely for this slice.

## Telemetry

Record safe query outcome, item count, duration and correlation ID. Do not log price payloads, package free text, operator staff identities, audit data or inventory adjustment reasons.

## Test matrix

### API / integration

1. Anonymous caller can list a valid published departure.
2. Draft and `ReadyForReview` departures are absent.
3. A published departure whose operator is no longer eligible is absent.
4. Published price snapshot, not mutable draft pricing, supplies the headline price.
5. Headline price uses the lowest occupancy that currently has matching positive availability.
6. A published row with no saleable occupancy is absent.
7. Currency and occupancy serialize as explicit stable values.
8. Results are deterministic and bounded.
9. No private/admin fields appear in the public DTO.

### Web

1. Loading state is accessible and does not collapse the package section.
2. Populated cards expose title, operator, dates, stay summary, starting price, availability and detail CTA.
3. Empty state contains no fabricated packages.
4. Network/error state offers retry and does not replace the page with a generic failure screen.
5. Keyboard, visible focus, screen-reader labels, reduced-motion behaviour and 390/360 px reflow pass.
6. Landing header, hero, trust row, service strip and footer remain visually unchanged except where authoritative package-card content replaces placeholders.

## Acceptance criteria

1. A public customer can browse only currently saleable published departures without authentication.
2. Draft/submitted/non-eligible offers cannot leak into discovery.
3. Every displayed starting price is derived from the immutable published PriceVersion and includes explicit currency.
4. The starting price corresponds to an occupancy with current positive availability.
5. Availability comes from Inventory and is not inferred by Catalogue or the UI.
6. The approved Landing package-card visual language is preserved.
7. Loading, empty, error and mobile states are implemented and accessible.
8. Applicable unit, PostgreSQL integration, frontend, E2E, accessibility, formatting/build and CI gates pass.
9. Product Owner reviews rendered desktop/mobile evidence before the slice is merged.

## Rollback / migration notes

VS-05 should not require a database migration. It is a read-only composition over state already owned by Catalogue, Operators, Pricing and Inventory. If implementation discovers that a new persisted projection is required, stop and justify it before adding schema or asynchronous projection infrastructure.
