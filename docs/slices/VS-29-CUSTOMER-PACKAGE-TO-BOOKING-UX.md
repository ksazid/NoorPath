# VS-29 — Customer Package-to-Booking UX

## Governing requirements

- INV-ACC-003 — occupancy offered for sale must be supported.
- INV-PRI-003 — customer totals are explainable before commitment.
- INV-PRI-006 — unsupported occupancy cannot be priced.
- INV-TRV-001 — multiple travellers may belong to one booking.
- INV-BKG-001 — a booking originates from a saleable offering.
- INV-BKG-002 — booking commercial references are fixed.

UX authority: `docs/05-design/UX-INFORMATION-ARCHITECTURE-AND-JOURNEYS.md`, especially Package Detail and the MVP Booking Journey.

## Outcome

A customer evaluating a published departure can choose a currently saleable occupancy on the Package Details page, continue directly into the existing booking planner with that choice preserved, add/select the required travellers, review the authoritative quote and secure availability without duplicate customer chrome or a dead-end navigation path.

## Design read

Preserve the approved NoorPath Package page: calm, editorial and trust-led. The change is conversion clarification, not a redesign. Occupancy becomes an explicit decision control inside the existing pricing card; the shared customer shell remains the single source of header/footer chrome.

## Interaction

1. Package detail loads the authoritative departure projection.
2. The pricing card presents each occupancy as a radio-style choice with price and effective availability.
3. The first available occupancy may be suggested, but no unavailable option can be chosen.
4. `Continue with <occupancy>` links to `/packages/{departureId}/plan?occupancy={value}`.
5. The planner accepts the requested occupancy only if it still exists and is available; otherwise it falls back to the first current available option.
6. The planner retains the existing authentication, traveller, quote, inventory-hold and payment boundaries.
7. Back navigation returns to the package details page.

## Customer shell

Package and planner routes already live inside `CustomerRouteShell`. Legacy page-local public header/footer chrome must not duplicate that shell. The shared shell footer remains consistent on public customer routes; transactional routes retain their approved compact footer where applicable.

## Accessibility and responsive contract

- Native radio controls remain keyboard operable.
- At least 44px target height for occupancy choice and primary actions.
- Disabled/unavailable state is represented in text, not colour only.
- Visible focus remains intact.
- No horizontal overflow at 390px viewport.
- Reduced motion is respected.
- Dynamic pricing/availability text uses existing NoorPath tokens and iconography only.

## Failure and stale-state behaviour

The planner revalidates the selected occupancy against the newly loaded package projection. A query-string choice never bypasses authoritative pricing or inventory checks. Existing quote/hold conflict handling remains unchanged.

## Verification

Rendered E2E must cover:

- package detail -> select available occupancy -> planner restores selection;
- unavailable occupancy cannot be selected;
- planner -> package back route;
- shared customer shell appears once;
- customer footer remains present and consistent;
- mobile keyboard/touch target and horizontal-overflow checks.

No production deployment is authorized by this slice.
