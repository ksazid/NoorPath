# ADR-001 — Module, PostgreSQL Schema, DbContext and Migration Ownership

Status: Accepted for V2 foundation
Date: 2026-07-29

## Context
NoorPath is a modular monolith. V2 requires clear capability ownership without introducing microservices or distributed persistence. The existing Catalogue persistence mixes catalogue, pricing and inventory concerns and its migration/snapshot lineage is not reliable enough to extend safely.

## Decision
1. NoorPath uses one managed PostgreSQL database per environment for MVP.
2. Major domain capabilities own logical PostgreSQL schemas.
3. Each major persistence-owning module uses its own EF Core `DbContext` boundary where practical.
4. A module's DbContext maps only its own schema/tables.
5. Cross-module relationships use stable identifiers/contracts, not shared EF navigation graphs.
6. No module writes another module's tables directly.
7. Each module owns its migration history and model snapshot.
8. Migrations must be EF-generated and reproducible; hand-condensed migrations or live-model snapshots are prohibited.
9. A merged migration is immutable except for an approved corrective process before release.
10. CI validates: no pending unexplained model changes, clean-database migration to head, and supported previous-release to head migration where relevant.
11. Risky schema evolution uses expand → migrate/backfill → contract.
12. No generic repository/UoW layer is required; application/infrastructure code may use the owning DbContext directly where appropriate.
13. Cross-module transactions are not created merely because modules share one database. Coordination uses explicit contracts/events and transactional Outbox where needed.

## Initial schema ownership
- `identity`
- `operators`
- `catalogue`
- `pricing`
- `inventory`
- `traveller`
- `booking`
- `payments`
- `documents`
- `visa`
- `notifications`
- `support`
- `audit`
- `reporting`
- `platform_config` where justified

Schemas are created only when the owning module is actually implemented; VS-00 must not create empty future-module projects/tables.

## Project topology
Preserve the repository shape:
- `apps/api`
- `apps/web`
- `src/Modules/<Module>`
- `tests/...`

A major module may use one project or separate domain/infrastructure projects depending on actual complexity. The boundary is semantic ownership, not a mandatory project-count pattern.

## Migration policy
For each persistence-owning module:
- migrations live with that module's infrastructure/persistence implementation;
- one generated snapshot represents that DbContext at migration head;
- migration names are descriptive and ordered by EF tooling conventions;
- CI uses a real PostgreSQL instance;
- destructive operations require review;
- production schema changes are applied only through approved migrations/release automation;
- manual production DDL is an exception and must be reconciled back into migrations.

## Existing Catalogue migration disposition
The current Catalogue migration lineage must not be extended as the V2 persistence baseline because it:
- includes pricing authority inside Catalogue;
- includes capacity/availability that belongs to Inventory;
- uses a custom snapshot that delegates to the current model instead of preserving a frozen generated model.

Before feature persistence resumes, VS-00 will replace/reset the development migration baseline in a controlled manner. Existing Git history and PR evidence preserve the old implementation; it is not deleted from history.

## Cross-module referential integrity
Within a module, ordinary foreign keys are encouraged where they protect local invariants.

Across module schemas, default to stable external IDs without ORM navigation. Cross-module validity is protected through owner validation, commitment snapshots, integration events/reconciliation and tests. A cross-schema FK may be approved only when it does not undermine ownership or independent evolution.

## Consequences
### Positive
- prevents Catalogue/Pricing/Inventory ownership collapse;
- reduces migration blast radius;
- makes module boundaries testable;
- supports later extraction if ever justified without forcing it now;
- avoids global DbContext/model coupling.

### Costs
- more DbContexts/migration sets over time;
- some cross-module queries require projections/composition rather than ORM navigation;
- developers must explicitly manage module boundaries.

These costs are accepted because they protect domain ownership while retaining a single PostgreSQL deployment.

## Rejected alternatives
### One global NoorPathDbContext
Rejected because it encourages shared entity graphs, cross-module writes and one migration history for unrelated capabilities.

### Separate database per module
Rejected for MVP because it adds deployment, transaction, backup, observability and operational complexity without demonstrated need.

### Generic repository + generic unit of work
Rejected because it obscures EF Core semantics and does not provide meaningful domain isolation by itself.

### Microservices
Rejected for MVP; logical boundaries are sufficient until evidence requires independent deployment/scaling.

## Verification
VS-00 exit evidence must prove:
- architecture tests prevent persistence leakage across modules;
- a module migration can build an empty schema from scratch;
- generated snapshot matches migration head;
- no pending unexplained model changes;
- CI PostgreSQL migration validation is deterministic.
