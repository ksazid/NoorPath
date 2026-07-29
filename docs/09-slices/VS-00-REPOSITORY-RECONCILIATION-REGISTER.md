# VS-00 Repository Reconciliation Register

Status: Working implementation gate
Purpose: classify the existing NoorPath repository against the V2 baseline before foundation code changes begin.

## Classification meanings
- **KEEP** — aligned with V2; preserve unless a later task exposes a concrete defect.
- **ADAPT** — useful and mostly aligned, but must be changed to satisfy V2 ownership, neutrality, quality or design rules.
- **REPLACE** — conflicts materially with V2 or carries known structural debt; do not extend it as-is.
- **ARCHIVE** — retain as historical/prototype/evidence reference only; not target runtime authority.
- **VERIFY** — expected to be useful, but must be inspected in the implementation environment before change.

## KEEP

### `AGENTS.md`
Reason: already mandates modular monolith, inward dependencies, vertical slices, selective CQRS, no generic repository/UoW, module table isolation, explicit contracts/outbox, tenant isolation, payment/document controls, WCAG 2.2 AA and slice-level Definition of Done.
Action: later update governing-source references to point explicitly at V2 canonical docs; core engineering rules remain.

### Toolchain pins and workspace approach
Artifacts: `global.json`, `.node-version`, root `package.json`, pnpm workspace/lockfile.
Reason: .NET 10 + Node/pnpm model aligns with V2 and existing CI.
Action: preserve; only upgrade through explicit dependency/toolchain review.

### Repository shape
Artifacts/concepts: `apps/api`, `apps/web`, `src/Modules`, `tests`, `packages/design-tokens`.
Reason: matches modular-monolith + journey-oriented frontend direction without requiring a repository rewrite.
Action: preserve shape; add module projects only when slices need them.

### PostgreSQL local/CI concept
Artifacts: `compose.yaml`, GitHub Actions PostgreSQL service/container pattern, `NOORPATH_TEST_DB` concept.
Reason: real PostgreSQL integration testing is required by V2.
Action: keep platform concept and make migration/test orchestration module-neutral.

### Secret scanning
Artifact: `.github/workflows/ci.yml` gitleaks job.
Reason: directly aligned with security baseline.
Action: retain.

### Architecture/integration/E2E test categories
Artifacts/concepts: architecture tests, PostgreSQL-backed integration tests, Playwright E2E/accessibility/visual evidence.
Reason: directly aligned with V2 testing strategy.
Action: retain categories; adapt tests that encode old S02 contracts.

### Approved visual references and design-system authority
Reason: Landing + Package references and MASTER remain the visual source of truth under V2.
Action: preserve as design evidence and authority.

## ADAPT

### `README.md`
Reason: architecture and repository layout are broadly correct, but it describes the former pilot and current foundation as if catalogue migration state is healthy.
Action: update after VS-00 to V2 canonical source hierarchy, slice workflow, new migration conventions and current local setup.

### `NoorPath.slnx`
Current content includes API, BuildingBlocks, Catalogue domain/infrastructure and associated tests.
Reason: solution format is fine; current module composition reflects S02 rather than V2 foundation.
Action: preserve solution, remove/replace only projects proven incompatible, add projects slice-by-slice rather than pre-creating all modules.

### `apps/api/NoorPath.Api.csproj` and API composition
Reason: API host is correct, but references are currently Catalogue-centric.
Action: keep host; make composition module-neutral and register only implemented modules.

### `src/NoorPath.BuildingBlocks`
Reason: shared technical primitives are valid only when truly cross-cutting.
Action: inspect each type; keep minimal neutral primitives, move domain-specific items back to owning modules, avoid creating a shared dumping ground.

### Catalogue domain/application code
Reason: S02 contains potentially reusable package/departure authoring/publication behaviour, but V2 introduces stricter separation between Catalogue, Pricing and Inventory and revised domain semantics.
Action: review class-by-class during catalogue re-entry. Preserve only catalogue-owned facts and state transitions; remove price/inventory truth from Catalogue.

### Catalogue infrastructure project
Reason: project boundary is useful, but package versions currently depend on old persistence/migration semantics and old dependency versions.
Action: retain project only if its responsibilities are narrowed to Catalogue persistence/adapters and regenerated from V2 schema ownership.

### `.github/workflows/ci.yml`
Reason: strong foundation exists, but jobs/steps explicitly reference Catalogue/S02 migration tests and S02 browser evidence.
Action: generalize into foundation gates plus slice-specific jobs triggered by implemented capabilities. Keep secret scan, Node/.NET validation, real PostgreSQL and artifact evidence.

### `scripts/validate-catalogue-migrations.sh`
Reason: migration-state validation is valuable, but Catalogue-specific naming/assumptions should become a repeatable module migration validation pattern.
Action: replace usage with module-aware migration validation while retaining pending-model/clean-database/upgrade principles.

### `packages/design-tokens`
Reason: executable tokens are correct direction; V2 design baseline now requires Figma variables and code tokens to be one semantic system.
Action: audit token values/names against approved MASTER/Figma before UI-bearing implementation.

### Existing web/admin/customer components and S02 browser tests
Reason: likely reusable visual/flow assets, but their contract and ownership assumptions predate V2.
Action: preserve as candidates/reference; reapprove through slice design and adapt selectors/tests to V2 states/contracts.

## REPLACE

### `CatalogueDbContextModelSnapshot.cs`
Current implementation calls `CatalogueDbContext.Configure(modelBuilder)` from `BuildModel` rather than containing a frozen generated snapshot.
Reason: this defeats migration drift detection because the snapshot follows the live model instead of representing the migration state.
Action: delete/replace as part of a clean EF-generated migration baseline before feature persistence resumes.

### `202607280001_InitialCatalogue.cs` migration lineage
Reason: hand-condensed migration is known to be out of sync with the current model and mixes Catalogue-owned content with Pricing/Inventory truth. It lacks the normal generated migration/snapshot lineage needed for reliable model-change detection.
Action: do not build V2 on top of this lineage. Recreate an EF-generated baseline appropriate to pre-production state after module ownership is corrected.

### Catalogue-owned `price_versions`
Reason: V2 assigns authoritative price versions/quotes to Pricing, not Catalogue.
Action: move future price persistence to Pricing when VS-03/VS-07 requires it; Catalogue may retain only approved references/projections where justified.

### Catalogue-owned `Capacity` / `Availability` as inventory truth
Reason: V2 assigns capacity, holds, reservations and availability truth to Inventory.
Action: remove as authoritative Catalogue state. Catalogue/public discovery may consume an availability projection/label from Inventory.

### S02-specific CI release semantics
Examples: "Apply the S02 migration", "S02 browser...", `s02-browser-evidence` as general pipeline assumptions.
Reason: CI must be capability/slice-neutral at foundation level.
Action: replace with generic gates and named slice suites only where that slice exists.

## ARCHIVE

### Old S02 implementation decisions that conflict with V2
Includes: combined Catalogue/Pricing/Inventory persistence model, old requirement interpretation, obsolete migration assumptions, superseded slice scope.
Action: retain via Git history/PR #11 and design/prototype evidence; do not copy forward as target contracts.

### S02 visual/prototype evidence
Reason: useful as functional/visual reference where it does not conflict with Landing + Package + MASTER + later approved Figma.
Action: archive as evidence, not canonical screen authority.

## VERIFY BEFORE FIRST CODE CHANGE

### `.agents/skills/ui-ux-pro-max/SKILL.md`
Expected status: previously confirmed installed.
VS-00 action: verify current branch/worktree file before design work.

### `.agents/skills/ponytail/SKILL.md`
Expected status: previously confirmed installed.
VS-00 action: verify before implementation/refactoring.

### Impeccable skill/plugin
Status: approved for workflow but not yet verified installed.
Action: install/verify before first design-bearing slice.

### Emil/design-engineering skill/resource
Status: approved for interaction/motion review but not yet verified installed.
Action: install/verify only before relevant design execution; do not block non-UI foundation tasks.

### Local repository worktree
Known risk: earlier local S02 debugging may still have temporary diagnostic migration/project changes and unrelated untracked skill directories.
Action: before local implementation, inspect `git status`, restore/clean only explicitly identified temporary files, never use blanket `git add .` or destructive cleanup.

## Repository decisions frozen by this register
1. No repository-wide rewrite.
2. Preserve the existing app/web/src/tests/packages structure.
3. Do not create empty projects for every future module during VS-00.
4. Do not extend the old Catalogue migration lineage.
5. Separate Pricing and Inventory truth before their feature slices begin.
6. Keep CI capabilities but remove S02-specific assumptions from foundation gates.
7. Existing UI/code is reusable only after V2 contract/design review.
8. Git history/PR #11 preserves superseded work; no need to keep incompatible runtime structures merely for history.

## Immediate next implementation decision
Before editing runtime code, create the minimum VS-00 ADR set in this order:
1. ADR — Module, schema, DbContext and migration ownership convention.
2. ADR — Identity provider + local/CI authentication strategy.
3. ADR — Transactional Outbox implementation/dispatcher semantics.
4. ADR — Minimal hosting/IaC target for DEV/STAGING/PROD.
5. ADR — Figma variables/design-token synchronization contract.

Only ADRs 1–3 are required before backend foundation changes; hosting and design-token ADRs may proceed in parallel before VS-00 closes.