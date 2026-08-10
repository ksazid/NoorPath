# VS-38 — Operator Travel Fact Navigation Verification

## Required path

`Operator -> Departures -> /operator/departures/{departureId} -> Airline & airport facts -> /operator/departures/{departureId}/travel-facts -> add/edit/remove flight legs -> Save travel facts -> Back to package draft or Preview package`

## Verification matrix

| From | Action | Expected destination/state | Outcome |
| --- | --- | --- | --- |
| Existing departure authoring | Activate `Airline & airport facts` | `/operator/departures/{departureId}/travel-facts` | VERIFIED |
| Travel facts | Initial load with no facts | truthful empty state with `Add flight leg` | VERIFIED |
| Travel facts | Add flight leg | one editable pending leg appears | VERIFIED |
| Pending leg | Save partial supported facts | partial values remain pending; missing values are not invented | VERIFIED |
| Confirmed leg | Save without required airline/flight/airport facts | 422 validation feedback is shown | VERIFIED |
| Confirmed leg | Save complete supported facts | independent travel-fact version increments and success state renders | VERIFIED |
| Stale editor | Save older expected version | conflict state requires reload | VERIFIED |
| Ready-for-review/published departure | Open travel facts | existing facts render read-only | VERIFIED |
| Travel facts | Activate `Back to package draft` | `/operator/departures/{departureId}` | VERIFIED |
| Travel facts | Activate `Preview package` | `/operator/departures/{departureId}/preview` | VERIFIED |

## Security reachability

- The UI never accepts an operator identifier.
- API tenancy is resolved from active operator membership.
- A departure outside the resolved operator scope returns not found.
- The route requires the same operator administration permission as Catalogue draft authoring.

## Rendered checks

- Existing operator shell and NoorPath token language are reused.
- Interactive controls meet the 44px minimum target baseline.
- Focus treatment is visible for links, buttons and form controls.
- Desktop uses two-column fact groups; narrow viewports reflow to one column.
- Pending/confirmed meaning is textual and never depends on colour alone.
- Empty, error, saved, conflict and locked states are visible and actionable.
- Reduced-motion preference introduces no required motion.

## Evidence

- `apps/web/e2e/operator-travel-facts.spec.ts` verifies route rendering, empty state, add/edit/save flow, exact API payload, pending partial facts, accessibility, target sizes and horizontal overflow.
- `tests/NoorPath.Catalogue.Tests/TravelFactsTests.cs` verifies pending/confirmed domain semantics and normalization.
- Exact-head CI and rendered/navigation workflow results remain required before Product Owner acceptance.
