"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";
import { Icon, PublicFooter, PublicHeader } from "../../../public-ui";

type Occupancy = "double" | "triple" | "quad";

type OccupancyDetail = {
  occupancy: Occupancy;
  amount: number;
  availableQuantity: number;
  status: "available" | "unavailable";
};

type PackageDetails = {
  departureId: string;
  operator: { id: string; displayName: string };
  packageName: string;
  origin: string;
  departureDate: string;
  returnDate: string;
  durationNights: number;
  pricing: {
    currency: string;
    occupancies: OccupancyDetail[];
  };
};

type Traveller = {
  travellerId: string;
  fullName: string;
  dateOfBirth: string;
};

type QuoteInstalment = {
  sequence: number;
  dueDate: string;
  amount: number;
};

type Quote = {
  quoteId: string;
  departureId: string;
  priceVersionId: string;
  occupancy: Occupancy;
  travellerCount: number;
  currency: string;
  unitPrice: number;
  total: number;
  dueNow: number;
  remaining: number;
  instalments: QuoteInstalment[];
  createdAtUtc: string;
  expiresAtUtc: string;
  expired: boolean;
  availabilityReserved: boolean;
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
};

type PackageState =
  | { kind: "loading" }
  | { kind: "loaded"; details: PackageDetails }
  | { kind: "not-found" }
  | { kind: "error" };

type TravellerState =
  | { kind: "loading" }
  | { kind: "ready"; items: Traveller[] }
  | { kind: "unauthenticated" }
  | { kind: "error" };

type QuoteState =
  | { kind: "idle" }
  | { kind: "creating" }
  | { kind: "loaded"; quote: Quote }
  | { kind: "validation"; message: string }
  | { kind: "unavailable"; message: string }
  | { kind: "unauthenticated" }
  | { kind: "error"; message: string };

const occupancyMeta: Record<
  Occupancy,
  { label: string; travellers: number; detail: string }
> = {
  double: {
    label: "Double sharing",
    travellers: 2,
    detail: "Two adult travellers sharing one room",
  },
  triple: {
    label: "Triple sharing",
    travellers: 3,
    detail: "Three adult travellers sharing one room",
  },
  quad: {
    label: "Quad sharing",
    travellers: 4,
    detail: "Four adult travellers sharing one room",
  },
};

function requestHeaders(json = false): HeadersInit {
  const headers: Record<string, string> = {};
  if (json) headers["Content-Type"] = "application/json";
  const testIdentity = process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY;
  if (testIdentity) headers["X-NoorPath-Test-Identity"] = testIdentity;
  return headers;
}

export default function PlanJourneyPage() {
  const params = useParams<{ departureId: string }>();
  const departureId = params.departureId;
  const [packageState, setPackageState] = useState<PackageState>({
    kind: "loading",
  });
  const [travellerState, setTravellerState] = useState<TravellerState>({
    kind: "loading",
  });
  const [occupancy, setOccupancy] = useState<Occupancy | null>(null);
  const [selectedTravellerIds, setSelectedTravellerIds] = useState<string[]>(
    [],
  );
  const [quoteState, setQuoteState] = useState<QuoteState>({ kind: "idle" });
  const [fullName, setFullName] = useState("");
  const [dateOfBirth, setDateOfBirth] = useState("");
  const [travellerErrors, setTravellerErrors] = useState<
    Record<string, string>
  >({});
  const [savingTraveller, setSavingTraveller] = useState(false);

  const loadTravellers = useCallback(async () => {
    setTravellerState({ kind: "loading" });
    try {
      const response = await fetch("/api/v1/travellers", {
        cache: "no-store",
        credentials: "include",
        headers: requestHeaders(),
      });
      if (response.status === 401) {
        setTravellerState({ kind: "unauthenticated" });
        return;
      }
      if (!response.ok) {
        setTravellerState({ kind: "error" });
        return;
      }
      const body = (await response.json()) as { items: Traveller[] };
      setTravellerState({ kind: "ready", items: body.items });
    } catch {
      setTravellerState({ kind: "error" });
    }
  }, []);

  const loadPackage = useCallback(async () => {
    setPackageState({ kind: "loading" });
    try {
      const response = await fetch(
        `/api/v1/departures/${encodeURIComponent(departureId)}`,
        { cache: "no-store", credentials: "same-origin" },
      );
      if (response.status === 404) {
        setPackageState({ kind: "not-found" });
        return;
      }
      if (!response.ok) {
        setPackageState({ kind: "error" });
        return;
      }
      const details = (await response.json()) as PackageDetails;
      setPackageState({ kind: "loaded", details });
      const firstAvailable = details.pricing.occupancies.find(
        (item) => item.status === "available",
      );
      setOccupancy(firstAvailable?.occupancy ?? null);
    } catch {
      setPackageState({ kind: "error" });
    }
  }, [departureId]);

  useEffect(() => {
    void loadPackage();
    void loadTravellers();
  }, [loadPackage, loadTravellers]);

  useEffect(() => {
    if (quoteState.kind !== "loaded" || quoteState.quote.expired) return;
    const remaining =
      new Date(quoteState.quote.expiresAtUtc).getTime() - Date.now();
    if (remaining <= 0) {
      setQuoteState({
        kind: "loaded",
        quote: { ...quoteState.quote, expired: true },
      });
      return;
    }

    const timer = window.setTimeout(() => {
      setQuoteState((current) =>
        current.kind === "loaded"
          ? { kind: "loaded", quote: { ...current.quote, expired: true } }
          : current,
      );
    }, remaining);
    return () => window.clearTimeout(timer);
  }, [quoteState]);

  const packageDetails =
    packageState.kind === "loaded" ? packageState.details : null;
  const selectedMeta = occupancy ? occupancyMeta[occupancy] : null;
  const selectedPrice = packageDetails?.pricing.occupancies.find(
    (item) => item.occupancy === occupancy,
  );
  const travellers =
    travellerState.kind === "ready" ? travellerState.items : [];
  const canQuote =
    occupancy !== null &&
    selectedMeta !== null &&
    selectedTravellerIds.length === selectedMeta.travellers &&
    quoteState.kind !== "creating";

  const selectOccupancy = (value: Occupancy) => {
    setOccupancy(value);
    setSelectedTravellerIds([]);
    setQuoteState({ kind: "idle" });
  };

  const toggleTraveller = (travellerId: string) => {
    if (!selectedMeta) return;
    setQuoteState({ kind: "idle" });
    setSelectedTravellerIds((current) => {
      if (current.includes(travellerId)) {
        return current.filter((id) => id !== travellerId);
      }
      if (current.length >= selectedMeta.travellers) return current;
      return [...current, travellerId];
    });
  };

  const addTraveller = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setTravellerErrors({});
    setSavingTraveller(true);
    try {
      const response = await fetch("/api/v1/travellers", {
        method: "POST",
        credentials: "include",
        headers: requestHeaders(true),
        body: JSON.stringify({ fullName, dateOfBirth }),
      });
      const body = (await response.json()) as Traveller & ProblemDetails;
      if (response.status === 401) {
        setTravellerState({ kind: "unauthenticated" });
        return;
      }
      if (response.status === 422) {
        setTravellerErrors(
          Object.fromEntries(
            Object.entries(body.errors ?? {}).map(([key, values]) => [
              key,
              values[0] ?? "Review this field.",
            ]),
          ),
        );
        return;
      }
      if (!response.ok) {
        setTravellerErrors({
          form: body.detail ?? "Traveller could not be saved.",
        });
        return;
      }

      setTravellerState((current) =>
        current.kind === "ready"
          ? { kind: "ready", items: [...current.items, body] }
          : { kind: "ready", items: [body] },
      );
      setFullName("");
      setDateOfBirth("");
      if (
        selectedMeta &&
        selectedTravellerIds.length < selectedMeta.travellers
      ) {
        setSelectedTravellerIds((current) => [...current, body.travellerId]);
      }
    } catch {
      setTravellerErrors({
        form: "Traveller could not be saved. Try again safely.",
      });
    } finally {
      setSavingTraveller(false);
    }
  };

  const createQuote = async () => {
    if (!occupancy || !canQuote) return;
    setQuoteState({ kind: "creating" });
    try {
      const response = await fetch(
        `/api/v1/departures/${encodeURIComponent(departureId)}/quotes`,
        {
          method: "POST",
          credentials: "include",
          headers: requestHeaders(true),
          body: JSON.stringify({
            occupancy,
            travellerIds: selectedTravellerIds,
          }),
        },
      );
      const body = (await response.json()) as Quote & ProblemDetails;
      if (response.status === 401) {
        setQuoteState({ kind: "unauthenticated" });
        setTravellerState({ kind: "unauthenticated" });
        return;
      }
      if (response.status === 422) {
        const message =
          Object.values(body.errors ?? {}).flat()[0] ??
          body.detail ??
          "Review your plan.";
        setQuoteState({ kind: "validation", message });
        return;
      }
      if (response.status === 409 || response.status === 404) {
        setQuoteState({
          kind: "unavailable",
          message:
            body.detail ??
            "This option is no longer available for a quote. Review the latest package options.",
        });
        return;
      }
      if (!response.ok) {
        setQuoteState({
          kind: "error",
          message: body.detail ?? "We could not create your quote right now.",
        });
        return;
      }
      setQuoteState({ kind: "loaded", quote: body });
    } catch {
      setQuoteState({
        kind: "error",
        message:
          "We could not create your quote right now. Check your connection and try again.",
      });
    }
  };

  return (
    <div className="public-page plan-page">
      <PublicHeader />
      <main className="plan-main" id="main-content">
        {packageState.kind === "loading" ? <PlanLoading /> : null}
        {packageState.kind === "not-found" ? <PlanPackageUnavailable /> : null}
        {packageState.kind === "error" ? (
          <PlanPackageError onRetry={loadPackage} />
        ) : null}
        {packageDetails ? (
          <>
            <nav className="package-breadcrumbs" aria-label="Breadcrumb">
              <Link href="/">Home</Link>
              <span aria-hidden="true">›</span>
              <Link href={`/packages/${departureId}`}>Package</Link>
              <span aria-hidden="true">›</span>
              <span>Plan your journey</span>
            </nav>

            <section className="plan-context" aria-labelledby="plan-title">
              <div>
                <p className="plan-ahead-kicker">Plan before you commit</p>
                <h1 id="plan-title">Build your Umrah plan</h1>
                <p>
                  Choose the room, add the adults travelling with you, then see
                  the complete authoritative quote and payment schedule.
                </p>
              </div>
              <div className="plan-package-facts" aria-label="Selected package">
                <span>{packageDetails.operator.displayName}</span>
                <strong>{packageDetails.packageName}</strong>
                <small>
                  {packageDetails.origin} ·{" "}
                  {formatDate(packageDetails.departureDate)} ·{" "}
                  {packageDetails.durationNights} nights
                </small>
              </div>
            </section>

            <div className="plan-workspace">
              <div className="plan-steps">
                <section
                  className="plan-panel"
                  aria-labelledby="room-step-title"
                >
                  <StepHeading
                    number="01"
                    title="Choose your room sharing"
                    id="room-step-title"
                    copy="Published per-traveller pricing and current availability."
                  />
                  <div className="plan-occupancy-grid">
                    {packageDetails.pricing.occupancies.map((item) => {
                      const meta = occupancyMeta[item.occupancy];
                      const disabled = item.status !== "available";
                      return (
                        <label
                          className={`plan-occupancy-option${occupancy === item.occupancy ? " selected" : ""}${disabled ? " disabled" : ""}`}
                          key={item.occupancy}
                        >
                          <input
                            type="radio"
                            name="occupancy"
                            value={item.occupancy}
                            checked={occupancy === item.occupancy}
                            disabled={disabled}
                            onChange={() => selectOccupancy(item.occupancy)}
                          />
                          <span>
                            <strong>{meta.label}</strong>
                            <small>{meta.detail}</small>
                          </span>
                          <span className="plan-occupancy-price">
                            <strong>
                              {formatMoney(
                                item.amount,
                                packageDetails.pricing.currency,
                              )}
                            </strong>
                            <small>per traveller</small>
                          </span>
                        </label>
                      );
                    })}
                  </div>
                </section>

                <section
                  className="plan-panel"
                  aria-labelledby="traveller-step-title"
                >
                  <StepHeading
                    number="02"
                    title="Who is travelling?"
                    id="traveller-step-title"
                    copy={
                      selectedMeta
                        ? `Select exactly ${selectedMeta.travellers} adult travellers for ${selectedMeta.label.toLowerCase()}.`
                        : "Choose an available room option first."
                    }
                  />

                  {travellerState.kind === "loading" ? (
                    <p className="plan-inline-state" role="status">
                      Loading your travellers…
                    </p>
                  ) : null}
                  {travellerState.kind === "unauthenticated" ? (
                    <SignInNotice />
                  ) : null}
                  {travellerState.kind === "error" ? (
                    <div className="plan-inline-state error" role="alert">
                      <span>We could not load your travellers.</span>
                      <button type="button" onClick={loadTravellers}>
                        Try again
                      </button>
                    </div>
                  ) : null}

                  {travellerState.kind === "ready" ? (
                    <>
                      <div className="plan-selection-count" aria-live="polite">
                        <strong>{selectedTravellerIds.length}</strong>
                        <span>
                          of {selectedMeta?.travellers ?? 0} travellers selected
                        </span>
                      </div>

                      {travellers.length > 0 ? (
                        <div className="traveller-choice-list">
                          {travellers.map((traveller) => {
                            const selected = selectedTravellerIds.includes(
                              traveller.travellerId,
                            );
                            const limitReached =
                              !selected &&
                              selectedMeta !== null &&
                              selectedTravellerIds.length >=
                                selectedMeta.travellers;
                            return (
                              <label
                                className={`traveller-choice${selected ? " selected" : ""}`}
                                key={traveller.travellerId}
                              >
                                <input
                                  type="checkbox"
                                  checked={selected}
                                  disabled={!selectedMeta || limitReached}
                                  onChange={() =>
                                    toggleTraveller(traveller.travellerId)
                                  }
                                />
                                <span
                                  className="traveller-choice-icon"
                                  aria-hidden="true"
                                >
                                  <Icon name="user-circle" />
                                </span>
                                <span>
                                  <strong>{traveller.fullName}</strong>
                                  <small>
                                    Born {formatDate(traveller.dateOfBirth)}
                                  </small>
                                </span>
                              </label>
                            );
                          })}
                        </div>
                      ) : (
                        <p className="plan-empty-copy">
                          Add the adults travelling on this Umrah journey.
                        </p>
                      )}

                      <form
                        className="traveller-add-form"
                        onSubmit={addTraveller}
                      >
                        <div className="traveller-add-heading">
                          <strong>Add an adult traveller</strong>
                          <small>
                            For VS-07, travellers must be 18 or older on
                            departure day.
                          </small>
                        </div>
                        {travellerErrors.form ? (
                          <p className="field-error" role="alert">
                            {travellerErrors.form}
                          </p>
                        ) : null}
                        <label>
                          <span>Full name</span>
                          <input
                            type="text"
                            autoComplete="name"
                            value={fullName}
                            onChange={(event) =>
                              setFullName(event.target.value)
                            }
                            aria-invalid={Boolean(travellerErrors.fullName)}
                            aria-describedby={
                              travellerErrors.fullName
                                ? "traveller-name-error"
                                : undefined
                            }
                          />
                          {travellerErrors.fullName ? (
                            <small
                              className="field-error"
                              id="traveller-name-error"
                            >
                              {travellerErrors.fullName}
                            </small>
                          ) : null}
                        </label>
                        <label>
                          <span>Date of birth</span>
                          <input
                            type="date"
                            value={dateOfBirth}
                            onChange={(event) =>
                              setDateOfBirth(event.target.value)
                            }
                            aria-invalid={Boolean(travellerErrors.dateOfBirth)}
                            aria-describedby={
                              travellerErrors.dateOfBirth
                                ? "traveller-dob-error"
                                : undefined
                            }
                          />
                          {travellerErrors.dateOfBirth ? (
                            <small
                              className="field-error"
                              id="traveller-dob-error"
                            >
                              {travellerErrors.dateOfBirth}
                            </small>
                          ) : null}
                        </label>
                        <button type="submit" disabled={savingTraveller}>
                          {savingTraveller
                            ? "Adding traveller…"
                            : "Add traveller"}
                        </button>
                      </form>
                    </>
                  ) : null}
                </section>
              </div>

              <aside
                className="plan-quote-column"
                aria-labelledby="quote-title"
              >
                <div className="plan-quote-card">
                  <p className="plan-ahead-kicker">Your quote</p>
                  <h2 id="quote-title">Know the commitment before booking.</h2>

                  {quoteState.kind === "loaded" ? (
                    <QuoteSummary quote={quoteState.quote} />
                  ) : (
                    <>
                      <dl className="plan-preview-totals">
                        <div>
                          <dt>Room</dt>
                          <dd>{selectedMeta?.label ?? "Choose an option"}</dd>
                        </div>
                        <div>
                          <dt>Travellers</dt>
                          <dd>
                            {selectedTravellerIds.length} /{" "}
                            {selectedMeta?.travellers ?? 0}
                          </dd>
                        </div>
                        <div>
                          <dt>Published per traveller</dt>
                          <dd>
                            {selectedPrice
                              ? formatMoney(
                                  selectedPrice.amount,
                                  packageDetails.pricing.currency,
                                )
                              : "—"}
                          </dd>
                        </div>
                      </dl>

                      {quoteState.kind === "validation" ||
                      quoteState.kind === "unavailable" ||
                      quoteState.kind === "error" ? (
                        <p className="plan-quote-error" role="alert">
                          {quoteState.message}
                        </p>
                      ) : null}
                      {quoteState.kind === "unauthenticated" ? (
                        <SignInNotice />
                      ) : null}

                      <button
                        className="plan-primary-action"
                        type="button"
                        disabled={!canQuote}
                        onClick={createQuote}
                      >
                        {quoteState.kind === "creating"
                          ? "Creating your quote…"
                          : "See my complete quote"}
                      </button>
                      <p className="plan-quote-disclosure">
                        Creating a quote checks current availability but does
                        not reserve a place. Inventory is secured in the next
                        booking step.
                      </p>
                    </>
                  )}
                </div>
              </aside>
            </div>
          </>
        ) : null}
      </main>
      <PublicFooter />
    </div>
  );
}

function StepHeading({
  number,
  title,
  copy,
  id,
}: {
  number: string;
  title: string;
  copy: string;
  id: string;
}) {
  return (
    <div className="plan-step-heading">
      <span aria-hidden="true">{number}</span>
      <div>
        <h2 id={id}>{title}</h2>
        <p>{copy}</p>
      </div>
    </div>
  );
}

function QuoteSummary({ quote }: { quote: Quote }) {
  return (
    <div className={`authoritative-quote${quote.expired ? " expired" : ""}`}>
      <div className="quote-status-line">
        <span>{quote.expired ? "Quote expired" : "Authoritative quote"}</span>
        <small>
          {quote.expired
            ? "Create a new quote to continue"
            : `Valid until ${formatDateTime(quote.expiresAtUtc)}`}
        </small>
      </div>

      <dl className="quote-financials">
        <div className="quote-total">
          <dt>Total</dt>
          <dd>{formatMoney(quote.total, quote.currency)}</dd>
        </div>
        <div>
          <dt>Due now</dt>
          <dd>{formatMoney(quote.dueNow, quote.currency)}</dd>
        </div>
        <div>
          <dt>Remaining</dt>
          <dd>{formatMoney(quote.remaining, quote.currency)}</dd>
        </div>
      </dl>

      {quote.instalments.length > 0 ? (
        <div className="quote-schedule">
          <h3>Payment schedule</h3>
          <ol>
            {quote.instalments.map((item) => (
              <li key={item.sequence}>
                <span className="quote-schedule-marker" aria-hidden="true" />
                <span>
                  <strong>Instalment {item.sequence}</strong>
                  <small>{formatDate(item.dueDate)}</small>
                </span>
                <strong>{formatMoney(item.amount, quote.currency)}</strong>
              </li>
            ))}
          </ol>
        </div>
      ) : (
        <p className="quote-full-payment-note">
          This published package does not currently have future instalments for
          this quote; the full amount is due at the next payment step.
        </p>
      )}

      <div className="quote-next-step">
        <Icon name="shield-check" />
        <span>
          <strong>No place is reserved yet.</strong>
          <small>
            Next, NoorPath will secure availability before any payment
            commitment.
          </small>
        </span>
      </div>

      {quote.expired ? (
        <p className="plan-quote-disclosure">
          Prices and availability must be checked again before proceeding.
        </p>
      ) : null}
    </div>
  );
}

function SignInNotice() {
  const signInUrl = process.env.NEXT_PUBLIC_NOORPATH_SIGN_IN_URL;
  return (
    <div className="plan-sign-in" role="status">
      <Icon name="user-circle" />
      <span>
        <strong>Sign in to add travellers and create your quote.</strong>
        <small>
          Package browsing stays public. Personal traveller details are
          protected behind your NoorPath account.
        </small>
      </span>
      {signInUrl ? <a href={signInUrl}>Sign in</a> : null}
    </div>
  );
}

function PlanLoading() {
  return (
    <section className="plan-page-state" aria-live="polite" aria-busy="true">
      <p className="plan-ahead-kicker">Preparing your plan</p>
      <h1>Loading this Umrah journey…</h1>
    </section>
  );
}

function PlanPackageUnavailable() {
  return (
    <section className="plan-page-state" role="status">
      <p className="plan-ahead-kicker">Journey unavailable</p>
      <h1>This package can no longer be planned.</h1>
      <p>Return to published journeys and choose an available departure.</p>
      <Link href="/#packages">Browse packages</Link>
    </section>
  );
}

function PlanPackageError({ onRetry }: { onRetry: () => void }) {
  return (
    <section className="plan-page-state" role="alert">
      <p className="plan-ahead-kicker">Journey unavailable</p>
      <h1>We could not load this journey right now.</h1>
      <button type="button" onClick={onRetry}>
        Try again
      </button>
    </section>
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

function formatDateTime(value: string) {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat("en-IN", {
    day: "2-digit",
    month: "short",
    hour: "numeric",
    minute: "2-digit",
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
