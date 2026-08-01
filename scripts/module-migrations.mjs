#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { spawnSync } from "node:child_process";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const registry = JSON.parse(
  readFileSync(join(root, "delivery", "modules.json"), "utf8"),
);
const modules = registry.modules ?? [];
const extraDatabases = registry.extraDatabases ?? [];
const databases = [...modules, ...extraDatabases];

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

function registeredEnvironment() {
  return Object.fromEntries(
    databases.map((entry) => [
      entry.environment,
      connectionFor(entry.database),
    ]),
  );
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
  const databaseNames = new Set();
  const environments = new Set();

  for (const entry of databases) {
    for (const key of ["name", "database", "environment"]) {
      if (!entry[key]) fail(`Database registry entry is missing ${key}`);
    }
    if (names.has(entry.name)) fail(`Duplicate registry name: ${entry.name}`);
    if (databaseNames.has(entry.database)) {
      fail(`Duplicate database registration: ${entry.database}`);
    }
    if (environments.has(entry.environment)) {
      fail(`Duplicate environment registration: ${entry.environment}`);
    }
    names.add(entry.name);
    databaseNames.add(entry.database);
    environments.add(entry.environment);
  }

  for (const module of modules) {
    for (const key of ["project", "context"]) {
      if (!module[key])
        fail(`Persistence module ${module.name} is missing ${key}`);
    }
  }

  console.log(
    `Validated ${modules.length} migration modules and ${extraDatabases.length} additional test databases.`,
  );
}

function createDatabases(container) {
  if (!container) fail("create-databases requires a PostgreSQL container id");
  for (const entry of databases) {
    console.log(`Creating ${entry.database}`);
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
      `CREATE DATABASE ${entry.database};`,
    ]);
  }
}

function validateMigrations() {
  const environment = registeredEnvironment();
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
      { env: environment },
    );
  }
}

function runTests() {
  run(
    "dotnet",
    ["test", "NoorPath.slnx", "--configuration", "Release", "--no-build"],
    { env: registeredEnvironment() },
  );
}

const [command = "validate-registry", argument] = process.argv.slice(2);
if (command === "validate-registry") validateRegistry();
else if (command === "create-databases") createDatabases(argument);
else if (command === "validate") validateMigrations();
else if (command === "test") runTests();
else fail(`Unknown command '${command}'`);
