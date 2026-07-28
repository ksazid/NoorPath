#!/usr/bin/env bash
set -euo pipefail

# S01 deliberately has no persistence model. This guard prevents an EF Core
# dependency or migration from being introduced without replacing this check
# with model-aware migration validation in the product slice that owns it.
if find apps src -type f -name '*.csproj' -print0 | xargs -0 grep -Eiq 'Microsoft\.EntityFrameworkCore'; then
  echo 'EF Core was introduced; replace the S01 migration guard with model-aware validation.' >&2
  exit 1
fi

if find apps src -type f \( -path '*/Migrations/*' -o -name '*ModelSnapshot.cs' \) -print -quit | grep -q .; then
  echo 'Migration artifacts exist although S01 has no persistence model.' >&2
  exit 1
fi

echo 'Migration state valid: the foundation contains no EF Core model or migrations.'
