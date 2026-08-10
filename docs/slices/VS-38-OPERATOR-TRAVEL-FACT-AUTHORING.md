# VS-38 — Operator Airline & Airport Fact Authoring

## Outcome

Authorized NoorPath operator staff can record structured airline, flight-leg and airport facts for an existing private package departure without guessing supplier data. Existing Makkah and Madinah accommodation authoring remains unchanged and authoritative.

## Governing evidence

- `AGENTS.md` — current NoorPath engineering and UI governance.
- `design-system/MASTER.md` — approved NoorPath visual language and accessibility baseline.
- `docs/01-product/BUSINESS-RULES.md` — pending facts remain explicitly pending; accommodation claims use platform/operator facts.
- `docs/01-product/CONTROLLED-PILOT-PLAN.md` — pilot package facts require truthful airline/flight status and independent Makkah/Madinah hotel details.
- VS-37 handoff — operator hotel/airport/airline/flight-leg authoring follows in VS-38.

No new PRD identifier is invented because the current repository does not expose a more specific approved requirement ID for this post-pilot slice.

## Architecture

### Catalogue domain

`PackageTravelFactsDraft` owns validation and normalization for an ordered list of `FlightLegDraft` values.

Each leg records:

- airline name;
- optional airline code;
- flight number;
- departure airport name and code;
- arrival airport name and code; and
- `pending` or `confirmed` state.

Pending legs may contain partial facts. Confirmed legs require airline name, flight number, and both airport names/codes. The aggregate accepts at most eight legs as an input-defense boundary.

### Persistence

The existing `catalogue.package_versions` record receives:

- `TravelFactsJson` (`jsonb`) containing the validated ordered fact set; and
- `TravelFactsVersion` as an independent optimistic-concurrency token.

This is deliberately scoped to the existing Catalogue package version rather than adding a new subsystem. Independent travel-fact concurrency prevents a travel save from making the mature departure composer stale.

Forward-only migration: `20260810222500_VS38PackageTravelFacts`.

### API

`GET /api/v1/operator/departures/{departureId}/travel-facts`

Returns the tenant-scoped fact set, independent fact version, and whether the departure is editable.

`PUT /api/v1/operator/departures/{departureId}/travel-facts`

Accepts `expectedVersion` and the ordered leg list. It:

1. resolves the signed-in account server-side;
2. requires active operator membership with operator administration permission;
3. resolves the departure only inside that operator tenancy;
4. rejects non-draft writes;
5. validates and normalizes the fact set;
6. rejects stale fact versions;
7. writes an audit row and safe structured telemetry; and
8. never logs airline/airport values or other mutable user-entered details.

### Operator web

Existing departure authoring remains at `/operator/departures/{departureId}`. A reachable action opens:

`/operator/departures/{departureId}/travel-facts`

The screen provides:

- loading and safe access states;
- truthful empty state;
- add/remove ordered leg controls;
- explicit Pending/Confirmed selection per leg;
- server validation feedback;
- independent stale-write recovery;
- read-only state after review/publication;
- back-to-draft and preview navigation; and
- unsaved-change browser protection.

The screen uses the approved NoorPath ivory/raised/green/gold token language, existing operator shell, visible focus, minimum target sizes and responsive single-column reflow.

## Provider boundary

No Google Places, Google Routes, Amadeus, airline, airport, schedule, live-status, PNR or ticketing provider is configured in NoorPath's governed environment baseline. VS-38 therefore authors operator-supported facts only.

A future provider integration must define its own secrets, contracts, rate limits, data provenance, fallback behavior, privacy review and approval boundary. It must not silently replace operator facts.

## Existing hotel boundary

VS-38 does not create another hotel model. Makkah and Madinah hotel name, classification, distance disclosure, nights and confirmation state continue to be authored by the existing Catalogue departure composer.

## Data states and truthfulness

- **No legs:** no flight facts have been recorded.
- **Pending:** partial facts may be preserved; UI states that verification is incomplete.
- **Confirmed:** all required operator-supported facts are present.
- **Locked:** departure is ready for review or published; no mutation is offered.
- **Conflict:** another travel-fact write changed the independent version; reload is required.
- **Provider unavailable:** not applicable because VS-38 does not claim provider lookup.

## Security and privacy

- deny by default through existing authentication middleware;
- server-side operator resolution only;
- safe not-found for cross-tenant identifiers;
- no client-supplied operator identifier;
- bounded string lengths and bounded leg count;
- no sensitive or user-entered travel details in telemetry;
- database write guarded by optimistic concurrency.

## Verification

Deterministic evidence includes:

- `tests/NoorPath.Catalogue.Tests/TravelFactsTests.cs` for pending/confirmed validation and normalization;
- migration/model-snapshot consistency;
- `apps/web/e2e/operator-travel-facts.spec.ts` for rendered authoring, save payload, accessibility, target sizes and overflow; and
- navigation evidence in `VS-38-NAVIGATION-VERIFICATION.md`.

Exact-head CI, rendered review, navigation reachability and Product Owner screenshot acceptance remain required before merge.

## Explicit exclusions

- external provider lookup/enrichment;
- live flight/schedule/terminal facts;
- PNR or ticketing;
- duplicate hotel authoring;
- public package commercial-row changes;
- production deployment without explicit authorization.
