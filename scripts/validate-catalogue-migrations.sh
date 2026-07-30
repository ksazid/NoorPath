#!/usr/bin/env bash
set -euo pipefail

bash ./scripts/validate-module-migrations.sh \
  src/Modules/NoorPath.Catalogue.Infrastructure/NoorPath.Catalogue.Infrastructure.csproj \
  CatalogueDbContext \
  apps/api/NoorPath.Api.csproj \
  NOORPATH_CATALOGUE_TEST_DB
