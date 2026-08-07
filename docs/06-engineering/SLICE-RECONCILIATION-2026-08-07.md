# NoorPath Slice Reconciliation — 2026-08-07

## Purpose
Restore the formal `VS-xx` delivery contract after several follow-on PRs were named as informal UI/workspace refinements and were merged while certification-gated workflows were skipped.

This document does not rewrite Git history and does not claim skipped checks passed. It maps the merged changes back to their owning vertical slices and defines the mandatory recertification boundary before further product work is merged.

## Important correction: `completed-slices.json`
`delivery/completed-slices.json` is not a general progress ledger. It currently records VS-00 through VS-08 because those legacy completed slices pre-date the manifest-driven registry. `scripts/slice.mjs` uses that file so newer manifests can resolve dependencies on slices that do not have registry manifests. VS-09 and later are represented by `delivery/slices/VS-xx.json` and therefore should not be added to `completed-slices.json` merely because they were implemented.

## Registered implementation status

| Slice | Outcome | Implementation state | Current-main certification state |
| --- | --- | --- | --- |
| VS-00–VS-08 | Foundation through Inventory Hold | Implemented/merged | Historical completion retained |
| VS-09 | Booking & Payment | Implemented/merged | Historically certified; include in integrated regression |
| VS-10 | Confirmation | Implemented/merged and post-merge hardened | Historically certified; include in integrated regression |
| VS-11 | My Journey | Implemented/merged | Include in integrated customer regression |
| VS-12 | Documents | Implemented/merged | Include in identity/document regression |
| VS-13 | Visa Tracking | Implemented/merged | Include in identity/operator regression |
| VS-14 | Operational Support | Implemented/merged | **RECERTIFICATION REQUIRED** because PR #91 added a new booking-management operational entry/workspace and cross-module projection links |
| VS-15 | Family Booking & Mahram Linking | Implemented/merged | Historically certified; include in integrated customer regression |
| VS-16 | Cancellation & Refunds | Implemented/merged with navigation evidence | Historically certified; include in integrated regression |
| VS-17 | Production Readiness | Implemented/merged | **RECERTIFICATION REQUIRED** after all later product/auth/design changes; historical production-readiness evidence cannot certify current main |
| VS-18 | Design System Foundation | Implemented/merged | Include in current rendered regression |
| VS-19 | Customer Shell and Navigation Adoption | Implemented/merged | Include in current customer navigation regression |
| VS-20 | Identity Test Environment Completion | Implemented/merged | Demo-identity prerequisites exist; current identity smoke evidence must be refreshed |
| VS-21 | Auth0 Session Establishment Repair | Implemented/merged | Include in current protected-route regression |
| VS-22 | Auth0 API Token Handoff Repair | Implemented/merged in PR #82 | **REGISTRY REPAIRED HERE; CURRENT CERTIFICATION REQUIRED** |
| VS-23 | Operator Draft Package Builder | Implemented/merged | **RECERTIFICATION REQUIRED** after PRs #84–#90 changed the governed package-authoring/publishing surface |

## Reconciliation of informal PRs

| PR | Merged change | Owning / impacted slice contract |
| --- | --- | --- |
| #84 | Package inclusions/exclusions refinement | VS-23 |
| #85 | Journey, stays and intercity draft refinement | VS-23 |
| #86 | Pricing & occupancy presentation | VS-23 workflow; VS-03 commercial presentation contract impacted, domain unchanged |
| #87 | Payment milestones presentation | VS-23 workflow; existing Pricing/payment-plan behavior unchanged |
| #88 | Customer preview and publication handoff | VS-23 and VS-04 |
| #89 | Platform publication approval experience | VS-04; VS-23 handoff |
| #90 | Operator package management workspace | VS-23 package-library/continuation surface |
| #91 | Operator booking management workspace | VS-14 operational workspace/composition surface |

These mappings are traceability repairs only. They do not convert skipped CI, Rendered Slice Review or Navigation Reachability Review into successful evidence.

## Current mandatory certification boundary
Before another product slice may merge, create one certification PR from current `main` whose only purpose is to prove the integrated state. Apply `certify` before merge and require all applicable workflows to execute rather than skip.

Minimum evidence:

1. Slice Governance succeeds on the exact head.
2. Full CI succeeds on the exact head.
3. Navigation Reachability Review executes and succeeds; `skipped` is not acceptable.
4. Rendered Slice Review executes and succeeds for changed UI surfaces; `skipped` is not acceptable.
5. Demo customer, approved operator and Platform Administrator identities are exercised against protected routes.
6. Customer public/authenticated/transactional navigation is exercised on desktop and mobile.
7. Operator navigation covers Overview, Packages, Departures, Bookings, Visa, Support, Cancellations and Account, including direct/deep links.
8. Package authoring covers create/clone -> commercial setup -> payment plan -> customer preview -> publication review/approval.
9. Booking operations cover booking list -> linked departure/documents/visa/support/cancellation destinations without authorization bypass.
10. Platform Administrator publication review and role separation are verified.
11. Auth0 session establishment and server-side API token handoff are verified without client token exposure.
12. No unresolved review thread or known regression remains.
13. Product Owner approval is bound to the exact unchanged certified SHA.
14. Only after all above are green may the certification PR merge. Deployment remains separately approved.

## Next-slice rule
No future feature should be called simply “Booking Detail”, “Package Management”, “UI refinement”, or similar if it adds product behavior beyond an existing acceptance contract.

- If work is a bounded correction/refinement inside an existing slice, branch/PR naming must identify that owning slice, for example `slice/vs23-...` and `VS-23: ...`.
- If work introduces a new end-to-end outcome not already covered by an existing slice, register the next formal slice (`VS-24` or later) with manifest, specification, checklist and navigation contract before implementation.
- Product Owner approval never waives required certification gates.
- A required workflow marked `skipped`, `cancelled`, `timed_out` or unavailable is not a pass and blocks merge until the gate is successfully executed or the governing contract is explicitly changed in a separately reviewed governance change.
