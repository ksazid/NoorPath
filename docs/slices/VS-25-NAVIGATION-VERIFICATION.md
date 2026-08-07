# VS-25 Navigation Verification

| Path | Actor / precondition | Expected outcome | Evidence | Status |
| --- | --- | --- | --- | --- |
| `/operator/bookings` → `Open booking` | Approved operator with owned booking | Exact owned booking detail opens | `apps/web/e2e/operator-booking-amendment.spec.ts` | PENDING |
| Booking detail → `Amend booking` | Confirmed owned booking | `/operator/bookings/{bookingId}/amend` opens in existing operator shell | `apps/web/e2e/operator-booking-amendment.spec.ts` | PENDING |
| Amend → preview | Valid proposed travellers/occupancy | Server-authoritative before/after price impact renders | `apps/web/e2e/operator-booking-amendment.spec.ts` | PENDING |
| Preview → confirm | Explicit confirmation + unchanged booking version | Amendment commits and refreshed booking detail/history is reachable | `apps/web/e2e/operator-booking-amendment.spec.ts` | PENDING |
| Amend foreign booking | Active operator does not own id | Safe not-found with no tenancy disclosure | integration + rendered evidence | PENDING |
| Amend ineligible booking | Booking lifecycle is not amendable | Explicit ineligible feedback; no mutation | integration + rendered evidence | PENDING |
| Confirm stale preview | Booking version changed after preview | Conflict feedback with refresh/retry path; no partial mutation | integration + rendered evidence | PENDING |
| Payment/document/visa/cancellation destinations | Amendment has follow-up operational consequence | Existing module-owned workflows remain separate; VS-25 performs no direct mutation | architecture/integration evidence | PENDING |

## Certification rule

Before merge, every automatable row above must be `VERIFIED`. `PENDING` and `FAILED` block certification. Identity/provider-only limitations, if any are discovered, must be recorded explicitly rather than waived. Production deployment is `NOT_APPLICABLE` to this slice unless separately authorized.
