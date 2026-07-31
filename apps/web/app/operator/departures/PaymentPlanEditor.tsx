"use client";

import { useCallback, useEffect, useState } from "react";

type LoadState =
  | "idle"
  | "loading"
  | "ready"
  | "saving"
  | "saved"
  | "conflict"
  | "unauthenticated"
  | "forbidden"
  | "not-found"
  | "error";

type PaymentPlanResponse = {
  pricingVersion: number;
  paymentPlan: {
    enabled: true;
    depositPercent: number;
    instalmentDayOfMonth: number;
    finalPaymentDueDaysBeforeDeparture: number;
  } | null;
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
};

type FormState = {
  enabled: boolean;
  depositPercent: string;
  instalmentDayOfMonth: string;
  finalPaymentDueDaysBeforeDeparture: string;
};

const emptyForm: FormState = {
  enabled: false,
  depositPercent: "20",
  instalmentDayOfMonth: "5",
  finalPaymentDueDaysBeforeDeparture: "30",
};

function requestHeaders(json = false): HeadersInit {
  const headers: Record<string, string> = {};
  if (json) headers["Content-Type"] = "application/json";
  const testIdentity = process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY;
  if (testIdentity) headers["X-NoorPath-Test-Identity"] = testIdentity;
  return headers;
}

function responseToForm(response: PaymentPlanResponse): FormState {
  if (!response.paymentPlan) return emptyForm;
  return {
    enabled: true,
    depositPercent: String(response.paymentPlan.depositPercent),
    instalmentDayOfMonth: String(response.paymentPlan.instalmentDayOfMonth),
    finalPaymentDueDaysBeforeDeparture: String(
      response.paymentPlan.finalPaymentDueDaysBeforeDeparture,
    ),
  };
}

function sameForm(left: FormState, right: FormState) {
  return (
    left.enabled === right.enabled &&
    left.depositPercent === right.depositPercent &&
    left.instalmentDayOfMonth === right.instalmentDayOfMonth &&
    left.finalPaymentDueDaysBeforeDeparture ===
      right.finalPaymentDueDaysBeforeDeparture
  );
}

export default function PaymentPlanEditor({
  departureId,
  onDirtyChange,
  onBusyChange,
}: {
  departureId?: string;
  onDirtyChange: (dirty: boolean) => void;
  onBusyChange: (busy: boolean) => void;
}) {
  const [state, setState] = useState<LoadState>(departureId ? "loading" : "idle");
  const [pricingVersion, setPricingVersion] = useState(0);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [savedForm, setSavedForm] = useState<FormState>(emptyForm);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [problem, setProblem] = useState("");
  const dirty = !sameForm(form, savedForm);

  useEffect(() => onDirtyChange(dirty), [dirty, onDirtyChange]);
  useEffect(
    () => onBusyChange(state === "loading" || state === "saving"),
    [onBusyChange, state],
  );

  const load = useCallback(async () => {
    if (!departureId) return;
    setState("loading");
    setProblem("");
    try {
      const response = await fetch(
        `/api/v1/operator/departures/${departureId}/payment-plan`,
        {
          cache: "no-store",
          credentials: "include",
          headers: requestHeaders(),
        },
      );
      if (response.status === 401) return setState("unauthenticated");
      if (response.status === 403) return setState("forbidden");
      if (response.status === 404) return setState("not-found");
      if (!response.ok) throw new Error("payment plan unavailable");

      const body = (await response.json()) as PaymentPlanResponse;
      const next = responseToForm(body);
      setPricingVersion(body.pricingVersion);
      setForm(next);
      setSavedForm(next);
      setErrors({});
      setState("ready");
    } catch {
      setProblem("We couldn’t load the payment plan. Retry safely.");
      setState("error");
    }
  }, [departureId]);

  useEffect(() => {
    if (!departureId) return;
    const pending = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(pending);
  }, [departureId, load]);

  const change = <K extends keyof FormState>(key: K, value: FormState[K]) => {
    setForm((current) => ({ ...current, [key]: value }));
    setErrors((current) => {
      const next = { ...current };
      delete next[`paymentPlan.${key}`];
      delete next.paymentPlan;
      return next;
    });
    if (["saved", "conflict", "error"].includes(state)) setState("ready");
  };

  const save = async () => {
    if (!departureId) return;
    setState("saving");
    setProblem("");
    setErrors({});
    try {
      const response = await fetch(
        `/api/v1/operator/departures/${departureId}/payment-plan`,
        {
          method: "PUT",
          credentials: "include",
          headers: requestHeaders(true),
          body: JSON.stringify({
            expectedPricingVersion: pricingVersion,
            enabled: form.enabled,
            depositPercent: form.enabled ? Number(form.depositPercent) : null,
            instalmentDayOfMonth: form.enabled
              ? Number(form.instalmentDayOfMonth)
              : null,
            finalPaymentDueDaysBeforeDeparture: form.enabled
              ? Number(form.finalPaymentDueDaysBeforeDeparture)
              : null,
          }),
        },
      );
      const body = (await response.json()) as PaymentPlanResponse & ProblemDetails;
      if (response.status === 401) return setState("unauthenticated");
      if (response.status === 403) return setState("forbidden");
      if (response.status === 404) return setState("not-found");
      if (response.status === 422) {
        setErrors(
          Object.fromEntries(
            Object.entries(body.errors ?? {}).map(([key, values]) => [
              key,
              values[0] ?? "Review this field.",
            ]),
          ),
        );
        setState("ready");
        return;
      }
      if (response.status === 409) {
        setProblem(
          body.detail ??
            "Pricing changed. Reload the payment plan before saving again.",
        );
        setState("conflict");
        return;
      }
      if (!response.ok) throw new Error("save failed");

      const next = responseToForm(body);
      setPricingVersion(body.pricingVersion);
      setForm(next);
      setSavedForm(next);
      setState("saved");
    } catch {
      setProblem("We couldn’t save the payment plan. Your entries are preserved.");
      setState("error");
    }
  };

  if (!departureId) {
    return (
      <section className="form-card payment-plan-card is-muted">
        <div className="form-card-heading">
          <span>07</span>
          <div>
            <h2>Customer payment plan</h2>
            <p>Save the catalogue draft before configuring customer payments.</p>
          </div>
        </div>
      </section>
    );
  }

  return (
    <section className="form-card payment-plan-card" aria-busy={state === "loading" || state === "saving"}>
      <div className="form-card-heading">
        <span>07</span>
        <div>
          <h2>Customer payment plan</h2>
          <p>
            Define the published rules NoorPath will use to calculate each
            customer’s authoritative instalment schedule.
          </p>
        </div>
      </div>

      {pricingVersion === 0 && state !== "loading" ? (
        <div className="commercial-notice conflict" role="status">
          <strong>Pricing is required first</strong>
          <span>
            Save occupancy pricing, then refresh this section before enabling an
            instalment plan.
          </span>
          <button type="button" onClick={() => void load()}>
            Check pricing again
          </button>
        </div>
      ) : null}

      {state === "loading" ? (
        <p className="payment-plan-state" role="status">Loading payment plan…</p>
      ) : null}
      {state === "unauthenticated" ? (
        <p className="payment-plan-state" role="alert">Sign in again to continue.</p>
      ) : null}
      {state === "forbidden" ? (
        <p className="payment-plan-state" role="alert">You do not have access to change this payment plan.</p>
      ) : null}
      {state === "not-found" ? (
        <p className="payment-plan-state" role="alert">This draft is no longer available for editing.</p>
      ) : null}
      {state === "error" || state === "conflict" ? (
        <div className={`commercial-notice ${state}`} role="alert">
          <strong>{state === "conflict" ? "Pricing changed" : "Payment plan not saved"}</strong>
          <span>{problem}</span>
          <button type="button" onClick={() => void load()}>
            Reload payment plan
          </button>
        </div>
      ) : null}
      {state === "saved" ? (
        <div className="commercial-notice saved" role="status">
          <strong>Payment plan saved</strong>
          <span>
            Publication will freeze these rules with the immutable price version.
          </span>
        </div>
      ) : null}

      <fieldset
        className="payment-plan-fieldset"
        disabled={pricingVersion === 0 || state === "loading" || state === "saving"}
      >
        <label className="payment-plan-toggle">
          <input
            type="checkbox"
            checked={form.enabled}
            onChange={(event) => change("enabled", event.target.checked)}
          />
          <span>
            <strong>Offer instalments for this departure</strong>
            <small>
              If disabled, the customer’s authoritative quote requires the full
              amount at the next payment step.
            </small>
          </span>
        </label>

        {form.enabled ? (
          <div className="payment-plan-grid">
            <label>
              <span>Due initially</span>
              <div className="payment-plan-input-suffix">
                <input
                  inputMode="decimal"
                  value={form.depositPercent}
                  onChange={(event) => change("depositPercent", event.target.value)}
                  aria-invalid={Boolean(errors["paymentPlan.depositPercent"])}
                />
                <span>%</span>
              </div>
              <small>
                {errors["paymentPlan.depositPercent"] ??
                  "Percentage of the final customer quote due first."}
              </small>
            </label>
            <label>
              <span>Monthly due day</span>
              <input
                type="number"
                min="1"
                max="28"
                value={form.instalmentDayOfMonth}
                onChange={(event) => change("instalmentDayOfMonth", event.target.value)}
                aria-invalid={Boolean(errors["paymentPlan.instalmentDayOfMonth"])}
              />
              <small>
                {errors["paymentPlan.instalmentDayOfMonth"] ??
                  "Use day 1–28 so every month has a valid due date."}
              </small>
            </label>
            <label>
              <span>Final balance due</span>
              <div className="payment-plan-input-suffix">
                <input
                  type="number"
                  min="0"
                  max="180"
                  value={form.finalPaymentDueDaysBeforeDeparture}
                  onChange={(event) =>
                    change(
                      "finalPaymentDueDaysBeforeDeparture",
                      event.target.value,
                    )
                  }
                  aria-invalid={Boolean(
                    errors["paymentPlan.finalPaymentDueDaysBeforeDeparture"],
                  )}
                />
                <span>days before</span>
              </div>
              <small>
                {errors["paymentPlan.finalPaymentDueDaysBeforeDeparture"] ??
                  "NoorPath derives the number of instalments from when the pilgrim books."}
              </small>
            </label>
          </div>
        ) : null}
      </fieldset>

      <div className="payment-plan-footer">
        <span>
          {pricingVersion > 0
            ? `Pricing version ${pricingVersion}`
            : "Waiting for pricing"}
        </span>
        <button
          className="secondary-button"
          type="button"
          disabled={pricingVersion === 0 || !dirty || state === "saving"}
          onClick={() => void save()}
        >
          {state === "saving"
            ? "Saving payment plan…"
            : dirty
              ? "Save payment plan"
              : "Payment plan saved"}
        </button>
      </div>
    </section>
  );
}
