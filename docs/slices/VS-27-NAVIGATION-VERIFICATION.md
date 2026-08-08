# VS-27 Navigation Verification

| Path | Actor / precondition | Expected outcome | Evidence | Status |
| --- | --- | --- | --- | --- |
| `/operator/departures` → `Pilgrim manifest` | Approved operator with an owned departure | `/operator/departures/{departureId}/manifest` opens from the existing departure card | `apps/web/app/operator/OperatorCollectionPage.tsx` + `apps/web/e2e/operator-departure-manifest.spec.ts` | VERIFIED |
| Manifest deep link | Approved operator with an owned departure | Manifest loads in the established operator shell and preserves the departure id in the URL | `apps/web/app/operator/departures/[departureId]/manifest/page.tsx` + rendered test | VERIFIED |
| Manifest → search/filter travellers | Owned departure with confirmed-booking travellers | Name/booking-reference search and readiness filters update the visible manifest without leaving departure context | `apps/web/e2e/operator-departure-manifest.spec.ts` | VERIFIED |
| Manifest → operational follow-up | Owned confirmed-booking traveller and explicit note | Governed note/acknowledgement saves in place with the current version and remains on the manifest | `apps/web/e2e/operator-departure-manifest.spec.ts` + `OperatorDepartureManifestApiTests` | VERIFIED |
| Manifest stale operation | Existing operation changed after the operator loaded it | Conflict is returned with no duplicate/partial audit mutation and the UI gives recoverable refresh guidance | `OperatorDepartureManifestApiTests` + UI conflict state | VERIFIED |
| Manifest foreign departure | Active operator does not own departure id | Safe not-found response with no tenancy disclosure | `OperatorDepartureManifestApiTests` + rendered safe-not-found contract | VERIFIED |
| Manifest empty/filter-empty result | No travellers match current filter/search | Clear empty state is shown without breaking the departure context | `apps/web/e2e/operator-departure-manifest.spec.ts` | VERIFIED |
| Manifest → back to departure | Approved operator on owned manifest | `Back to departure` returns to `/operator/departures/{departureId}` | `apps/web/app/operator/OperatorDepartureManifest.tsx` | VERIFIED |
| Desktop and mobile manifest workspace | Approved operator | No horizontal overflow; controls remain keyboard/touch reachable and serious/critical accessibility checks pass | rendered review + `apps/web/e2e/operator-departure-manifest.spec.ts` | VERIFIED |

## Certification rule

The statuses above describe the implemented reachability contract and are not a substitute for exact-head certification. Merge still requires the navigation-reachability workflow and every other required VS-27 gate to actually run and pass on the unchanged certified SHA. A skipped required gate is not a pass. Production deployment is `NOT_APPLICABLE` to this slice unless separately authorized.
