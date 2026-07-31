# VS-07 Travellers & Authoritative Quote — Implementation Checklist

Status: In progress

## Baseline / governance

- [x] Verify latest upstream `main` before starting VS-07.
- [x] Create VS-07 branch from exact latest `main` SHA.
- [x] Read UI UX Pro Max skill and `design-system/MASTER.md`.
- [x] Define product/domain/API/UX scope before implementation.
- [ ] Keep PR Draft until every gate below is complete.

## Traveller capability

- [ ] Add `NoorPath.Traveller` domain project.
- [ ] Add `NoorPath.Traveller.Infrastructure` persistence project/schema.
- [ ] Implement minimum Traveller validation (full name + DOB only).
- [ ] Implement authenticated create/list endpoints.
- [ ] Enforce account ownership / cross-account hidden access.
- [ ] Add deterministic migration + migration validation.
- [ ] Add domain/integration tests.

## Pricing / payment-plan policy

- [ ] Add optional payment-plan definition to mutable Pricing plan.
- [ ] Validate deposit %, instalment day and final-payment deadline offset.
- [ ] Extend operator Pricing editor without redesigning admin UI.
- [ ] Snapshot payment-plan fields into immutable published PriceVersion.
- [ ] Preserve existing published prices with no plan as full-payment quotes.
- [ ] Add/update migration and Pricing tests.

## Authoritative Quote

- [ ] Add Pricing-owned immutable Quote persistence.
- [ ] Store Traveller IDs only, never Traveller PII in Pricing.
- [ ] Implement published PriceVersion + saleability/current availability guards.
- [ ] Enforce `double=2`, `triple=3`, `quad=4` Traveller count.
- [ ] Enforce VS-07 adult-only policy on departure date.
- [ ] Calculate total, due-now, remaining and future instalments server-side.
- [ ] Ensure rounding reconciles exactly.
- [ ] Apply explicit 30-minute quote expiry.
- [ ] Implement owner-only quote retrieval and expired projection.
- [ ] Prove quote creation does not create Inventory hold/reservation.
- [ ] Add API/domain/integration tests.

## Customer UX

- [ ] Add clear progression CTA from Package Details.
- [ ] Add `/packages/{departureId}/plan` route.
- [ ] Preserve package/departure context.
- [ ] Room/occupancy selection.
- [ ] Add/select minimum Traveller profiles.
- [ ] Render quote values exclusively from authoritative API response.
- [ ] Explain quote expiry and that inventory is not reserved yet.
- [ ] Loading/auth/validation/error/expired/unavailable states.
- [ ] 390px / 360px / desktop responsive review.
- [ ] Keyboard, focus, target size, screen-reader labels, 200% text and reduced-motion checks.

## Quality / delivery

- [ ] Architecture boundaries pass.
- [ ] Node format/check/test/build pass.
- [ ] .NET format/build/tests pass.
- [ ] PostgreSQL/migration validations pass.
- [ ] Secret scan passes.
- [ ] Latest branch CI green.
- [ ] Deploy exact tested product head to Netlify preview with demo-only fixtures isolated from PR/main where required.
- [ ] Verify Landing/Package regressions did not reappear.
- [ ] Product Owner reviews rendered VS-07 customer flow.
- [ ] Record acceptance evidence.

## Merge gate

**DO NOT MERGE until every required VS-07 gate is complete, including Product Owner acceptance.**
