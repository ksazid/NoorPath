# VS-05 — Implementation Checklist

## Contract

- [x] Slice outcome, actor, rules, exclusions and acceptance criteria specified.
- [x] Public discovery API shape specified.
- [x] Headline price and availability derivation specified.
- [x] Privacy/security boundary specified.
- [x] Existing Landing visual authority preserved.

## Backend

- [x] Add unauthenticated `GET /api/v1/departures` endpoint.
- [x] Compose published Catalogue facts with current operator eligibility, immutable published Pricing and current Inventory.
- [x] Fail closed on inconsistent/non-saleable rows.
- [x] Add deterministic ordering and bounded results.
- [x] Add safe telemetry.

## Web

- [x] Replace placeholder package-card data with VS-05 discovery data.
- [x] Preserve approved Landing composition.
- [x] Add loading state.
- [x] Add empty state.
- [x] Add retry/error state.
- [x] Keep package CTA routing by `departureId`; authoritative package-detail rendering remains VS-06.

## Verification

- [x] Core API/integration coverage for publication visibility, operator eligibility, published snapshot price and availability.
- [ ] Add explicit edge coverage for no-saleable-occupancy, deterministic/bounded results and public-response privacy.
- [ ] Add frontend unit coverage for populated/loading/empty/error states.
- [x] Playwright customer-discovery scenarios authored.
- [ ] Execute browser E2E and capture rendered evidence at desktop, 390 px and 360 px.
- [ ] Complete keyboard/accessibility browser verification from the rendered build.
- [x] CI run #279: formatting, TypeScript/ESLint, frontend unit tests, Next.js production build, .NET formatting/build/tests, migration-state checks, PostgreSQL validation and secret scanning are green.
- [ ] Product Owner visual acceptance before merge.

## Known slice boundary

The VS-05 Landing cards route to `/packages/[departureId]`, but the existing package-detail surface is not yet backed by the authoritative public contract for arbitrary published departures. That migration belongs to VS-06. Do not treat the current static preview detail data as authoritative VS-05 output, and do not merge/deploy VS-05 as a complete customer browse-to-detail journey until this boundary is accepted or VS-06 follows immediately.
