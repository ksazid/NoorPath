import { mkdir, readFile, writeFile } from "node:fs/promises";
import path from "node:path";
import process from "node:process";
import { pathToFileURL } from "node:url";

const expectedScope = Array.from(
  { length: 35 },
  (_, index) => `VS-${String(index).padStart(2, "0")}`,
);

const requiredSignals = [
  "api_readiness_failure",
  "payment_failure",
  "confirmation_exception",
  "inventory_commitment_failure",
  "document_processing_failure",
  "visa_transition_failure",
  "cancellation_failure",
  "refund_reconciliation_failure",
  "migration_failure",
];

function isObject(value) {
  return value !== null && typeof value === "object" && !Array.isArray(value);
}

function isNonEmptyString(value) {
  return typeof value === "string" && value.trim().length > 0;
}

function isNullablePositiveNumber(value) {
  return value === null || (typeof value === "number" && value > 0);
}

function isNullablePercentage(value) {
  return (
    value === null || (typeof value === "number" && value >= 0 && value <= 100)
  );
}

function isNullableString(value) {
  return value === null || isNonEmptyString(value);
}

function addRequiredDecision(unresolved, pathName, value) {
  if (value === null || value === undefined || value === "")
    unresolved.push(pathName);
}

export function validateReleaseConfiguration(
  configuration,
  { mode = "structure", sha } = {},
) {
  const errors = [];
  const unresolved = [];

  if (!isObject(configuration)) {
    return {
      errors: ["Release configuration must be a JSON object."],
      unresolved,
    };
  }

  if (!isNonEmptyString(configuration.id))
    errors.push("id must be a non-empty string.");

  if (!Array.isArray(configuration.releaseScope)) {
    errors.push("releaseScope must be an array.");
  } else {
    const unique = new Set(configuration.releaseScope);
    if (unique.size !== configuration.releaseScope.length)
      errors.push("releaseScope must not contain duplicates.");
    for (const slice of expectedScope) {
      if (!unique.has(slice)) errors.push(`releaseScope is missing ${slice}.`);
    }
    for (const slice of unique) {
      if (!expectedScope.includes(slice))
        errors.push(`releaseScope contains unknown slice ${slice}.`);
    }
  }

  if (!Array.isArray(configuration.deferredCapabilities)) {
    errors.push("deferredCapabilities must be an array.");
  } else if (
    configuration.deferredCapabilities.some((item) => !isNonEmptyString(item))
  ) {
    errors.push("deferredCapabilities entries must be non-empty strings.");
  }

  const thresholds = configuration.thresholds;
  if (!isObject(thresholds)) {
    errors.push("thresholds must be an object.");
  } else {
    if (!isNullablePositiveNumber(thresholds.p95ApiLatencyMs))
      errors.push(
        "thresholds.p95ApiLatencyMs must be null or greater than zero.",
      );
    if (!isNullablePercentage(thresholds.maximumErrorRatePercent))
      errors.push(
        "thresholds.maximumErrorRatePercent must be null or between 0 and 100.",
      );
    if (!isNullablePositiveNumber(thresholds.maximumConcurrentCheckouts))
      errors.push(
        "thresholds.maximumConcurrentCheckouts must be null or greater than zero.",
      );
  }

  const recovery = configuration.recovery;
  if (!isObject(recovery)) {
    errors.push("recovery must be an object.");
  } else {
    if (!isNullablePositiveNumber(recovery.rpoMinutes))
      errors.push("recovery.rpoMinutes must be null or greater than zero.");
    if (!isNullablePositiveNumber(recovery.rtoMinutes))
      errors.push("recovery.rtoMinutes must be null or greater than zero.");
    if (!isNullablePositiveNumber(recovery.backupRetentionDays))
      errors.push(
        "recovery.backupRetentionDays must be null or greater than zero.",
      );
    if (!isNullableString(recovery.restoreOwner))
      errors.push("recovery.restoreOwner must be null or a non-empty string.");
  }

  const monitoring = configuration.monitoring;
  if (!isObject(monitoring)) {
    errors.push("monitoring must be an object.");
  } else {
    if (!isNullableString(monitoring.alertOwner))
      errors.push("monitoring.alertOwner must be null or a non-empty string.");
    if (!isNullableString(monitoring.escalationTarget))
      errors.push(
        "monitoring.escalationTarget must be null or a non-empty string.",
      );
    if (!Array.isArray(monitoring.requiredSignals)) {
      errors.push("monitoring.requiredSignals must be an array.");
    } else {
      for (const signal of requiredSignals) {
        if (!monitoring.requiredSignals.includes(signal))
          errors.push(`monitoring.requiredSignals is missing ${signal}.`);
      }
    }
  }

  const features = configuration.features;
  if (!isObject(features)) {
    errors.push("features must be an object.");
  } else {
    for (const featureName of [
      "refundExecution",
      "documentStorage",
      "externalNotifications",
    ]) {
      const feature = features[featureName];
      if (!isObject(feature)) {
        errors.push(`features.${featureName} must be an object.`);
        continue;
      }
      if (
        typeof feature.enabled !== "boolean" ||
        typeof feature.approved !== "boolean"
      ) {
        errors.push(
          `features.${featureName} enabled and approved must be boolean values.`,
        );
      } else if (feature.enabled && !feature.approved) {
        errors.push(
          `features.${featureName} cannot be enabled without explicit approval.`,
        );
      }
    }
  }

  const controls = configuration.releaseControls;
  if (!isObject(controls)) {
    errors.push("releaseControls must be an object.");
  } else {
    if (!isNullableString(controls.releaseOperator))
      errors.push(
        "releaseControls.releaseOperator must be null or a non-empty string.",
      );
    if (!isNullableString(controls.rollbackAuthority))
      errors.push(
        "releaseControls.rollbackAuthority must be null or a non-empty string.",
      );
    if (!isNullableString(controls.changeWindow))
      errors.push(
        "releaseControls.changeWindow must be null or a non-empty string.",
      );
    if (!isNullablePositiveNumber(controls.observationMinutes))
      errors.push(
        "releaseControls.observationMinutes must be null or greater than zero.",
      );
    if (controls.productionDeploymentRequiresSeparateApproval !== true)
      errors.push(
        "Production deployment must require a separate explicit approval.",
      );
  }

  if (!Array.isArray(configuration.postDeploymentSmokeTests)) {
    errors.push("postDeploymentSmokeTests must be an array.");
  } else if (
    configuration.postDeploymentSmokeTests.length < 5 ||
    configuration.postDeploymentSmokeTests.some(
      (item) => !isNonEmptyString(item),
    )
  ) {
    errors.push(
      "postDeploymentSmokeTests must contain at least five non-empty steps.",
    );
  }

  if (mode === "certify") {
    if (!/^[0-9a-f]{40}$/i.test(sha ?? ""))
      errors.push("Certification requires an exact 40-character commit SHA.");

    addRequiredDecision(
      unresolved,
      "thresholds.p95ApiLatencyMs",
      thresholds?.p95ApiLatencyMs,
    );
    addRequiredDecision(
      unresolved,
      "thresholds.maximumErrorRatePercent",
      thresholds?.maximumErrorRatePercent,
    );
    addRequiredDecision(
      unresolved,
      "thresholds.maximumConcurrentCheckouts",
      thresholds?.maximumConcurrentCheckouts,
    );
    addRequiredDecision(
      unresolved,
      "recovery.rpoMinutes",
      recovery?.rpoMinutes,
    );
    addRequiredDecision(
      unresolved,
      "recovery.rtoMinutes",
      recovery?.rtoMinutes,
    );
    addRequiredDecision(
      unresolved,
      "recovery.backupRetentionDays",
      recovery?.backupRetentionDays,
    );
    addRequiredDecision(
      unresolved,
      "recovery.restoreOwner",
      recovery?.restoreOwner,
    );
    addRequiredDecision(
      unresolved,
      "monitoring.alertOwner",
      monitoring?.alertOwner,
    );
    addRequiredDecision(
      unresolved,
      "monitoring.escalationTarget",
      monitoring?.escalationTarget,
    );
    addRequiredDecision(
      unresolved,
      "releaseControls.releaseOperator",
      controls?.releaseOperator,
    );
    addRequiredDecision(
      unresolved,
      "releaseControls.rollbackAuthority",
      controls?.rollbackAuthority,
    );
    addRequiredDecision(
      unresolved,
      "releaseControls.changeWindow",
      controls?.changeWindow,
    );
    addRequiredDecision(
      unresolved,
      "releaseControls.observationMinutes",
      controls?.observationMinutes,
    );

    if (unresolved.length > 0)
      errors.push(
        `Production decisions remain unresolved: ${unresolved.join(", ")}.`,
      );
  }

  return { errors, unresolved };
}

export async function loadReleaseConfiguration(configurationPath) {
  const raw = await readFile(configurationPath, "utf8");
  return JSON.parse(raw);
}

export async function writeReleaseEvidence({
  configuration,
  sha,
  outputDirectory,
}) {
  const validation = validateReleaseConfiguration(configuration, {
    mode: "certify",
    sha,
  });
  if (validation.errors.length > 0)
    throw new Error(validation.errors.join("\n"));

  await mkdir(outputDirectory, { recursive: true });
  const recordedAtUtc = new Date().toISOString();
  const evidence = {
    releaseId: configuration.id,
    exactSha: sha,
    recordedAtUtc,
    releaseScope: configuration.releaseScope,
    deferredCapabilities: configuration.deferredCapabilities,
    thresholds: configuration.thresholds,
    recovery: configuration.recovery,
    enabledFeatures: Object.entries(configuration.features)
      .filter(([, value]) => value.enabled)
      .map(([name]) => name),
    productionDeploymentAuthorized: false,
  };

  await writeFile(
    path.join(outputDirectory, "release-readiness.json"),
    `${JSON.stringify(evidence, null, 2)}\n`,
  );

  const markdown = [
    "# NoorPath production-readiness evidence",
    "",
    `- Release: ${evidence.releaseId}`,
    `- Exact SHA: \`${sha}\``,
    `- Recorded at: ${recordedAtUtc}`,
    `- Release-scope slices: ${configuration.releaseScope.join(", ")}`,
    `- P95 API latency threshold: ${configuration.thresholds.p95ApiLatencyMs} ms`,
    `- Maximum error rate: ${configuration.thresholds.maximumErrorRatePercent}%`,
    `- Maximum concurrent checkouts: ${configuration.thresholds.maximumConcurrentCheckouts}`,
    `- RPO: ${configuration.recovery.rpoMinutes} minutes`,
    `- RTO: ${configuration.recovery.rtoMinutes} minutes`,
    "- Production deployment authorized by this evidence: no",
    "",
    "Production deployment still requires the protected production environment and a separate explicit Product Owner approval.",
    "",
  ].join("\n");
  await writeFile(path.join(outputDirectory, "release-readiness.md"), markdown);

  return evidence;
}

function parseOptions(args) {
  const options = {};
  for (let index = 0; index < args.length; index += 1) {
    const key = args[index];
    if (!key.startsWith("--")) continue;
    options[key.slice(2)] = args[index + 1];
    index += 1;
  }
  return options;
}

async function main() {
  const [
    ,
    ,
    command = "validate",
    configurationPath = "delivery/releases/pilot-v1.json",
    ...rest
  ] = process.argv;
  const options = parseOptions(rest);
  const configuration = await loadReleaseConfiguration(configurationPath);

  if (command === "validate" || command === "certify") {
    const validation = validateReleaseConfiguration(configuration, {
      mode: command === "certify" ? "certify" : "structure",
      sha: options.sha,
    });
    if (validation.errors.length > 0) {
      for (const error of validation.errors) console.error(`- ${error}`);
      process.exitCode = 1;
      return;
    }
    console.log(`Validated ${configuration.id} in ${command} mode.`);
    if (command === "validate" && validation.unresolved.length > 0)
      console.log(
        `Unresolved production decisions: ${validation.unresolved.join(", ")}`,
      );
    return;
  }

  if (command === "evidence") {
    const outputDirectory = options.output ?? "artifacts/production-readiness";
    await writeReleaseEvidence({
      configuration,
      sha: options.sha,
      outputDirectory,
    });
    console.log(`Wrote release evidence to ${outputDirectory}.`);
    return;
  }

  throw new Error(`Unknown release-readiness command: ${command}`);
}

const executedDirectly =
  process.argv[1] &&
  import.meta.url === pathToFileURL(path.resolve(process.argv[1])).href;
if (executedDirectly) {
  main().catch((error) => {
    console.error(error instanceof Error ? error.message : error);
    process.exitCode = 1;
  });
}
