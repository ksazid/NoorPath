# VS-26 — Room Assignment & Accommodation Operations

## Outcome
Approved operator staff can assign confirmed booking travellers to Makkah and Madinah room allocations, reassign them safely, see unassigned travellers, and preserve an auditable operational history without changing the booking commercial snapshot.

## Scope
- operator-isolated accommodation workspace from booking detail;
- Makkah and Madinah stay room inventories scoped to the booking/departure context;
- room capacity derived from 2/3/4-sharing room type;
- assign, unassign and reassign confirmed booking travellers;
- prevent duplicate traveller placement within the same stay;
- prevent room over-capacity;
- expose unassigned travellers and incomplete allocation state;
- optimistic concurrency for room assignment mutations;
- append-only assignment audit history;
- operational cutoff support so assignments can be locked when the departure workflow reaches a configured cutoff;
- accessible responsive operator UI and desktop/mobile navigation coverage.

## Invariants
- VS-26 does not change booking occupancy, traveller count, booking price, instalments or payment history;
- only travellers belonging to the operator-owned booking may be assigned;
- each traveller may occupy at most one room per stay;
- a room may never exceed its configured capacity;
- stale assignment versions are rejected rather than silently overwritten;
- assignment history is append-only;
- cross-operator booking, stay, room and traveller access returns safe not-found responses;
- Documents, Visa, Payments, Cancellation and booking amendment ownership remain unchanged.

## Explicit exclusions
- changing commercial occupancy or package pricing;
- hotel contracting or supplier inventory management;
- payment collection/refund execution;
- passport/document mutation;
- visa transitions;
- cancellation execution;
- customer self-service room selection;
- production deployment.

## Navigation contract
`Operator bookings -> Booking detail -> Accommodation -> Assign/reassign room -> Review allocation`

## Merge rule
The PR stays Draft until implementation is complete. `certify` must be applied and every required exact-head workflow must actually run and pass. A skipped, cancelled or unavailable required gate is not a pass. Product Owner approval applies only to the unchanged certified head SHA. Deployment is separately authorized.
