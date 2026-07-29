#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -lt 2 ]; then
  echo "usage: $0 <project-path> <dbcontext> [startup-project]" >&2
  exit 64
fi

project="$1"
context="$2"
startup="${3:-apps/api/NoorPath.Api.csproj}"
project_dir="$(dirname "$project")"
migrations_dir="$project_dir/Migrations"

if [ ! -f "$project" ]; then
  echo "migration project not found: $project" >&2
  exit 66
fi

if [ ! -f "$startup" ]; then
  echo "startup project not found: $startup" >&2
  exit 66
fi

if [ ! -d "$migrations_dir" ]; then
  echo "migrations directory not found: $migrations_dir" >&2
  exit 65
fi

snapshot="$(find "$migrations_dir" -maxdepth 1 -type f -name '*ModelSnapshot.cs' -print -quit)"
if [ -z "$snapshot" ]; then
  echo "generated model snapshot is required for $context" >&2
  exit 65
fi

# A snapshot must be frozen generated metadata. Delegating back to the live
# DbContext makes drift detection meaningless because the snapshot changes with
# the runtime model.
if rg -q '=>\s*[A-Za-z0-9_]+DbContext\.Configure\(modelBuilder\)' "$snapshot"; then
  echo "invalid live-model snapshot detected: $snapshot" >&2
  echo "regenerate the migration baseline with dotnet ef; do not hand-wire snapshots to DbContext.Configure" >&2
  exit 65
fi

migration_count="$(find "$migrations_dir" -maxdepth 1 -type f -name '*.cs' ! -name '*ModelSnapshot.cs' ! -name '*.Designer.cs' | wc -l | tr -d ' ')"
designer_count="$(find "$migrations_dir" -maxdepth 1 -type f -name '*.Designer.cs' | wc -l | tr -d ' ')"
snapshot_count="$(find "$migrations_dir" -maxdepth 1 -type f -name '*ModelSnapshot.cs' | wc -l | tr -d ' ')"

if [ "$migration_count" -lt 1 ]; then
  echo "no generated migrations found for $context" >&2
  exit 65
fi

if [ "$designer_count" -ne "$migration_count" ]; then
  echo "expected one generated .Designer.cs per migration for $context; migrations=$migration_count designers=$designer_count" >&2
  exit 65
fi

if [ "$snapshot_count" -ne 1 ]; then
  echo "expected exactly one generated model snapshot for $context; found $snapshot_count" >&2
  exit 65
fi

echo "Validating EF model/migration parity for $context"
dotnet ef migrations has-pending-model-changes \
  --project "$project" \
  --startup-project "$startup" \
  --context "$context" \
  --no-build

echo "Migration metadata and model parity are valid for $context. Database application/upgrade evidence remains PostgreSQL-backed integration-test responsibility."
