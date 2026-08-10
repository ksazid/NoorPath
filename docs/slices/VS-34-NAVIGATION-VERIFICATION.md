# VS-34 Navigation Verification

| Path / action | Actor / precondition | Expected outcome | Evidence | Status |
| --- | --- | --- | --- | --- |
| `/admin` -> Platform operations | Configured Platform Administrator demo identity | Shared Platform Admin shell and command centre load with lifecycle summary and operator queue | `platform-operator-administration.spec.ts`, `platform-admin-shell.spec.ts` | VERIFIED |
| `/admin` -> `Operators` | Platform Administrator | Section navigation reaches `#operators`, keeps the shared shell visible and marks Operators as the current navigation item | `platform-admin-shell.spec.ts` | VERIFIED |
| `/admin` -> pending operator -> Apply approval | Platform Administrator, pending provisioned operator | Version-checked transition records approval and the queue reloads to authoritative Approved state | integration + rendered tests | VERIFIED |
| `/admin` -> operator -> View history | Platform Administrator, operator with lifecycle decisions | Append-only lifecycle decision history is reachable and readable | `platform-operator-administration.spec.ts` | VERIFIED |
| `/admin` -> `Publication reviews` | Platform Administrator | Shared navigation reaches `/platform/publications`; the same header, identity treatment, navigation and workspace footer remain visible | `platform-publication-approval.spec.ts` | VERIFIED |
| `/platform/publications` -> `Review for publication` | Platform Administrator, submitted departure | Click-through reaches `/platform/publications/{departureId}`, keeps `Publication reviews` current and preserves the shared Platform Admin shell | `platform-publication-approval.spec.ts` | VERIFIED |
| `/platform/publications/{departureId}` -> `Back to queue` | Platform Administrator reviewing a submitted departure | Task-level action returns to the publication queue without creating competing route chrome | `platform-publication-approval.spec.ts` | VERIFIED |
| Direct `/platform/publications/{departureId}` load | Platform Administrator | Deep link renders inside the shared Platform Admin shell; publication workflow authorization and task logic remain unchanged | `platform-publication-approval.spec.ts` | VERIFIED |
| Customer demo identity -> account/operator/platform access | `customer-account` | Account allowed; operator and platform administration denied | persona authorization integration test | VERIFIED |
| Approved operator demo identity -> account/operator/platform access | `approved-account` | Account and operator access allowed; platform administration denied | persona authorization integration test | VERIFIED |
| Platform Administrator demo identity -> account/operator/platform access | `platform-administrator` without operator membership | Account and platform administration allowed; operator administration denied | persona authorization integration test | VERIFIED |
| Pending operator member -> operator access before/after approval | Active member of PendingApproval operator, then Platform Administrator approval | Operator access is denied before approval and allowed immediately after approval through the unchanged operator-access endpoint | platform operator administration integration test | VERIFIED |
| `/admin` at 390 x 844 | Configured Platform Administrator demo identity | Command centre reflows without horizontal overflow and remains keyboard/touch operable | `platform-operator-administration.spec.ts` | VERIFIED |
| `/platform/publications` at 390 x 844 | Configured Platform Administrator demo identity | Desktop sidebar collapses to the shared `Platform Admin menu`; current-route state, minimum targets and no-horizontal-overflow checks remain valid | `platform-publication-approval.spec.ts` | VERIFIED |
| Shared Platform Admin header/footer consistency | Configured Platform Administrator demo identity | `/admin`, publication queue and publication detail use the same NoorPath staff header/navigation/footer contract; legacy workflow chrome is suppressed when embedded | `platform-admin-shell.spec.ts`, `platform-publication-approval.spec.ts` | VERIFIED |
| Unauthorized `/admin` shell access | Signed-in identity without Platform Administrator permission | Protected navigation and admin content do not render; safe account/home handoff remains available | `platform-admin-shell.spec.ts` | VERIFIED |
| Customer phone OTP provider delivery/configuration | Customer | Provider configuration is explicitly deferred from VS-34 | VS-34 scope contract | NOT_APPLICABLE |
| Knowledge Pack | Any | Knowledge Pack work is explicitly deferred from VS-34 | VS-34 scope contract | NOT_APPLICABLE |
| Production deployment | Any | Deployment remains separately authorized and outside VS-34 | standing release discipline | NOT_APPLICABLE |

## Certification rule

These rows describe the intended implementation contract and executable evidence registered by VS-34. Exact-head CI, Slice Governance, Rendered Slice Review and Navigation Reachability Review must execute and pass on the unchanged final SHA before merge. Customer phone OTP delivery, Knowledge Pack work and production deployment are deliberately outside this certification.
