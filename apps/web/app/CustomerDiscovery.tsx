"use client";

import Image from "next/image";
import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { Icon } from "./public-ui";

type DiscoveryItem = {
  departureId: string;
  operator: {
    id: string;
    displayName: string;
  };
  packageName: string;
  summary: string;
  origin: string;
  departureDate: string;
  returnDate: string;
  durationNights: number;
  makkah: StaySummary;
  madinah: StaySummary;
  travelConfirmationState: "confirmed" | "pending";
  inclusionHighlights: string[];
  headlinePrice: {
    amount: number;
    currency: string;
    occupancy: "double" | "triple" | "quad";
  };
  availability: {
    status: "available";
    occupancies: Array<{
      occupancy: "double" | "triple" | "quad";
      availableQuantity: number;
    }>;
  };
};

type StaySummary = {
  hotelName: string;
  classification: string;
  distanceDisclosure: string;
  nights: number;
  confirmationState: "confirmed" | "pending";
};

type DiscoveryResponse = {
  items: DiscoveryItem[];
};

type DiscoveryState =
  | { kind: "loading" }
  | { kind: "loaded"; items: DiscoveryItem[] }
  | { kind: "error"; correlationId?: string };

const packageImages = [
  "/assets/kaaba-reference.svg",
  "/assets/madinah-reference.svg",
  "/assets/kaaba-reference.svg",
] as const;

export function CustomerDiscovery() {
  const [state, setState] = useState<DiscoveryState>({ kind: "loading" });

  const load = useCallback(async () => {
    setState({ kind: "loading" });

    try {
      const response = await fetch("/api/v1/departures", {
        cache: "no-store",
        credentials: "same-origin",
      });
      const correlationId = response.headers.get("X-Correlation-ID") ?? undefined;

      if (!response.ok) {
        setState({ kind: "error", correlationId });
        return;
      }

      const body = (await response.json()) as DiscoveryResponse;
      setState({ kind: "loaded", items: body.items ?? [] });
    } catch {
      setState({ kind: "error" });
    }
  }, []);

  useEffect(() => {
    const pending = window.setTimeout(load, 0);
    return () => window.clearTimeout(pending);
  }, [load]);

  if (state.kind === "loading") {
    return (
      <div
        className="landing-package-grid discovery-loading"
        aria-live="polite"
        aria-busy="true"
        aria-label="Loading published Umrah packages"
      >
        {[0, 1, 2].map((item) => (
          <article className="landing-package-card discovery-skeleton" key={item}>
            <div className="landing-package-image" />
            <div className="landing-package-body">
              <span className="discovery-skeleton-line discovery-skeleton-title" />
              <span className="discovery-skeleton-line" />
              <span className="discovery-skeleton-line discovery-skeleton-short" />
            </div>
          </article>
        ))}
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div className="discovery-state" role="alert">
        <p className="discovery-kicker">Published journeys unavailable</p>
        <h3>We could not load Umrah packages right now.</h3>
        <p>Please check your connection and try again.</p>
        {state.correlationId ? (
          <small>Reference: {state.correlationId}</small>
        ) : null}
        <button className="discovery-retry" type="button" onClick={load}>
          Try again
        </button>
      </div>
    );
  }

  if (state.items.length === 0) {
    return (
      <div className="discovery-state" role="status">
        <p className="discovery-kicker">Published journeys</p>
        <h3>No Umrah packages are currently available.</h3>
        <p>New verified departures will appear here after publication.</p>
      </div>
    );
  }

  return (
    <div className="landing-package-grid">
      {state.items.map((item, index) => {
        const image = packageImages[index % packageImages.length];
        const inclusionHighlights = item.inclusionHighlights.slice(0, 3);

        return (
          <article className="landing-package-card" key={item.departureId}>
            <div
              className={`landing-package-image landing-package-image-${(index % 3) + 1}`}
            >
              <Image
                src={image}
                alt={
                  image.includes("madinah")
                    ? "Al-Masjid an-Nabawi in Madinah"
                    : "Masjid al-Haram and the Kaaba in Makkah"
                }
                fill
                sizes="(max-width: 760px) 100vw, (max-width: 980px) 50vw, 33vw"
              />
              <span className="package-tier">Verified operator</span>
            </div>

            <div className="landing-package-body">
              <h3>{item.packageName}</h3>
              <p className="package-stay">
                Makkah {item.makkah.nights} Nights
                <span aria-hidden="true">·</span>
                Madinah {item.madinah.nights} Nights
              </p>

              {inclusionHighlights.length > 0 ? (
                <div
                  className="package-inclusions"
                  aria-label="Inclusion highlights"
                >
                  {inclusionHighlights.map((inclusion) => (
                    <span key={inclusion}>
                      <Icon name="seal-check" />
                      {inclusion}
                    </span>
                  ))}
                </div>
              ) : null}

              <dl className="package-departure">
                <div>
                  <dt>Departure</dt>
                  <dd>{formatDate(item.departureDate)}</dd>
                </div>
                <div>
                  <dt>From {originLabel(item.origin)}</dt>
                  <dd>Available</dd>
                </div>
              </dl>

              <div className="package-card-footer">
                <span>
                  <strong>From {formatMoney(item.headlinePrice)}</strong>
                  <small>{item.operator.displayName}</small>
                </span>
                <Link href={`/packages/${item.departureId}`}>
                  View package <span aria-hidden="true">→</span>
                </Link>
              </div>
            </div>
          </article>
        );
      })}
    </div>
  );
}

function formatDate(value: string) {
  const date = new Date(`${value}T00:00:00Z`);
  if (Number.isNaN(date.getTime())) return value;

  return new Intl.DateTimeFormat("en-IN", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(date);
}

function originLabel(origin: string) {
  const value = origin.trim();
  const codeStart = value.indexOf("(");
  return codeStart > 0 ? value.slice(0, codeStart).trim() : value;
}

function formatMoney(price: DiscoveryItem["headlinePrice"]) {
  try {
    return new Intl.NumberFormat("en-IN", {
      style: "currency",
      currency: price.currency,
      maximumFractionDigits: Number.isInteger(price.amount) ? 0 : 2,
    }).format(price.amount);
  } catch {
    return `${price.currency} ${price.amount.toFixed(2)}`;
  }
}
