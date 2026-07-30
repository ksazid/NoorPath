# VS-05 — Implementation Checklist

## Contract

- [x] Slice outcome, actor, rules, exclusions and acceptance criteria specified.
- [x] Public discovery API shape specified.
- [x] Headline price and availability derivation specified.
- [x] Privacy/security boundary specified.
- [x] Existing Landing visual authority preserved.

## Backend

- [ ] Add unauthenticated `GET /api/v1/departures` endpoint.
- [ ] Compose published Catalogue facts with current operator eligibility, immutable published Pricing and current Inventory.
- [ ] Fail closed on inconsistent/non-saleable rows.
- [ ] Add deterministic ordering and bounded results.
- [ ] Add safe telemetry.

## Web

- [ ] Replace placeholder package-card data with VS-05 discovery data.
- [ ] Preserve approved Landing composition.
- [ ] Add loading state.
- [ ] Add empty state.
- [ ] Add retry/error state.
- [ ] Keep package CTA routing by `departureId`.

## Verification

- [ ] API/integration coverage for visibility, eligibility, published price and availability rules.
- [ ] Frontend unit coverage for populated/loading/empty/error states.
- [ ] Playwright customer discovery coverage.
- [ ] 390 px and 360 px responsive evidence.
- [ ] Keyboard/accessibility check.
- [ ] Formatting, TypeScript, lint, production build, .NET build/test, migration-state checks and secret scanning green.
- [ ] Product Owner visual acceptance before merge.
