"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import {
  calculateJourneyDuration,
  normalizePackageItems,
} from "../packages/packageDraftStandards";

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

type State =
  | { kind: "loading" }
  | { kind: "error" }
  | { kind: "ready"; draft: Draft };

const formatter = new Intl.DateTimeFormat("en-IN", {
  day: "numeric",
  month: "short",
  year: "numeric",
});

function date(value?: string) {
  return value
    ? formatter.format(new Date(`${value}T00:00:00Z`))
    : "To be confirmed";
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
        const response = await fetch(
          `/api/v1/operator/departures/${departureId}`,
          {
            credentials: "include",
            cache: "no-store",
          },
        );
        if (!response.ok) throw new Error();
        const draft = (await response.json()) as Draft;
        if (!cancelled) setState({ kind: "ready", draft });
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
          <p>Loading the saved package facts.</p>
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

  const { draft } = state;
  const inclusions = normalizePackageItems(draft.inclusions);
  const exclusions = normalizePackageItems(draft.exclusions);

  return (
    <main className="package-page operator-package-preview">
      <div className="operator-preview-bar">
        <div>
          <strong>Customer preview</strong>
          <span>Private draft · not visible to customers</span>
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

      <section className="package-hero">
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
          <section className="package-section">
            <p className="eyebrow">Stay</p>
            <h2>Makkah & Madinah accommodation</h2>
            <div className="package-stay-grid">
              <article className="package-card">
                <h3>Makkah</h3>
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
              <article className="package-card">
                <h3>Madinah</h3>
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

          <section className="package-section">
            <p className="eyebrow">Included</p>
            <h2>What this package includes</h2>
            <div className="package-inclusion-grid">
              {inclusions.map((item) => (
                <div className="package-inclusion" key={item}>
                  <span aria-hidden="true">✓</span>
                  <strong>{item}</strong>
                </div>
              ))}
            </div>
          </section>

          <section className="package-section">
            <p className="eyebrow">Not included</p>
            <h2>Additional customer costs</h2>
            <ul>
              {exclusions.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </section>
        </div>

        <aside className="package-booking-card">
          <p className="eyebrow">Draft commercial preview</p>
          <h2>Pricing appears after occupancy rates are saved</h2>
          <p>
            The final customer page will show the booking amount, payment option
            and authoritative milestone schedule from the saved commercial
            version.
          </p>
          <Link
            className="primary-button"
            href={`/operator/departures/${departureId}`}
          >
            Complete pricing
          </Link>
          <small>
            Platform review is required before this package can be published.
          </small>
        </aside>
      </section>
    </main>
  );
}
