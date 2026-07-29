#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -lt 2 ]; then
  echo "usage: $0 <project-path> <dbcontext> [startup-project]" >&2
  exit 64
fi

project="$1"
context="$2"
startup="${3:-apps/api/NoorPath.Api.csproj}"

if [ ! -f "$project" ]; then
  echo "migration project not found: $project" >&2
  exit 66
fi

if [ ! -f "$startup" ]; then
  echo "startup project not found: $startup" >&2
  exit 66
fi

echo "Validating EF model/migration parity for $context"
dotnet ef migrations has-pending-model-changes \
  --project "$project" \
  --startup-project "$startup" \
  --context "$context" \
  --no-build

migration_count="$(find "$(dirname "$project")/Migrations" -maxdepth 1 -type f -name '*.cs' ! -name '*ModelSnapshot.cs' ! -name '*.Designer.cs' 2>/dev/null | wc -l | tr -d ' ')"
if [ "$migration_count" -lt 1 ]; then
  echo "no generated migrations found for $context" >&2
  exit 65
fi

snapshot_count="$(find "$(dirname "$project")/Migrations" -maxdepth 1 -type f -name '*ModelSnapshot.cs' 2>/dev/null | wc -l | tr -d ' ')"
if [ "$snapshot_count" -ne 1 ]; then
  echo "expected exactly one generated model snapshot for $context; found $snapshot_count" >&2
  exit 65
fi

echo "Migration metadata is structurally present for $context. Database upgrade validation is exercised by PostgreSQL-backed integration tests."
