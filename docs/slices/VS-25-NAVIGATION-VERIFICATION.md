# VS-25 Navigation Verification

| Path | Actor / precondition | Expected outcome | Evidence | Status |
| --- | --- | --- | --- | --- |
| `/operator/bookings` → `Open booking` | Approved operator with owned booking | Exact owned booking detail opens | VS-24 reachability contract + `apps/web/e2e/operator-booking-amendment.spec.ts` | VERIFIED |
| Booking detail → `Amend booking` | Confirmed owned booking | `/operator/bookings/{bookingId}/amend` opens in existing operator shell | `apps/web/e2e/operator-booking-amendment.spec.ts` | VERIFIED |
| Amend → preview | Valid proposed travellers/occupancy | Server-authoritative before/after price impact renders | `apps/web/e2e/operator-booking-amendment.spec.ts` | VERIFIED |
| Preview → confirm | Explicit confirmation + unchanged booking version | Amendment commits and refreshed booking detail is reachable | `apps/web/e2e/operator-booking-amendment.spec.ts` + integration contract | VERIFIED |
| Amend foreign booking | Active operator does not own id | Safe not-found with no tenancy disclosure | `OperatorBookingAmendmentApiTests` + rendered contract | VERIFIED |
| Amend ineligible booking | Booking lifecycle is not amendable | Explicit ineligible feedback; no mutation | domain policy + rendered contract | VERIFIED |
| Confirm stale preview | Booking version changed after preview | Conflict feedback with refresh/retry path; no partial mutation | `OperatorBookingAmendmentApiTests` + rendered contract | VERIFIED |
| Payment/document/visa/cancellation destinations | Amendment has follow-up operational consequence | Existing module-owned workflows remain separate; VS-25 performs no direct cross-module mutation | architecture + integration contract | VERIFIED |

## Certification rule

The statuses above describe the implemented reachability contract and are not a substitute for exact-head certification. Merge still requires the navigation-reachability workflow and every other required VS-25 gate to actually run and pass on the unchanged certified SHA. A skipped required gate is not a pass. Production deployment is `NOT_APPLICABLE` to this slice unless separately authorized.
