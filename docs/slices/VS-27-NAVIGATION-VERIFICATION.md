# VS-27 Navigation Verification

## Required journey
1. Open the existing operator departures surface.
2. Open a departure owned by the active operator.
3. Navigate to `Pilgrim manifest` using a keyboard-reachable link/action.
4. Confirm the manifest route loads within the established operator shell.
5. Search/filter travellers without losing the departure context.
6. Open a traveller readiness action/detail surface and return to the manifest without a dead end.
7. Verify a foreign-operator departure remains a safe not-found/permission-safe experience.

## Expected route contract
- Departure detail exposes a visible `Pilgrim manifest` entry point.
- Manifest route is deep-linkable and retains the departure id in the URL.
- Back/breadcrumb navigation returns to the same departure context.
- Mobile and desktop layouts have no horizontal overflow.
- Keyboard focus order follows the visual task order.

## Certification status
PENDING until implementation and rendered navigation tests pass on the exact PR head.
