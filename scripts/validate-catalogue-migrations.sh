#!/usr/bin/env bash
set -euo pipefail
migration="src/Modules/NoorPath.Catalogue.Infrastructure/Migrations/202607280001_InitialCatalogue.cs"
snapshot="src/Modules/NoorPath.Catalogue.Infrastructure/Migrations/CatalogueDbContextModelSnapshot.cs"
test -f "$migration" && test -f "$snapshot"
for table in packages batches inclusions price_versions publication_audits; do
  rg -q "CreateTable\\(name: \"$table\"" "$migration"
done
rg -q 'IsConcurrencyToken' src/Modules/NoorPath.Catalogue.Infrastructure/CatalogueDbContext.cs
