#!/usr/bin/env bash
set -euo pipefail

exec "$(dirname "$0")/validate-module-migrations.sh" \
  "src/Modules/NoorPath.Traveller.Infrastructure/NoorPath.Traveller.Infrastructure.csproj" \
  "TravellerDbContext" \
  "apps/api/NoorPath.Api.csproj" \
  "NOORPATH_TRAVELLER_TEST_DB"
