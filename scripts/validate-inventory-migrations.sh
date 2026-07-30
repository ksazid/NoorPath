#!/usr/bin/env bash
set -euo pipefail

exec "$(dirname "$0")/validate-module-migrations.sh" \
  "src/Modules/NoorPath.Inventory.Infrastructure/NoorPath.Inventory.Infrastructure.csproj" \
  "InventoryDbContext" \
  "apps/api/NoorPath.Api.csproj" \
  "NOORPATH_INVENTORY_TEST_DB"
