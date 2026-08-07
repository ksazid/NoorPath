"use client";

import Link from "next/link";
import { useCallback, useMemo, useState } from "react";
import { useDeferredInitialLoad } from "../../lib/use-deferred-initial-load";
import OperatorWorkspaceShell from "./OperatorWorkspaceShell";

type BookingItem = {
  bookingId: string;
  reference: string;
  accountId: string;
  departureId: string;
  packageName: string;
  origin: string;
  departureDate?: string | null;
  returnDate?: string | null;
  state: string;
  occupancy: string;
  travellerCount: number;
  travellers: Array<{
    travellerId: string;
    fullName: string;
    dateOfBirth: string;
  }>;
  payment: {
    currency: string;
    total: number;
    paid: number;
    outstanding: number;
    status: string;
    nextInstalment?: {
      sequence: number;
      dueDate: string;
      amount: number;
    } | null;
  };
  documents: { status: string; required: number; approved: number };
  visa: { status: string; total: number; approved: number };
  createdAtUtc: string;
  updatedAtUtc: string;
};

type BookingResponse = {
  summary: {
    total: number;
    confirmed: number;
    actionRequired: number;
    travellers: number;
  };
  items: BookingItem[];
};

type State =
  | { kind: "loading" }
  | { kind: "error" }
  | { kind: "ready"; response: BookingResponse };

const dateFormatter = new Intl.DateTimeFormat("en-IN", {
  day: "numeric",
  month: "short",
  year: "numeric",
});
const moneyFormatter = new Intl.NumberFormat("en-IN", {
  maximumFractionDigits: 0,
});

function displayDate(value?: string | null) {
  if (!value) return "To be confirmed";
  return dateFormatter.format(new Date(`${value}T00:00:00Z`));
}

function label(value: string) {
  return value
    .replace(/([A-Z])/g, " $1")
    .replace(/^./, (character) => character.toUpperCase());
}

function statusTone(value: string) {
  if (["confirmed", "paid", "ready", "approved"].includes(value)) return "good";
  if (
    [
      "actionRequired",
      "rejected",
      "paymentFailed",
      "confirmationException",
    ].includes(value)
  ) {
    return "attention";
  }
  if (["cancelled", "paymentCancelled"].includes(value)) return "muted";
  return "progress";
}

function money(currency: string, value: number) {
  return `${currency} ${moneyFormatter.format(value)}`;
}

export default function OperatorBookingManagement() {
  const [state, setState] = useState<State>({ kind: "loading" });
  const [query, setQuery] = useState("");
  const [filter, setFilter] = useState("all");

  const load = useCallback(async () => {
    setState({ kind: "loading" });
    try {
      const response = await fetch("/api/v1/operator/bookings", {
        credentials: "include",
        cache: "no-store",
      });
      if (!response.ok) throw new Error();
      setState({
        kind: "ready",
        response: (await response.json()) as BookingResponse,
      });
    } catch {
      setState({ kind: "error" });
    }
  }, []);

  useDeferredInitialLoad(load);

  const visible = useMemo(() => {
    if (state.kind !== "ready") return [];
    const normalized = query.trim().toLowerCase();
    return state.response.items.filter((item) => {
      const matchesQuery =
        !normalized ||
        item.reference.toLowerCase().includes(normalized) ||
        item.packageName.toLowerCase().includes(normalized) ||
        item.origin.toLowerCase().includes(normalized) ||
        item.travellers.some((traveller) =>
          traveller.fullName.toLowerCase().includes(normalized),
        );
      const needsAction =
        item.payment.status !== "paid" ||
        ["actionRequired", "rejected"].includes(item.visa.status) ||
        item.documents.status === "actionRequired" ||
        ["paymentFailed", "confirmationException"].includes(item.state);
      return (
        matchesQuery &&
        (filter === "all" ||
          (filter === "confirmed" && item.state === "confirmed") ||
          (filter === "action" && needsAction) ||
          (filter === "cancelled" && item.state === "cancelled"))
      );
    });
  }, [filter, query, state]);

  return (
    <OperatorWorkspaceShell
      title="Bookings"
      summary="Manage every booking against your packages from one operational view, including travellers, occupancy, payments, documents and visa progress."
    >
      <section className="operator-booking-workspace" aria-live="polite">
        {state.kind === "loading" ? (
          <div className="operator-booking-state">Loading bookings…</div>
        ) : null}

        {state.kind === "error" ? (
          <div className="operator-booking-state">
            <strong>Bookings are temporarily unavailable.</strong>
            <button className="auth-secondary" type="button" onClick={load}>
              Retry
            </button>
          </div>
        ) : null}

        {state.kind === "ready" ? (
          <>
            <div
              className="operator-booking-metrics"
              aria-label="Booking summary"
            >
              <article>
                <span>Total bookings</span>
                <strong>{state.response.summary.total}</strong>
              </article>
              <article>
                <span>Confirmed</span>
                <strong>{state.response.summary.confirmed}</strong>
              </article>
              <article>
                <span>Needs attention</span>
                <strong>{state.response.summary.actionRequired}</strong>
              </article>
              <article>
                <span>Travellers</span>
                <strong>{state.response.summary.travellers}</strong>
              </article>
            </div>

            <div className="operator-booking-toolbar">
              <label>
                <span>Search bookings</span>
                <input
                  type="search"
                  value={query}
                  onChange={(event) => setQuery(event.target.value)}
                  placeholder="Reference, package, origin or traveller"
                />
              </label>
              <div
                className="operator-booking-filters"
                aria-label="Filter bookings"
              >
                {[
                  ["all", "All"],
                  ["confirmed", "Confirmed"],
                  ["action", "Needs attention"],
                  ["cancelled", "Cancelled"],
                ].map(([value, text]) => (
                  <button
                    key={value}
                    type="button"
                    className={filter === value ? "active" : undefined}
                    onClick={() => setFilter(value)}
                    aria-pressed={filter === value}
                  >
                    {text}
                  </button>
                ))}
              </div>
            </div>

            {state.response.items.length === 0 ? (
              <div className="operator-booking-empty">
                <h2>No bookings yet</h2>
                <p>
                  Customer bookings for your published departures will appear
                  here.
                </p>
                <Link className="auth-secondary" href="/operator/packages">
                  View packages
                </Link>
              </div>
            ) : visible.length === 0 ? (
              <div className="operator-booking-empty">
                <h2>No matching bookings</h2>
                <p>Try a different search or lifecycle filter.</p>
              </div>
            ) : (
              <div className="operator-booking-list">
                {visible.map((item) => (
                  <article
                    className="operator-booking-card"
                    key={item.bookingId}
                  >
                    <div className="operator-booking-card-head">
                      <div>
                        <p className="auth-eyebrow">Booking {item.reference}</p>
                        <h2>{item.packageName}</h2>
                        <p>
                          {item.origin} · {displayDate(item.departureDate)} —{" "}
                          {displayDate(item.returnDate)}
                        </p>
                      </div>
                      <span
                        className={`operator-booking-badge ${statusTone(item.state)}`}
                      >
                        {label(item.state)}
                      </span>
                    </div>

                    <div className="operator-booking-core-facts">
                      <div>
                        <span>Occupancy</span>
                        <strong>{label(item.occupancy)} sharing</strong>
                      </div>
                      <div>
                        <span>Travellers</span>
                        <strong>{item.travellerCount}</strong>
                      </div>
                      <div>
                        <span>Total value</span>
                        <strong>
                          {money(item.payment.currency, item.payment.total)}
                        </strong>
                      </div>
                    </div>

                    <div className="operator-booking-progress-grid">
                      <section>
                        <div className="operator-booking-progress-title">
                          <span>Payment</span>
                          <strong className={statusTone(item.payment.status)}>
                            {label(item.payment.status)}
                          </strong>
                        </div>
                        <p>
                          {money(item.payment.currency, item.payment.paid)} paid
                          ·{" "}
                          {money(
                            item.payment.currency,
                            item.payment.outstanding,
                          )}{" "}
                          remaining
                        </p>
                        {item.payment.nextInstalment ? (
                          <small>
                            Next instalment{" "}
                            {item.payment.nextInstalment.sequence}:{" "}
                            {money(
                              item.payment.currency,
                              item.payment.nextInstalment.amount,
                            )}{" "}
                            due{" "}
                            {displayDate(item.payment.nextInstalment.dueDate)}
                          </small>
                        ) : (
                          <small>No outstanding scheduled instalment.</small>
                        )}
                      </section>

                      <section>
                        <div className="operator-booking-progress-title">
                          <span>Documents</span>
                          <strong className={statusTone(item.documents.status)}>
                            {label(item.documents.status)}
                          </strong>
                        </div>
                        <p>
                          {item.documents.approved} of {item.documents.required}{" "}
                          requirements approved
                        </p>
                        <Link href="/operator/documents">
                          Open document review
                        </Link>
                      </section>

                      <section>
                        <div className="operator-booking-progress-title">
                          <span>Visa</span>
                          <strong className={statusTone(item.visa.status)}>
                            {label(item.visa.status)}
                          </strong>
                        </div>
                        <p>
                          {item.visa.approved} of {item.visa.total} travellers
                          approved
                        </p>
                        <Link href="/operator/visa">Open visa processing</Link>
                      </section>
                    </div>

                    <div className="operator-booking-travellers">
                      <span>Travellers</span>
                      <div>
                        {item.travellers.map((traveller) => (
                          <span key={traveller.travellerId}>
                            {traveller.fullName}
                          </span>
                        ))}
                      </div>
                    </div>

                    <div className="operator-booking-actions">
                      <Link
                        className="auth-primary"
                        href={`/operator/bookings/${item.bookingId}`}
                      >
                        Open booking
                      </Link>
                      <Link
                        className="auth-secondary"
                        href={`/operator/departures/${item.departureId}`}
                      >
                        Open departure
                      </Link>
                      <Link className="auth-secondary" href="/operator/support">
                        Support actions
                      </Link>
                      {item.state === "confirmed" ? (
                        <Link
                          className="auth-secondary"
                          href="/operator/cancellations"
                        >
                          Cancellation requests
                        </Link>
                      ) : null}
                    </div>
                  </article>
                ))}
              </div>
            )}
          </>
        ) : null}
      </section>
    </OperatorWorkspaceShell>
  );
}
