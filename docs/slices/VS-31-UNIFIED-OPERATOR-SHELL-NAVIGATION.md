# VS-31 — Unified Operator Shell and Navigation

Status: Implementation

## Outcome

Every current operator route uses one NoorPath operator workspace chrome: the approved NoorPath wordmark, operator business context, signed-in member menu, grouped navigation, consistent content alignment, and responsive mobile navigation. Existing domain screens remain functionally unchanged.

## Traceability

- VS-19 Customer Shell and Navigation Adoption
- VS-30 Account Identity and Staff Access
- Existing operator package, booking, visa, support, accommodation, manifest and handover routes
- UX baseline: approved NoorPath Landing and Package visual language
- Product-owner feedback: operator pages must stop drifting in header, logo, navigation and page alignment

No new operator permissions or business rules are introduced.

## Included

- one shared `OperatorWorkspaceShell` for operator navigation and account chrome;
- approved NoorPath wordmark instead of competing text/diamond brand treatments;
- grouped Workspace, Operations and Account navigation;
- desktop sidebar plus native mobile details navigation;
- shared page header alignment for existing operator dashboard/queue screens;
- legacy package authoring, departure authoring, customer preview and publication review embedded inside the shared operator shell while retaining their task-specific content;
- current-page indication, keyboard focus, 44px navigation targets and mobile reflow;
- existing back links and contextual actions remain available.

## Excluded

- redesigning operator domain screens or changing their workflows;
- moving staff routes to a dedicated portal hostname;
- changing operator authorization or tenant isolation;
- changing package, payment, visa, accommodation, manifest or handover rules;
- replacing the NoorPath icon family or visual identity;
- production deployment.

## UX rules

1. The operator shell is an Operate surface. Scanability and consistency outrank decorative expression.
2. The approved NoorPath wordmark and design tokens remain authoritative.
3. Navigation uses real routes, not hash placeholders.
4. Desktop and mobile expose the same navigation destinations.
5. Mobile navigation uses a native `details` disclosure and does not depend on hover.
6. A visible skip link reaches operator content.
7. Every interactive navigation target remains keyboard reachable with visible focus and a minimum 44px touch target where applicable.
8. Legacy authoring screens may keep their internal task layout, but their competing operator sidebar/brand is suppressed when embedded in the shared shell.
9. No customer footer or customer visual identity is changed by this slice.

## Failure and access states

The shared shell keeps the existing fail-closed access behavior:

- unauthenticated users receive secure sign-in;
- Platform Administrators without operator membership are directed to administration;
- forbidden users receive no operator data;
- transient access failures expose retry without leaking internal details.

## Verification

- representative overview, packages and departures routes render the same operator header/sidebar geometry;
- package quick start, departure editor, customer preview and publication review render inside the same operator chrome without a competing sidebar/brand;
- operator business name remains separate from the signed-in member name;
- active navigation is correct on nested routes;
- 390px mobile routes have no horizontal overflow and expose a usable Operator menu;
- exact-head CI, Slice Governance, Rendered Slice Review and Navigation Reachability must pass before merge;
- production deployment remains separately authorized.
