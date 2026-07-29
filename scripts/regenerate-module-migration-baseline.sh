#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -lt 4 ]; then
  echo "usage: $0 <project-path> <dbcontext> <migration-name> <startup-project>" >&2
  exit 64
fi

project="$1"
context="$2"
migration_name="$3"
startup="$4"
project_dir="$(dirname "$project")"
migrations_dir="$project_dir/Migrations"

if [ ! -f "$project" ] || [ ! -f "$startup" ]; then
  echo "project or startup project not found" >&2
  exit 66
fi

if ! git diff --quiet || ! git diff --cached --quiet; then
  echo "working tree has tracked changes; commit or stash them before regenerating migrations" >&2
  exit 65
fi

if [ -n "$(git ls-files --others --exclude-standard "$migrations_dir" 2>/dev/null)" ]; then
  echo "untracked migration files exist under $migrations_dir; resolve them first" >&2
  exit 65
fi

backup_dir="$(mktemp -d)"
cleanup() { rm -rf "$backup_dir"; }
trap cleanup EXIT

if [ -d "$migrations_dir" ]; then
  cp -R "$migrations_dir" "$backup_dir/Migrations"
fi

restore_backup() {
  rm -rf "$migrations_dir"
  if [ -d "$backup_dir/Migrations" ]; then
    cp -R "$backup_dir/Migrations" "$migrations_dir"
  fi
}

trap 'echo "migration regeneration failed; restoring previous migration directory" >&2; restore_backup; cleanup' ERR

rm -rf "$migrations_dir"
mkdir -p "$migrations_dir"

dotnet ef migrations add "$migration_name" \
  --project "$project" \
  --startup-project "$startup" \
  --context "$context" \
  --output-dir Migrations

./scripts/validate-module-migrations.sh "$project" "$context" "$startup"

echo "Generated a fresh EF baseline for $context at $migrations_dir"
echo "Review the migration carefully before committing. This helper does not apply it to any database."
