# VS-24 — Navigation Verification Matrix

This matrix is required for the exact VS-24 implementation head. `VERIFIED` rows are bound to the registered rendered/navigation suite and exact-head API integration coverage; certification must still execute those tests successfully on the final SHA.

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Operator | `/operator/bookings` | `Open booking` | `/operator/bookings/{bookingId}` | Approved operator owning booking | VERIFIED | `apps/web/e2e/operator-booking-detail.spec.ts` | Real click source asserts the exact booking destination. |
| Operator | Booking detail | `Back to bookings` | `/operator/bookings` | Approved operator owning booking | VERIFIED | `apps/web/e2e/operator-booking-detail.spec.ts` | Source control remains in the active Bookings shell. |
| Operator | Booking detail | `Open departure` | `/operator/departures/{departureId}` | Approved operator owning booking | VERIFIED | `apps/web/e2e/operator-booking-detail.spec.ts` | Existing departure route; no duplicate departure behavior. |
| Operator | Booking detail | `Document review` | `/operator/documents` | Permissioned approved operator | VERIFIED | `apps/web/e2e/operator-booking-detail.spec.ts` + Documents suite | No duplicated document mutation. |
| Operator | Booking detail | `Visa processing` | `/operator/visa` | Permissioned approved operator | VERIFIED | `apps/web/e2e/operator-booking-detail.spec.ts` + Visa suite | No duplicated visa mutation. |
| Operator | Booking detail | `Support actions` | `/operator/support` | Permissioned approved operator | VERIFIED | `apps/web/e2e/operator-booking-detail.spec.ts` + Support suite | Existing domain action surface. |
| Operator | Booking detail | `Cancellation requests` when confirmed | `/operator/cancellations` | Permissioned approved operator | VERIFIED | `apps/web/e2e/operator-booking-detail.spec.ts` + Cancellation suite | Lifecycle rules remain module-owned. |
| Operator | Foreign booking deep link/API | Direct route/API | Safe not-found | Approved operator for another operator | VERIFIED | `tests/NoorPath.Commercial.Integration.Tests/OperatorBookingDetailApiTests.cs` | Query binds booking ID and active operator ID before any composed detail is returned. |
| Operator | Missing booking deep link/API | Direct route/API | Safe not-found | Approved operator | VERIFIED | `tests/NoorPath.Commercial.Integration.Tests/OperatorBookingDetailApiTests.cs` | Missing and foreign resources share non-disclosing not-found behavior. |
| Operator | Booking detail API | Direct API | Forbidden/unauthorized | Customer or unauthenticated identity | VERIFIED | `tests/NoorPath.Commercial.Integration.Tests/OperatorBookingDetailApiTests.cs` | Active operator membership plus `operator.admin.access` remains mandatory. |
| Operator mobile | Booking list and detail | Shell/menu and booking actions | Same destinations | Approved operator | VERIFIED | Registered `operator-booking-detail.spec.ts` rendered on mobile WebKit | Certification enforces no horizontal overflow, accessibility and minimum target sizes. |
