"use client";

import { useEffect, useState } from "react";

type OccupancyKey = "double" | "triple" | "quad";
type CapabilityState = "ready" | "saving" | "saved" | "conflict" | "error";
type LoadState =
  "loading" | "ready" | "unauthenticated" | "forbidden" | "not-found" | "error";
type OccupancyValues = Record<OccupancyKey, string>;
type FieldErrors = Record<string, string>;

type PricingResponse = {
  version: number;
  currency: string;
  occupancies: Array<{ occupancy: OccupancyKey; amount: number }>;
};

type InventoryResponse = {
  version: number;
  pools: Array<{
    occupancy: OccupancyKey;
    capacity: number;
    availableQuantity: number;
  }>;
};

type CommercialResponse = {
  departureId: string;
  pricing: PricingResponse | null;
  inventory: InventoryResponse | null;
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

type CommercialLoadResult =
  | { state: "ready"; body: CommercialResponse }
  | { state: Exclude<LoadState, "loading" | "ready"> };

const occupancies: ReadonlyArray<{
  key: OccupancyKey;
  label: string;
  detail: string;
}> = [
  { key: "double", label: "Double", detail: "Two adults sharing one room" },
  { key: "triple", label: "Triple", detail: "Three adults sharing one room" },
  { key: "quad", label: "Quad", detail: "Four adults sharing one room" },
];

export const emptyOccupancyValues = (): OccupancyValues => ({
  double: "",
  triple: "",
  quad: "",
});

function sameValues(left: OccupancyValues, right: OccupancyValues) {
  return occupancies.every(({ key }) => left[key] === right[key]);
}

export function validatePricing(
  currency: string,
  values: OccupancyValues,
): FieldErrors {
  const errors: FieldErrors = {};
  if (!/^[A-Za-z]{3}$/.test(currency.trim())) {
    errors.currency = "Use a three-letter currency code such as INR.";
  }

  const configured = occupancies.filter(({ key }) => values[key].trim() !== "");
  if (configured.length === 0) {
    errors.pricing = "Add a price for at least one supported occupancy.";
  }

  configured.forEach(({ key, label }) => {
    const raw = values[key].trim();
    if (!/^\d+(?:\.\d{1,2})?$/.test(raw) || Number(raw) <= 0) {
      errors[`price.${key}`] =
        `${label} price must be greater than zero with at most two decimal places.`;
    }
  });

  return errors;
}

export function validateInventory(
  values: OccupancyValues,
  reason: string,
): FieldErrors {
  const errors: FieldErrors = {};
  const configured = occupancies.filter(({ key }) => values[key].trim() !== "");

  if (configured.length === 0) {
    errors.inventory = "Add capacity for at least one supported occupancy.";
  }

  configured.forEach(({ key, label }) => {
    const raw = values[key].trim();
    if (!/^\d+$/.test(raw)) {
      errors[`capacity.${key}`] =
        `${label} capacity must be a whole number of zero or more.`;
    }
  });

  const cleanReason = reason.trim();
  if (!cleanReason) {
    errors.reason = "Explain why this capacity is being set or changed.";
  } else if (cleanReason.length > 240) {
    errors.reason = "Adjustment reason must be 240 characters or fewer.";
  }

  return errors;
}

function requestHeaders(json = false): HeadersInit {
  const headers: Record<string, string> = {};
  if (json) headers["Content-Type"] = "application/json";
  const testIdentity = process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY;
  if (testIdentity) headers["X-NoorPath-Test-Identity"] = testIdentity;
  return headers;
}

function valuesFromPricing(pricing: PricingResponse | null): OccupancyValues {
  const values = emptyOccupancyValues();
  pricing?.occupancies.forEach((item) => {
    values[item.occupancy] = String(item.amount);
  });
  return values;
}

function valuesFromInventory(
  inventory: InventoryResponse | null,
): OccupancyValues {
  const values = emptyOccupancyValues();
  inventory?.pools.forEach((item) => {
    values[item.occupancy] = String(item.capacity);
  });
  return values;
}

function apiErrors(problem: ProblemDetails): FieldErrors {
  return Object.fromEntries(
    Object.entries(problem.errors ?? {}).map(([key, values]) => [
      key,
      values[0] ?? "Review this field.",
    ]),
  );
}

async function fetchCommercial(
  departureId: string,
): Promise<CommercialLoadResult> {
  try {
    const response = await fetch(
      `/api/v1/operator/departures/${departureId}/commercial`,
      {
        cache: "no-store",
        credentials: "include",
        headers: requestHeaders(),
      },
    );

    if (response.status === 401) return { state: "unauthenticated" };
    if (response.status === 403) return { state: "forbidden" };
    if (response.status === 404) return { state: "not-found" };
    if (!response.ok) return { state: "error" };

    return {
      state: "ready",
      body: (await response.json()) as CommercialResponse,
    };
  } catch {
    return { state: "error" };
  }
}

function capabilityButtonLabel(
  state: CapabilityState,
  dirty: boolean,
  version: number,
  capability: "Pricing" | "Inventory",
) {
  if (state === "saving") return `Saving ${capability.toLowerCase()}…`;
  if (dirty) return `Save ${capability.toLowerCase()}`;
  return version > 0 ? `${capability} saved` : `${capability} not configured`;
}

function CapabilityNotice({
  state,
  problem,
  savedCopy,
  onReload,
}: {
  state: CapabilityState;
  problem: string;
  savedCopy: string;
  onReload: () => void;
}) {
  if (!["saved", "conflict", "error"].includes(state)) return null;

  return (
    <div
      className={`commercial-notice ${state}`}
      role={state === "saved" ? "status" : "alert"}
      aria-live="polite"
    >
      <strong>
        {state === "saved"
          ? savedCopy
          : state === "conflict"
            ? "A newer version exists"
            : "Changes not saved"}
      </strong>
      {state !== "saved" && <span>{problem}</span>}
      {state === "conflict" && (
        <button type="button" onClick={onReload}>
          Reload this section
        </button>
      )}
    </div>
  );
}

export default function CommercialEditor({
  departureId,
  onDirtyChange,
  onBusyChange,
}: {
  departureId?: string;
  onDirtyChange: (dirty: boolean) => void;
  onBusyChange: (busy: boolean) => void;
}) {
  const [loadState, setLoadState] = useState<LoadState>(
    departureId ? "loading" : "ready",
  );
  const [pricingState, setPricingState] = useState<CapabilityState>("ready");
  const [inventoryState, setInventoryState] =
    useState<CapabilityState>("ready");
  const [pricingProblem, setPricingProblem] = useState("");
  const [inventoryProblem, setInventoryProblem] = useState("");
  const [pricingErrors, setPricingErrors] = useState<FieldErrors>({});
  const [inventoryErrors, setInventoryErrors] = useState<FieldErrors>({});
  const [currency, setCurrency] = useState("");
  const [savedCurrency, setSavedCurrency] = useState("");
  const [prices, setPrices] = useState<OccupancyValues>(emptyOccupancyValues);
  const [savedPrices, setSavedPrices] =
    useState<OccupancyValues>(emptyOccupancyValues);
  const [capacities, setCapacities] =
    useState<OccupancyValues>(emptyOccupancyValues);
  const [savedCapacities, setSavedCapacities] =
    useState<OccupancyValues>(emptyOccupancyValues);
  const [adjustmentReason, setAdjustmentReason] = useState("");
  const [pricingVersion, setPricingVersion] = useState(0);
  const [inventoryVersion, setInventoryVersion] = useState(0);

  const pricingDirty =
    currency !== savedCurrency || !sameValues(prices, savedPrices);
  const inventoryDirty = !sameValues(capacities, savedCapacities);
  const busy = pricingState === "saving" || inventoryState === "saving";

  useEffect(
    () => onDirtyChange(pricingDirty || inventoryDirty),
    [inventoryDirty, onDirtyChange, pricingDirty],
  );
  useEffect(() => onBusyChange(busy), [busy, onBusyChange]);

  useEffect(() => {
    if (!departureId) return;

    let active = true;
    void fetchCommercial(departureId).then((result) => {
      if (!active) return;

      if (result.state !== "ready") {
        setLoadState(result.state);
        return;
      }

      const pricingValues = valuesFromPricing(result.body.pricing);
      const inventoryValues = valuesFromInventory(result.body.inventory);
      setPricingVersion(result.body.pricing?.version ?? 0);
      setCurrency(result.body.pricing?.currency ?? "");
      setSavedCurrency(result.body.pricing?.currency ?? "");
      setPrices(pricingValues);
      setSavedPrices(pricingValues);
      setInventoryVersion(result.body.inventory?.version ?? 0);
      setCapacities(inventoryValues);
      setSavedCapacities(inventoryValues);
      setAdjustmentReason("");
      setPricingErrors({});
      setInventoryErrors({});
      setPricingProblem("");
      setInventoryProblem("");
      setPricingState("ready");
      setInventoryState("ready");
      setLoadState("ready");
    });

    return () => {
      active = false;
    };
  }, [departureId]);

  const applyPricing = (pricing: PricingResponse | null) => {
    const next = valuesFromPricing(pricing);
    setPricingVersion(pricing?.version ?? 0);
    setCurrency(pricing?.currency ?? "");
    setSavedCurrency(pricing?.currency ?? "");
    setPrices(next);
    setSavedPrices(next);
    setPricingErrors({});
    setPricingProblem("");
    setPricingState("ready");
  };

  const applyInventory = (inventory: InventoryResponse | null) => {
    const next = valuesFromInventory(inventory);
    setInventoryVersion(inventory?.version ?? 0);
    setCapacities(next);
    setSavedCapacities(next);
    setAdjustmentReason("");
    setInventoryErrors({});
    setInventoryProblem("");
    setInventoryState("ready");
  };

  const reload = async (scope: "all" | "pricing" | "inventory") => {
    if (!departureId) return;
    if (scope === "all") setLoadState("loading");

    const result = await fetchCommercial(departureId);
    if (result.state !== "ready") {
      if (scope === "pricing") {
        setPricingProblem(
          "We couldn’t reload pricing. Your inventory edits are untouched.",
        );
        setPricingState("error");
      } else if (scope === "inventory") {
        setInventoryProblem(
          "We couldn’t reload inventory. Your pricing edits are untouched.",
        );
        setInventoryState("error");
      } else {
        setLoadState(result.state);
      }
      return;
    }

    if (scope === "all" || scope === "pricing")
      applyPricing(result.body.pricing);
    if (scope === "all" || scope === "inventory") {
      applyInventory(result.body.inventory);
    }
    setLoadState("ready");
  };

  const changePrice = (key: OccupancyKey, value: string) => {
    setPrices((current) => ({ ...current, [key]: value }));
    setPricingErrors((current) => {
      const next = { ...current };
      delete next[`price.${key}`];
      delete next.pricing;
      return next;
    });
    if (["saved", "error"].includes(pricingState)) setPricingState("ready");
    if (pricingState !== "conflict") setPricingProblem("");
  };

  const changeCapacity = (key: OccupancyKey, value: string) => {
    setCapacities((current) => ({ ...current, [key]: value }));
    setInventoryErrors((current) => {
      const next = { ...current };
      delete next[`capacity.${key}`];
      delete next.inventory;
      return next;
    });
    if (["saved", "error"].includes(inventoryState)) setInventoryState("ready");
    if (inventoryState !== "conflict") setInventoryProblem("");
  };

  const savePricing = async () => {
    if (!departureId) return;
    const validation = validatePricing(currency, prices);
    setPricingErrors(validation);
    if (Object.keys(validation).length > 0) return;

    setPricingState("saving");
    setPricingProblem("");
    try {
      const response = await fetch(
        `/api/v1/operator/departures/${departureId}/pricing`,
        {
          method: "PUT",
          credentials: "include",
          headers: requestHeaders(true),
          body: JSON.stringify({
            expectedVersion: pricingVersion,
            currency: currency.trim(),
            occupancies: occupancies
              .filter(({ key }) => prices[key].trim() !== "")
              .map(({ key }) => ({
                occupancy: key,
                amount: Number(prices[key]),
              })),
          }),
        },
      );

      if (response.status === 401) {
        setPricingState("ready");
        setLoadState("unauthenticated");
        return;
      }
      if (response.status === 403) {
        setPricingState("ready");
        setLoadState("forbidden");
        return;
      }
      if (response.status === 404) {
        setPricingState("ready");
        setLoadState("not-found");
        return;
      }

      const body = (await response.json()) as PricingResponse & ProblemDetails;
      if (response.status === 409) {
        setPricingProblem(body.detail ?? "Pricing changed in another session.");
        setPricingState("conflict");
        return;
      }
      if (response.status === 422) {
        setPricingErrors(apiErrors(body));
        setPricingState("ready");
        return;
      }
      if (!response.ok) {
        throw new Error(body.detail ?? body.title ?? "pricing save failed");
      }

      const next = valuesFromPricing(body);
      setPricingVersion(body.version);
      setCurrency(body.currency);
      setSavedCurrency(body.currency);
      setPrices(next);
      setSavedPrices(next);
      setPricingState("saved");
    } catch {
      setPricingProblem(
        "We couldn’t save pricing. Your entries are still here; retry safely.",
      );
      setPricingState("error");
    }
  };

  const saveInventory = async () => {
    if (!departureId) return;
    const validation = validateInventory(capacities, adjustmentReason);
    setInventoryErrors(validation);
    if (Object.keys(validation).length > 0) return;

    setInventoryState("saving");
    setInventoryProblem("");
    try {
      const response = await fetch(
        `/api/v1/operator/departures/${departureId}/inventory`,
        {
          method: "PUT",
          credentials: "include",
          headers: requestHeaders(true),
          body: JSON.stringify({
            expectedVersion: inventoryVersion,
            adjustmentReason: adjustmentReason.trim(),
            pools: occupancies
              .filter(({ key }) => capacities[key].trim() !== "")
              .map(({ key }) => ({
                occupancy: key,
                capacity: Number(capacities[key]),
              })),
          }),
        },
      );

      if (response.status === 401) {
        setInventoryState("ready");
        setLoadState("unauthenticated");
        return;
      }
      if (response.status === 403) {
        setInventoryState("ready");
        setLoadState("forbidden");
        return;
      }
      if (response.status === 404) {
        setInventoryState("ready");
        setLoadState("not-found");
        return;
      }

      const body = (await response.json()) as InventoryResponse &
        ProblemDetails;
      if (response.status === 409) {
        setInventoryProblem(
          body.detail ?? "Inventory changed in another session.",
        );
        setInventoryState("conflict");
        return;
      }
      if (response.status === 422) {
        setInventoryErrors(apiErrors(body));
        setInventoryState("ready");
        return;
      }
      if (!response.ok) {
        throw new Error(body.detail ?? body.title ?? "inventory save failed");
      }

      const next = valuesFromInventory(body);
      setInventoryVersion(body.version);
      setCapacities(next);
      setSavedCapacities(next);
      setAdjustmentReason("");
      setInventoryState("saved");
    } catch {
      setInventoryProblem(
        "We couldn’t save inventory. Your entries are still here; retry safely.",
      );
      setInventoryState("error");
    }
  };

  const savedReady = (key: OccupancyKey) =>
    savedPrices[key] !== "" && Number(savedCapacities[key]) > 0;

  if (!departureId) {
    return (
      <section
        className="form-card commercial-locked-card"
        aria-labelledby="commercial-locked-title"
      >
        <div className="form-card-heading">
          <span>06</span>
          <div>
            <h2 id="commercial-locked-title">Pricing & inventory</h2>
            <p>
              Save the journey draft first. Commercial facts need a stable
              departure identity.
            </p>
          </div>
        </div>
        <div className="commercial-lock-note" role="note">
          Pricing and inventory remain separate private configurations. Neither
          makes the journey public.
        </div>
      </section>
    );
  }

  if (loadState !== "ready") {
    const copy: Record<Exclude<LoadState, "ready">, [string, string]> = {
      loading: [
        "Loading commercial configuration",
        "Checking saved pricing and inventory facts.",
      ],
      unauthenticated: [
        "Sign in required",
        "Sign in to configure operator pricing and inventory.",
      ],
      forbidden: [
        "Commercial access unavailable",
        "Your account cannot configure this operator departure.",
      ],
      "not-found": [
        "Departure unavailable",
        "This departure is unavailable or belongs to another operator.",
      ],
      error: [
        "Commercial configuration unavailable",
        "Check the connection and retry without changing the journey draft.",
      ],
    };
    const [title, detail] = copy[loadState];

    return (
      <section
        className="form-card commercial-state-card"
        role="status"
        aria-live="polite"
      >
        <div className="form-card-heading">
          <span>06</span>
          <div>
            <h2>{title}</h2>
            <p>{detail}</p>
          </div>
        </div>
        {loadState === "error" && (
          <button
            className="secondary-button"
            type="button"
            onClick={() => void reload("all")}
          >
            Retry commercial load
          </button>
        )}
      </section>
    );
  }

  return (
    <section className="commercial-editor" aria-labelledby="commercial-title">
      <div className="commercial-heading">
        <div>
          <span className="eyebrow">
            Pricing & inventory · Private configuration
          </span>
          <h2 id="commercial-title">Configure what can be sold later</h2>
          <p>
            Double, Triple and Quad are the only supported VS-03 occupancies.
            Pricing and inventory save independently; publication comes later.
          </p>
        </div>
        <span className="commercial-private-pill">Draft only</span>
      </div>

      <div className="commercial-grid">
        <section
          className="form-card commercial-capability-card"
          aria-labelledby="pricing-title"
        >
          <div className="form-card-heading">
            <span>06</span>
            <div>
              <h2 id="pricing-title">Occupancy pricing</h2>
              <p>Record explicit currency and adult room-sharing prices.</p>
            </div>
          </div>

          <CapabilityNotice
            state={pricingState}
            problem={pricingProblem}
            savedCopy={`Pricing version ${pricingVersion} saved`}
            onReload={() => void reload("pricing")}
          />

          {Object.keys(pricingErrors).length > 0 && (
            <div className="commercial-validation" role="alert">
              <strong>Review pricing</strong>
              <ul>
                {Object.values(pricingErrors).map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            </div>
          )}

          <fieldset
            disabled={pricingState === "saving" || pricingState === "conflict"}
          >
            <label className="commercial-currency-field">
              <span>Currency *</span>
              <input
                aria-invalid={Boolean(pricingErrors.currency)}
                autoComplete="off"
                inputMode="text"
                maxLength={3}
                placeholder="INR"
                value={currency}
                onChange={(event) => {
                  setCurrency(event.target.value.toUpperCase());
                  setPricingErrors((current) => {
                    const next = { ...current };
                    delete next.currency;
                    return next;
                  });
                  if (["saved", "error"].includes(pricingState)) {
                    setPricingState("ready");
                  }
                }}
              />
              <small>
                Three-letter code; no currency is assumed by the server.
              </small>
            </label>

            <div className="commercial-row-list">
              {occupancies.map(({ key, label, detail }) => (
                <label className="commercial-row" key={key}>
                  <span className="commercial-row-copy">
                    <strong>{label}</strong>
                    <small>{detail}</small>
                  </span>
                  <span className="commercial-input-wrap">
                    <span aria-hidden="true">{currency || "CUR"}</span>
                    <input
                      aria-label={`${label} price`}
                      aria-invalid={Boolean(pricingErrors[`price.${key}`])}
                      inputMode="decimal"
                      placeholder="Not configured"
                      value={prices[key]}
                      onChange={(event) => changePrice(key, event.target.value)}
                    />
                  </span>
                </label>
              ))}
            </div>
          </fieldset>

          <div className="commercial-card-footer">
            <span>Version {pricingVersion || "—"}</span>
            <button
              className="secondary-button commercial-save-button"
              type="button"
              disabled={
                !pricingDirty ||
                pricingState === "saving" ||
                pricingState === "conflict"
              }
              onClick={() => void savePricing()}
            >
              {capabilityButtonLabel(
                pricingState,
                pricingDirty,
                pricingVersion,
                "Pricing",
              )}
            </button>
          </div>
        </section>

        <section
          className="form-card commercial-capability-card"
          aria-labelledby="inventory-title"
        >
          <div className="form-card-heading">
            <span>07</span>
            <div>
              <h2 id="inventory-title">Sellable capacity</h2>
              <p>
                Capacity is authoritative here; available quantity is derived.
              </p>
            </div>
          </div>

          <CapabilityNotice
            state={inventoryState}
            problem={inventoryProblem}
            savedCopy={`Inventory version ${inventoryVersion} saved`}
            onReload={() => void reload("inventory")}
          />

          {Object.keys(inventoryErrors).length > 0 && (
            <div className="commercial-validation" role="alert">
              <strong>Review inventory</strong>
              <ul>
                {Object.values(inventoryErrors).map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            </div>
          )}

          <fieldset
            disabled={
              inventoryState === "saving" || inventoryState === "conflict"
            }
          >
            <div className="commercial-row-list">
              {occupancies.map(({ key, label, detail }) => (
                <label className="commercial-row" key={key}>
                  <span className="commercial-row-copy">
                    <strong>{label}</strong>
                    <small>{detail}</small>
                  </span>
                  <span className="commercial-input-wrap capacity">
                    <input
                      aria-label={`${label} capacity`}
                      aria-invalid={Boolean(inventoryErrors[`capacity.${key}`])}
                      inputMode="numeric"
                      placeholder="Not configured"
                      value={capacities[key]}
                      onChange={(event) =>
                        changeCapacity(key, event.target.value)
                      }
                    />
                    <span aria-hidden="true">places</span>
                  </span>
                </label>
              ))}
            </div>

            <label className="commercial-reason-field">
              <span>
                Adjustment reason{" "}
                {inventoryDirty && <em aria-hidden="true">*</em>}
              </span>
              <textarea
                aria-invalid={Boolean(inventoryErrors.reason)}
                maxLength={240}
                rows={3}
                placeholder="e.g. Initial room allocation confirmed by operations"
                value={adjustmentReason}
                onChange={(event) => {
                  setAdjustmentReason(event.target.value);
                  setInventoryErrors((current) => {
                    const next = { ...current };
                    delete next.reason;
                    return next;
                  });
                }}
              />
              <small>
                Required on every capacity write and stored in the inventory
                audit trail.
              </small>
            </label>
          </fieldset>

          <div className="commercial-card-footer">
            <span>Version {inventoryVersion || "—"}</span>
            <button
              className="secondary-button commercial-save-button"
              type="button"
              disabled={
                !inventoryDirty ||
                inventoryState === "saving" ||
                inventoryState === "conflict"
              }
              onClick={() => void saveInventory()}
            >
              {capabilityButtonLabel(
                inventoryState,
                inventoryDirty,
                inventoryVersion,
                "Inventory",
              )}
            </button>
          </div>
        </section>
      </div>

      <section
        className="form-card commercial-readiness-card"
        aria-labelledby="readiness-title"
      >
        <div className="form-card-heading">
          <span>08</span>
          <div>
            <h2 id="readiness-title">Commercial readiness</h2>
            <p>
              Readiness means saved price plus positive saved capacity. It does
              not mean published or bookable.
            </p>
          </div>
        </div>
        <div className="commercial-readiness-grid">
          {occupancies.map(({ key, label }) => {
            const rowDirty =
              currency !== savedCurrency ||
              prices[key] !== savedPrices[key] ||
              capacities[key] !== savedCapacities[key];
            const ready = savedReady(key);
            let status = "Incomplete";
            let statusClass = "incomplete";
            if (rowDirty) {
              status = "Unsaved edits";
              statusClass = "unsaved";
            } else if (ready) {
              status = "Ready for later review";
              statusClass = "ready";
            }

            return (
              <div className="commercial-readiness-row" key={key}>
                <strong>{label}</strong>
                <span className={statusClass}>{status}</span>
              </div>
            );
          })}
        </div>
      </section>
    </section>
  );
}
