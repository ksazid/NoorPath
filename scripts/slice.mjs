#!/usr/bin/env node

import { existsSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const manifestDir = join(root, "delivery", "slices");
const completedPath = join(root, "delivery", "completed-slices.json");
const requiredGates = [
  "format",
  "static-analysis",
  "unit",
  "integration",
  "architecture",
  "migration",
  "security",
  "accessibility",
  "rendered-regression",
  "journey-linking",
  "telemetry",
  "product-owner",
];

function readJson(path) {
  return JSON.parse(readFileSync(path, "utf8"));
}

function manifests() {
  if (!existsSync(manifestDir)) return [];
  return readdirSync(manifestDir)
    .filter((name) => /^VS-\d{2}\.json$/.test(name))
    .sort()
    .map((name) => ({ path: join(manifestDir, name), value: readJson(join(manifestDir, name)) }));
}

function completedSlices() {
  return existsSync(completedPath) ? new Set(readJson(completedPath).completed ?? []) : new Set();
}

function validateManifest(manifest, allIds, completed) {
  const errors = [];
  const requiredStrings = ["id", "title", "slug", "outcome", "actor", "specPath", "checklistPath"];
  for (const key of requiredStrings) {
    if (typeof manifest[key] !== "string" || manifest[key].trim() === "") errors.push(`${manifest.id ?? "unknown"}: ${key} is required`);
  }
  if (!/^VS-\d{2}$/.test(manifest.id ?? "")) errors.push(`${manifest.id ?? "unknown"}: id must match VS-00`);
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(manifest.slug ?? "")) errors.push(`${manifest.id ?? "unknown"}: slug must be kebab-case`);

  for (const key of ["dependsOn", "modules", "routes", "acceptance", "exclusions", "qualityGates"]) {
    if (!Array.isArray(manifest[key]) || manifest[key].length === 0) errors.push(`${manifest.id}: ${key} must be a non-empty array`);
  }

  for (const dependency of manifest.dependsOn ?? []) {
    if (!completed.has(dependency) && !allIds.has(dependency)) errors.push(`${manifest.id}: unknown dependency ${dependency}`);
  }

  for (const gate of requiredGates) {
    if (!(manifest.qualityGates ?? []).includes(gate)) errors.push(`${manifest.id}: missing required quality gate ${gate}`);
  }

  if (typeof manifest.rendered !== "object" || manifest.rendered === null) {
    errors.push(`${manifest.id}: rendered configuration is required`);
  } else if (manifest.rendered.enabled && !manifest.rendered.testFile) {
    errors.push(`${manifest.id}: rendered.testFile is required when rendered review is enabled`);
  }

  return errors;
}

function validateAll() {
  const loaded = manifests();
  const ids = loaded.map(({ value }) => value.id);
  const duplicateIds = ids.filter((id, index) => ids.indexOf(id) !== index);
  const allIds = new Set(ids);
  const completed = completedSlices();
  const errors = duplicateIds.map((id) => `duplicate manifest id ${id}`);

  for (const { value } of loaded) errors.push(...validateManifest(value, allIds, completed));

  for (const { value } of loaded) {
    for (const key of ["specPath", "checklistPath"]) {
      if (!existsSync(join(root, value[key]))) errors.push(`${value.id}: missing ${value[key]}`);
    }
  }

  if (errors.length > 0) {
    console.error(errors.map((error) => `- ${error}`).join("\n"));
    process.exitCode = 1;
    return;
  }

  console.log(`Validated ${loaded.length} slice manifests: ${ids.join(", ")}`);
}

function findManifest(idOrBranch, optional = false) {
  const token = String(idOrBranch ?? "").toLowerCase();
  const match = manifests().find(({ value }) => {
    const idToken = value.id.toLowerCase();
    return token === idToken || token.includes(idToken.replace("-", "")) || token.includes(value.slug);
  });
  if (!match && !optional) throw new Error(`No slice manifest matches '${idOrBranch}'`);
  return match?.value ?? null;
}

function renderSpec(manifest) {
  return `# ${manifest.id} — ${manifest.title}\n\n## Status\nSpecification prepared. Implementation must remain Draft until exact-head certification and Product Owner acceptance.\n\n## Outcome\n${manifest.outcome}\n\n## Actor\n${manifest.actor}\n\n## Dependencies\n${manifest.dependsOn.map((item) => `- ${item}`).join("\n")}\n\n## Modules and ownership\n${manifest.modules.map((item) => `- ${item}`).join("\n")}\n\n## Customer journey and routes\n${manifest.routes.map((item) => `- ${item}`).join("\n")}\n\n## Acceptance criteria\n${manifest.acceptance.map((item) => `- [ ] ${item}`).join("\n")}\n\n## Explicit exclusions\n${manifest.exclusions.map((item) => `- ${item}`).join("\n")}\n\n## Quality and merge rule\nThe slice is mergeable only when every gate in the implementation checklist is complete on the exact final SHA, rendered evidence is available where applicable, no unresolved review thread remains, and the Product Owner has recorded acceptance.\n`;
}

function renderChecklist(manifest) {
  const gateLabels = {
    format: "Formatting",
    "static-analysis": "Type checking, linting and static analysis",
    unit: "Unit tests",
    integration: "Integration and contract tests",
    architecture: "Architecture and ownership tests",
    migration: "Deterministic migration and clean-database validation",
    security: "Authentication, authorization, privacy and secret scanning",
    accessibility: "Keyboard, focus, semantics, target size and axe checks",
    "rendered-regression": "Desktop, mobile, 200% text and reduced-motion rendered evidence",
    "journey-linking": "Route and customer-journey linking",
    telemetry: "Logs, traces, metrics and failure evidence",
    "product-owner": "Product Owner acceptance on the exact certified SHA",
  };

  return `# ${manifest.id} — ${manifest.title} Implementation Checklist\n\n## Development mode\n- [ ] Keep the PR Draft while implementation is changing.\n- [ ] Do not apply the \`certify\` label until the complete feature and all required states are implemented.\n- [ ] Remove the \`certify\` label before further development after a failed or superseded certification.\n\n## Product completeness\n${manifest.acceptance.map((item) => `- [ ] ${item}`).join("\n")}\n\n## Certification gates\n${manifest.qualityGates.map((gate) => `- [ ] ${gateLabels[gate] ?? gate}`).join("\n")}\n\n## Final merge gate\n- [ ] Full CI passed on the exact final SHA.\n- [ ] Rendered Slice Review passed on the exact final SHA when enabled.\n- [ ] Evidence artifact and certification comment reference the exact final SHA.\n- [ ] No unresolved review thread or known regression remains.\n- [ ] \`po-approved\` is present only after Product Owner review.\n- [ ] NoorPath Merge Gate is successful.\n`;
}

function scaffold(id) {
  const manifest = findManifest(id);
  for (const [path, content] of [
    [manifest.specPath, renderSpec(manifest)],
    [manifest.checklistPath, renderChecklist(manifest)],
  ]) {
    const absolute = join(root, path);
    if (existsSync(absolute)) {
      console.log(`exists: ${path}`);
      continue;
    }
    writeFileSync(absolute, content, "utf8");
    console.log(`created: ${path}`);
  }
  console.log(`branch: agent/${manifest.id.toLowerCase().replace("-", "")}-${manifest.slug}`);
  console.log(`next: keep Draft; apply 'certify' only after implementation and local review are complete`);
}

function status(id) {
  const manifest = findManifest(id);
  const checklist = readFileSync(join(root, manifest.checklistPath), "utf8");
  const complete = (checklist.match(/- \[x\]/gi) ?? []).length;
  const open = (checklist.match(/- \[ \]/g) ?? []).length;
  console.log(`${manifest.id} ${manifest.title}: ${complete} complete, ${open} open`);
}

function resolveManifest(args) {
  const optional = args.includes("--optional");
  const branchFlag = args.indexOf("--branch");
  const branch = branchFlag >= 0 ? args[branchFlag + 1] : process.env.GITHUB_HEAD_REF || process.env.GITHUB_REF_NAME || "";
  const manifest = findManifest(branch, optional);
  const output = {
    slice_id: manifest?.id ?? "NONE",
    title: manifest?.title ?? "Non-slice change",
    enabled: String(Boolean(manifest?.rendered?.enabled)),
    test_file: manifest?.rendered?.testFile ?? "",
    artifact_name: manifest ? `${manifest.id.toLowerCase()}-rendered-review` : "rendered-review",
    spec_path: manifest?.specPath ?? "",
    checklist_path: manifest?.checklistPath ?? "",
  };

  if (args.includes("--github-output") && process.env.GITHUB_OUTPUT) {
    for (const [key, value] of Object.entries(output)) writeFileSync(process.env.GITHUB_OUTPUT, `${key}=${value}\n`, { flag: "a" });
  } else {
    console.log(JSON.stringify(output, null, 2));
  }
}

const [command = "validate", ...args] = process.argv.slice(2);
try {
  if (command === "validate") validateAll();
  else if (command === "new") scaffold(args[0]);
  else if (command === "status") status(args[0]);
  else if (command === "resolve") resolveManifest(args);
  else throw new Error(`Unknown command '${command}'`);
} catch (error) {
  console.error(error instanceof Error ? error.message : error);
  process.exitCode = 1;
}
