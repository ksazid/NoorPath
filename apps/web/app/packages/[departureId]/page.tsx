"use client";

import Image from "next/image";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Icon, PublicHeader } from "../../public-ui";

type ConfirmationState = "confirmed" | "pending";
type Occupancy = "double" | "triple" | "quad";

type StayDetails = {
  hotelName: string;
  classification: string;
  distanceDisclosure: string;
  nights: number;
  confirmationState: ConfirmationState;
};

type OccupancyDetail = {
  occupancy: Occupancy;
  amount: number;
  availableQuantity: number;
  status: "available" | "unavailable";
};

type PackageDetails = {
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
  makkah: StayDetails;
  madinah: StayDetails;
  travel: {
    routeSummary: string;
    details: string;
    confirmationState: ConfirmationState;
  };
  inclusions: string[];
  exclusions: string[];
  pricing: {
    currency: string;
    occupancies: OccupancyDetail[];
  };
};

type DetailState =
  | { kind: "loading" }
  | { kind: "loaded"; details: PackageDetails }
  | { kind: "not-found" }
  | { kind: "error"; correlationId?: string };

export default function PackageDetailsPage() {
  const params = useParams<{ departureId: string }>();
  const departureId = params.departureId;
  const [state, setState] = useState<DetailState>({ kind: "loading" });

  const load = useCallback(async () => {
    setState({ kind: "loading" });

    try {
      const response = await fetch(
        `/api/v1/departures/${encodeURIComponent(departureId)}`,
        { cache: "no-store", credentials: "same-origin" },
      );
      const correlationId =
        response.headers.get("X-Correlation-ID") ?? undefined;

      if (response.status === 404) {
        setState({ kind: "not-found" });
        return;
      }

      if (!response.ok) {
        setState({ kind: "error", correlationId });
        return;
      }

      setState({
        kind: "loaded",
        details: (await response.json()) as PackageDetails,
      });
    } catch {
      setState({ kind: "error" });
    }
  }, [departureId]);

  useEffect(() => {
    const pending = window.setTimeout(load, 0);
    return () => window.clearTimeout(pending);
  }, [load]);

  return (
    <div className="public-page package-page">
      <PublicHeader />

      <main className="package-main" id="main-content">
        {state.kind === "loading" ? <PackageLoading /> : null}
        {state.kind === "not-found" ? <PackageUnavailable /> : null}
        {state.kind === "error" ? (
          <PackageError correlationId={state.correlationId} onRetry={load} />
        ) : null}
        {state.kind === "loaded" ? (
          <PackageContent details={state.details} />
        ) : null}
      </main>

      {state.kind === "loaded" ? (
        <StickySummary details={state.details} />
      ) : null}
    </div>
  );
}

function PackageContent({ details }: { details: PackageDetails }) {
  const confirmedFacts = factLabels(details, "confirmed");
  const pendingFacts = factLabels(details, "pending");

  return (
    <>
      <nav className="package-breadcrumbs" aria-label="Breadcrumb">
        <Link href="/">Home</Link>
        <span aria-hidden="true">›</span>
        <Link href="/#packages">Umrah Packages</Link>
        <span aria-hidden="true">›</span>
        <span>{details.packageName}</span>
      </nav>

      <section className="package-overview" aria-labelledby="package-title">
        <div className="package-gallery">
          <div className="package-gallery-primary">
            <Image
              src="/assets/kaaba-reference.svg"
              alt="Masjid al-Haram and the Kaaba in Makkah"
              fill
              priority
              sizes="(max-width: 960px) 55vw, 25vw"
            />
          </div>
          <div className="package-gallery-secondary">
            <Image
              src="/assets/madinah-reference.svg"
              alt="Al-Masjid an-Nabawi and the green dome in Madinah"
              fill
              sizes="(max-width: 960px) 45vw, 20vw"
            />
          </div>
          <span className="package-gallery-status">
            Availability
            <strong>Available now</strong>
          </span>
        </div>

        <div className="package-operator-summary">
          <p className="verified-operator">
            <Icon name="seal-check" /> Verified operator
          </p>
          <h1 id="package-title">{details.operator.displayName}</h1>
          <p className="package-detail-name">{details.packageName}</p>
          <div className="operator-credentials" aria-label="Journey details">
            <span>
              <Icon name="airplane-tilt" /> {details.origin}
            </span>
            <span>
              <Icon name="clock" /> {details.durationNights} nights
            </span>
            <span>
              <Icon name="map-trifold" /> {formatDate(details.departureDate)}
            </span>
          </div>
          <StaySummary city="Makkah" stay={details.makkah} />
          <StaySummary city="Madinah" stay={details.madinah} />
        </div>

        <PricingSummary details={details} />
      </section>

      <div className="package-content-grid" id="journey-facts">
        <section className="package-panel itinerary-panel">
          <h2>Journey &amp; travel</h2>
          <ol className="itinerary-list">
            <JourneyFact
              label="Depart"
              icon="airplane-tilt"
              title={`${formatDate(details.departureDate)} · ${details.origin}`}
              copy="Published departure date and origin"
            />
            <JourneyFact
              label="Route"
              icon="map-trifold"
              title={details.travel.routeSummary}
              copy={confirmationCopy(details.travel.confirmationState)}
            />
            <JourneyFact
              label="Travel"
              icon="bus"
              title="Operator travel details"
              copy={details.travel.details}
            />
            <JourneyFact
              label="Return"
              icon="airplane-tilt"
              title={formatDate(details.returnDate)}
              copy={`${details.durationNights}-night published journey`}
            />
          </ol>
        </section>

        <div className="package-feature-column">
          <FactGrid
            title="Package includes"
            items={details.inclusions}
            icon="seal-check"
            empty="No inclusions have been published for this package."
          />
          <FactGrid
            title="Not included"
            items={details.exclusions}
            icon="file-text"
            empty="No exclusions have been published for this package."
          />
        </div>

        <div className="package-status-column">
          <section className="package-panel fact-status-panel">
            <div className="status-tabs" aria-label="Fact status legend">
              <span>Confirmed</span>
              <span>Pending</span>
            </div>
            <div className="status-columns">
              <ul>
                {confirmedFacts.length > 0 ? (
                  confirmedFacts.map((item) => (
                    <li key={item}>
                      <Icon name="seal-check" /> {item}
                    </li>
                  ))
                ) : (
                  <li>No material journey facts are confirmed yet.</li>
                )}
              </ul>
              <ul className="pending-list">
                {pendingFacts.length > 0 ? (
                  pendingFacts.map((item) => (
                    <li key={item}>
                      <Icon name="clock" /> {item}
                    </li>
                  ))
                ) : (
                  <li>No material journey facts are pending.</li>
                )}
              </ul>
            </div>
          </section>

          <section className="package-panel cancellation-panel">
            <h2>Booking terms</h2>
            <dl>
              <div>
                <dt>Cancellation entitlement</dt>
                <dd>Shown before booking commitment</dd>
              </div>
              <div>
                <dt>Refund terms</dt>
                <dd>Shown before payment</dd>
              </div>
              <div>
                <dt>Payment schedule</dt>
                <dd>Not quoted on this page</dd>
              </div>
              <div>
                <dt>Need clarification?</dt>
                <dd>Human support is available</dd>
              </div>
            </dl>
            <a href="mailto:support@noorpath.example">
              Ask about booking terms <span aria-hidden="true">›</span>
            </a>
          </section>
        </div>
      </div>

      <section className="package-detail-summary" aria-label="Package summary">
        <h2>About this package</h2>
        <p>{details.summary}</p>
        <p>
          Published prices and current availability are shown above. A
          traveller-specific quote and any applicable payment schedule are
          shown before commitment.
        </p>
      </section>
    </>
  );
}

function PricingSummary({ details }: { details: PackageDetails }) {
  return (
    <aside
      className="payment-summary-card"
      id="pricing-and-availability"
      aria-label="Published pricing and availability"
    >
      <h2>Pricing &amp; availability</h2>
      <div className="occupancy-price-list">
        {details.pricing.occupancies.map((item) => (
          <div className="occupancy-price-row" key={item.occupancy}>
            <span>
              <strong>{occupancyLabel(item.occupancy)}</strong>
              <small>
                {item.status === "available"
                  ? `${item.availableQuantity} available`
                  : "Currently unavailable"}
              </small>
            </span>
            <strong>{formatMoney(item.amount, details.pricing.currency)}</strong>
          </div>
        ))}
      </div>
      <p className="package-pricing-note">
        Published occupancy pricing. No payment is taken on this page.
      </p>
      <a href="mailto:support@noorpath.example">
        Ask about this package <span aria-hidden="true">›</span>
      </a>
    </aside>
  );
}

function StaySummary({
  city,
  stay,
}: {
  city: "Makkah" | "Madinah";
  stay: StayDetails;
}) {
  return (
    <div className="stay-summary">
      <span>{city} Hotel</span>
      <div>
        <strong>{stay.hotelName}</strong>
        <em>{stay.distanceDisclosure}</em>
      </div>
      <p>
        {stay.nights} Nights <span aria-hidden="true">·</span>{" "}
        {stay.classification} <span aria-hidden="true">·</span>{" "}
        {confirmationCopy(stay.confirmationState)}
      </p>
    </div>
  );
}

function JourneyFact({
  label,
  icon,
  title,
  copy,
}: {
  label: string;
  icon: string;
  title: string;
  copy: string;
}) {
  return (
    <li>
      <span className="itinerary-day">{label}</span>
      <span className="itinerary-icon">
        <Icon name={icon} />
      </span>
      <div>
        <strong>{title}</strong>
        <p>{copy}</p>
      </div>
    </li>
  );
}

function FactGrid({
  title,
  items,
  icon,
  empty,
}: {
  title: string;
  items: string[];
  icon: string;
  empty: string;
}) {
  return (
    <section className="package-panel icon-grid-panel package-fact-grid">
      <h2>{title}</h2>
      {items.length > 0 ? (
        <ul>
          {items.map((item) => (
            <li key={item}>
              <Icon name={icon} />
              <span>{item}</span>
            </li>
          ))}
        </ul>
      ) : (
        <p>{empty}</p>
      )}
    </section>
  );
}

function StickySummary({ details }: { details: PackageDetails }) {
  const available = details.pricing.occupancies.filter(
    (item) => item.status === "available",
  );
  const startingPrice = available.reduce((lowest, item) =>
    item.amount < lowest.amount ? item : lowest,
  );

  return (
    <div
      className="package-sticky-action"
      role="region"
      aria-label="Package actions"
    >
      <SummaryCell label="Journey" value={`${details.durationNights} nights`} />
      <SummaryCell
        label="Published from"
        value={formatMoney(startingPrice.amount, details.pricing.currency)}
        tone="green"
      />
      <SummaryCell
        label="Availability"
        value={`${available.length} occupanc${available.length === 1 ? "y" : "ies"}`}
        tone="gold"
      />
      <a href="#pricing-and-availability">
        Review options <span aria-hidden="true">›</span>
      </a>
    </div>
  );
}

function SummaryCell({
  label,
  value,
  tone,
}: {
  label: string;
  value: string;
  tone?: "green" | "gold";
}) {
  return (
    <div className={`price-cell${tone ? ` price-cell-${tone}` : ""}`}>
      <span>{label}</span>
      <strong>{value}</strong>
    </div>
  );
}

function PackageLoading() {
  return (
    <section
      className="package-detail-state package-detail-loading"
      aria-live="polite"
      aria-busy="true"
      aria-label="Loading published package details"
    >
      <div className="package-detail-loading-gallery" />
      <div className="package-detail-loading-copy">
        <span />
        <span />
        <span />
      </div>
      <p>Loading published journey details…</p>
    </section>
  );
}

function PackageUnavailable() {
  return (
    <section className="package-detail-state" role="status">
      <p className="package-state-kicker">Published journey unavailable</p>
      <h1>This package is not currently available.</h1>
      <p>
        It may have changed, sold out, or no longer be eligible for public sale.
      </p>
      <Link href="/#packages">Browse available packages</Link>
    </section>
  );
}

function PackageError({
  correlationId,
  onRetry,
}: {
  correlationId?: string;
  onRetry: () => void;
}) {
  return (
    <section className="package-detail-state" role="alert">
      <p className="package-state-kicker">Package details unavailable</p>
      <h1>We could not load this package right now.</h1>
      <p>Please check your connection and try again.</p>
      {correlationId ? <small>Reference: {correlationId}</small> : null}
      <button type="button" onClick={onRetry}>
        Try again
      </button>
    </section>
  );
}

function factLabels(details: PackageDetails, state: ConfirmationState) {
  const facts = [
    ["Makkah stay", details.makkah.confirmationState],
    ["Madinah stay", details.madinah.confirmationState],
    ["Travel details", details.travel.confirmationState],
  ] as const;

  return facts.filter(([, value]) => value === state).map(([label]) => label);
}

function confirmationCopy(state: ConfirmationState) {
  return state === "confirmed" ? "Confirmed" : "Pending confirmation";
}

function occupancyLabel(value: Occupancy) {
  return `${value.charAt(0).toUpperCase()}${value.slice(1)} sharing`;
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

function formatMoney(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat("en-IN", {
      style: "currency",
      currency,
      maximumFractionDigits: Number.isInteger(amount) ? 0 : 2,
    }).format(amount);
  } catch {
    return `${currency} ${amount.toFixed(2)}`;
  }
}
