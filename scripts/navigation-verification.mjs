#!/usr/bin/env node

import { existsSync, readFileSync, readdirSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const manifestDir = join(root, "delivery", "slices");
const navigationGate = "navigation-reachability";
const firstRequiredSliceNumber = 16;
const outcomePattern =
  /\|\s*(PENDING|VERIFIED|BLOCKED_IDENTITY|NOT_APPLICABLE|FAILED)\s*\|/gi;

function readJson(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

function manifests() {
  if (!existsSync(manifestDir)) return [];
  return readdirSync(manifestDir)
    .filter((name) => /^VS-\d{2}\.json$/.test(name))
    .sort()
    .map((name) => ({
      path: join(manifestDir, name),
      value: readJson(join(manifestDir, name)),
    }));
}

function sliceNumber(manifest) {
  const match = /^VS-(\d+)$/i.exec(String(manifest.id ?? ""));
  return match ? Number.parseInt(match[1], 10) : null;
}

function requiresNavigation(manifest) {
  const number = sliceNumber(manifest);
  return (
    (number !== null && number >= firstRequiredSliceNumber) ||
    (manifest.qualityGates ?? []).includes(navigationGate)
  );
}

function findManifest(branch, optional) {
  const token = String(branch ?? "").toLowerCase();
  const match = manifests().find(({ value }) => {
    const idToken = String(value.id ?? "").toLowerCase();
    return (
      token === idToken ||
      token.includes(idToken.replace("-", "")) ||
      token.includes(String(value.slug ?? "").toLowerCase())
    );
  });

  if (!match && !optional) {
    throw new Error(`No slice manifest matches '${branch}'`);
  }
  return match?.value ?? null;
}

function validateRegistrations() {
  const errors = [];
  const registered = [];

  for (const { value } of manifests()) {
    if (!requiresNavigation(value)) continue;

    if (!(value.qualityGates ?? []).includes(navigationGate)) {
      errors.push(
        `${value.id}: qualityGates must include ${navigationGate} for VS-${String(firstRequiredSliceNumber).padStart(2, "0")} and later`,
      );
    }

    if (
      typeof value.navigationPath !== "string" ||
      value.navigationPath.trim() === ""
    ) {
      errors.push(
        `${value.id}: navigationPath is required when ${navigationGate} is enabled`,
      );
      continue;
    }

    const absolute = join(root, value.navigationPath);
    if (!existsSync(absolute)) {
      errors.push(`${value.id}: missing ${value.navigationPath}`);
      continue;
    }

    registered.push(`${value.id}:${value.navigationPath}`);
  }

  if (errors.length > 0) {
    throw new Error(errors.map((error) => `- ${error}`).join("\n"));
  }

  console.log(
    registered.length > 0
      ? `Validated navigation matrices: ${registered.join(", ")}`
      : "No slice currently declares the navigation-reachability gate.",
  );
}

function verifyMatrix(manifest) {
  if (!manifest) {
    console.log(
      "No slice resolved; navigation certification is not applicable.",
    );
    return;
  }

  if (!requiresNavigation(manifest)) {
    console.log(
      `${manifest.id} predates mandatory ${navigationGate} verification.`,
    );
    return;
  }

  const matrixPath = join(root, manifest.navigationPath);
  const content = readFileSync(matrixPath, "utf8");
  const outcomes = [...content.matchAll(outcomePattern)].map((match) =>
    match[1].toUpperCase(),
  );

  if (outcomes.length === 0) {
    throw new Error(
      `${manifest.id}: ${manifest.navigationPath} contains no recorded navigation outcomes`,
    );
  }

  const blocking = outcomes.filter(
    (outcome) => outcome === "PENDING" || outcome === "FAILED",
  );
  if (blocking.length > 0) {
    const counts = blocking.reduce((summary, outcome) => {
      summary[outcome] = (summary[outcome] ?? 0) + 1;
      return summary;
    }, {});
    throw new Error(
      `${manifest.id}: navigation certification blocked by ${Object.entries(
        counts,
      )
        .map(([outcome, count]) => `${count} ${outcome}`)
        .join(", ")} row(s) in ${manifest.navigationPath}`,
    );
  }

  console.log(
    `${manifest.id}: navigation matrix is certification-ready with ${outcomes.length} recorded outcome(s).`,
  );
}

function option(args, name) {
  const index = args.indexOf(name);
  return index >= 0 ? args[index + 1] : null;
}

const [command = "validate", ...args] = process.argv.slice(2);

try {
  if (command === "validate") {
    validateRegistrations();
  } else if (command === "verify") {
    validateRegistrations();
    const branch =
      option(args, "--branch") ||
      process.env.GITHUB_HEAD_REF ||
      process.env.GITHUB_REF_NAME ||
      "";
    verifyMatrix(findManifest(branch, args.includes("--optional")));
  } else {
    throw new Error(`Unknown command '${command}'`);
  }
} catch (error) {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
}
