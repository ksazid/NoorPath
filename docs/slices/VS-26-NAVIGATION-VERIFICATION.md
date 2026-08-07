# VS-26 Navigation Verification

## Required route
`/operator/bookings/{bookingId}/accommodation`

## Reachability matrix

| Start | Action | Destination | Required outcome |
|---|---|---|---|
| `/operator/bookings` | Open booking | `/operator/bookings/{bookingId}` | Booking detail loads for operator-owned booking. |
| `/operator/bookings/{bookingId}` | Open accommodation | `/operator/bookings/{bookingId}/accommodation` | Accommodation workspace loads with stays, rooms and traveller allocation state. |
| Accommodation workspace | Assign/reassign traveller | Same route | Allocation updates without leaving the workspace and stale/conflict states are recoverable. |

## Verification requirements
- Desktop Chromium click-through.
- Mobile 390px click-through.
- Keyboard-reachable accommodation entry point.
- Safe not-found behavior for foreign-operator booking ids.
- Empty/unassigned/loading/error/retry states remain navigable.

## Status
PENDING until implementation and rendered/navigation certification complete.
