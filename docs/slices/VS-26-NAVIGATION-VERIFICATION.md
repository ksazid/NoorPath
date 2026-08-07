# VS-26 Navigation Verification

| Path | Actor / precondition | Expected outcome | Evidence | Status |
| --- | --- | --- | --- | --- |
| `/operator/bookings` → `Open booking` | Approved operator with owned booking | Exact owned booking detail opens | Existing booking-management reachability + `apps/web/e2e/operator-accommodation.spec.ts` | VERIFIED |
| Booking detail → `Accommodation` | Confirmed owned booking | `/operator/bookings/{bookingId}/accommodation` opens in the existing operator shell | `apps/web/e2e/operator-accommodation.spec.ts` | VERIFIED |
| Accommodation → assign traveller | Traveller belongs to booking and room has capacity | Assignment saves in place, room occupancy refreshes, and audit history is visible | `apps/web/e2e/operator-accommodation.spec.ts` + `OperatorAccommodationApiTests` | VERIFIED |
| Accommodation → reassign traveller | Traveller is already assigned in the same stay and source/destination versions are current | Existing stay assignment moves atomically to the destination room with one current assignment | `OperatorAccommodationApiTests` | VERIFIED |
| Accommodation → lock room | Confirmed booking, current room version and explicit reason | Room becomes locked, lock action is auditable, and assignment controls are blocked | `apps/web/e2e/operator-accommodation.spec.ts` + integration contract | VERIFIED |
| Accommodation with stale room version | Another operator action changed the room version | Conflict is returned without a partial assignment/audit write; operator can refresh and retry | `OperatorAccommodationApiTests` | VERIFIED |
| Accommodation foreign booking | Active operator does not own booking id | Safe not-found with no tenancy disclosure | `OperatorAccommodationApiTests` + rendered contract | VERIFIED |
| Empty stay allocation | Confirmed booking has no room rows for a stay | Clear empty state remains usable and room creation is reachable | `apps/web/e2e/operator-accommodation.spec.ts` | VERIFIED |
| Desktop and mobile accommodation workspace | Approved operator | No horizontal overflow; controls remain keyboard/touch reachable and accessibility checks pass | rendered review + `apps/web/e2e/operator-accommodation.spec.ts` | VERIFIED |

## Certification rule

The statuses above describe the implemented reachability contract and are not a substitute for exact-head certification. Merge still requires the navigation-reachability workflow and every other required VS-26 gate to actually run and pass on the unchanged certified SHA. A skipped required gate is not a pass. Production deployment is `NOT_APPLICABLE` to this slice unless separately authorized.
