# VS-28 Navigation Verification

Status: PENDING

## Required journey
1. Operator opens Departures.
2. Operator opens an owned departure.
3. Operator can reach Pilgrim manifest.
4. Operator can reach Final handover from the departure operational flow.
5. Final handover shows readiness/blockers and completion state.
6. Foreign-operator departure identifiers remain safely undisclosed.
7. Desktop and mobile rendered journeys remain reachable without horizontal overflow.

## Verification evidence required
- route/link implementation;
- Playwright journey coverage in `apps/web/e2e/operator-departure-handover.spec.ts`;
- desktop and 390px mobile evidence;
- accessibility and minimum-target assertions;
- exact-head Navigation Reachability Review success.

This document must be updated to the repository-supported VERIFIED matrix only after implementation evidence exists. Documentation alone does not substitute for the exact-head workflow gate.
