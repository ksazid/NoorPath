# VS-33 Implementation Checklist

## Specify / Design

- [x] Preserve Catalogue as authority for package/departure relationship.
- [x] Preserve VS-27 as authority for traveller readiness.
- [x] Treat group leader as optional departure operational metadata, not a traveller.
- [x] Reuse the existing handover operational record/version/audit model instead of adding a new aggregate.
- [x] Collect only the group-leader name required by the approved slice.

## Build

- [ ] Add nullable group-leader name to Booking handover persistence.
- [ ] Add forward-only Booking migration and model snapshot.
- [ ] Add operator-isolated, version-checked group-leader endpoint.
- [ ] Append audit evidence for set/update/clear operations.
- [ ] Block mutation after final handover completion.
- [ ] Expose group leader in manifest and handover responses.
- [ ] Add `View package being fulfilled` link to manifest and handover.
- [ ] Add accessible manifest add/update/clear UI with explicit non-traveller helper text.
- [ ] Add integration and rendered/navigation coverage.
- [ ] Preserve 44px controls, focus, mobile reflow and no horizontal overflow.

## Safety

- [x] No readiness derivation change is in scope.
- [x] No commercial/package ownership mutation is in scope.
- [x] No payment/document/visa/accommodation mutation is in scope.
- [x] No group-leader phone, passport, DOB, gender or other speculative PII is collected.
- [x] Known VS-28 duplicated readiness computation is not expanded by this slice.
- [x] Production deployment remains separately authorized.

## Verify / Close

- [ ] CI exact head passes.
- [ ] Slice Governance exact head passes.
- [ ] Rendered Slice Review exact head passes.
- [ ] Navigation Reachability exact head passes.
- [ ] Migration registry/model parity passes.
- [ ] Owned, foreign, stale and completed-handover group-leader tests pass.
- [ ] 390px fulfilment panel remains usable with no horizontal overflow.
- [ ] Standing Product Owner authorization is applied only to the final certified SHA.
- [ ] Any post-ready / post-label required checks pass on the unchanged exact head.
- [ ] Merge to `main` only after all exact-head gates pass.
