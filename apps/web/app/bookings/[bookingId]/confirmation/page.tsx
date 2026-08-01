"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Icon, PublicFooter, PublicHeader } from "../../../public-ui";
import "../../../confirmation.css";

type Booking = {
  bookingId: string;
  bookingReference: string;
  departureId: string;
  currency: string;
  total: number;
  dueNow: number;
  remaining: number;
  state:
    | "PaymentSucceeded"
    | "PendingConfirmation"
    | "Confirming"
    | "Confirmed"
    | "ConfirmationException";
  confirmedAtUtc?: string | null;
  confirmationException?: { code: string; message: string } | null;
};

type State =
  | { kind: "loading" }
  | { kind: "ready"; booking: Booking }
  | { kind: "error"; message: string };

export default function ConfirmationPage() {
  const { bookingId } = useParams<{ bookingId: string }>();
  const [state, setState] = useState<State>({ kind: "loading" });
  const load = useCallback(async () => {
    try {
      const headers: HeadersInit = {};
      if (process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY)
        headers["X-NoorPath-Test-Identity"] =
          process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY;
      const response = await fetch(
        `/api/v1/bookings/${encodeURIComponent(bookingId)}`,
        { cache: "no-store", credentials: "include", headers },
      );
      if (!response.ok) throw new Error();
      setState({ kind: "ready", booking: (await response.json()) as Booking });
    } catch {
      setState({
        kind: "error",
        message:
          "We could not load the latest confirmation status. Your payment record is unchanged.",
      });
    }
  }, [bookingId]);

  useEffect(() => {
    const timer = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(timer);
  }, [load]);

  const booking = state.kind === "ready" ? state.booking : null;
  const confirmed = booking?.state === "Confirmed";
  const exception = booking?.state === "ConfirmationException";
  return (
    <div className="confirmation-page">
      <PublicHeader mode="detail" />
      <main id="main-content" className="confirmation-main">
        <nav className="package-breadcrumbs" aria-label="Breadcrumb">
          <Link href="/">Home</Link>
          <span>/</span>
          <span aria-current="page">Confirmation</span>
        </nav>
        {state.kind === "loading" ? (
          <Status
            title="Checking your confirmation"
            message="Payment received. NoorPath is safely committing your place."
            icon="clock"
          />
        ) : null}
        {state.kind === "error" ? (
          <Status
            title="Status temporarily unavailable"
            message={state.message}
            icon="headset"
          >
            <button onClick={() => void load()}>Retry status</button>
          </Status>
        ) : null}
        {booking ? (
          <>
            <Status
              title={
                confirmed
                  ? "Your booking is confirmed"
                  : exception
                    ? "Payment received — action required"
                    : "Payment received — confirmation processing"
              }
              message={
                confirmed
                  ? "Your place is securely committed. Your commercial snapshot and confirmation time are now fixed."
                  : exception
                    ? (booking.confirmationException?.message ??
                      "NoorPath support is reviewing your booking. Do not pay again.")
                    : "No action is needed. NoorPath is converting your held availability into a durable commitment."
              }
              icon={confirmed ? "seal-check" : exception ? "headset" : "clock"}
            >
              <span
                className={`confirmation-state ${exception ? "warning" : ""}`}
              >
                {confirmed
                  ? "Confirmed"
                  : exception
                    ? "Action required"
                    : "Processing"}
              </span>
            </Status>
            <section
              className="confirmation-summary"
              aria-labelledby="confirmation-summary-title"
            >
              <div>
                <p className="public-eyebrow">Booking record</p>
                <h2 id="confirmation-summary-title">
                  {booking.bookingReference}
                </h2>
              </div>
              <dl>
                <div>
                  <dt>Total price</dt>
                  <dd>{money(booking.currency, booking.total)}</dd>
                </div>
                <div>
                  <dt>Paid now</dt>
                  <dd>{money(booking.currency, booking.dueNow)}</dd>
                </div>
                <div>
                  <dt>Remaining balance</dt>
                  <dd>{money(booking.currency, booking.remaining)}</dd>
                </div>
                {booking.confirmedAtUtc ? (
                  <div>
                    <dt>Confirmed</dt>
                    <dd>
                      <time dateTime={booking.confirmedAtUtc}>
                        {new Date(booking.confirmedAtUtc).toLocaleString(
                          "en-IN",
                        )}
                      </time>
                    </dd>
                  </div>
                ) : null}
              </dl>
              {confirmed ? (
                <Link
                  className="confirmation-primary"
                  href={`/bookings/${booking.bookingId}/journey`}
                >
                  Continue to My Journey
                </Link>
              ) : (
                <button
                  className="confirmation-primary"
                  onClick={() => void load()}
                >
                  Refresh status
                </button>
              )}
              {exception ? (
                <p className="confirmation-support">
                  <Icon name="phone" /> Contact NoorPath support with reference{" "}
                  <strong>{booking.bookingReference}</strong>. Recovery is
                  restricted to authorised operators.
                </p>
              ) : null}
            </section>
          </>
        ) : null}
      </main>
      <PublicFooter />
    </div>
  );
}

function Status({
  title,
  message,
  icon,
  children,
}: {
  title: string;
  message: string;
  icon: string;
  children?: React.ReactNode;
}) {
  return (
    <section className="confirmation-hero" role="status" aria-live="polite">
      <span className="confirmation-icon">
        <Icon name={icon} />
      </span>
      <div>
        <p className="public-eyebrow">NoorPath confirmation</p>
        <h1>{title}</h1>
        <p>{message}</p>
        {children}
      </div>
    </section>
  );
}

function money(currency: string, amount: number) {
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(amount);
}
