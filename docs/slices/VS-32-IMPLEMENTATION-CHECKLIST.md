# VS-32 Implementation Checklist

## Specify / Design

- [x] Preserve VS-02 ordered inclusion/exclusion string contract.
- [x] Preserve VS-31 shared operator shell and NoorPath visual authority.
- [x] Treat current market/category research as advisory suggestions only.
- [x] Use native drag-and-drop with a complete visible non-drag alternative.
- [x] Reuse the existing authored NoorPath SVG line-icon family; add no UI dependency.

## Build

- [x] Render explicit Included and Not included selected boards.
- [x] Enforce mutually exclusive move behavior in the authoring UI.
- [x] Support pointer drag between selected boards.
- [x] Provide keyboard/touch Move actions for every selected item.
- [x] Keep variable package/travel/Umrah-kit items as operator-selected suggestions.
- [x] Add custom-item entry with curated existing icon choices and destination choice.
- [x] Announce move/add results through a polite live region.
- [x] Preserve existing draft POST payload and failure behavior.
- [x] Preserve visible focus, 44px targets, reduced motion and 390px reflow.
- [x] Add rendered/navigation E2E coverage.

## Safety

- [x] No operator permission or tenant-isolation change is in scope.
- [x] No Catalogue schema/API compatibility change is in scope.
- [x] No scraped competitor promise becomes NoorPath commercial policy.
- [x] No pricing, inventory, publication, booking or payment policy changes are in scope.
- [x] Production deployment remains separately authorized.

## Verify / Close

- [ ] CI exact head passes.
- [ ] Slice Governance exact head passes.
- [ ] Rendered Slice Review exact head passes.
- [ ] Navigation Reachability exact head passes.
- [ ] Desktop drag and visible Move alternatives are verified.
- [ ] 390px composer has no horizontal overflow and actionable targets remain usable.
- [ ] Standing Product Owner authorization is applied only to the final certified SHA.
- [ ] Any post-ready / post-label required checks pass on the unchanged exact head.
- [ ] Merge to `main` only after all required exact-head gates pass.
