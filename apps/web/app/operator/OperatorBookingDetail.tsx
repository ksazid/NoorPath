"use client";

import Link from "next/link";
import { useCallback, useState } from "react";
import { useDeferredInitialLoad } from "../../lib/use-deferred-initial-load";
import OperatorWorkspaceShell from "./OperatorWorkspaceShell";

type BookingDetail = {
  bookingId: string;
  reference: string;
  departureId: string;
  packageName: string;
  origin: string;
  departureDate?: string | null;
  returnDate?: string | null;
  state: string;
  occupancy: string;
  travellerCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  payment: {
    currency: string;
    total: number;
    paid: number;
    outstanding: number;
    status: string;
    instalments: Array<{
      sequence: number;
      dueDate: string;
      amount: number;
      status: string;
    }>;
  };
  documents: { required: number; approved: number };
  visa: { total: number; approved: number };
  travellers: Array<{
    travellerId: string;
    fullName: string;
    dateOfBirth: string;
    documents: Array<{
      requirementId: string;
      kind: string;
      status: string;
    }>;
    visa: { status: string; customerAction?: string | null };
  }>;
};

type State =
  | { kind: "loading" }
  | { kind: "ready"; detail: BookingDetail }
  | { kind: "forbidden" }
  | { kind: "not-found" }
  | { kind: "error" };

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

function displayDateTime(value: string) {
  return dateFormatter.format(new Date(value));
}

function label(value: string) {
  return value
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replace(/_/g, " ")
    .replace(/^./, (character) => character.toUpperCase());
}

function money(currency: string, value: number) {
  return `${currency} ${moneyFormatter.format(value)}`;
}

function tone(value: string) {
  const normalized = value.toLowerCase();
  if (["confirmed", "paid", "approved", "ready"].includes(normalized)) {
    return "good";
  }
  if (
    normalized.includes("rejected") ||
    normalized.includes("failed") ||
    normalized.includes("actionrequired") ||
    normalized.includes("exception")
  ) {
    return "attention";
  }
  if (normalized.includes("cancelled")) return "muted";
  return "progress";
}

export default function OperatorBookingDetail({
  bookingId,
}: {
  bookingId: string;
}) {
  const [state, setState] = useState<State>({ kind: "loading" });

  const load = useCallback(async () => {
    setState({ kind: "loading" });
    try {
      const response = await fetch(`/api/v1/operator/bookings/${bookingId}`, {
        credentials: "include",
        cache: "no-store",
      });
      if (response.status === 403) {
        setState({ kind: "forbidden" });
        return;
      }
      if (response.status === 404) {
        setState({ kind: "not-found" });
        return;
      }
      if (!response.ok) throw new Error();
      setState({
        kind: "ready",
        detail: (await response.json()) as BookingDetail,
      });
    } catch {
      setState({ kind: "error" });
    }
  }, [bookingId]);

  useDeferredInitialLoad(load);

  return (
    <OperatorWorkspaceShell
      title="Booking detail"
      summary="Service one booking from a single operational view while keeping payments, documents, visa, support and cancellation actions in their governed workspaces."
    >
      <section className="operator-booking-detail" aria-live="polite">
        {state.kind === "loading" ? (
          <div className="operator-booking-state">Loading booking…</div>
        ) : null}

        {state.kind === "forbidden" ? (
          <div className="operator-booking-state">
            <strong>You do not have access to this operator booking.</strong>
            <Link className="auth-secondary" href="/operator/bookings">
              Back to bookings
            </Link>
          </div>
        ) : null}

        {state.kind === "not-found" ? (
          <div className="operator-booking-state">
            <strong>Booking not found.</strong>
            <p>
              This booking is unavailable or does not belong to your operator
              account.
            </p>
            <Link className="auth-secondary" href="/operator/bookings">
              Back to bookings
            </Link>
          </div>
        ) : null}

        {state.kind === "error" ? (
          <div className="operator-booking-state">
            <strong>Booking details are temporarily unavailable.</strong>
            <button className="auth-secondary" type="button" onClick={load}>
              Retry
            </button>
          </div>
        ) : null}

        {state.kind === "ready" ? <Detail detail={state.detail} /> : null}
      </section>
    </OperatorWorkspaceShell>
  );
}

function Detail({ detail }: { detail: BookingDetail }) {
  return (
    <>
      <div className="operator-booking-detail__back">
        <Link href="/operator/bookings">← Back to bookings</Link>
      </div>

      <article className="operator-booking-detail__hero">
        <div>
          <p className="auth-eyebrow">Booking {detail.reference}</p>
          <h2>{detail.packageName}</h2>
          <p>
            {detail.origin} · {displayDate(detail.departureDate)} —{" "}
            {displayDate(detail.returnDate)}
          </p>
        </div>
        <span className={`operator-booking-badge ${tone(detail.state)}`}>
          {label(detail.state)}
        </span>
      </article>

      <div
        className="operator-booking-detail__facts"
        aria-label="Booking facts"
      >
        <article>
          <span>Occupancy</span>
          <strong>{label(detail.occupancy)} sharing</strong>
        </article>
        <article>
          <span>Travellers</span>
          <strong>{detail.travellerCount}</strong>
        </article>
        <article>
          <span>Booked</span>
          <strong>{displayDateTime(detail.createdAtUtc)}</strong>
        </article>
        <article>
          <span>Last updated</span>
          <strong>{displayDateTime(detail.updatedAtUtc)}</strong>
        </article>
      </div>

      <section
        className="operator-booking-detail__section"
        aria-labelledby="payment-heading"
      >
        <div className="operator-booking-detail__section-head">
          <div>
            <p className="auth-eyebrow">Financial timeline</p>
            <h2 id="payment-heading">Payments & instalments</h2>
          </div>
          <span
            className={`operator-booking-badge ${tone(detail.payment.status)}`}
          >
            {label(detail.payment.status)}
          </span>
        </div>
        <div className="operator-booking-detail__money">
          <article>
            <span>Total</span>
            <strong>
              {money(detail.payment.currency, detail.payment.total)}
            </strong>
          </article>
          <article>
            <span>Paid</span>
            <strong>
              {money(detail.payment.currency, detail.payment.paid)}
            </strong>
          </article>
          <article>
            <span>Outstanding</span>
            <strong>
              {money(detail.payment.currency, detail.payment.outstanding)}
            </strong>
          </article>
        </div>
        {detail.payment.instalments.length === 0 ? (
          <p className="operator-booking-detail__muted">
            No scheduled instalments are recorded for this booking.
          </p>
        ) : (
          <ol className="operator-booking-detail__timeline">
            {detail.payment.instalments.map((instalment) => (
              <li key={instalment.sequence}>
                <div>
                  <strong>Instalment {instalment.sequence}</strong>
                  <span>Due {displayDate(instalment.dueDate)}</span>
                </div>
                <div>
                  <strong>
                    {money(detail.payment.currency, instalment.amount)}
                  </strong>
                  <span className={tone(instalment.status)}>
                    {label(instalment.status)}
                  </span>
                </div>
              </li>
            ))}
          </ol>
        )}
      </section>

      <section
        className="operator-booking-detail__section"
        aria-labelledby="travellers-heading"
      >
        <div className="operator-booking-detail__section-head">
          <div>
            <p className="auth-eyebrow">Traveller operations</p>
            <h2 id="travellers-heading">Travellers</h2>
          </div>
          <p className="operator-booking-detail__muted">
            Documents {detail.documents.approved}/{detail.documents.required}{" "}
            approved · Visa {detail.visa.approved}/{detail.visa.total} approved
          </p>
        </div>
        <div className="operator-booking-detail__travellers">
          {detail.travellers.map((traveller) => (
            <article key={traveller.travellerId}>
              <header>
                <div>
                  <h3>{traveller.fullName}</h3>
                  <p>Date of birth {displayDate(traveller.dateOfBirth)}</p>
                </div>
                <span
                  className={`operator-booking-badge ${tone(traveller.visa.status)}`}
                >
                  Visa: {label(traveller.visa.status)}
                </span>
              </header>
              <div className="operator-booking-detail__documents">
                <span>Documents</span>
                {traveller.documents.length === 0 ? (
                  <p>No document requirements created yet.</p>
                ) : (
                  <ul>
                    {traveller.documents.map((document) => (
                      <li key={document.requirementId}>
                        <span>{label(document.kind)}</span>
                        <strong className={tone(document.status)}>
                          {label(document.status)}
                        </strong>
                      </li>
                    ))}
                  </ul>
                )}
              </div>
              {traveller.visa.customerAction ? (
                <p className="operator-booking-detail__action-note">
                  Customer action: {traveller.visa.customerAction}
                </p>
              ) : null}
            </article>
          ))}
        </div>
      </section>

      <section
        className="operator-booking-detail__section"
        aria-labelledby="actions-heading"
      >
        <div className="operator-booking-detail__section-head">
          <div>
            <p className="auth-eyebrow">Governed actions</p>
            <h2 id="actions-heading">Continue in the owning workspace</h2>
          </div>
        </div>
        <div className="operator-booking-detail__actions">
          {detail.state === "confirmed" ? (
            <Link
              className="auth-primary"
              href={`/operator/bookings/${detail.bookingId}/amend`}
            >
              Amend booking
            </Link>
          ) : null}
          <Link
            className="auth-secondary"
            href={`/operator/departures/${detail.departureId}`}
          >
            Open departure
          </Link>
          <Link className="auth-secondary" href="/operator/documents">
            Document review
          </Link>
          <Link className="auth-secondary" href="/operator/visa">
            Visa processing
          </Link>
          <Link className="auth-secondary" href="/operator/support">
            Support actions
          </Link>
          {detail.state === "confirmed" ? (
            <Link className="auth-secondary" href="/operator/cancellations">
              Cancellation requests
            </Link>
          ) : null}
        </div>
      </section>
    </>
  );
}
