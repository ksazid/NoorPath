# VS-33 Navigation Verification

| Path / action | Actor / precondition | Expected outcome | Evidence | Status |
| --- | --- | --- | --- | --- |
| `/operator/departures/{departureId}/manifest` -> View package being fulfilled | Approved operator member, owned departure | Existing operator departure/package preview opens for the same departure | `departure-fulfilment-group-leader.spec.ts` | VERIFIED |
| Pilgrim manifest -> Add/update accompanying group leader | Approved operator member, owned open handover | Group leader is saved as departure operational metadata and remains separate from traveller counts | integration + rendered tests | VERIFIED |
| Pilgrim manifest -> Clear group leader | Approved operator member, existing leader, open handover | Leader is cleared with version progression and audit evidence | integration + rendered tests | VERIFIED |
| Pilgrim manifest -> Final handover | Approved operator member | Existing VS-28 handover route opens and displays the same package/group-leader context | `departure-fulfilment-group-leader.spec.ts` | VERIFIED |
| Final handover -> View package being fulfilled | Approved operator member | Existing operator departure/package preview remains reachable | `departure-fulfilment-group-leader.spec.ts` | VERIFIED |
| Foreign departure -> group-leader mutation | Operator member outside resource scope | Safe 404; no metadata is created or changed | integration test | VERIFIED |
| Completed handover -> group-leader mutation | Approved operator member, completed closeout | 409 `handover_completed`; immutable closeout is preserved | integration test | VERIFIED |
| Production deployment | Any | Deployment is outside VS-33 | standing release discipline | NOT_APPLICABLE |

## Certification rule

These rows describe the intended implementation contract. Exact-head CI, Slice Governance, Rendered Slice Review and Navigation Reachability Review must execute and pass on the unchanged final SHA before merge.
