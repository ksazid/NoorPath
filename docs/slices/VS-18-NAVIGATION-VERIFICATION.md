# VS-18 — Navigation Verification Matrix

VS-18 establishes reusable design-system primitives and internal showcase routes. It deliberately does not adopt the new shell across completed live customer or staff journeys. This matrix records that boundary so navigation verification remains truthful.

| Surface | Source | Control | Destination | Identity | Result | Evidence | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Internal showcase | Direct development/test route | `/design-system` | Component catalogue | Development or explicitly enabled non-production environment | `NOT_APPLICABLE` | `apps/web/e2e/design-system-foundation.spec.ts`; Rendered Slice Review run `30894206584` | The catalogue is intentionally not linked from live customer or staff navigation. |
| Internal showcase | Direct development/test route | `/design-system/customer-shell` | Synthetic customer-shell example | Development or explicitly enabled non-production environment | `NOT_APPLICABLE` | `apps/web/e2e/design-system-foundation.spec.ts`; Rendered Slice Review run `30894206584` | This validates shell components; it is not a customer journey destination. |
| Internal showcase | Direct development/test route | `/design-system/staff-shell` | Synthetic staff-shell example | Development or explicitly enabled non-production environment | `NOT_APPLICABLE` | `apps/web/e2e/design-system-foundation.spec.ts`; Rendered Slice Review run `30894206584` | This validates role-neutral shell structure; it is not an authorized staff workspace. |
| Production guard | Any unapproved production request | Showcase route guard | `404` unless `NOORPATH_ENABLE_DESIGN_SYSTEM_SHOWCASE=true` | Public | `VERIFIED` | `apps/web/app/design-system/requireShowcase.ts`; VS-18 contract tests | Internal showcase routes fail closed when not explicitly enabled. |
| Live customer and staff pages | Existing route navigation | Existing page-specific controls | Existing destinations | Applicable existing identities | `NOT_APPLICABLE` | VS-18 scope and changed-file review | VS-18 did not rewrite or adopt shells on live pages; adoption belongs to a later approved slice. |

## Completion rule

- No live page may claim VS-18 navigation adoption merely because the shared shell exists.
- Internal showcase routes must remain unlinked from customer and staff production navigation unless a later approved slice changes that boundary.
- A later shell-adoption slice must supply its own click-through matrix for every affected live route.
