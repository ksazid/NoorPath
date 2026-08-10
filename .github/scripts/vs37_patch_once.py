from pathlib import Path
import json
import re


def replace_block(text: str, pattern: str, replacement: str, label: str) -> str:
    updated, count = re.subn(pattern, replacement, text, count=1, flags=re.S)
    if count != 1:
        raise SystemExit(f"Could not replace {label}; matched {count}")
    return updated


page = Path("apps/web/app/packages/[departureId]/page.tsx")
text = page.read_text()
text = text.replace(
    'import { useCallback, useEffect, useState } from "react";',
    'import { useCallback, useEffect, useRef, useState } from "react";',
)

experience_and_dates = r'''function PackageExperience({ details }: { details: PackageDetails }) {
  const firstAvailable = details.pricing.occupancies.find(
    (item) => item.status === "available",
  );
  const [occupancy, setOccupancy] = useState<Occupancy>(
    firstAvailable?.occupancy ?? "double",
  );
  const [paymentMode, setPaymentMode] = useState<PaymentMode>("pay-full");
  const [bookingOpen, setBookingOpen] = useState(false);
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
          <Link href="/">Umrah Packages</Link>
          <span aria-hidden="true">›</span>
          <span>{details.packageName}</span>
        </nav>

        <section
          className="package-conversion-overview"
          aria-labelledby="package-title"
        >
          <div className="package-conversion-primary">
            <div className="package-conversion-hero-row">
              <Gallery selected={selected} />
              <OperatorSummary details={details} />
            </div>
            <div className="package-conversion-content">
              <Journey details={details} />
              <PackageContent details={details} />
            </div>
          </div>
          <BookingCard
            details={details}
            selected={selected}
            paymentMode={paymentMode}
            onOccupancyChange={setOccupancy}
            onPaymentModeChange={setPaymentMode}
            onBookNow={() => setBookingOpen(true)}
          />
        </section>

        <div className="package-conversion-secondary">
          <TrustAndTerms details={details} />
          <section className="package-conversion-about">
            <h2>About this package</h2>
            <p>{details.summary}</p>
            <p>
              Published pricing, current room availability and payment commitments
              are visible before you start booking.
            </p>
          </section>
        </div>
      </main>

      <PublicFooter />
      <StickyBookingBar
        details={details}
        selected={selected}
        paymentMode={paymentMode}
        onBookNow={() => setBookingOpen(true)}
      />
      <BookingAuthSheet
        open={bookingOpen}
        onClose={() => setBookingOpen(false)}
        details={details}
        selected={selected}
        paymentMode={paymentMode}
      />
    </>
  );
}

function TravelDates({ details }: { details: PackageDetails }) {
  const scrollerRef = useRef<HTMLDivElement>(null);
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

  const moveDates = (direction: -1 | 1) => {
    scrollerRef.current?.scrollBy({ left: direction * 220, behavior: "auto" });
  };

  return (
    <section
      className="package-travel-dates"
      aria-labelledby="travel-dates-title"
    >
      <div className="package-travel-dates-heading">
        <div>
          <h2 id="travel-dates-title">Available Travel Dates</h2>
          <p>Published departures from {details.origin}</p>
        </div>
        <div className="package-date-controls" aria-label="Browse travel dates">
          <button
            type="button"
            aria-label="Previous travel dates"
            onClick={() => moveDates(-1)}
          >
            ‹
          </button>
          <button
            type="button"
            aria-label="Next travel dates"
            onClick={() => moveDates(1)}
          >
            ›
          </button>
        </div>
      </div>
      <div
        ref={scrollerRef}
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

'''
text = replace_block(
    text,
    r"function PackageExperience\(.*?\nfunction Gallery",
    experience_and_dates + "function Gallery",
    "PackageExperience and TravelDates",
)

booking_card = r'''function BookingCard({
  details,
  selected,
  paymentMode,
  onOccupancyChange,
  onPaymentModeChange,
  onBookNow,
}: {
  details: PackageDetails;
  selected: OccupancyDetail;
  paymentMode: PaymentMode;
  onOccupancyChange: (value: Occupancy) => void;
  onPaymentModeChange: (value: PaymentMode) => void;
  onBookNow: () => void;
}) {
  const financials = selected.financials;
  const hasFuturePlan =
    financials.remaining > 0 &&
    financials.instalments.length > 0 &&
    financials.finalDueDate !== null;
  const effectivePaymentMode: PaymentMode = hasFuturePlan
    ? paymentMode
    : "pay-full";
  const visibleFinancials = paymentPreview(financials, effectivePaymentMode);

  return (
    <aside
      className="package-booking-card"
      aria-label="Booking and payment summary"
    >
      <div className="package-booking-heading">
        <h2>Plan your booking</h2>
        <p>Review the full commitment before you book.</p>
      </div>

      <TravelDates details={details} />

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

      <fieldset className="package-payment-mode">
        <legend>Payment Options</legend>
        <div className="package-payment-mode-options">
          <label
            className={effectivePaymentMode === "pay-full" ? "selected" : ""}
          >
            <input
              type="radio"
              name="payment-mode"
              checked={effectivePaymentMode === "pay-full"}
              onChange={() => onPaymentModeChange("pay-full")}
            />
            <span>
              <strong>Pay Full</strong>
              <small>Pay the complete package amount today</small>
            </span>
          </label>
          {hasFuturePlan ? (
            <>
              <label
                className={
                  effectivePaymentMode === "milestone" ? "selected" : ""
                }
              >
                <input
                  type="radio"
                  name="payment-mode"
                  checked={effectivePaymentMode === "milestone"}
                  onChange={() => onPaymentModeChange("milestone")}
                />
                <span>
                  <strong>Milestone</strong>
                  <small>Pay in published stages before departure</small>
                </span>
              </label>
              <label
                className={
                  effectivePaymentMode === "pay-later" ? "selected" : ""
                }
              >
                <input
                  type="radio"
                  name="payment-mode"
                  checked={effectivePaymentMode === "pay-later"}
                  onChange={() => onPaymentModeChange("pay-later")}
                />
                <span>
                  <strong>Pay Later</strong>
                  <small>
                    Minimum today, remaining balance by final deadline
                  </small>
                </span>
              </label>
            </>
          ) : null}
        </div>
      </fieldset>

      <PaymentBreakdown
        financials={financials}
        currency={details.pricing.currency}
        mode={effectivePaymentMode}
      />

      <div className="package-price-breakdown">
        <h3>Price Breakdown</h3>
        <dl>
          <div>
            <dt>Airline</dt>
            <dd className="pending-value">To be published</dd>
          </div>
          <div>
            <dt>Departure Route</dt>
            <dd>{details.origin} to Jeddah</dd>
          </div>
          <div>
            <dt>Return Route</dt>
            <dd>Jeddah to {details.origin}</dd>
          </div>
          <div className="package-unit-price">
            <dt>
              {formatMoney(selected.amount, details.pricing.currency)} ×{" "}
              {financials.adultGuests}
              <small>
                ({occupancyLabel(selected.occupancy)}, {financials.adultGuests}{" "}
                people)
              </small>
            </dt>
            <dd>{formatMoney(financials.total, details.pricing.currency)}</dd>
          </div>
          <div>
            <dt>Total Price Before Discount</dt>
            <dd>{formatMoney(financials.total, details.pricing.currency)}</dd>
          </div>
          <div className="discount">
            <dt>Discount</dt>
            <dd>0% · {formatMoney(0, details.pricing.currency)}</dd>
          </div>
          <div>
            <dt>Total Price After Discount</dt>
            <dd>{formatMoney(financials.total, details.pricing.currency)}</dd>
          </div>
          <div>
            <dt>Service Provider</dt>
            <dd>{details.operator.displayName}</dd>
          </div>
          <div>
            <dt>Powered &amp; Supported by</dt>
            <dd>NoorPath</dd>
          </div>
          <div className="total">
            <dt>TOTAL</dt>
            <dd>{formatMoney(financials.total, details.pricing.currency)}</dd>
          </div>
          <div className="due">
            <dt>Pay today</dt>
            <dd>
              {formatMoney(visibleFinancials.dueNow, details.pricing.currency)}
            </dd>
          </div>
          <div>
            <dt>Remaining</dt>
            <dd>
              {formatMoney(
                visibleFinancials.remaining,
                details.pricing.currency,
              )}
            </dd>
          </div>
        </dl>
        <p className="package-tax-note">
          Taxes, if applicable, will be shown before payment. Discount and tax
          rules are not yet operator-configurable.
        </p>
      </div>

      {!hasFuturePlan ? (
        <p className="package-full-payment">
          <Icon name="receipt" /> This departure is published as full payment
          only.
        </p>
      ) : null}
      <p className="package-payment-note">
        Price and availability are rechecked before a place is reserved.
      </p>
      <button className="package-book-now" type="button" onClick={onBookNow}>
        <Icon name="lock" /> Book now <span aria-hidden="true">›</span>
      </button>
    </aside>
  );
}

'''
text = replace_block(
    text,
    r"function BookingCard\(.*?\nfunction GuestSelector",
    booking_card + "function GuestSelector",
    "BookingCard",
)

sticky_and_auth = r'''function StickyBookingBar({
  details,
  selected,
  paymentMode,
  onBookNow,
}: {
  details: PackageDetails;
  selected: OccupancyDetail;
  paymentMode: PaymentMode;
  onBookNow: () => void;
}) {
  const hasFuturePlan =
    selected.financials.remaining > 0 &&
    selected.financials.instalments.length > 0 &&
    selected.financials.finalDueDate !== null;
  const effectivePaymentMode: PaymentMode = hasFuturePlan
    ? paymentMode
    : "pay-full";
  const visibleFinancials = paymentPreview(
    selected.financials,
    effectivePaymentMode,
  );

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
            {formatMoney(visibleFinancials.dueNow, details.pricing.currency)}
          </strong>
        </span>
        <span className="remaining">
          <small>Remaining</small>
          <strong>
            {formatMoney(visibleFinancials.remaining, details.pricing.currency)}
          </strong>
        </span>
      </div>
      <button type="button" onClick={onBookNow}>
        Book now <span aria-hidden="true">›</span>
      </button>
    </aside>
  );
}

function BookingAuthSheet({
  open,
  onClose,
  details,
  selected,
  paymentMode,
}: {
  open: boolean;
  onClose: () => void;
  details: PackageDetails;
  selected: OccupancyDetail;
  paymentMode: PaymentMode;
}) {
  const [stage, setStage] = useState<"phone" | "otp" | "travellers">("phone");
  const [phone, setPhone] = useState("");
  const [otp, setOtp] = useState("");
  const [travellers, setTravellers] = useState([""]);
  const phoneRef = useRef<HTMLInputElement>(null);
  const hasFuturePlan =
    selected.financials.remaining > 0 &&
    selected.financials.instalments.length > 0 &&
    selected.financials.finalDueDate !== null;
  const effectivePaymentMode: PaymentMode = hasFuturePlan
    ? paymentMode
    : "pay-full";
  const visibleFinancials = paymentPreview(
    selected.financials,
    effectivePaymentMode,
  );

  useEffect(() => {
    if (!open) return;
    setStage("phone");
    setPhone("");
    setOtp("");
    setTravellers([""]);
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    window.setTimeout(() => phoneRef.current?.focus(), 0);
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") onClose();
    };
    window.addEventListener("keydown", onKeyDown);
    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [open, onClose]);

  if (!open) return null;

  const maxTravellers = selected.financials.adultGuests;
  const travellerNamesComplete =
    travellers.length === maxTravellers &&
    travellers.every((name) => name.trim().length > 0);

  return (
    <div className="package-auth-overlay" role="presentation">
      <section
        className="package-auth-sheet"
        role="dialog"
        aria-modal="true"
        aria-labelledby="package-auth-title"
      >
        <header>
          <div>
            <p>Secure booking</p>
            <h2 id="package-auth-title">
              {stage === "travellers" ? "Add travellers" : "Login with mobile OTP"}
            </h2>
          </div>
          <button type="button" onClick={onClose} aria-label="Close booking login">
            ×
          </button>
        </header>

        <div className="package-auth-summary">
          <span>
            <small>{details.packageName}</small>
            <strong>{formatDate(details.departureDate)}</strong>
          </span>
          <span>
            <small>{occupancyLabel(selected.occupancy)}</small>
            <strong>{selected.financials.adultGuests} adults</strong>
          </span>
          <span>
            <small>Pay today</small>
            <strong>
              {formatMoney(visibleFinancials.dueNow, details.pricing.currency)}
            </strong>
          </span>
        </div>

        {stage === "phone" ? (
          <div className="package-auth-step">
            <label htmlFor="booking-mobile">Mobile number</label>
            <div className="package-phone-field">
              <span>+91</span>
              <input
                ref={phoneRef}
                id="booking-mobile"
                type="tel"
                inputMode="numeric"
                autoComplete="tel-national"
                placeholder="Enter mobile number"
                value={phone}
                onChange={(event) =>
                  setPhone(event.target.value.replace(/\D/g, "").slice(0, 10))
                }
              />
            </div>
            <p>
              We will use this number for booking updates and, once enabled,
              WhatsApp journey notifications.
            </p>
            <button
              className="package-auth-primary"
              type="button"
              disabled={phone.length !== 10}
              onClick={() => setStage("otp")}
            >
              Send code
            </button>
            <small className="package-preview-note">
              Design preview: no SMS is sent yet. Phone OTP will be connected in
              the authentication slice.
            </small>
          </div>
        ) : null}

        {stage === "otp" ? (
          <div className="package-auth-step">
            <button
              className="package-auth-back"
              type="button"
              onClick={() => setStage("phone")}
            >
              ‹ Change mobile number
            </button>
            <label htmlFor="booking-otp">6-digit verification code</label>
            <input
              id="booking-otp"
              className="package-otp-field"
              inputMode="numeric"
              autoComplete="one-time-code"
              placeholder="000000"
              value={otp}
              onChange={(event) =>
                setOtp(event.target.value.replace(/\D/g, "").slice(0, 6))
              }
            />
            <button
              className="package-auth-primary"
              type="button"
              disabled={otp.length !== 6}
              onClick={() => setStage("travellers")}
            >
              Verify & continue
            </button>
            <small className="package-preview-note">
              Design preview only. This step does not authenticate an account.
            </small>
          </div>
        ) : null}

        {stage === "travellers" ? (
          <div className="package-auth-step package-traveller-step">
            <p>
              Add the names of the {maxTravellers} travellers for this room.
              Passport details can be completed later in the governed journey.
            </p>
            <div className="package-traveller-list">
              {travellers.map((name, index) => (
                <label key={index}>
                  <span>Traveller {index + 1}</span>
                  <input
                    aria-label={`Traveller ${index + 1}`}
                    type="text"
                    autoComplete="name"
                    placeholder="Full name"
                    value={name}
                    onChange={(event) =>
                      setTravellers((current) =>
                        current.map((item, itemIndex) =>
                          itemIndex === index ? event.target.value : item,
                        ),
                      )
                    }
                  />
                </label>
              ))}
            </div>
            {travellers.length < maxTravellers ? (
              <button
                className="package-add-traveller"
                type="button"
                onClick={() => setTravellers((current) => [...current, ""])}
              >
                + Add traveller
              </button>
            ) : null}
            <button
              className="package-auth-primary"
              type="button"
              disabled={!travellerNamesComplete}
            >
              Continue to secure booking
            </button>
            <small className="package-preview-note">
              Booking continuation will activate only after real OTP verification
              is connected.
            </small>
          </div>
        ) : null}
      </section>
    </div>
  );
}

'''
text = replace_block(
    text,
    r"function StickyBookingBar\(.*?\nfunction occupancyForGuests",
    sticky_and_auth + "function occupancyForGuests",
    "StickyBookingBar and BookingAuthSheet",
)
text = text.replace(
    '<Link href="/#packages">Browse available packages</Link>',
    '<Link href="/">Browse available packages</Link>',
)
page.write_text(text)

css = Path("apps/web/app/packages/[departureId]/package-conversion.css")
css.write_text(
    css.read_text()
    + r'''

/* VS-37: Package Details dossier layout and booking-auth design preview */
.package-conversion-overview {
  grid-template-columns: minmax(0, 1fr) minmax(340px, 390px);
  gap: 30px;
}

.package-conversion-primary {
  min-width: 0;
}

.package-conversion-hero-row {
  display: grid;
  grid-template-columns: minmax(0, 1.12fr) minmax(250px, 0.78fr);
  gap: 24px;
  align-items: start;
}

.package-conversion-primary > .package-conversion-content {
  grid-template-columns: minmax(0, 0.88fr) minmax(0, 1.12fr);
  margin-top: 22px;
}

.package-conversion-secondary {
  display: grid;
  gap: 22px;
  margin-top: 24px;
}

.package-conversion-secondary .package-conversion-about {
  margin-top: 0;
}

.package-booking-card {
  position: sticky;
  top: 92px;
}

.package-booking-card .package-travel-dates {
  margin: 0 0 14px;
  padding-bottom: 14px;
  border-bottom: 1px solid var(--public-line);
}

.package-booking-card .package-travel-dates-heading {
  align-items: center;
  gap: 12px;
  margin-bottom: 10px;
}

.package-booking-card .package-travel-dates-heading h2 {
  font-family: inherit;
  font-size: 0.9375rem;
  font-weight: 700;
  letter-spacing: 0;
}

.package-booking-card .package-travel-dates-heading p {
  font-size: 0.75rem;
}

.package-date-controls {
  display: flex;
  flex: 0 0 auto;
  gap: 8px;
}

.package-date-controls button {
  display: grid;
  width: 44px;
  height: 44px;
  place-items: center;
  padding: 0;
  border: 1px solid var(--public-line);
  border-radius: 50%;
  background: var(--package-surface);
  color: var(--public-ink);
  cursor: pointer;
  font: inherit;
  font-size: 1.35rem;
  transition:
    border-color 140ms var(--package-ease-out),
    transform 120ms var(--package-ease-out);
}

.package-date-controls button:active,
.package-book-now:active,
.package-conversion-sticky > button:active,
.package-auth-primary:active,
.package-add-traveller:active {
  transform: scale(0.97);
}

.package-booking-card .package-date-scroller {
  gap: 8px;
  padding-bottom: 5px;
}

.package-booking-card .package-date-card {
  min-width: 184px;
  min-height: 92px;
  flex-basis: 184px;
  padding: 12px;
}

.package-unit-price dt {
  display: grid;
  gap: 2px;
}

.package-unit-price dt small {
  color: var(--public-muted);
  font-size: 0.75rem;
}

.package-price-breakdown .discount dd {
  color: var(--public-green-deep);
}

.package-price-breakdown .pending-value {
  color: var(--public-muted);
  font-weight: 600;
}

.package-price-breakdown dl > div {
  align-items: baseline;
}

.package-price-breakdown dt {
  min-width: 0;
}

.package-price-breakdown dd {
  max-width: 58%;
  overflow-wrap: anywhere;
}

.package-tax-note {
  margin: 14px 0 0;
  padding-top: 12px;
  border-top: 1px solid var(--public-line);
  color: var(--public-muted);
  font-size: 0.75rem;
  line-height: 1.5;
  text-align: center;
}

.package-book-now,
.package-conversion-sticky > button {
  border: 0;
  font: inherit;
  cursor: pointer;
}

.package-book-now {
  display: flex;
  width: 100%;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.package-book-now .public-icon {
  width: 18px;
  height: 18px;
}

.package-conversion-sticky > button {
  display: inline-flex;
  min-height: 48px;
  align-items: center;
  justify-content: center;
  padding: 0 22px;
  border-radius: 10px;
  background: var(--public-green);
  color: white;
  font-weight: 700;
}

.package-auth-overlay {
  position: fixed;
  z-index: 1200;
  inset: 0;
  display: grid;
  place-items: center;
  padding: 24px;
  background: rgb(23 23 21 / 52%);
}

.package-auth-sheet {
  width: min(100%, 560px);
  max-height: min(760px, calc(100dvh - 48px));
  overflow-y: auto;
  box-sizing: border-box;
  padding: 24px;
  border: 1px solid var(--public-line);
  border-radius: 18px;
  background: var(--package-surface);
  box-shadow: 0 28px 80px rgb(23 23 21 / 22%);
}

.package-auth-sheet > header {
  display: flex;
  align-items: start;
  justify-content: space-between;
  gap: 18px;
  margin-bottom: 18px;
}

.package-auth-sheet > header p {
  margin: 0 0 4px;
  color: var(--public-green-deep);
  font-size: 0.75rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.package-auth-sheet > header h2 {
  margin: 0;
  color: var(--public-ink);
  font-family: Georgia, "Times New Roman", serif;
  font-size: 1.65rem;
  font-weight: 500;
}

.package-auth-sheet > header > button {
  display: grid;
  width: 44px;
  height: 44px;
  flex: 0 0 auto;
  place-items: center;
  padding: 0;
  border: 1px solid var(--public-line);
  border-radius: 50%;
  background: var(--package-surface);
  color: var(--public-ink);
  cursor: pointer;
  font-size: 1.35rem;
}

.package-auth-summary {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 8px;
  margin-bottom: 20px;
  padding: 12px;
  border-radius: 12px;
  background: var(--package-soft);
}

.package-auth-summary span {
  display: grid;
  gap: 3px;
  min-width: 0;
}

.package-auth-summary small {
  overflow: hidden;
  color: var(--public-muted);
  font-size: 0.6875rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.package-auth-summary strong {
  color: var(--public-ink);
  font-size: 0.8125rem;
  font-variant-numeric: tabular-nums;
}

.package-auth-step {
  display: grid;
  gap: 12px;
}

.package-auth-step > label,
.package-traveller-list label > span {
  color: var(--public-ink);
  font-size: 0.875rem;
  font-weight: 700;
}

.package-auth-step > p {
  margin: 0;
  color: var(--public-muted);
  font-size: 0.875rem;
  line-height: 1.55;
}

.package-phone-field {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr);
  align-items: center;
  min-height: 54px;
  overflow: hidden;
  border: 1px solid var(--public-line);
  border-radius: 12px;
  background: var(--package-surface);
}

.package-phone-field > span {
  padding: 0 13px;
  border-right: 1px solid var(--public-line);
  color: var(--public-muted);
  font-weight: 700;
}

.package-phone-field input,
.package-otp-field,
.package-traveller-list input {
  min-width: 0;
  min-height: 52px;
  box-sizing: border-box;
  border: 1px solid var(--public-line);
  border-radius: 10px;
  background: var(--package-surface);
  color: var(--public-ink);
  font: inherit;
  font-size: 1rem;
}

.package-phone-field input {
  border: 0;
  border-radius: 0;
  padding: 0 13px;
  outline-offset: -3px;
}

.package-otp-field,
.package-traveller-list input {
  width: 100%;
  padding: 0 14px;
}

.package-otp-field {
  font-size: 1.35rem;
  letter-spacing: 0.25em;
  font-variant-numeric: tabular-nums;
}

.package-auth-primary,
.package-add-traveller,
.package-auth-back {
  min-height: 48px;
  border-radius: 10px;
  font: inherit;
  cursor: pointer;
}

.package-auth-primary {
  border: 0;
  background: var(--public-green);
  color: white;
  font-weight: 700;
}

.package-auth-primary:disabled {
  cursor: not-allowed;
  opacity: 0.45;
}

.package-auth-back,
.package-add-traveller {
  justify-self: start;
  padding: 0 14px;
  border: 1px solid var(--public-line);
  background: var(--package-surface);
  color: var(--public-green-deep);
  font-weight: 700;
}

.package-preview-note {
  color: var(--public-muted);
  font-size: 0.75rem;
  line-height: 1.45;
}

.package-traveller-list {
  display: grid;
  gap: 12px;
}

.package-traveller-list label {
  display: grid;
  gap: 6px;
}

@media (max-width: 980px) {
  .package-conversion-overview {
    grid-template-columns: 1fr;
  }

  .package-booking-card {
    position: static;
  }

  .package-conversion-hero-row,
  .package-conversion-primary > .package-conversion-content {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 640px) {
  .package-conversion-main {
    width: min(calc(100% - 28px), 1240px);
  }

  .package-conversion-hero-row {
    gap: 14px;
  }

  .package-conversion-gallery {
    min-height: 310px;
  }

  .package-conversion-operator {
    padding: 8px 0 4px;
  }

  .package-booking-card {
    padding: 18px 14px;
  }

  .package-booking-card .package-date-card {
    min-width: min(78vw, 230px);
    flex-basis: min(78vw, 230px);
  }

  .package-auth-overlay {
    align-items: end;
    padding: 0;
  }

  .package-auth-sheet {
    width: 100%;
    max-height: calc(100dvh - 16px);
    padding: 20px 16px calc(20px + env(safe-area-inset-bottom));
    border-radius: 20px 20px 0 0;
  }

  .package-auth-summary {
    grid-template-columns: 1fr;
  }

  .package-auth-summary small {
    white-space: normal;
  }

  .package-price-breakdown dl > div {
    gap: 12px;
  }

  .package-price-breakdown dd {
    max-width: 52%;
  }
}

@media (prefers-reduced-motion: reduce) {
  .package-date-controls button,
  .package-book-now,
  .package-conversion-sticky > button,
  .package-auth-primary,
  .package-add-traveller {
    transition: none;
  }
}
'''
)

spec = """# VS-37 — Package Details Booking Decision & OTP Design

## Outcome
A prospective customer can evaluate a published departure in one continuous Package Details dossier, choose among same-origin dates without hash navigation, review a complete commercial price breakdown, default to Pay Full, and open a design-first phone OTP/traveller flow before any booking commitment.

## Product rules
- Pay Full is the default payment choice.
- Available Travel Dates replaces the single Travel date selector inside the booking card.
- Previous/next date controls scroll in place and never write a hash fragment to the address bar.
- Itinerary and operator-authored package content sit directly below the image/operator profile, not below the booking card row.
- Price Breakdown is visible before Book now and includes route, unit price, pre-discount total, discount row, after-discount total, service provider, NoorPath support attribution, tax disclosure, final total, pay-today and remaining values.
- Until discount/tax rules are implemented, the UI must not invent a discount; it displays zero discount and explicitly states configuration is pending.
- Book now opens a design preview of mobile OTP. No SMS is sent and no account is authenticated in this slice.
- The design preview exposes the post-login traveller-name step with + Add traveller up to the selected adult guest count.
- Child/infant pricing remains out of scope.

## Visual authority
Approved NoorPath Package reference → design-system/MASTER.md → Design Taste Frontend for the package surface → UI UX Pro Max → Impeccable bounded refinement → Emil purposeful feedback → Ponytail minimum implementation.

## Exclusions
- Real phone OTP/Auth0 configuration or authentication persistence.
- Traveller persistence from the preview.
- Discount/tax operator configuration.
- Airline data integration; airline is intentionally shown as pending until operator flight authoring is implemented.
- Hotel/airport/airline operator authoring; that follows as VS-38.
- Deployment.
"""
Path("docs/slices/VS-37-PACKAGE-DETAILS-BOOKING-AUTH-DESIGN.md").write_text(spec)

checklist = """# VS-37 Implementation Checklist

- [x] Read AGENTS.md and NoorPath design skills.
- [x] Preserve approved Package visual language.
- [x] Remove hash navigation from changed Package Details interactions.
- [x] Default payment mode to Pay Full.
- [x] Move Available Travel Dates into booking card with previous/next controls.
- [x] Recompose image/operator + itinerary/inclusions into a continuous primary column.
- [x] Add complete pre-booking price breakdown with truthful zero-discount placeholder.
- [x] Add design-preview mobile OTP sheet.
- [x] Add post-login traveller-name design with + Add traveller.
- [ ] Exact-head format/static/unit/integration gates.
- [ ] Rendered desktop/mobile/200%/reduced-motion/keyboard review.
- [ ] Product Owner screenshot acceptance.
"""
Path("docs/slices/VS-37-IMPLEMENTATION-CHECKLIST.md").write_text(checklist)

navigation = """# VS-37 Navigation Verification

| Path | Expected outcome | Evidence |
| --- | --- | --- |
| `/packages/{departureId}` → Previous/Next travel dates | Date rail moves in place; URL remains hash-free | `apps/web/e2e/package-details.spec.ts` |
| `/packages/{departureId}` → available sibling date | Navigates to sibling departure without a hash fragment; browser Back returns | `apps/web/e2e/package-details.spec.ts` |
| Package Details → Guests/Room/Payment | Local selection changes do not mutate URL hash | `apps/web/e2e/package-details.spec.ts` |
| Package Details → Book now | Opens OTP design sheet without route/hash mutation | `apps/web/e2e/package-details.spec.ts` |
| OTP preview → traveller step → + Add traveller | Adds name-only traveller rows up to selected adult count; no persistence | `apps/web/e2e/package-details.spec.ts` |
"""
Path("docs/slices/VS-37-NAVIGATION-VERIFICATION.md").write_text(navigation)

manifest = {
    "id": "VS-37",
    "title": "Package Details Booking Decision & OTP Design",
    "slug": "package-details-booking-auth-design",
    "outcome": "Customers evaluate dates, package facts and the complete price commitment in one hash-free Package Details dossier, then preview phone OTP and traveller-name capture before booking.",
    "actor": "Unauthenticated prospective customers evaluating a published NoorPath Umrah departure.",
    "dependsOn": ["VS-36", "VS-29", "VS-30"],
    "modules": [
        "Web-only design/interaction correction on the public package surface.",
        "Existing authoritative package, occupancy and payment preview data remains unchanged.",
        "No authentication, traveller, discount or tax persistence is introduced.",
        "Approved NoorPath Package reference and design-system/MASTER.md remain visual authority.",
    ],
    "routes": [
        "Public discovery -> /packages/{departureId} -> Package Details dossier.",
        "Package Details -> available sibling date -> /packages/{siblingDepartureId}, without hash fragments.",
        "Package Details -> Previous/Next travel dates -> in-place rail movement, URL unchanged.",
        "Package Details -> Book now -> phone OTP design preview, URL unchanged.",
        "OTP preview -> traveller names -> + Add traveller, no persistence.",
    ],
    "acceptance": [
        "Pay Full is selected by default.",
        "Available Travel Dates replaces the single Travel date row and exposes visible previous/next controls.",
        "No changed Package Details section interaction writes a hash fragment into the address bar.",
        "Your itinerary and operator-authored Package Includes/Not Included content render directly below image/operator profile in the primary content column.",
        "Price Breakdown before Book now includes route, unit price, pre-discount, discount, after-discount, service provider, NoorPath attribution, tax disclosure, TOTAL, pay today and remaining.",
        "Until real discount rules exist, the UI displays a truthful zero discount and a pending-configuration disclosure.",
        "Book now opens a clearly labelled design preview for mobile OTP; no SMS/authentication is claimed.",
        "The design preview exposes post-login name-only traveller capture and + Add traveller up to the authoritative adult count.",
        "Desktop, mobile, keyboard, focus, target-size, axe, 200-percent-text, reduced-motion and no-horizontal-overflow checks pass.",
        "Product Owner visually accepts screenshots for the exact final SHA before merge.",
    ],
    "exclusions": [
        "Real OTP/Auth0 provider configuration or account authentication.",
        "Traveller persistence from the design preview.",
        "Operator discount/tax configuration.",
        "Airline, hotel, airport or flight-leg provider integration; those are VS-38.",
        "Child/infant pricing or operator configuration.",
        "Production deployment before explicit authorization.",
    ],
    "qualityGates": [
        "slice-governance",
        "format",
        "static-analysis",
        "unit",
        "integration",
        "architecture",
        "component",
        "migration",
        "security",
        "journey-linking",
        "navigation-reachability",
        "telemetry",
        "accessibility",
        "keyboard",
        "focus",
        "target-size",
        "responsive-reflow",
        "rendered-regression",
        "design-token-consistency",
        "product-owner",
    ],
    "rendered": {"enabled": True, "testFile": "apps/web/e2e/package-details.spec.ts"},
    "specPath": "docs/slices/VS-37-PACKAGE-DETAILS-BOOKING-AUTH-DESIGN.md",
    "checklistPath": "docs/slices/VS-37-IMPLEMENTATION-CHECKLIST.md",
    "navigationPath": "docs/slices/VS-37-NAVIGATION-VERIFICATION.md",
}
Path("delivery/slices/VS-37.json").write_text(json.dumps(manifest, indent=2) + "\n")

# Update rendered interaction coverage.
test = Path("apps/web/e2e/package-details.spec.ts")
t = test.read_text()
t = t.replace(
    'await expect(page.getByRole("radio", { name: /Milestone/ })).toBeChecked();',
    'await expect(page.getByRole("radio", { name: /Pay Full/ })).toBeChecked();',
)
t = t.replace(
    '''  await expect(\n    page.getByRole("heading", { name: "Milestone payment breakdown" }),\n  ).toBeVisible();\n  await expect(page.getByText("₹88,000").first()).toBeVisible();\n\n  await page.getByRole("radio", { name: /Pay Full/ }).check();''',
    '''  await expect(\n    page.getByRole("heading", { name: "Pay full breakdown" }),\n  ).toBeVisible();\n  await expect(\n    page.locator(".package-payment-breakdown").getByText("₹2,20,000"),\n  ).toBeVisible();\n  await expect(page.getByText("Total Price Before Discount")).toBeVisible();\n  await expect(page.getByText("Total Price After Discount")).toBeVisible();\n  await expect(page.getByText("Service Provider")).toBeVisible();\n  await expect(page.getByText("Powered & Supported by")).toBeVisible();\n  await expect(page.getByText("0% · ₹0")).toBeVisible();\n\n  await page.getByRole("radio", { name: /Milestone/ }).check();\n  await expect(\n    page.getByRole("heading", { name: "Milestone payment breakdown" }),\n  ).toBeVisible();\n  await expect(page.getByText("₹88,000").first()).toBeVisible();\n\n  await page.getByRole("radio", { name: /Pay Full/ }).check();''',
)
# Old Book-now link assertion becomes a button assertion.
t = re.sub(
    r'''  await expect\(\n    page\.getByRole\("link", \{ name: /Book now/ \}\)\.first\(\),\n  \)\.toHaveAttribute\(.*?\n  \);''',
    '''  await expect(\n    page.getByRole("button", { name: /Book now/ }).first(),\n  ).toBeVisible();''',
    t,
    count=1,
    flags=re.S,
)
t = t.replace(
    'page.getByRole("link", { name: /Book now/ }).first()',
    'page.getByRole("button", { name: /Book now/ }).first()',
)
insert_before = 'test("package detail exposes safe unavailable and retry states"'
new_test = r'''test("date controls stay hash-free and Book now previews OTP then traveller names", async ({
  page,
}) => {
  await page.goto(`/packages/${departureId}`);
  await expect(page).not.toHaveURL(/#/);

  await page.getByRole("button", { name: "Next travel dates" }).click();
  await page.getByRole("button", { name: "Previous travel dates" }).click();
  await expect(page).not.toHaveURL(/#/);

  await page.getByRole("button", { name: /Book now/ }).first().click();
  await expect(page).not.toHaveURL(/#/);
  await expect(
    page.getByRole("heading", { name: "Login with mobile OTP" }),
  ).toBeVisible();
  await expect(page.getByText(/no SMS is sent yet/i)).toBeVisible();

  await page.getByLabel("Mobile number").fill("9876543210");
  await page.getByRole("button", { name: "Send code" }).click();
  await page.getByLabel("6-digit verification code").fill("123456");
  await page.getByRole("button", { name: "Verify & continue" }).click();
  await expect(page.getByRole("heading", { name: "Add travellers" })).toBeVisible();
  await expect(page.getByLabel("Traveller 1")).toBeVisible();
  await page.getByRole("button", { name: "+ Add traveller" }).click();
  await expect(page.getByLabel("Traveller 2")).toBeVisible();
  await expect(page.getByRole("button", { name: "+ Add traveller" })).toHaveCount(0);
  await expect(page).not.toHaveURL(/#/);
  await expectNoA11yViolations(page);
});

'''
if insert_before not in t:
    raise SystemExit("E2E insertion marker not found")
t = t.replace(insert_before, new_test + insert_before, 1)
test.write_text(t)
