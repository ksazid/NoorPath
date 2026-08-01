# VS-11 — My Journey Implementation Checklist

## Development mode
- [ ] VS-10 is merged and customer-safe projections are stable.
- [ ] Keep the PR Draft while implementation is changing.
- [ ] Apply `certify` only after every authoritative, empty, delayed and error state is complete.

## Contract and ownership
- [ ] Booking, Payments, Catalogue and Traveller projections are explicit and read-only.
- [ ] Account scope is enforced server-side for every list/detail query.
- [ ] Documents and visa remain truthful future-capability placeholders.
- [ ] Support context excludes unnecessary personal or payment data.

## Product completeness
- [ ] Account-owned journey list and empty state are complete.
- [ ] Confirmed booking dashboard presents authoritative journey and commercial facts.
- [ ] Payment state and instalment schedule are authoritative.
- [ ] Loading, delayed projection, unavailable, error and safe not-found states are complete.
- [ ] Approved NoorPath header, footer, layout and visual language are preserved.
- [ ] Landing, Discovery, Package, Plan, Booking and My Journey are interlinked.
- [ ] Support entry carries safe booking context.

## Accessibility and responsive behaviour
- [ ] Keyboard order and visible focus are correct.
- [ ] Semantic headings, landmarks and live statuses are correct.
- [ ] Interactive targets meet 44px minimum.
- [ ] Desktop, 390px, 360px and 200% text have no horizontal overflow.
- [ ] Reduced motion is respected.
- [ ] Axe checks and rendered regression evidence pass.

## Certification gates
- [ ] Formatting and static analysis pass.
- [ ] Unit, integration, contract and architecture tests pass.
- [ ] Migration validation passes or the slice explicitly proves no migration is introduced.
- [ ] Authentication, authorization, privacy and secret scanning pass.
- [ ] Route graph and journey-linking tests pass.
- [ ] Logs, traces, metrics and projection-delay evidence are verified.
- [ ] Product Owner accepts the exact certified SHA.

## Final merge gate
- [ ] Full CI passed on the exact final SHA.
- [ ] Rendered Slice Review passed on the exact final SHA.
- [ ] Evidence artifact and certification comment reference the exact final SHA.
- [ ] No unresolved review thread or known regression remains.
- [ ] `po-approved` is present only after Product Owner review.
- [ ] NoorPath Merge Gate is successful.
