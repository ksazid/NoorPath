# VS-31 Navigation Verification

| Path | Actor / precondition | Expected outcome | Evidence | Status |
| --- | --- | --- | --- | --- |
| `/operator` -> Packages | Approved operator member | Shared operator navigation opens `/operator/packages` and marks Packages current | `operator-shell-navigation.spec.ts` | VERIFIED |
| `/operator/packages` -> Departures | Approved operator member | Shared operator navigation opens `/operator/departures` with the same header/sidebar chrome | `operator-shell-navigation.spec.ts` | VERIFIED |
| `/operator/packages/new` | Approved operator member | Package quick start or clone flow renders inside shared operator chrome; legacy competing sidebar/brand is hidden | `operator-shell-navigation.spec.ts` | VERIFIED |
| `/operator/departures/new` | Approved operator member | Departure authoring renders inside shared operator chrome | route wrapper + rendered coverage | VERIFIED |
| `/operator/departures/{id}` | Approved operator member | Existing departure editor stays linked from Departures and renders in shared operator chrome | route wrapper | VERIFIED |
| `/operator/departures/{id}/preview` | Approved operator member | Customer preview remains contextual and reachable inside shared operator chrome | route wrapper | VERIFIED |
| `/operator/departures/{id}/review` | Approved operator member | Publication review remains reachable with Back to draft / customer preview actions inside shared chrome | route wrapper | VERIFIED |
| Operator account menu | Approved operator member | Business name and signed-in member identity remain distinct; account/settings/help/logout stay reachable | `OperatorWorkspaceShell.tsx` | VERIFIED |
| 390px operator workspace | Approved operator member | Desktop sidebar collapses to native Operator menu, targets remain usable and there is no horizontal overflow | `operator-shell-navigation.spec.ts` | VERIFIED |
| Production deployment | Any | Deployment is not part of this slice | standing release discipline | NOT_APPLICABLE |

## Certification rule

The rows above document the implemented navigation contract. Exact-head Slice Governance, CI, Rendered Slice Review and Navigation Reachability Review must run and pass on the unchanged final SHA before merge.
