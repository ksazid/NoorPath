#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const registry = JSON.parse(readFileSync(join(root, "delivery", "modules.json"), "utf8"));
const modules = registry.modules ?? [];

function fail(message) {
  console.error(message);
  process.exit(1);
}

function connectionFor(database) {
  const base = process.env.NOORPATH_TEST_DB;
  if (!base) fail("NOORPATH_TEST_DB is required");
  const parts = base
    .split(";")
    .filter(Boolean)
    .filter((part) => !part.toLowerCase().startsWith("database="));
  parts.push(`Database=${database}`);
  return parts.join(";");
}

function run(command, args, options = {}) {
  const result = spawnSync(command, args, {
    cwd: root,
    env: { ...process.env, ...options.env },
    stdio: "inherit",
    shell: false,
  });
  if (result.error) throw result.error;
  if (result.status !== 0) process.exit(result.status ?? 1);
}

function validateRegistry() {
  const names = new Set();
  for (const module of modules) {
    for (const key of ["name", "project", "context", "database", "environment"]) {
      if (!module[key]) fail(`Module registry entry is missing ${key}`);
    }
    if (names.has(module.name)) fail(`Duplicate module registry entry: ${module.name}`);
    names.add(module.name);
  }
  console.log(`Validated ${modules.length} persistence module registrations.`);
}

function createDatabases(container) {
  if (!container) fail("create-databases requires a PostgreSQL container id");
  for (const module of modules) {
    console.log(`Creating ${module.database}`);
    run("docker", [
      "exec",
      container,
      "psql",
      "-v",
      "ON_ERROR_STOP=1",
      "-U",
      "noorpath",
      "-d",
      "noorpath_test",
      "-c",
      `CREATE DATABASE ${module.database};`,
    ]);
  }
}

function validateMigrations() {
  for (const module of modules) {
    console.log(`Validating ${module.name} migrations`);
    run(
      "bash",
      [
        "scripts/validate-module-migrations.sh",
        module.project,
        module.context,
        "apps/api/NoorPath.Api.csproj",
        module.environment,
      ],
      {
        env: {
          [module.environment]: connectionFor(module.database),
        },
      },
    );
  }
}

const [command = "validate-registry", argument] = process.argv.slice(2);
if (command === "validate-registry") validateRegistry();
else if (command === "create-databases") createDatabases(argument);
else if (command === "validate") validateMigrations();
else fail(`Unknown command '${command}'`);
