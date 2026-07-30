#!/usr/bin/env bash
set -euo pipefail

exec "$(dirname "$0")/validate-module-migrations.sh" \
  "src/Modules/NoorPath.Pricing.Infrastructure/NoorPath.Pricing.Infrastructure.csproj" \
  "PricingDbContext" \
  "apps/api/NoorPath.Api.csproj" \
  "NOORPATH_PRICING_TEST_DB"
