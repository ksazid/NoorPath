"use client";

import Link from "next/link";
import { FormEvent, useCallback, useEffect, useId, useState } from "react";
import { useParams } from "next/navigation";
import { Icon, PublicFooter, PublicHeader } from "../../../public-ui";

type Occupancy = "double" | "triple" | "quad";
type PaymentMode = "pay-full" | "milestone" | "pay-later";
type HoldStatus = "active" | "released" | "expired";

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

type InventoryHold = {
  holdId: string;
  quoteId: string;
  departureId: string;
  occupancy: Occupancy;
  quantity: number;
  status: HoldStatus;
  createdAtUtc: string;
  expiresAtUtc: string;
  terminalAtUtc?: string | null;
  availabilityReserved: boolean;
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  code?: string;
  holdId?: string;
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

type HoldState =
  | { kind: "idle" }
  | { kind: "securing" }
  | { kind: "active"; hold: InventoryHold }
  | { kind: "releasing"; hold: InventoryHold }
  | { kind: "released"; hold: InventoryHold }
  | { kind: "expired"; hold: InventoryHold }
  | { kind: "quote-expired"; message: string }
  | { kind: "unavailable"; message: string }
  | { kind: "unauthenticated" }
  | {
      kind: "error";
      message: string;
      uncertain: boolean;
      hold?: InventoryHold;
    };

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

function isOccupancy(value: string | null): value is Occupancy {
  return value === "double" || value === "triple" || value === "quad";
}

function isPaymentMode(value: string | null): value is PaymentMode {
  return value === "pay-full" || value === "milestone" || value === "pay-later";
}

function requestHeaders(
  json = false,
  additional: Record<string, string> = {},
): HeadersInit {
  const headers: Record<string, string> = { ...additional };
  if (json) headers["Content-Type"] = "application/json";
  const testIdentity = process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY;
  if (testIdentity) headers["X-NoorPath-Test-Identity"] = testIdentity;
  return headers;
}

function idempotencyStorageKey(quoteId: string) {
  return `noorpath:inventory-hold:key:${quoteId}`;
}

function holdStorageKey(quoteId: string) {
  return `noorpath:inventory-hold:id:${quoteId}`;
}

function getOrCreateIdempotencyKey(quoteId: string) {
  const storageKey = idempotencyStorageKey(quoteId);
  const existing = window.sessionStorage.getItem(storageKey);
  if (existing) return existing;
  const created = `hold-${window.crypto.randomUUID()}`;
  window.sessionStorage.setItem(storageKey, created);
  return created;
}

function rememberHold(hold: InventoryHold) {
  window.sessionStorage.setItem(holdStorageKey(hold.quoteId), hold.holdId);
}

function clearHoldAttempt(quoteId: string) {
  window.sessionStorage.removeItem(idempotencyStorageKey(quoteId));
  window.sessionStorage.removeItem(holdStorageKey(quoteId));
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
  const [paymentMode, setPaymentMode] = useState<PaymentMode>("pay-full");
  const [selectedTravellerIds, setSelectedTravellerIds] = useState<string[]>(
    [],
  );
  const [quoteState, setQuoteState] = useState<QuoteState>({ kind: "idle" });
  const [holdState, setHoldState] = useState<HoldState>({ kind: "idle" });
  const [fullName, setFullName] = useState("");
  const [dateOfBirth, setDateOfBirth] = useState("");
  const [travellerErrors, setTravellerErrors] = useState<
    Record<string, string>
  >({});
  const [savingTraveller, setSavingTraveller] = useState(false);
  const [showTravellerForm, setShowTravellerForm] = useState(false);

  const applyHold = useCallback((hold: InventoryHold) => {
    if (hold.status === "active") {
      rememberHold(hold);
      setHoldState({ kind: "active", hold });
      return;
    }

    clearHoldAttempt(hold.quoteId);
    setHoldState(
      hold.status === "released"
        ? { kind: "released", hold }
        : { kind: "expired", hold },
    );
  }, []);

  const loadHold = useCallback(
    async (holdId: string) => {
      try {
        const response = await fetch(
          `/api/v1/inventory-holds/${encodeURIComponent(holdId)}`,
          {
            cache: "no-store",
            credentials: "include",
            headers: requestHeaders(),
          },
        );
        if (response.status === 401) {
          setHoldState({ kind: "unauthenticated" });
          return;
        }
        if (response.status === 404) {
          setHoldState({
            kind: "error",
            message: "We could not recover this availability hold.",
            uncertain: false,
          });
          return;
        }
        if (!response.ok) {
          setHoldState({
            kind: "error",
            message:
              "We could not confirm the latest hold status. Try again safely.",
            uncertain: true,
          });
          return;
        }
        applyHold((await response.json()) as InventoryHold);
      } catch {
        setHoldState({
          kind: "error",
          message:
            "We could not confirm the latest hold status. Check your connection and try again.",
          uncertain: true,
        });
      }
    },
    [applyHold],
  );

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
      const searchParams = new URLSearchParams(window.location.search);
      const requestedOccupancy = searchParams.get("occupancy");
      const requestedPaymentMode = searchParams.get("paymentMode");
      setPaymentMode(
        isPaymentMode(requestedPaymentMode) ? requestedPaymentMode : "pay-full",
      );
      const requestedAvailable = isOccupancy(requestedOccupancy)
        ? details.pricing.occupancies.find(
            (item) =>
              item.occupancy === requestedOccupancy &&
              item.status === "available",
          )
        : undefined;
      const firstAvailable = details.pricing.occupancies.find(
        (item) => item.status === "available",
      );
      setOccupancy(
        requestedAvailable?.occupancy ?? firstAvailable?.occupancy ?? null,
      );
    } catch {
      setPackageState({ kind: "error" });
    }
  }, [departureId]);

  useEffect(() => {
    const pending = window.setTimeout(() => {
      void loadPackage();
      void loadTravellers();
    }, 0);
    return () => window.clearTimeout(pending);
  }, [loadPackage, loadTravellers]);

  useEffect(() => {
    if (quoteState.kind !== "loaded" || quoteState.quote.expired) return;
    const remaining =
      new Date(quoteState.quote.expiresAtUtc).getTime() - Date.now();
    const timer = window.setTimeout(
      () => {
        setQuoteState((current) =>
          current.kind === "loaded"
            ? { kind: "loaded", quote: { ...current.quote, expired: true } }
            : current,
        );
      },
      Math.max(remaining, 0),
    );
    return () => window.clearTimeout(timer);
  }, [quoteState]);

  useEffect(() => {
    if (holdState.kind !== "active") return;
    const hold = holdState.hold;
    const refresh = () => void loadHold(hold.holdId);
    const remaining = new Date(hold.expiresAtUtc).getTime() - Date.now();
    const expiryTimer = window.setTimeout(refresh, Math.max(remaining, 0) + 50);
    window.addEventListener("focus", refresh);
    window.addEventListener("online", refresh);
    return () => {
      window.clearTimeout(expiryTimer);
      window.removeEventListener("focus", refresh);
      window.removeEventListener("online", refresh);
    };
  }, [holdState, loadHold]);

  const packageDetails =
    packageState.kind === "loaded" ? packageState.details : null;
  const selectedMeta = occupancy ? occupancyMeta[occupancy] : null;
  const selectedPrice = packageDetails?.pricing.occupancies.find(
    (item) => item.occupancy === occupancy,
  );
  const travellers =
    travellerState.kind === "ready" ? travellerState.items : [];
  const selectionLocked =
    holdState.kind === "active" ||
    holdState.kind === "releasing" ||
    holdState.kind === "securing" ||
    (holdState.kind === "error" && holdState.uncertain);
  const canQuote =
    occupancy !== null &&
    selectedMeta !== null &&
    selectedTravellerIds.length === selectedMeta.travellers &&
    quoteState.kind !== "creating" &&
    !selectionLocked;

  const resetQuote = () => {
    if (quoteState.kind === "loaded") {
      clearHoldAttempt(quoteState.quote.quoteId);
    }
    setQuoteState({ kind: "idle" });
    setHoldState({ kind: "idle" });
  };

  const selectOccupancy = (value: Occupancy) => {
    if (selectionLocked) return;
    setOccupancy(value);
    setSelectedTravellerIds([]);
    resetQuote();
  };

  const toggleTraveller = (travellerId: string) => {
    if (!selectedMeta || selectionLocked) return;
    resetQuote();
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
    if (selectionLocked) return;
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
      setShowTravellerForm(false);
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
    if (quoteState.kind === "loaded") {
      clearHoldAttempt(quoteState.quote.quoteId);
    }
    setHoldState({ kind: "idle" });
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
            paymentMode,
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

  const secureAvailability = async () => {
    if (quoteState.kind !== "loaded" || quoteState.quote.expired) return;
    const quote = quoteState.quote;
    setHoldState({ kind: "securing" });
    const idempotencyKey = getOrCreateIdempotencyKey(quote.quoteId);

    try {
      const response = await fetch(
        `/api/v1/quotes/${encodeURIComponent(quote.quoteId)}/holds`,
        {
          method: "POST",
          credentials: "include",
          headers: requestHeaders(false, {
            "Idempotency-Key": idempotencyKey,
          }),
        },
      );
      const body = (await response.json()) as InventoryHold & ProblemDetails;

      if (response.status === 401) {
        setHoldState({ kind: "unauthenticated" });
        return;
      }
      if (response.status === 410) {
        clearHoldAttempt(quote.quoteId);
        setQuoteState({
          kind: "loaded",
          quote: { ...quote, expired: true },
        });
        setHoldState({
          kind: "quote-expired",
          message:
            body.detail ?? "Create a fresh quote before securing availability.",
        });
        return;
      }
      if (
        response.status === 409 &&
        body.code === "active_hold_exists" &&
        body.holdId
      ) {
        await loadHold(body.holdId);
        return;
      }
      if (response.status === 409 || response.status === 404) {
        setHoldState({
          kind: "unavailable",
          message:
            body.detail ??
            "This room-sharing option is no longer available for a hold.",
        });
        return;
      }
      if (!response.ok) {
        setHoldState({
          kind: "error",
          message:
            body.detail ??
            "We could not confirm whether availability was secured. Retry safely with the same request.",
          uncertain: true,
        });
        return;
      }

      applyHold(body);
    } catch {
      setHoldState({
        kind: "error",
        message:
          "We could not confirm whether availability was secured. Check your connection, then retry safely.",
        uncertain: true,
      });
    }
  };

  const releaseHold = async () => {
    const hold =
      holdState.kind === "active" || holdState.kind === "releasing"
        ? holdState.hold
        : holdState.kind === "error"
          ? holdState.hold
          : undefined;
    if (!hold) return;

    setHoldState({ kind: "releasing", hold });
    try {
      const response = await fetch(
        `/api/v1/inventory-holds/${encodeURIComponent(hold.holdId)}/release`,
        {
          method: "POST",
          credentials: "include",
          headers: requestHeaders(),
        },
      );
      if (response.status === 401) {
        setHoldState({ kind: "unauthenticated" });
        return;
      }
      if (!response.ok) {
        const body = (await response.json()) as ProblemDetails;
        setHoldState({
          kind: "error",
          message:
            body.detail ??
            "We could not confirm whether availability was released. Try again safely.",
          uncertain: true,
          hold,
        });
        return;
      }
      applyHold((await response.json()) as InventoryHold);
    } catch {
      setHoldState({
        kind: "error",
        message:
          "We could not confirm whether availability was released. Check your connection and try again.",
        uncertain: true,
        hold,
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
                  Choose the room, add the adults travelling with you, review
                  the authoritative quote, then secure that availability for a
                  short, explicit period.
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
                    copy="Published per-traveller pricing and current effective availability."
                  />
                  <div className="plan-occupancy-grid">
                    {packageDetails.pricing.occupancies.map((item) => {
                      const meta = occupancyMeta[item.occupancy];
                      const disabled = item.status !== "available";
                      return (
                        <label
                          className={`plan-occupancy-option${occupancy === item.occupancy ? " selected" : ""}${disabled || selectionLocked ? " disabled" : ""}`}
                          key={item.occupancy}
                        >
                          <input
                            type="radio"
                            name="occupancy"
                            value={item.occupancy}
                            checked={occupancy === item.occupancy}
                            disabled={disabled || selectionLocked}
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

                      {selectionLocked ? (
                        <p className="plan-selection-lock" role="status">
                          This room and traveller selection is locked while
                          NoorPath confirms or holds availability. Release the
                          active hold before editing it.
                        </p>
                      ) : null}

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
                                className={`traveller-choice${selected ? " selected" : ""}${selectionLocked ? " disabled" : ""}`}
                                key={traveller.travellerId}
                              >
                                <input
                                  type="checkbox"
                                  checked={selected}
                                  disabled={
                                    !selectedMeta ||
                                    limitReached ||
                                    selectionLocked
                                  }
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

                      <button
                        className="traveller-add-toggle"
                        type="button"
                        disabled={selectionLocked}
                        aria-expanded={showTravellerForm}
                        onClick={() => setShowTravellerForm((value) => !value)}
                      >
                        <span aria-hidden="true">+</span> Add traveller
                      </button>

                      {showTravellerForm ? (
                        <form
                          className="traveller-add-form"
                          onSubmit={addTraveller}
                        >
                          <div className="traveller-add-heading">
                            <strong>Add an adult traveller</strong>
                            <small>
                              Start with their name. Date of birth confirms
                              adult eligibility before saving.
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
                              disabled={selectionLocked}
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
                              disabled={selectionLocked}
                              onChange={(event) =>
                                setDateOfBirth(event.target.value)
                              }
                              aria-invalid={Boolean(
                                travellerErrors.dateOfBirth,
                              )}
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
                          <div className="traveller-add-actions">
                            <button
                              type="submit"
                              disabled={savingTraveller || selectionLocked}
                            >
                              {savingTraveller
                                ? "Adding traveller…"
                                : "Save traveller"}
                            </button>
                            <button
                              type="button"
                              className="traveller-add-cancel"
                              onClick={() => setShowTravellerForm(false)}
                            >
                              Cancel
                            </button>
                          </div>
                        </form>
                      ) : null}
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
                    <QuoteSummary
                      quote={quoteState.quote}
                      holdState={holdState}
                      onSecure={secureAvailability}
                      onRelease={releaseHold}
                      onCreateFreshQuote={createQuote}
                    />
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
                        Creating a quote checks effective availability but does
                        not hold it. You can secure the selected room after
                        reviewing the complete quote.
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

function QuoteSummary({
  quote,
  holdState,
  onSecure,
  onRelease,
  onCreateFreshQuote,
}: {
  quote: Quote;
  holdState: HoldState;
  onSecure: () => void;
  onRelease: () => void;
  onCreateFreshQuote: () => void;
}) {
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
          this quote; the full amount is due at the future payment step.
        </p>
      )}

      <HoldJourney
        quote={quote}
        state={holdState}
        onSecure={onSecure}
        onRelease={onRelease}
        onCreateFreshQuote={onCreateFreshQuote}
      />
    </div>
  );
}

function HoldJourney({
  quote,
  state,
  onSecure,
  onRelease,
  onCreateFreshQuote,
}: {
  quote: Quote;
  state: HoldState;
  onSecure: () => void;
  onRelease: () => void;
  onCreateFreshQuote: () => void;
}) {
  if (quote.expired || state.kind === "quote-expired") {
    return (
      <div className="inventory-hold-state expired" role="status">
        <Icon name="shield-check" />
        <div>
          <strong>Availability was not held.</strong>
          <p>
            {state.kind === "quote-expired"
              ? state.message
              : "Prices and availability must be checked again before continuing."}
          </p>
          <button type="button" onClick={onCreateFreshQuote}>
            Create a fresh quote
          </button>
        </div>
      </div>
    );
  }

  if (state.kind === "active" || state.kind === "releasing") {
    return (
      <div
        className="inventory-hold-state active"
        role="status"
        aria-live="polite"
      >
        <Icon name="shield-check" />
        <div>
          <strong>Availability secured</strong>
          <p>
            One {occupancyMeta[state.hold.occupancy].label.toLowerCase()} room
            allocation is held until{" "}
            <time dateTime={state.hold.expiresAtUtc}>
              {formatDateTime(state.hold.expiresAtUtc)}
            </time>
            .
          </p>
          <HoldCountdown expiresAtUtc={state.hold.expiresAtUtc} />
          <p className="inventory-hold-boundary">
            Booking and payment have not started.
          </p>
          <button
            type="button"
            className="inventory-hold-release"
            disabled={state.kind === "releasing"}
            onClick={onRelease}
          >
            {state.kind === "releasing"
              ? "Releasing availability…"
              : "Release and edit plan"}
          </button>
        </div>
      </div>
    );
  }

  if (state.kind === "released" || state.kind === "expired") {
    const expired = state.kind === "expired";
    return (
      <div className={`inventory-hold-state ${state.kind}`} role="status">
        <Icon name="shield-check" />
        <div>
          <strong>
            {expired ? "Availability hold expired" : "Availability released"}
          </strong>
          <p>
            {expired
              ? "The room allocation has returned to current availability."
              : "You can now change the room or traveller selection safely."}
          </p>
          <button type="button" onClick={onSecure}>
            Secure availability again
          </button>
        </div>
      </div>
    );
  }

  if (state.kind === "unavailable") {
    return (
      <div className="inventory-hold-state unavailable" role="alert">
        <Icon name="shield-check" />
        <div>
          <strong>Availability could not be secured</strong>
          <p>{state.message}</p>
          <Link href={`/packages/${quote.departureId}`}>
            Review latest package options
          </Link>
        </div>
      </div>
    );
  }

  if (state.kind === "unauthenticated") {
    return <SignInNotice action="secure availability" />;
  }

  if (state.kind === "error") {
    return (
      <div className="inventory-hold-state error" role="alert">
        <Icon name="shield-check" />
        <div>
          <strong>Hold status needs confirmation</strong>
          <p>{state.message}</p>
          <button type="button" onClick={state.hold ? onRelease : onSecure}>
            {state.hold ? "Try release again" : "Retry safely"}
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="inventory-hold-state idle" role="status">
      <Icon name="shield-check" />
      <div>
        <strong>
          {state.kind === "securing"
            ? "Securing availability…"
            : "Availability is not held yet."}
        </strong>
        <p>
          NoorPath can temporarily hold one selected room allocation until an
          exact server deadline. No booking or payment is created.
        </p>
        <button
          type="button"
          className="plan-primary-action"
          disabled={state.kind === "securing"}
          onClick={onSecure}
        >
          {state.kind === "securing"
            ? "Securing availability…"
            : "Secure availability"}
        </button>
      </div>
    </div>
  );
}

function HoldCountdown({ expiresAtUtc }: { expiresAtUtc: string }) {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, []);

  const remainingSeconds = Math.max(
    0,
    Math.ceil((new Date(expiresAtUtc).getTime() - now) / 1000),
  );
  const minutes = Math.floor(remainingSeconds / 60);
  const seconds = remainingSeconds % 60;

  return (
    <div className="inventory-hold-countdown">
      <span aria-hidden="true">
        {String(minutes).padStart(2, "0")}:{String(seconds).padStart(2, "0")}
      </span>
      <small>Time remaining on this hold</small>
      <span className="plan-visually-hidden">
        Availability is held until {formatDateTime(expiresAtUtc)}.
      </span>
    </div>
  );
}

function SignInNotice({ action = "create your quote" }: { action?: string }) {
  const id = useId();
  const headingId = `${id}-phone-auth-title`;
  const mobileId = `${id}-mobile-number`;
  const otpId = `${id}-otp-code`;
  const [mobile, setMobile] = useState("");
  const [showCodePreview, setShowCodePreview] = useState(false);

  const previewOtp = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setShowCodePreview(true);
  };

  return (
    <section className="plan-phone-auth" aria-labelledby={headingId}>
      <div className="plan-phone-auth-heading">
        <Icon name="user-circle" />
        <span>
          <strong id={headingId}>Login or sign up with phone OTP</strong>
          <small>
            Continue securely to add travellers and {action}. No password is
            required.
          </small>
        </span>
      </div>

      <form className="plan-phone-auth-form" onSubmit={previewOtp}>
        <label htmlFor={mobileId}>Mobile number</label>
        <div className="plan-phone-field">
          <span aria-hidden="true">+91</span>
          <input
            id={mobileId}
            type="tel"
            inputMode="numeric"
            autoComplete="tel-national"
            pattern="[0-9]{10}"
            maxLength={10}
            placeholder="10-digit mobile number"
            value={mobile}
            onChange={(event) =>
              setMobile(event.target.value.replace(/\D/g, ""))
            }
            required
          />
        </div>
        <button type="submit">Send Code</button>
      </form>

      {showCodePreview ? (
        <div className="plan-otp-preview" role="status">
          <label htmlFor={otpId}>Verification code</label>
          <input
            id={otpId}
            inputMode="numeric"
            placeholder="6-digit code"
            disabled
          />
          <button type="button" disabled>
            Verify &amp; continue
          </button>
          <p>
            OTP setup preview only — no code was sent. Delivery activates when
            the SMS provider is configured.
          </p>
        </div>
      ) : (
        <p className="plan-phone-auth-note">
          Phone OTP delivery is being configured. Your package selections stay
          on this page.
        </p>
      )}
    </section>
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
