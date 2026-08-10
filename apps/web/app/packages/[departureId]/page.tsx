"use client";

import Image from "next/image";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Icon, PublicFooter, PublicHeader } from "../../public-ui";
import {
  PackageContentIcon,
  type PackageContentIconName,
} from "./PackageContentIcon";
import "./package-conversion.css";

type ConfirmationState = "confirmed" | "pending";
type Occupancy = "double" | "triple" | "quad";
type PaymentMode = "milestone" | "pay-later";
type ContentGroup = "package" | "travel-kit" | "umrah-kit" | "excluded";

type StayDetails = {
  hotelName: string;
  classification: string;
  distanceDisclosure: string;
  nights: number;
  confirmationState: ConfirmationState;
};

type PaymentInstalment = {
  sequence: number;
  dueDate: string;
  amount: number;
};

type FinancialPreview = {
  adultGuests: number;
  total: number;
  dueNow: number;
  remaining: number;
  instalments: PaymentInstalment[];
  finalDueDate: string | null;
};

type OccupancyDetail = {
  occupancy: Occupancy;
  amount: number;
  availableQuantity: number;
  status: "available" | "unavailable";
  financials: FinancialPreview;
};

type TravelDateOption = {
  departureId: string;
  departureDate: string;
  returnDate: string;
  status: "available" | "sold-out";
};

type PackageDetails = {
  departureId: string;
  operator: { id: string; displayName: string };
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
  travelDates: TravelDateOption[];
  pricing: { currency: string; occupancies: OccupancyDetail[] };
};

type DetailState =
  | { kind: "loading" }
  | { kind: "loaded"; details: PackageDetails }
  | { kind: "not-found" }
  | { kind: "error"; correlationId?: string };

type ContentMetadata = {
  group: ContentGroup;
  icon: PackageContentIconName;
  label?: string;
};

const contentMeta: Record<string, ContentMetadata> = {
  "Return flights": { group: "package", icon: "plane" },
  "Visa included": {
    group: "package",
    icon: "visa",
    label: "Umrah visa included",
  },
  "Visa support": { group: "package", icon: "visa" },
  "Makkah accommodation": { group: "package", icon: "hotel" },
  "Madinah accommodation": { group: "package", icon: "mosque" },
  "Breakfast, lunch and dinner": { group: "package", icon: "meal" },
  Breakfast: { group: "package", icon: "meal" },
  "Intercity travel": { group: "package", icon: "bus" },
  "Ziyarat transport": { group: "package", icon: "mosque" },
  "Umrah guidance": { group: "package", icon: "guide" },
  "Luggage tag": { group: "travel-kit", icon: "baggage" },
  "Neck pouch / document wallet": {
    group: "travel-kit",
    icon: "wallet",
    label: "Document wallet",
  },
  "ID card": { group: "travel-kit", icon: "custom" },
  "SIM / eSIM guidance": { group: "travel-kit", icon: "custom" },
  "Emergency contact card": { group: "travel-kit", icon: "custom" },
  "Ihram for men / prayer essentials option": {
    group: "umrah-kit",
    icon: "custom",
    label: "Ihram / prayer essentials",
  },
  "Drawstring bag": { group: "umrah-kit", icon: "bag" },
  "Unscented toiletries": { group: "umrah-kit", icon: "custom" },
  "Pocket Dua guide": { group: "umrah-kit", icon: "book" },
  "Zamzam handling guidance": { group: "umrah-kit", icon: "water" },
  "Personal expenses": { group: "excluded", icon: "wallet" },
  "Optional excursions": { group: "excluded", icon: "custom" },
  "Travel insurance unless stated": { group: "excluded", icon: "shield" },
  "Extra baggage": { group: "excluded", icon: "baggage" },
  "Room upgrade": { group: "excluded", icon: "bed" },
  Laundry: { group: "excluded", icon: "laundry" },
};

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
    <div className="public-page package-page package-conversion-page">
      <PublicHeader />
      {state.kind === "loaded" ? (
        <PackageExperience details={state.details} />
      ) : (
        <main className="package-main" id="main-content">
          {state.kind === "loading" ? <PackageLoading /> : null}
          {state.kind === "not-found" ? <PackageUnavailable /> : null}
          {state.kind === "error" ? (
            <PackageError correlationId={state.correlationId} onRetry={load} />
          ) : null}
        </main>
      )}
    </div>
  );
}

function PackageExperience({ details }: { details: PackageDetails }) {
  const firstAvailable = details.pricing.occupancies.find(
    (item) => item.status === "available",
  );
  const [occupancy, setOccupancy] = useState<Occupancy>(
    firstAvailable?.occupancy ?? "double",
  );
  const [paymentMode, setPaymentMode] = useState<PaymentMode>("milestone");
  const selected =
    details.pricing.occupancies.find(
      (item) => item.occupancy === occupancy && item.status === "available",
    ) ?? firstAvailable;

  if (!selected) {
    return (
      <main className="package-main" id="main-content">
        <PackageUnavailable />
      </main>
    );
  }

  return (
    <>
      <main className="package-main package-conversion-main" id="main-content">
        <nav className="package-breadcrumbs" aria-label="Breadcrumb">
          <Link href="/">Home</Link>
          <span aria-hidden="true">›</span>
          <Link href="/#packages">Umrah Packages</Link>
          <span aria-hidden="true">›</span>
          <span>{details.packageName}</span>
        </nav>

        <TravelDates details={details} />

        <section
          className="package-conversion-overview"
          aria-labelledby="package-title"
        >
          <Gallery selected={selected} />
          <OperatorSummary details={details} />
          <BookingCard
            details={details}
            selected={selected}
            paymentMode={paymentMode}
            onOccupancyChange={setOccupancy}
            onPaymentModeChange={setPaymentMode}
          />
        </section>

        <div className="package-conversion-content">
          <Journey details={details} />
          <PackageContent details={details} />
          <TrustAndTerms details={details} />
        </div>

        <section className="package-conversion-about">
          <h2>About this package</h2>
          <p>{details.summary}</p>
          <p>
            Published pricing, current room availability and payment commitments
            are visible before you start booking.
          </p>
        </section>
      </main>

      <PublicFooter />
      <StickyBookingBar
        details={details}
        selected={selected}
        paymentMode={paymentMode}
      />
    </>
  );
}

function TravelDates({ details }: { details: PackageDetails }) {
  const dates = details.travelDates?.length
    ? details.travelDates
    : [
        {
          departureId: details.departureId,
          departureDate: details.departureDate,
          returnDate: details.returnDate,
          status: "available" as const,
        },
      ];

  return (
    <section
      className="package-travel-dates"
      aria-labelledby="travel-dates-title"
    >
      <div className="package-travel-dates-heading">
        <div>
          <h2 id="travel-dates-title">Available Travel Dates</h2>
          <p>Other published departures from {details.origin}</p>
        </div>
        <span>{dates.length} dates</span>
      </div>
      <div
        className="package-date-scroller"
        aria-label={`Published ${details.origin} travel dates`}
      >
        {dates.map((option) => {
          const current = option.departureId === details.departureId;
          const soldOut = option.status === "sold-out";
          const content = (
            <>
              <span className="package-date-card-topline">
                <Icon name="airplane-tilt" />
                <small>{current ? "Selected" : details.origin}</small>
                {soldOut ? <em>Sold out</em> : null}
              </span>
              <strong>{formatDate(option.departureDate)}</strong>
              <span>{details.origin} → Jeddah</span>
            </>
          );

          return soldOut && !current ? (
            <article
              className="package-date-card sold-out"
              aria-label={`${formatDate(option.departureDate)} sold out`}
              key={option.departureId}
            >
              {content}
            </article>
          ) : (
            <Link
              className={`package-date-card${current ? " current" : ""}`}
              aria-current={current ? "page" : undefined}
              href={`/packages/${option.departureId}`}
              key={option.departureId}
            >
              {content}
            </Link>
          );
        })}
      </div>
    </section>
  );
}

function Gallery({ selected }: { selected: OccupancyDetail }) {
  return (
    <div className="package-conversion-gallery">
      <div className="package-conversion-image package-conversion-image-primary">
        <Image
          src="/assets/kaaba-reference.svg"
          alt="Masjid al-Haram and the Kaaba in Makkah"
          fill
          priority
          sizes="(max-width: 800px) 62vw, 28vw"
        />
      </div>
      <div className="package-conversion-image package-conversion-image-secondary">
        <Image
          src="/assets/madinah-reference.svg"
          alt="Al-Masjid an-Nabawi in Madinah"
          fill
          sizes="(max-width: 800px) 38vw, 14vw"
        />
      </div>
      <span className="package-availability-badge">
        Available seats
        <strong>{selected.availableQuantity}</strong>
      </span>
    </div>
  );
}

function OperatorSummary({ details }: { details: PackageDetails }) {
  return (
    <section className="package-conversion-operator">
      <p className="verified-operator">
        <Icon name="seal-check" /> Verified operator
      </p>
      <h1 id="package-title">{details.operator.displayName}</h1>
      <p className="package-detail-name">{details.packageName}</p>

      <div className="package-journey-chips" aria-label="Journey summary">
        <span>
          <Icon name="airplane-tilt" /> {details.origin}
        </span>
        <span>
          <Icon name="clock" /> {details.durationNights} nights
        </span>
        <span>
          <Icon name="calendar-blank" /> {formatDate(details.departureDate)}
        </span>
      </div>

      <Stay city="Makkah" stay={details.makkah} />
      <Stay city="Madinah" stay={details.madinah} />
    </section>
  );
}

function Stay({
  city,
  stay,
}: {
  city: "Makkah" | "Madinah";
  stay: StayDetails;
}) {
  return (
    <div className="package-stay-summary">
      <span>{city} Hotel</span>
      <strong>{stay.hotelName}</strong>
      <small>{stay.distanceDisclosure}</small>
      <p>
        {stay.nights} nights · {stay.classification} ·{" "}
        {confirmationCopy(stay.confirmationState)}
      </p>
    </div>
  );
}

function BookingCard({
  details,
  selected,
  paymentMode,
  onOccupancyChange,
  onPaymentModeChange,
}: {
  details: PackageDetails;
  selected: OccupancyDetail;
  paymentMode: PaymentMode;
  onOccupancyChange: (value: Occupancy) => void;
  onPaymentModeChange: (value: PaymentMode) => void;
}) {
  const financials = selected.financials;
  const hasFuturePlan =
    financials.remaining > 0 &&
    financials.instalments.length > 0 &&
    financials.finalDueDate !== null;

  return (
    <aside
      className="package-booking-card"
      aria-label="Booking and payment summary"
    >
      <div className="package-booking-heading">
        <h2>Plan your booking</h2>
        <p>Review the full commitment before you book.</p>
      </div>

      <a className="package-selection-row" href="#travel-dates-title">
        <span>
          <strong>Travel date</strong>
          <small>{formatDate(details.departureDate)}</small>
        </span>
        <span className="package-selection-change">Change</span>
      </a>

      <GuestSelector
        details={details}
        selected={selected}
        onOccupancyChange={onOccupancyChange}
      />

      <fieldset className="package-room-options">
        <legend>Room Sharing</legend>
        {details.pricing.occupancies.map((item) => (
          <label
            className={`package-room-option${selected.occupancy === item.occupancy ? " selected" : ""}${item.status !== "available" ? " unavailable" : ""}`}
            key={item.occupancy}
          >
            <input
              type="radio"
              name="package-occupancy"
              checked={selected.occupancy === item.occupancy}
              disabled={item.status !== "available"}
              onChange={() => onOccupancyChange(item.occupancy)}
            />
            <span>
              <strong>{occupancyLabel(item.occupancy)}</strong>
              <small>
                {item.status === "available"
                  ? `${item.availableQuantity} available`
                  : "Currently unavailable"}
              </small>
            </span>
            <strong>
              {formatMoney(item.amount, details.pricing.currency)}
              <small>/ person</small>
            </strong>
          </label>
        ))}
      </fieldset>

      <div className="package-price-breakdown">
        <h3>Price Breakdown</h3>
        <dl>
          <div>
            <dt>
              Adult · {formatMoney(selected.amount, details.pricing.currency)} ×{" "}
              {financials.adultGuests}
            </dt>
            <dd>{formatMoney(financials.total, details.pricing.currency)}</dd>
          </div>
          <div className="total">
            <dt>Total package</dt>
            <dd>{formatMoney(financials.total, details.pricing.currency)}</dd>
          </div>
          <div className="due">
            <dt>Minimum to book today</dt>
            <dd>{formatMoney(financials.dueNow, details.pricing.currency)}</dd>
          </div>
          <div>
            <dt>Remaining</dt>
            <dd>
              {formatMoney(financials.remaining, details.pricing.currency)}
            </dd>
          </div>
        </dl>
      </div>

      {hasFuturePlan ? (
        <>
          <fieldset className="package-payment-mode">
            <legend>Pay the remaining balance</legend>
            <div className="package-payment-mode-options">
              <label className={paymentMode === "milestone" ? "selected" : ""}>
                <input
                  type="radio"
                  name="payment-mode"
                  checked={paymentMode === "milestone"}
                  onChange={() => onPaymentModeChange("milestone")}
                />
                <span>
                  <strong>Milestone plan</strong>
                  <small>Split by published dates</small>
                </span>
              </label>
              <label className={paymentMode === "pay-later" ? "selected" : ""}>
                <input
                  type="radio"
                  name="payment-mode"
                  checked={paymentMode === "pay-later"}
                  onChange={() => onPaymentModeChange("pay-later")}
                />
                <span>
                  <strong>Pay later</strong>
                  <small>Balance by final deadline</small>
                </span>
              </label>
            </div>
          </fieldset>
          <PaymentBreakdown
            financials={financials}
            currency={details.pricing.currency}
            mode={paymentMode}
          />
        </>
      ) : (
        <p className="package-full-payment">
          <Icon name="receipt" /> Full payment applies to this departure.
        </p>
      )}

      <p className="package-payment-note">
        Price and availability are rechecked before a place is reserved.
      </p>
      <Link
        className="package-book-now"
        href={bookingHref(details.departureId, selected.occupancy, paymentMode)}
      >
        Book now <span aria-hidden="true">›</span>
      </Link>
    </aside>
  );
}

function GuestSelector({
  details,
  selected,
  onOccupancyChange,
}: {
  details: PackageDetails;
  selected: OccupancyDetail;
  onOccupancyChange: (value: Occupancy) => void;
}) {
  const [open, setOpen] = useState(true);
  const adults = selected.financials.adultGuests;
  const previous = occupancyForGuests(details, adults - 1);
  const next = occupancyForGuests(details, adults + 1);

  return (
    <section className={`package-guest-picker${open ? " open" : ""}`}>
      <button
        className="package-guest-summary"
        type="button"
        aria-expanded={open}
        aria-controls="package-guest-options"
        onClick={() => setOpen((value) => !value)}
      >
        <span>
          <strong>Guests</strong>
          <small>{adults} Guests</small>
        </span>
        <span className="package-chevron" aria-hidden="true" />
      </button>

      {open ? (
        <div className="package-guest-options" id="package-guest-options">
          <GuestRow
            label="Adults"
            detail="Current adult pricing"
            count={adults}
            countLabel="Adult guests"
            decrementDisabled={!previous}
            incrementDisabled={!next}
            onDecrement={() =>
              previous && onOccupancyChange(previous.occupancy)
            }
            onIncrement={() => next && onOccupancyChange(next.occupancy)}
          />
          <GuestRow
            label="Children (2–11 years)"
            detail="With Bed · Pricing not enabled yet"
            count={0}
            countLabel="Children with bed"
            disabled
          />
          <GuestRow
            label="Children (2–4 years)"
            detail="Without Bed · Pricing not enabled yet"
            count={0}
            countLabel="Children without bed"
            disabled
          />
          <GuestRow
            label="Infants (0–2 years)"
            detail="Without Bed · Pricing not enabled yet"
            count={0}
            countLabel="Infants"
            disabled
          />
          <p className="package-guest-note">
            Child and infant online pricing will activate only when the operator
            publishes those rates.
          </p>
        </div>
      ) : null}
    </section>
  );
}

function GuestRow({
  label,
  detail,
  count,
  countLabel,
  disabled = false,
  decrementDisabled = false,
  incrementDisabled = false,
  onDecrement,
  onIncrement,
}: {
  label: string;
  detail: string;
  count: number;
  countLabel: string;
  disabled?: boolean;
  decrementDisabled?: boolean;
  incrementDisabled?: boolean;
  onDecrement?: () => void;
  onIncrement?: () => void;
}) {
  return (
    <div className={`package-guest-row${disabled ? " unavailable" : ""}`}>
      <span className="package-guest-copy">
        <strong>{label}</strong>
        <small>{detail}</small>
      </span>
      <span className="package-stepper">
        <button
          type="button"
          aria-label={`Decrease ${countLabel.toLowerCase()}`}
          disabled={disabled || decrementDisabled}
          onClick={onDecrement}
        >
          −
        </button>
        <output aria-label={countLabel}>{count}</output>
        <button
          type="button"
          aria-label={`Increase ${countLabel.toLowerCase()}`}
          disabled={disabled || incrementDisabled}
          onClick={onIncrement}
        >
          +
        </button>
      </span>
    </div>
  );
}

function PaymentBreakdown({
  financials,
  currency,
  mode,
}: {
  financials: FinancialPreview;
  currency: string;
  mode: PaymentMode;
}) {
  return (
    <section className="package-payment-breakdown" aria-live="polite">
      <h3>
        {mode === "milestone"
          ? "Milestone payment breakdown"
          : "Pay later breakdown"}
      </h3>
      <ol>
        <li>
          <span aria-hidden="true" />
          <div>
            <strong>{formatMoney(financials.dueNow, currency)}</strong>
            <small>Book your place</small>
          </div>
          <time>Today</time>
        </li>
        {mode === "milestone" ? (
          financials.instalments.map((item, index) => (
            <li key={`${item.sequence}-${item.dueDate}`}>
              <span aria-hidden="true" />
              <div>
                <strong>{formatMoney(item.amount, currency)}</strong>
                <small>
                  {index === financials.instalments.length - 1
                    ? "Final journey balance"
                    : `Payment milestone ${item.sequence}`}
                </small>
              </div>
              <time dateTime={item.dueDate}>{formatDate(item.dueDate)}</time>
            </li>
          ))
        ) : financials.finalDueDate ? (
          <li>
            <span aria-hidden="true" />
            <div>
              <strong>{formatMoney(financials.remaining, currency)}</strong>
              <small>Remaining balance</small>
            </div>
            <time dateTime={financials.finalDueDate}>
              {formatDate(financials.finalDueDate)}
            </time>
          </li>
        ) : null}
      </ol>
    </section>
  );
}

function Journey({ details }: { details: PackageDetails }) {
  const makkahEnd = Math.max(details.makkah.nights, 1);
  const madinahStart = makkahEnd + 1;

  return (
    <section className="package-conversion-journey">
      <h2>Your itinerary</h2>
      <ol>
        <JourneyItem
          label="Day 1"
          icon="airplane-tilt"
          title="Arrival & transfer"
          copy={`${formatDate(details.departureDate)} · ${details.origin}`}
        />
        <JourneyItem
          label={`Day 1–${makkahEnd}`}
          icon="building"
          title="Makkah stay"
          copy={`${details.makkah.nights} nights · ${details.makkah.hotelName}`}
        />
        <JourneyItem
          label={`Day ${makkahEnd + 1}`}
          icon="bus"
          title="Intercity travel"
          copy={details.travel.details}
        />
        <JourneyItem
          label={`Day ${madinahStart}–${details.durationNights}`}
          icon="building"
          title="Madinah stay"
          copy={`${details.madinah.nights} nights · ${details.madinah.hotelName}`}
        />
        <JourneyItem
          label="Return"
          icon="airplane-tilt"
          title="Departure"
          copy={`${formatDate(details.returnDate)} · ${details.travel.routeSummary}`}
        />
      </ol>
    </section>
  );
}

function JourneyItem({
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
      <span className="package-journey-day">{label}</span>
      <span className="package-journey-icon" aria-hidden="true">
        <Icon name={icon} />
      </span>
      <div>
        <strong>{title}</strong>
        <p>{copy}</p>
      </div>
    </li>
  );
}

function PackageContent({ details }: { details: PackageDetails }) {
  return (
    <div className="package-conversion-features">
      <ContentGrid
        title="Package includes"
        items={contentItems(details.inclusions, "package")}
        fallback="package"
      />
      <ContentGrid
        title="Travel kit included"
        items={contentItems(details.inclusions, "travel-kit")}
        fallback="travel-kit"
        hideWhenEmpty
      />
      <ContentGrid
        title="Umrah kit included"
        items={contentItems(details.inclusions, "umrah-kit")}
        fallback="umrah-kit"
        hideWhenEmpty
      />
      <ContentGrid
        title="Not included"
        items={details.exclusions}
        fallback="excluded"
      />
    </div>
  );
}

function ContentGrid({
  title,
  items,
  fallback,
  hideWhenEmpty = false,
}: {
  title: string;
  items: string[];
  fallback: ContentGroup;
  hideWhenEmpty?: boolean;
}) {
  if (hideWhenEmpty && items.length === 0) return null;

  return (
    <section className="package-content-grid">
      <h2>{title}</h2>
      {items.length ? (
        <ul>
          {items.map((item) => {
            const meta = contentMetadata(item, fallback);
            return (
              <li key={item}>
                <span className="package-content-icon">
                  <PackageContentIcon name={meta.icon} />
                </span>
                <span>{meta.label ?? item}</span>
              </li>
            );
          })}
        </ul>
      ) : (
        <p>No items have been published in this section.</p>
      )}
    </section>
  );
}

function TrustAndTerms({ details }: { details: PackageDetails }) {
  const confirmed = factLabels(details, "confirmed");
  const pending = factLabels(details, "pending");

  return (
    <div className="package-conversion-status">
      <section className="package-fact-status">
        <h2>Confirmed &amp; pending</h2>
        <div>
          <ul>
            {confirmed.map((item) => (
              <li key={item}>
                <Icon name="seal-check" /> {item}
              </li>
            ))}
          </ul>
          <ul className="pending">
            {pending.map((item) => (
              <li key={item}>
                <Icon name="clock" /> {item}
              </li>
            ))}
          </ul>
        </div>
      </section>

      <section className="package-cancellation-summary">
        <h2>Cancellation summary</h2>
        <dl>
          <div>
            <dt>Cancellation policy</dt>
            <dd>Reviewed before payment</dd>
          </div>
          <div>
            <dt>Refund entitlement</dt>
            <dd>Shown before commitment</dd>
          </div>
          <div>
            <dt>Payment schedule</dt>
            <dd>Shown before Book now</dd>
          </div>
          <div>
            <dt>Need clarification?</dt>
            <dd>Human support available</dd>
          </div>
        </dl>
        <a href="mailto:support@noorpath.example">
          Ask about payment &amp; refund terms ›
        </a>
      </section>
    </div>
  );
}

function StickyBookingBar({
  details,
  selected,
  paymentMode,
}: {
  details: PackageDetails;
  selected: OccupancyDetail;
  paymentMode: PaymentMode;
}) {
  return (
    <aside className="package-conversion-sticky" aria-label="Booking summary">
      <div>
        <span>
          <small>Total package</small>
          <strong>
            {formatMoney(selected.financials.total, details.pricing.currency)}
          </strong>
        </span>
        <span>
          <small>Pay today</small>
          <strong className="due">
            {formatMoney(selected.financials.dueNow, details.pricing.currency)}
          </strong>
        </span>
        <span className="remaining">
          <small>Remaining</small>
          <strong>
            {formatMoney(
              selected.financials.remaining,
              details.pricing.currency,
            )}
          </strong>
        </span>
      </div>
      <Link
        href={bookingHref(details.departureId, selected.occupancy, paymentMode)}
      >
        Book now <span aria-hidden="true">›</span>
      </Link>
    </aside>
  );
}

function occupancyForGuests(details: PackageDetails, guests: number) {
  return details.pricing.occupancies.find(
    (item) =>
      item.financials.adultGuests === guests && item.status === "available",
  );
}

function contentMetadata(
  item: string,
  fallback: ContentGroup,
): ContentMetadata {
  if (item.startsWith("Intercity travel by ")) {
    return { group: "package", icon: "bus" };
  }
  return contentMeta[item] ?? { group: fallback, icon: "custom" };
}

function contentItems(items: string[], group: ContentGroup) {
  return items.filter(
    (item) => contentMetadata(item, "package").group === group,
  );
}

function factLabels(details: PackageDetails, state: ConfirmationState) {
  const facts: string[] = [];
  if (details.makkah.confirmationState === state)
    facts.push(`Makkah hotel ${state}`);
  if (details.madinah.confirmationState === state)
    facts.push(`Madinah hotel ${state}`);
  if (details.travel.confirmationState === state)
    facts.push(`Travel schedule ${state}`);
  if (state === "confirmed") {
    if (details.inclusions.includes("Visa included"))
      facts.push("Umrah visa included");
    if (
      details.inclusions.includes("Breakfast, lunch and dinner") ||
      details.inclusions.includes("Breakfast")
    )
      facts.push("Meals included");
    if (details.inclusions.includes("Ziyarat transport"))
      facts.push("Ziyarat transport included");
    if (details.inclusions.includes("Umrah guidance"))
      facts.push("Umrah guidance included");
  }
  return facts;
}

function bookingHref(
  departureId: string,
  occupancy: Occupancy,
  paymentMode: PaymentMode,
) {
  const query = new URLSearchParams({ occupancy, paymentMode });
  return `/packages/${departureId}/plan?${query.toString()}`;
}

function occupancyLabel(occupancy: Occupancy) {
  return occupancy === "double"
    ? "Double sharing"
    : occupancy === "triple"
      ? "Triple sharing"
      : "Quad sharing";
}

function confirmationCopy(state: ConfirmationState) {
  return state === "confirmed" ? "Confirmed" : "Pending confirmation";
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en-IN", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  }).format(new Date(`${value}T00:00:00Z`));
}

function formatMoney(amount: number, currency: string) {
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(amount);
}

function PackageLoading() {
  return (
    <section className="package-state-card" role="status" aria-live="polite">
      <span className="package-state-icon">
        <Icon name="clock" />
      </span>
      <h1>Loading package details</h1>
      <p>Checking the latest published journey, pricing and availability.</p>
    </section>
  );
}

function PackageUnavailable() {
  return (
    <section className="package-state-card" role="status">
      <span className="package-state-icon">
        <Icon name="map-trifold" />
      </span>
      <h1>This package is not currently available.</h1>
      <p>
        It may have been withdrawn, sold out, or is no longer eligible for
        public booking.
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
    <section className="package-state-card error" role="alert">
      <span className="package-state-icon">
        <Icon name="shield-check" />
      </span>
      <h1>We could not load this package right now.</h1>
      <p>Your booking has not changed. Retry when you are ready.</p>
      {correlationId ? <small>Reference: {correlationId}</small> : null}
      <button type="button" onClick={onRetry}>
        Try again
      </button>
    </section>
  );
}
