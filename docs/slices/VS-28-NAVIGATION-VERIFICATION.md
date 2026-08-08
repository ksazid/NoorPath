# VS-28 Navigation Verification

| Path | Actor / precondition | Expected outcome | Evidence | Status |
| --- | --- | --- | --- | --- |
| `/operator/departures` → owned departure → `Pilgrim manifest` | Approved operator with an owned departure | Existing departure operations remain the entry point to the VS-27 manifest | existing departure navigation + VS-27 rendered coverage | VERIFIED |
| Pilgrim manifest → `Final handover` | Approved operator with an owned departure manifest | `/operator/departures/{departureId}/handover` opens without losing departure context | `apps/web/app/operator/OperatorDepartureManifest.tsx` + `apps/web/e2e/operator-departure-handover.spec.ts` | VERIFIED |
| Final handover deep link | Approved operator with an owned departure | Handover workspace loads in the established operator shell and preserves the departure id in the URL | `apps/web/app/operator/departures/[departureId]/handover/page.tsx` + rendered test | VERIFIED |
| Final handover → readiness blockers | Owned departure with unresolved readiness | Payment/document/visa/accommodation blocker counts are visible and completion remains disabled | `apps/web/e2e/operator-departure-handover.spec.ts` + `OperatorDepartureHandoverApiTests` | VERIFIED |
| Final handover → complete | Owned departure with all readiness gates clear and explicit final note | Governed completion sends the current expected version and reloads into immutable completed state | `apps/web/e2e/operator-departure-handover.spec.ts` + domain/API coverage | VERIFIED |
| Completed handover replay | Existing completed closeout | Repeated completion is idempotent and does not append another audit mutation | `OperatorDepartureHandoverApiTests` + `DepartureHandoverPolicyTests` | VERIFIED |
| Final handover foreign departure | Active operator does not own departure id | Safe not-found response with no operator/tenancy disclosure | `OperatorDepartureHandoverApiTests` + rendered safe-not-found state | VERIFIED |
| Final handover → back to pilgrim manifest | Approved operator on owned handover | `Back to pilgrim manifest` returns to `/operator/departures/{departureId}/manifest` | `apps/web/app/operator/OperatorDepartureHandover.tsx` | VERIFIED |
| Desktop and 390px mobile handover workspace | Approved operator | No horizontal overflow; controls remain keyboard/touch reachable and serious/critical accessibility checks pass | `apps/web/e2e/operator-departure-handover.spec.ts` + rendered review | VERIFIED |

## Certification rule

The statuses above describe the implemented reachability contract and are not a substitute for exact-head certification. Merge still requires the Navigation Reachability Review and every other required VS-28 gate to actually run and pass on the unchanged certified SHA. A skipped required gate is not a pass. Production deployment is `NOT_APPLICABLE` to this slice unless separately authorized.
