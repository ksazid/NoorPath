#!/usr/bin/env bash
set -euo pipefail

bash ./scripts/validate-module-migrations.sh \
  src/Modules/NoorPath.Operators.Infrastructure/NoorPath.Operators.Infrastructure.csproj \
  OperatorsDbContext \
  apps/api/NoorPath.Api.csproj
