# VS-35 — Package Details Conversion UX Implementation Checklist

## Development mode
- [ ] Keep the PR Draft while implementation is changing.
- [ ] Do not apply the `certify` label until implementation and source review are complete.
- [ ] Do not merge or deploy without exact-head governance gates and explicit Product Owner authorization.

## Product completeness
- [ ] Same-origin date scroller shows current, available and sold-out departures truthfully.
- [ ] Adult guest and room-sharing controls use only supported current occupancy rules.
- [ ] Payment summary uses server-authoritative immutable published pricing and payment-plan facts.
- [ ] Milestone and Pay Later views reconcile to the same total, due-now, remaining and final deadline.
- [ ] Journey is generated only from stored departure, stay and travel facts.
- [ ] Standard inclusions/exclusions use NoorPath semantic icons and package/travel-kit/Umrah-kit grouping.
- [ ] Confirmed/pending and cancellation/payment disclosures remain factual.
- [ ] Book now preserves occupancy/payment-mode selection into the existing planner.
- [ ] Child/infant pricing/configuration, detailed itinerary persistence and auth-flow rewrite remain excluded.

## Certification gates
- [ ] Slice governance.
- [ ] Formatting.
- [ ] Type checking, linting and static analysis.
- [ ] Unit tests.
- [ ] Integration and contract tests.
- [ ] Architecture and ownership tests.
- [ ] Deterministic migration and clean-database validation; no migration is expected.
- [ ] Authentication, authorization, privacy and secret scanning.
- [ ] Keyboard, focus, semantics, target size and axe checks.
- [ ] Desktop, mobile, 200% text and reduced-motion rendered evidence.
- [ ] Route and customer-journey linking.
- [ ] Navigation reachability.
- [ ] Telemetry and safe failure evidence.
- [ ] Design-token consistency.
- [ ] Product Owner acceptance on the exact certified SHA.

## Final merge gate
- [ ] Full CI passed on the exact final SHA.
- [ ] Rendered Slice Review passed on the exact final SHA.
- [ ] Evidence artifact and certification comment reference the exact final SHA.
- [ ] No unresolved review thread or known regression remains.
- [ ] `po-approved` is present only after Product Owner review.
- [ ] NoorPath Merge Gate is successful.
