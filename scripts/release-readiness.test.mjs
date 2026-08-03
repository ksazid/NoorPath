import assert from "node:assert/strict";
import test from "node:test";
import { validateReleaseConfiguration } from "./release-readiness.mjs";

const exactSha = "a".repeat(40);

function configuration() {
  return {
    id: "noorpath-pilot-v1",
    releaseScope: Array.from(
      { length: 18 },
      (_, index) => `VS-${String(index).padStart(2, "0")}`,
    ),
    deferredCapabilities: ["Production refunds remain disabled"],
    thresholds: {
      p95ApiLatencyMs: null,
      maximumErrorRatePercent: null,
      maximumConcurrentCheckouts: null,
    },
    recovery: {
      rpoMinutes: null,
      rtoMinutes: null,
      backupRetentionDays: null,
      restoreOwner: null,
    },
    monitoring: {
      alertOwner: null,
      escalationTarget: null,
      requiredSignals: [
        "api_readiness_failure",
        "payment_failure",
        "confirmation_exception",
        "inventory_commitment_failure",
        "document_processing_failure",
        "visa_transition_failure",
        "cancellation_failure",
        "refund_reconciliation_failure",
        "migration_failure",
      ],
    },
    features: {
      refundExecution: { enabled: false, approved: false },
      documentStorage: { enabled: false, approved: false },
      externalNotifications: { enabled: false, approved: false },
    },
    releaseControls: {
      releaseOperator: null,
      rollbackAuthority: null,
      changeWindow: null,
      observationMinutes: null,
      productionDeploymentRequiresSeparateApproval: true,
    },
    postDeploymentSmokeTests: [
      "live",
      "ready",
      "discovery",
      "customer sign-in",
      "operator sign-in",
    ],
  };
}

function completeDecisions(value) {
  value.thresholds = {
    p95ApiLatencyMs: 1500,
    maximumErrorRatePercent: 1,
    maximumConcurrentCheckouts: 10,
  };
  value.recovery = {
    rpoMinutes: 60,
    rtoMinutes: 120,
    backupRetentionDays: 7,
    restoreOwner: "release-operator",
  };
  value.monitoring.alertOwner = "release-operator";
  value.monitoring.escalationTarget = "product-owner";
  value.releaseControls.releaseOperator = "release-operator";
  value.releaseControls.rollbackAuthority = "product-owner";
  value.releaseControls.changeWindow = "approved-change-window";
  value.releaseControls.observationMinutes = 30;
  return value;
}

test("planning configuration can remain structurally valid with unresolved decisions", () => {
  const result = validateReleaseConfiguration(configuration());
  assert.deepEqual(result.errors, []);
});

test("certification fails closed while production decisions are unresolved", () => {
  const result = validateReleaseConfiguration(configuration(), {
    mode: "certify",
    sha: exactSha,
  });
  assert.ok(result.errors.some((error) => error.includes("remain unresolved")));
  assert.ok(result.unresolved.includes("thresholds.p95ApiLatencyMs"));
  assert.ok(result.unresolved.includes("releaseControls.rollbackAuthority"));
});

test("completed decisions and exact SHA satisfy certification validation", () => {
  const result = validateReleaseConfiguration(
    completeDecisions(configuration()),
    {
      mode: "certify",
      sha: exactSha,
    },
  );
  assert.deepEqual(result.errors, []);
  assert.deepEqual(result.unresolved, []);
});

test("high-risk features cannot be enabled without explicit approval", () => {
  const value = configuration();
  value.features.refundExecution.enabled = true;

  const result = validateReleaseConfiguration(value);
  assert.ok(
    result.errors.includes(
      "features.refundExecution cannot be enabled without explicit approval.",
    ),
  );
});

test("unknown or missing slices invalidate the release scope", () => {
  const value = configuration();
  value.releaseScope = value.releaseScope.filter((slice) => slice !== "VS-12");
  value.releaseScope.push("VS-99");

  const result = validateReleaseConfiguration(value);
  assert.ok(result.errors.includes("releaseScope is missing VS-12."));
  assert.ok(
    result.errors.includes("releaseScope contains unknown slice VS-99."),
  );
});

test("certification rejects a non-exact commit reference", () => {
  const result = validateReleaseConfiguration(
    completeDecisions(configuration()),
    {
      mode: "certify",
      sha: "main",
    },
  );
  assert.ok(
    result.errors.includes(
      "Certification requires an exact 40-character commit SHA.",
    ),
  );
});
