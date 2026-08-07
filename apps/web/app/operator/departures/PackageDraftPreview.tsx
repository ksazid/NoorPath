"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import {
  calculateJourneyDuration,
  normalizePackageItems,
} from "../packages/packageDraftStandards";

type OccupancyKey = "double" | "triple" | "quad";

type Draft = {
  departureId: string;
  packageName?: string;
  summary?: string;
  origin?: string;
  departureDate?: string;
  returnDate?: string;
  inclusions?: string[];
  exclusions?: string[];
  makkah?: {
    hotelName?: string;
    classification?: string;
    distanceDisclosure?: string;
    nights?: number;
  };
  madinah?: {
    hotelName?: string;
    classification?: string;
    distanceDisclosure?: string;
    nights?: number;
  };
  travel?: { routeSummary?: string; details?: string };
};

type Commercial = {
  pricing: {
    currency: string;
    occupancies: Array<{ occupancy: OccupancyKey; amount: number }>;
  } | null;
  inventory: {
    pools: Array<{
      occupancy: OccupancyKey;
      capacity: number;
      availableQuantity: number;
    }>;
  } | null;
};

type PaymentPlan = {
  paymentPlan: {
    enabled: true;
    depositPercent: number;
    instalmentDayOfMonth: number;
    finalPaymentDueDaysBeforeDeparture: number;
  } | null;
};

type State =
  | { kind: "loading" }
  | { kind: "error" }
  | {
      kind: "ready";
      draft: Draft;
      commercial: Commercial | null;
      paymentPlan: PaymentPlan | null;
    };

const formatter = new Intl.DateTimeFormat("en-IN", {
  day: "numeric",
  month: "short",
  year: "numeric",
});

const occupancyLabels: Record<OccupancyKey, string> = {
  double: "Double sharing",
  triple: "Triple sharing",
  quad: "Quad sharing",
};

function date(value?: string) {
  return value
    ? formatter.format(new Date(`${value}T00:00:00Z`))
    : "To be confirmed";
}

function money(currency: string, amount: number) {
  try {
    return new Intl.NumberFormat("en-IN", {
      style: "currency",
      currency,
      maximumFractionDigits: 0,
    }).format(amount);
  } catch {
    return `${currency} ${amount.toLocaleString("en-IN")}`;
  }
}

async function optionalJson<T>(url: string): Promise<T | null> {
  try {
    const response = await fetch(url, {
      credentials: "include",
      cache: "no-store",
    });
    if (!response.ok) return null;
    return (await response.json()) as T;
  } catch {
    return null;
  }
}

export default function PackageDraftPreview({
  departureId,
}: {
  departureId: string;
}) {
  const [state, setState] = useState<State>({ kind: "loading" });

  useEffect(() => {
    let cancelled = false;
    const load = async () => {
      try {
        const draftResponse = await fetch(
          `/api/v1/operator/departures/${departureId}`,
          {
            credentials: "include",
            cache: "no-store",
          },
        );
        if (!draftResponse.ok) throw new Error();

        const draft = (await draftResponse.json()) as Draft;
        const [commercial, paymentPlan] = await Promise.all([
          optionalJson<Commercial>(
            `/api/v1/operator/departures/${departureId}/commercial`,
          ),
          optionalJson<PaymentPlan>(
            `/api/v1/operator/departures/${departureId}/payment-plan`,
          ),
        ]);

        if (!cancelled) {
          setState({ kind: "ready", draft, commercial, paymentPlan });
        }
      } catch {
        if (!cancelled) setState({ kind: "error" });
      }
    };
    void load();
    return () => {
      cancelled = true;
    };
  }, [departureId]);

  const duration = useMemo(() => {
    if (state.kind !== "ready") return null;
    return calculateJourneyDuration(
      state.draft.departureDate ?? "",
      state.draft.returnDate ?? "",
    );
  }, [state]);

  if (state.kind === "loading") {
    return (
      <main className="composer-state-page">
        <section className="composer-state-card">
          <h1>Preparing customer preview</h1>
          <p>Loading the saved package and commercial facts.</p>
        </section>
      </main>
    );
  }

  if (state.kind === "error") {
    return (
      <main className="composer-state-page">
        <section className="composer-state-card">
          <h1>Preview unavailable</h1>
          <p>Save the draft and retry.</p>
          <Link
            className="secondary-button"
            href={`/operator/departures/${departureId}`}
          >
            Back to draft
          </Link>
        </section>
      </main>
    );
  }

  const { draft, commercial, paymentPlan } = state;
  const inclusions = normalizePackageItems(draft.inclusions);
  const exclusions = normalizePackageItems(draft.exclusions);
  const pricing = commercial?.pricing;
  const inventory = commercial?.inventory;
  const plan = paymentPlan?.paymentPlan;
  const priceRows = pricing?.occupancies ?? [];

  return (
    <main className="package-page operator-package-preview">
      <div className="operator-preview-bar operator-preview-refined-bar">
        <div>
          <strong>Customer preview</strong>
          <span>Private draft · exactly how saved facts will be presented</span>
        </div>
        <div>
          <Link
            className="secondary-button"
            href={`/operator/departures/${departureId}`}
          >
            Edit draft
          </Link>
          <Link
            className="primary-button"
            href={`/operator/departures/${departureId}/review`}
          >
            Review publication
          </Link>
        </div>
      </div>

      <section className="package-hero operator-preview-hero">
        <div className="package-hero-copy">
          <p className="eyebrow">
            Umrah package from {draft.origin || "India"}
          </p>
          <h1>{draft.packageName || "Untitled Umrah package"}</h1>
          <p>{draft.summary || "Package summary will appear here."}</p>
          <div className="package-facts">
            <span>
              {duration
                ? `${duration.days} Days / ${duration.nights} Nights`
                : "Duration to be confirmed"}
            </span>
            <span>
              {date(draft.departureDate)} – {date(draft.returnDate)}
            </span>
            <span>
              {draft.travel?.routeSummary || "Travel route to be confirmed"}
            </span>
          </div>
        </div>
      </section>

      <section className="package-content-grid">
        <div className="package-main-column">
          <section className="package-section operator-preview-section">
            <p className="eyebrow">Stay</p>
            <h2>Makkah & Madinah accommodation</h2>
            <div className="package-stay-grid">
              <article className="package-card operator-preview-stay-card">
                <span className="operator-preview-city">Makkah</span>
                <strong>
                  {draft.makkah?.hotelName || "Hotel to be confirmed"}
                </strong>
                <p>
                  {draft.makkah?.classification || "Classification pending"}
                </p>
                <p>
                  {draft.makkah?.distanceDisclosure ||
                    "Distance disclosure pending"}
                </p>
                <span>{draft.makkah?.nights ?? 0} nights</span>
              </article>
              <article className="package-card operator-preview-stay-card">
                <span className="operator-preview-city">Madinah</span>
                <strong>
                  {draft.madinah?.hotelName || "Hotel to be confirmed"}
                </strong>
                <p>
                  {draft.madinah?.classification || "Classification pending"}
                </p>
                <p>
                  {draft.madinah?.distanceDisclosure ||
                    "Distance disclosure pending"}
                </p>
                <span>{draft.madinah?.nights ?? 0} nights</span>
              </article>
            </div>
          </section>

          <section className="package-section operator-preview-section">
            <p className="eyebrow">Included</p>
            <h2>What this package includes</h2>
            <div className="package-inclusion-grid operator-preview-inclusion-grid">
              {inclusions.map((item) => (
                <div className="package-inclusion" key={item}>
                  <span aria-hidden="true">✓</span>
                  <strong>{item}</strong>
                </div>
              ))}
            </div>
          </section>

          <section className="package-section operator-preview-section">
            <p className="eyebrow">Not included</p>
            <h2>Additional customer costs</h2>
            <div className="operator-preview-exclusion-list">
              {exclusions.map((item) => (
                <div key={item}>
                  <span aria-hidden="true">×</span>
                  <strong>{item}</strong>
                </div>
              ))}
            </div>
          </section>
        </div>

        <aside className="package-booking-card operator-commercial-preview-card">
          <p className="eyebrow">Customer price preview</p>
          <h2>
            {priceRows.length > 0
              ? "Choose room sharing"
              : "Pricing not configured yet"}
          </h2>

          {priceRows.length > 0 && pricing ? (
            <div className="operator-preview-price-list">
              {priceRows.map((row) => {
                const availability = inventory?.pools.find(
                  (pool) => pool.occupancy === row.occupancy,
                );
                return (
                  <div key={row.occupancy}>
                    <span>
                      <strong>{occupancyLabels[row.occupancy]}</strong>
                      <small>
                        {availability
                          ? `${availability.availableQuantity} places available`
                          : "Availability pending"}
                      </small>
                    </span>
                    <strong>{money(pricing.currency, row.amount)}</strong>
                  </div>
                );
              })}
            </div>
          ) : (
            <p>
              Save occupancy pricing to see the same commercial choices the
              customer will receive.
            </p>
          )}

          <div className="operator-preview-payment-summary">
            <span>Payment option</span>
            {plan ? (
              <>
                <strong>{plan.depositPercent}% due initially</strong>
                <small>
                  Monthly on day {plan.instalmentDayOfMonth} · final balance {" "}
                  {plan.finalPaymentDueDaysBeforeDeparture} days before departure
                </small>
              </>
            ) : (
              <>
                <strong>Full balance</strong>
                <small>No instalment plan is currently enabled.</small>
              </>
            )}
          </div>

          <Link
            className="secondary-button operator-preview-edit-commercial"
            href={`/operator/departures/${departureId}`}
          >
            Edit commercial setup
          </Link>
          <Link
            className="primary-button"
            href={`/operator/departures/${departureId}/review`}
          >
            Continue to publication review
          </Link>
          <small>
            This remains private until independent platform approval completes.
          </small>
        </aside>
      </section>
    </main>
  );
}
