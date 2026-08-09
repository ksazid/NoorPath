# VS-32 — Package Inclusions Composer

Status: Implementation

## Outcome

An approved operator member can author the customer-facing Included and Not included package terms as one explicit, mutually exclusive composition: move selected items between lists by drag or an equivalent keyboard/touch control, add common suggestions, and add a custom item with a curated NoorPath icon without changing the existing Catalogue persistence contract.

## Traceability

- `INV-ID-003`, `INV-ID-004`, `INV-ID-005`
- `INV-CAT-002`, `INV-CAT-003`, `INV-CAT-005`
- VS-02 Package & Departure Authoring
- VS-31 Unified Operator Shell and Navigation

No stable approved PRD requirement ID is invented here.

## Product boundary

VS-02 remains authoritative for operator scope, draft validation, ordered inclusion/exclusion strings, optimistic concurrency, audit evidence and the `POST/GET/PUT /api/v1/operator/departures` contract. VS-32 is a bounded authoring UX refinement. It must not introduce price, inventory, publication, booking or payment policy and must not turn visual icon metadata into a new domain fact.

## Included

- two explicit selected lists: `Included` and `Not included`;
- native browser drag-and-drop between those lists;
- a visible Move control on every selected item as the keyboard, touch and assistive-technology alternative to dragging;
- mutual exclusion: after a move, an item is represented in exactly one selected list;
- current standard package defaults remain preselected and operator-editable;
- unselected package, travel-kit, Umrah-kit and common exclusion suggestions remain available without becoming guarantees;
- inline custom-item entry with a curated picker using the existing authored NoorPath SVG line-icon family;
- live movement/addition feedback;
- desktop and 390px responsive composition, visible focus and reduced-motion behavior;
- E2E coverage for default state, move in both directions, drag, custom icon choice, navigation and mobile reflow.

## Market-reference guardrail

A current category scan on 2026-08-09 found recurring patterns across Indian/international Umrah offerings: return flights, visa support/visa, Makkah and Madinah accommodation, meals, transfers/intercity transport, Ziyarat and operator guidance/support are commonly bundled. Personal expenses, optional services/excursions, excess baggage, room service and insurance treatment vary materially by package and operator. Travel-kit items, laundry, SIM/eSIM, Zamzam handling and similar complimentary items also vary.

This evidence validates exposing useful suggestions, not a NoorPath commercial promise. The existing standard defaults remain unchanged in this slice. Variable items remain suggestions until an operator explicitly selects them. No third-party price, tax, visa, refund, insurance or non-refundable policy is imported into NoorPath.

Reference set reviewed: Umrah Online India, Umrah Tours of India, Islamic Relief Umrah guidance, Umrah My Trip, GoSalam, Islamic Travel, UmrahPackages.in, Toubah and Mak Bros public package descriptions.

## Interaction contract

### Move by drag

1. A selected item exposes the native `draggable` affordance.
2. Dragging over the opposite selected list exposes a bounded visual drop state.
3. Dropping calls the same move operation used by the visible Move control.
4. The source selection is removed and the destination selection is added.
5. The UI announces the completed move through a polite live region.

### Move without drag

Each selected item has one explicit action:

- Included item: `Move to Not included`.
- Not included item: `Move to Included`.

This is the complete functional alternative for keyboard, touch and assistive-technology users. Dragging is never the only path.

### Suggestions

Suggestions not already selected may be explicitly added to Included or Not included. Selecting a suggestion never silently changes another commercial fact.

### Custom item

The operator enters a non-empty unique label, chooses one curated existing line icon, chooses Included or Not included, and adds the item. The label participates in the existing ordered string payload. The icon is authoring presentation metadata only in VS-32 and is not persisted after the draft leaves this surface.

## Defaults

The current VS-02 quick-start defaults remain:

Included:
- Return flights
- Visa included
- Makkah accommodation
- Madinah accommodation
- Breakfast, lunch and dinner
- Intercity travel
- Ziyarat transport
- Umrah guidance

Not included:
- Personal expenses
- Optional excursions
- Travel insurance unless stated

Other available items are suggestions, not preselected promises.

## Accessibility and responsive behavior

- move actions and custom-item controls have actionable targets of at least 44px;
- drag-and-drop always has the visible non-drag Move alternative;
- focus is visible on selected-item actions, suggestion actions, custom fields and icon choices;
- icon choices use native radio semantics and text labels;
- state is not conveyed by color alone;
- keyboard-triggered moves do not depend on animation;
- motion/transition feedback respects `prefers-reduced-motion`;
- the two boards stack on narrow screens and the page has no horizontal overflow at 390px.

## Failure and trust behavior

- duplicate/blank custom labels are not added;
- a move is idempotent from the UI perspective and does not leave the item in both lists;
- the existing parent draft state remains intact if later draft creation fails;
- no auth token, customer data or competitor content is logged or added to telemetry;
- server-side authorization and tenant isolation remain unchanged.

## Verification

- delivery manifest validation;
- existing package-draft standards tests;
- web static analysis, format, tests and build;
- existing .NET solution, migration registry and integration suite;
- rendered Playwright coverage for the package composer;
- navigation reachability evidence for shared operator navigation;
- desktop/mobile accessibility and overflow assertions.

## Acceptance

VS-32 closes only when the exact unchanged head passes CI, Slice Governance, Rendered Slice Review and Navigation Reachability Review, then the standing Product Owner authorization is applied to that exact state and any ready/label-triggered required checks also pass before merge. Production deployment remains separately authorized.
