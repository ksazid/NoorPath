# VS-00 — Platform Foundation Readiness & Repository Reconciliation

Status: Draft for Product Owner approval
Slice: VS-00

## 1. Outcome
NoorPath has a clean, deployable V2 foundation that preserves useful existing work, removes baseline-breaking debt, and establishes only the minimum platform capabilities required by later MVP slices.

## 2. Why this slice exists
The repository already contains useful foundation and S02-era implementation, but the V2 planning baseline changed the ownership model, migration rules, security expectations, design workflow and slice Definition of Done. VS-00 reconciles the repository to that baseline before product features resume.

## 3. Evidence from the current repository

### Existing structure worth preserving
- `NoorPath.slnx` already separates API host, BuildingBlocks, Catalogue domain/infrastructure and tests.
- README already targets Next.js + ASP.NET Core .NET 10 modular monolith, PostgreSQL, vertical slices and shared design tokens.
- AGENTS.md already enforces modular-monolith boundaries, explicit contracts, outbox events, tenant isolation, WCAG 2.2 AA and slice-level DoD.
- CI already has Node validation, .NET validation, PostgreSQL service startup, browser/E2E infrastructure and secret scanning.

### Existing debt that blocks V2 foundation acceptance
- Current Catalogue migration is hand-condensed and not a normal generated EF migration artifact.
- Current `CatalogueDbContextModelSnapshot` delegates to live `CatalogueDbContext.Configure(modelBuilder)` instead of being a frozen generated snapshot.
- Catalogue Infrastructure currently pins EF/Npgsql package versions that previously required dependency/security remediation.
- CI still contains S02-specific migration/browser steps and naming, so it is not yet a neutral V2 foundation pipeline.
- Existing Catalogue model currently mixes facts that V2 assigns to separate Catalogue, Pricing and Inventory capabilities; keeping it unchanged would violate V2 ownership.

## 4. Reconciliation classification

### KEEP
- Repository root/toolchain approach: `.NET 10`, Node/pnpm, solution/workspace model.
- `apps/api` and `apps/web` as runtime hosts.
- `src/Modules` modular structure.
- architecture/integration/E2E test project categories.
- PostgreSQL as local/CI integration database.
- `packages/design-tokens` concept.
- secret scanning and core CI validation concepts.
- AGENTS.md principles, subject to V2 governance-order update.
- approved Landing + Package references and MASTER-derived visual governance.

### ADAPT
- `NoorPath.BuildingBlocks`: retain only genuinely cross-cutting technical primitives; remove capability-specific leakage.
- Catalogue projects: reshape around V2 Catalogue ownership and move price/inventory truth into their future modules/slices.
- API composition: keep host but register modules through explicit module composition rather than feature coupling.
- CI: generalize S02-specific jobs to reusable foundation gates.
- design-token package: align with approved MASTER/Figma semantic tokens before broad component implementation.
- test infrastructure: keep PostgreSQL/Playwright approach but make it slice-neutral and risk-based.

### REPLACE
- defective/manual Catalogue migration history and non-frozen snapshot before V2 persistence continues.
- any API/domain contracts that expose the obsolete S02 ownership model as future authority.
- any persistence coupling that treats Pricing/Inventory as Catalogue-owned truth.

### ARCHIVE / REFERENCE
- S02 prototype/reference screenshots remain valuable for approved flow/visual evidence where they align with Landing/Package references and V2 product decisions.
- old S02 implementation decisions that conflict with V2 stay as historical evidence only.

## 5. VS-00 scope

### 5.1 Repository topology
Freeze the target foundation topology:

```text
apps/
  api/
  web/
  worker/              # create only when outbox/background work requires a distinct host
src/
  BuildingBlocks/
  Modules/
    Identity/
    Operators/
    Catalogue/
    Pricing/
    Inventory/
    Booking/
    Traveller/
    Payments/
    Documents/
    Visa/
    Notifications/
    Support/
tests/
  Architecture/
  Integration/
  Web.E2E/
packages/
  design-tokens/
docs/
  00-governance/
  01-product/
  02-domain/
  03-architecture/
  04-security/
  05-design/
  06-engineering/
  07-operations/
  08-capabilities/
  09-slices/
```

Not every module needs a concrete project in VS-00. Create projects only for capabilities needed by VS-00/VS-01; reserve names/ownership in documentation for the rest.

### 5.2 Foundation ADRs required before VS-00 closes
Only ADRs with immediate implementation consequence:
1. Identity provider / authentication integration direction for MVP.
2. Module project and DbContext/schema convention.
3. EF migration ownership/generation/reset strategy.
4. Transactional Outbox implementation pattern.
5. Hosting/IaC target sufficient for deployable DEV/STAGING skeleton.
6. Figma/token/code synchronization source-of-truth rule.

Do not create speculative ADRs for Redis, Service Bus, microservices, search or multi-region.

### 5.3 Persistence reset
Before feature persistence resumes:
- determine whether any real production data must be preserved; current planning assumes pilot/dev data only unless evidence says otherwise;
- remove/replace defective Catalogue migration artifacts in a controlled foundation change;
- generate canonical EF migrations/snapshots from the approved V2-owned model when the first owning module needs persistence;
- add CI checks for pending model changes, clean database migration and supported forward migration path;
- no hand-authored snapshot shortcuts.

### 5.4 Authentication and authorization foundation
Implement only primitives needed by VS-01:
- authenticated principal mapping to internal Account/Principal ID;
- operator membership/scope abstraction;
- permission/resource-scope authorization policy pattern;
- deny-by-default server-side enforcement;
- privileged-user MFA requirement represented in design/contract even if provider configuration is environment-specific;
- integration-test seam for authenticated customer/operator/platform principals.

### 5.5 Outbox foundation
Provide the minimum reliable mechanism:
- module transaction writes committed integration event record atomically with owned state;
- dispatcher/worker reads pending records;
- at-least-once delivery assumption;
- event ID/version/correlation/causation metadata;
- retry and poison/failure visibility;
- consumers idempotent.

No external broker is required for MVP foundation.

### 5.6 Configuration, secrets and observability
- environment-based strongly validated configuration;
- secrets outside source and browser bundles;
- local/CI test secrets only through disposable environment configuration;
- structured logs with correlation/trace context;
- OpenTelemetry-compatible traces/metrics/logging conventions;
- sensitive-data logging filters/guardrails;
- readiness/liveness endpoints as appropriate to chosen hosting model.

### 5.7 CI foundation
A neutral V2 CI pipeline should gate:
- secret scanning;
- dependency restore/audit;
- formatting/lint/type checking;
- .NET and web builds;
- unit/application/architecture tests;
- PostgreSQL integration tests;
- migration validation when migration-bearing modules exist;
- browser/accessibility/visual checks only for slices that require them, not as S02-hardcoded pipeline semantics;
- generated artifacts/evidence where useful.

### 5.8 Design foundation
Before the first UI-bearing implementation task:
- verify installed UI UX Pro Max and Ponytail skill files;
- install/verify Impeccable and Emil resources before using them;
- confirm Figma workspace/file and authority structure;
- establish semantic token source aligned to approved Landing/Package/Master baseline;
- create only foundational primitives required by VS-01, not the entire future component library.

## 6. Explicit non-goals
VS-00 does not build package authoring, pricing, inventory, booking, payments, documents, visa, advanced admin workflow, customer discovery, Redis, external message broker, Kubernetes, search cluster, multi-region or AI features.

## 7. Acceptance criteria
VS-00 is ready to close when:
1. Target repository/module topology is documented and reflected in the solution without speculative empty complexity.
2. Existing code is classified KEEP/ADAPT/REPLACE/ARCHIVE and all blocking REPLACE items are removed or isolated.
3. Defective migration/snapshot approach is eliminated and migration governance is executable in CI.
4. Authentication and operator/resource authorization test seams are implemented.
5. Module boundaries are enforced by architecture tests.
6. PostgreSQL integration tests run deterministically in CI.
7. Transactional Outbox foundation has an integration test proving atomic persistence + retry-safe dispatch semantics.
8. Configuration/secrets conventions and observability primitives are present.
9. CI is V2/slice-neutral and green.
10. Design skill availability is verified; Figma/token workflow is ready for VS-01 design.
11. A minimal DEV deployment path exists or is proven through the chosen hosting/IaC ADR.
12. README/AGENTS/governance references point to the V2 baseline and VS-00 workflow.
13. No later MVP feature is pre-implemented merely to prove the foundation.

## 8. Implementation order inside VS-00
1. Repository inventory and exact KEEP/ADAPT/REPLACE/ARCHIVE register.
2. ADRs for identity, module/DbContext/migrations, outbox, hosting/IaC, Figma-token sync.
3. Repair/reset persistence baseline.
4. Normalize solution/module boundaries and architecture tests.
5. Add authentication/authorization primitives + test identities.
6. Add outbox + worker/dispatcher only as required.
7. Add configuration/secrets/observability foundation.
8. Generalize CI from S02-specific to slice-neutral gates.
9. Verify/install design skills and Figma/token baseline.
10. Deploy/test skeleton and close acceptance evidence.

## 9. Product Owner gate
No production-feature development begins until VS-00 Definition of Ready is approved. After approval, implementation proceeds task-by-task rather than as a repository-wide rewrite.
