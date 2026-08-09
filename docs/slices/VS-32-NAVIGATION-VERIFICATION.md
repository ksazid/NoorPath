# VS-32 Navigation Verification

| Path / action | Actor / precondition | Expected outcome | Evidence | Status |
| --- | --- | --- | --- | --- |
| `/operator/packages` -> Create package | Approved operator member | Shared operator package navigation reaches `/operator/packages/new` inside the VS-31 workspace shell | existing package route + `package-inclusions-composer.spec.ts` | VERIFIED |
| `/operator/packages/new` -> Package inclusions & exclusions | Approved operator member | Quick-start authoring renders explicit Included and Not included selected lists | `package-inclusions-composer.spec.ts` | VERIFIED |
| Included -> Move to Not included | Approved operator member | Visible Move action removes the item from Included and adds it once to Not included | `package-inclusions-composer.spec.ts` | VERIFIED |
| Not included -> Move to Included | Approved operator member | Visible Move action removes the item from Not included and adds it once to Included | `package-inclusions-composer.spec.ts` | VERIFIED |
| Included -> drag -> Not included | Approved operator member using pointer | Native drag reaches the destination board and performs the same mutually exclusive move | `package-inclusions-composer.spec.ts` | VERIFIED |
| Add custom item | Approved operator member | Inline custom form exposes icon and destination choices and adds the item to the selected board | `package-inclusions-composer.spec.ts` | VERIFIED |
| 390px package authoring -> Operator menu -> Packages | Approved operator member | Shared mobile navigation remains reachable and package composer has no horizontal overflow | `package-inclusions-composer.spec.ts` | VERIFIED |
| Draft creation | Approved operator member with valid required journey facts | Existing VS-02 POST contract receives ordered inclusion/exclusion strings and continues to the departure editor | `PackageQuickStart.tsx` existing contract | VERIFIED |
| Production deployment | Any | Deployment is not part of this slice | standing release discipline | NOT_APPLICABLE |

## Certification rule

The rows above document the implemented navigation and interaction contract. Exact-head Slice Governance, CI, Rendered Slice Review and Navigation Reachability Review must run and pass on the unchanged final SHA before merge.
