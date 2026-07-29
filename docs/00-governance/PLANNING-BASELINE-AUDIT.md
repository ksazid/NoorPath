# NoorPath V2 Planning Baseline Audit & Gap Closure

Status: Baseline audit before implementation
Step: 20

## Objective
Verify that the planning work through Step 19 forms one coherent, implementable MVP baseline before VS-00 begins. This audit is a gate against rediscovering product, architecture, security, design, migration or operational decisions while coding.

## Canonical sources
- GitHub planning branch: `planning/noorpath-v2-baseline`
- Basic Memory project: `NoorPath`
- Approved Landing and Package visual references remain the visual source of truth.

GitHub is the versioned engineering/product artifact authority. Basic Memory is the durable AI/project-context mirror. Figma becomes canonical for approved editable UI screens/components when slice design begins. If mirrors disagree, reconcile them before implementation rather than choosing silently.

## Baseline coverage

### Product
Covered: Product Charter; Capability Map; MVP Scope; Business Rules and Invariants.
Assessment: sufficient to define the MVP boundary, but unresolved policy decisions must be closed before the affected slice reaches Definition of Ready.

### Domain
Covered: Domain Map/Capability Ownership; Data Dictionary; Core Domain Model; State Machines; Domain Events/Integration Contracts.
Assessment: strong ownership baseline. Physical entity/table design remains slice-level implementation work and must not override logical ownership.

### Architecture and data
Covered: Architecture Baseline; Data Architecture; MVP NFR/Reliability; API/Application Contract; Deployment/Environment.
Assessment: sufficient architecture guardrails. Hosting/IaC/identity/payment/storage provider selections that remain open should be resolved through focused ADRs when first required, not speculative platform work.

### Security/privacy
Covered: Security and Privacy Baseline.
Assessment: sufficient engineering baseline. Production legal/compliance confirmation, retention periods and provider-specific threat controls remain release/slice decisions.

### UX/design
Covered: UX/IA/Journey Baseline; Design System/Figma Baseline.
Assessment: design governance exists, but implementation must not start for a UI-bearing slice until required Figma states are approved. Existing approved Landing and Package references remain authoritative visual anchors.

### Engineering quality
Covered: Testing and Quality Strategy; MVP Vertical Slice Map.
Assessment: slice quality and sequencing are defined. Each slice still requires a concrete feature specification and test matrix before coding.

## Cross-document consistency decisions
1. Modular monolith remains the MVP backend architecture.
2. PostgreSQL remains structured system of record; private object storage owns document bytes.
3. Capability ownership is authoritative; UI/admin/composition layers do not own business truth.
4. Account, Booking Owner and Traveller remain distinct.
5. Operator scope/resource ownership are explicit security boundaries.
6. Catalogue, Pricing and Inventory are separate authorities.
7. Quote belongs to Pricing until commitment; Booking stores immutable commercial evidence at commitment.
8. Payment state remains independent from Booking state; settlement alone does not silently imply confirmation.
9. Inventory hold/commitment is concurrency-sensitive and cannot be implemented as a simple editable count.
10. Documents and Visa remain separate capabilities; readiness is derived.
11. Cross-module writes are prohibited; explicit contracts/events coordinate capabilities.
12. Transactional Outbox is the MVP reliability baseline for committed integration facts.
13. REST `/api/v1` with explicit DTOs/Problem Details/idempotency/concurrency rules remains the public API baseline.
14. UX extends approved NoorPath references; no generic redesign.
15. Testing is risk-based; migrations, authorization, payments, inventory and sensitive documents receive stronger evidence.
16. Infrastructure is added on measured need; Redis, broker, Kubernetes, search cluster and multi-region are not MVP defaults.

## Gaps to close before or during VS-00

### Must resolve before VS-00 exits
- Canonical repository/module/project topology for V2 and treatment of existing S01/S02 code.
- Authentication/identity provider choice and local/CI testing strategy.
- Authorization primitive and operator-scope enforcement pattern.
- PostgreSQL module schema + DbContext + migration ownership convention.
- Outbox storage/dispatcher implementation pattern.
- Local/CI PostgreSQL strategy and deterministic migration validation.
- Configuration/secrets conventions.
- Observability baseline implementation.
- CI gate layout and branch/PR quality expectations.
- Initial design-token source and Figma/code synchronization approach.
- Hosting/IaC ADR only to the extent necessary to make the skeleton deployable.

### Resolve before the first affected slice, not in VS-00
- Operator approval/suspension operational policy — before VS-01/VS-04 as applicable.
- Publication approval workflow — before VS-04.
- Pricing/occupancy/payment schedule policies — before VS-03/VS-07/VS-09.
- Inventory hold duration/expiry/cutoff policies — before VS-08.
- Cancellation/refund policies — before cancellation/refund capability is introduced.
- Payment provider/webhook/reconciliation specifics — before VS-09.
- Document requirements, retention and storage provider specifics — before VS-12.
- Visa workflow/evidence/customer wording — before VS-13.
- Production SLO/alerts/backup/restore operational values — finalized before VS-15.

## Existing-code strategy
Do not blindly continue or delete the earlier S02 implementation. At VS-00 start, inventory existing code and classify each artifact as:
- KEEP — already conforms to V2 baseline.
- ADAPT — useful but needs boundary/contract/design changes.
- REPLACE — conflicts with approved baseline or creates migration/security/ownership debt.
- ARCHIVE — evidence/reference only, not part of the target runtime.

No old migration, API contract, package dependency or UI component becomes V2 authority merely because it already exists.

## Design/skill readiness
Before the first design-bearing slice:
- verify repository-installed UI UX Pro Max and Ponytail skills;
- install/verify Impeccable and Emil-related skills/resources as previously planned;
- confirm Figma connection/workspace;
- preserve Landing + Package references and MASTER authority;
- create/approve tokens and minimum primitives required by the slice;
- use design skills as review/refinement tools, not as authority to replace NoorPath identity.

## Step 20 conclusion
The baseline is coherent enough to move toward implementation. The remaining gaps are mostly **decision-at-first-use** items rather than reasons to continue broad planning.

The next action should not be another large architecture document. It should be **VS-00 Definition of Ready + Repository Reconciliation**, producing the exact V2 starting topology, ADRs needed for foundation choices, existing-code keep/adapt/replace decisions, and the first executable implementation checklist.

## Implementation gate
VS-00 may begin when:
- this baseline is accepted by the Product Owner;
- canonical sources are clear;
- no unresolved contradiction blocks foundation work;
- VS-00 has its own feature/engineering specification and acceptance criteria.
