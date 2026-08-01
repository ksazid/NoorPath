"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Icon, PublicFooter, PublicHeader } from "../../../public-ui";

type BookingTraveller = {
  travellerId: string;
  position: number;
  fullName: string;
  dateOfBirth: string;
};

type BookingInstalment = {
  sequence: number;
  dueDate: string;
  amount: number;
};

type Booking = {
  bookingId: string;
  bookingReference: string;
  departureId: string;
  quoteId: string;
  priceVersionId: string;
  inventoryHoldId: string;
  occupancy: "double" | "triple" | "quad";
  travellerCount: number;
  currency: string;
  unitPrice: number;
  total: number;
  dueNow: number;
  remaining: number;
  state:
    | "PendingPayment"
    | "PaymentInProgress"
    | "PaymentSucceeded"
    | "PaymentFailed"
    | "PaymentCancelled";
  travellers: BookingTraveller[];
  instalments: BookingInstalment[];
  createdAtUtc: string;
  updatedAtUtc: string;
};

type Checkout = {
  provider: string;
  providerSessionId: string;
  publicKeyId: string;
  amountSubunits: number;
  currency: string;
  checkoutScriptUri: string;
  expiresAtUtc: string;
};

type Payment = {
  paymentAttemptId: string;
  bookingId: string;
  currency: string;
  amount: number;
  state:
    | "Created"
    | "ProviderPending"
    | "RequiresAction"
    | "Succeeded"
    | "Failed"
    | "Cancelled";
  provider: string;
  providerPaymentId?: string | null;
  failureCode?: string | null;
  checkout?: Checkout | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  settledAtUtc?: string | null;
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  code?: string;
  paymentAttemptId?: string;
};

type PageState =
  | { kind: "loading" }
  | { kind: "ready"; booking: Booking }
  | { kind: "not-found" }
  | { kind: "unauthenticated" }
  | { kind: "error"; message: string };

type PaymentState =
  | { kind: "idle" }
  | { kind: "starting" }
  | { kind: "ready"; payment: Payment }
  | { kind: "opening"; payment: Payment }
  | { kind: "waiting"; payment: Payment; message: string }
  | { kind: "error"; message: string; payment?: Payment };

type RazorpayResult = {
  razorpay_order_id: string;
  razorpay_payment_id: string;
  razorpay_signature: string;
};

type RazorpayInstance = {
  open: () => void;
  on: (event: "payment.failed", handler: () => void) => void;
};

type RazorpayConstructor = new (options: {
  key: string;
  amount: number;
  currency: string;
  name: string;
  description: string;
  order_id: string;
  handler: (response: RazorpayResult) => void | Promise<void>;
  modal: { ondismiss: () => void };
  theme: { color: string };
}) => RazorpayInstance;

declare global {
  interface Window {
    Razorpay?: RazorpayConstructor;
  }
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

function paymentStorageKey(bookingId: string) {
  return `noorpath:payment:key:${bookingId}`;
}

function getOrCreatePaymentKey(bookingId: string) {
  const storageKey = paymentStorageKey(bookingId);
  const existing = window.sessionStorage.getItem(storageKey);
  if (existing) return existing;
  const created = `payment-${window.crypto.randomUUID()}`;
  window.sessionStorage.setItem(storageKey, created);
  return created;
}

function clearPaymentKey(bookingId: string) {
  window.sessionStorage.removeItem(paymentStorageKey(bookingId));
}

function money(currency: string, amount: number) {
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(amount);
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat("en-IN", {
    day: "numeric",
    month: "short",
    year: "numeric",
  }).format(new Date(`${value}T00:00:00`));
}

function occupancyLabel(value: Booking["occupancy"]) {
  return `${value[0].toUpperCase()}${value.slice(1)} sharing`;
}

function loadCheckoutScript(uri: string) {
  if (window.Razorpay) return Promise.resolve();
  const existing = document.querySelector<HTMLScriptElement>(
    `script[data-noorpath-checkout="${CSS.escape(uri)}"]`,
  );
  if (existing) {
    return new Promise<void>((resolve, reject) => {
      existing.addEventListener("load", () => resolve(), { once: true });
      existing.addEventListener(
        "error",
        () => reject(new Error("Checkout could not be loaded.")),
        { once: true },
      );
    });
  }

  return new Promise<void>((resolve, reject) => {
    const script = document.createElement("script");
    script.src = uri;
    script.async = true;
    script.dataset.noorpathCheckout = uri;
    script.addEventListener("load", () => resolve(), { once: true });
    script.addEventListener(
      "error",
      () => reject(new Error("Checkout could not be loaded.")),
      { once: true },
    );
    document.head.appendChild(script);
  });
}

export default function BookingPaymentPage() {
  const params = useParams<{ bookingId: string }>();
  const bookingId = params.bookingId;
  const [pageState, setPageState] = useState<PageState>({ kind: "loading" });
  const [paymentState, setPaymentState] = useState<PaymentState>({ kind: "idle" });
  const pollTimer = useRef<number | null>(null);

  const stopPolling = useCallback(() => {
    if (pollTimer.current !== null) {
      window.clearTimeout(pollTimer.current);
      pollTimer.current = null;
    }
  }, []);

  const loadBooking = useCallback(async () => {
    setPageState({ kind: "loading" });
    try {
      const response = await fetch(
        `/api/v1/bookings/${encodeURIComponent(bookingId)}`,
        {
          cache: "no-store",
          credentials: "include",
          headers: requestHeaders(),
        },
      );
      if (response.status === 401) {
        setPageState({ kind: "unauthenticated" });
        return;
      }
      if (response.status === 404) {
        setPageState({ kind: "not-found" });
        return;
      }
      if (!response.ok) {
        setPageState({
          kind: "error",
          message: "We could not load this booking safely.",
        });
        return;
      }
      setPageState({ kind: "ready", booking: (await response.json()) as Booking });
    } catch {
      setPageState({
        kind: "error",
        message: "We could not load this booking. Check your connection and retry.",
      });
    }
  }, [bookingId]);

  const loadPayment = useCallback(
    async (paymentAttemptId: string, polling = false): Promise<Payment | null> => {
      try {
        const response = await fetch(
          `/api/v1/payments/${encodeURIComponent(paymentAttemptId)}`,
          {
            cache: "no-store",
            credentials: "include",
            headers: requestHeaders(),
          },
        );
        if (response.status === 401) {
          setPageState({ kind: "unauthenticated" });
          return null;
        }
        if (!response.ok) {
          if (!polling) {
            setPaymentState({
              kind: "error",
              message: "We could not recover the latest payment state.",
            });
          }
          return null;
        }
        const payment = (await response.json()) as Payment;
        if (payment.state === "Succeeded") {
          stopPolling();
          clearPaymentKey(bookingId);
          setPaymentState({ kind: "ready", payment });
          await loadBooking();
        } else if (polling) {
          setPaymentState({
            kind: "waiting",
            payment,
            message:
              "Payment identity is verified. NoorPath is waiting for the authenticated provider settlement event.",
          });
          pollTimer.current = window.setTimeout(
            () => void loadPayment(payment.paymentAttemptId, true),
            2500,
          );
        } else {
          setPaymentState({ kind: "ready", payment });
        }
        return payment;
      } catch {
        if (!polling) {
          setPaymentState({
            kind: "error",
            message: "We could not recover the payment state. Retry safely.",
          });
        }
        return null;
      }
    },
    [bookingId, loadBooking, stopPolling],
  );

  useEffect(() => {
    const timer = window.setTimeout(() => void loadBooking(), 0);
    return () => {
      window.clearTimeout(timer);
      stopPolling();
    };
  }, [loadBooking, stopPolling]);

  const booking = pageState.kind === "ready" ? pageState.booking : null;
  const payment =
    paymentState.kind === "ready" ||
    paymentState.kind === "opening" ||
    paymentState.kind === "waiting"
      ? paymentState.payment
      : paymentState.kind === "error"
        ? paymentState.payment
        : undefined;

  const dueLabel = useMemo(
    () => (booking ? money(booking.currency, booking.dueNow) : ""),
    [booking],
  );

  const startPayment = async (fresh = false) => {
    if (!booking) return;
    stopPolling();
    if (fresh) clearPaymentKey(booking.bookingId);
    setPaymentState({ kind: "starting" });
    try {
      const response = await fetch(
        `/api/v1/bookings/${encodeURIComponent(booking.bookingId)}/payments`,
        {
          method: "POST",
          credentials: "include",
          headers: requestHeaders(false, {
            "Idempotency-Key": getOrCreatePaymentKey(booking.bookingId),
          }),
        },
      );
      const body = (await response.json()) as Payment & ProblemDetails;
      if (response.status === 401) {
        setPageState({ kind: "unauthenticated" });
        return;
      }
      if (
        response.status === 409 &&
        body.code === "payment_attempt_exists" &&
        body.paymentAttemptId
      ) {
        await loadPayment(body.paymentAttemptId);
        return;
      }
      if (!response.ok) {
        setPaymentState({
          kind: "error",
          message:
            body.detail ??
            "Payment could not be started. No payment was confirmed.",
        });
        return;
      }
      setPaymentState({ kind: "ready", payment: body });
    } catch {
      setPaymentState({
        kind: "error",
        message:
          "Payment could not be started. Check your connection and retry safely.",
      });
    }
  };

  const openCheckout = async () => {
    if (!booking || !payment?.checkout) return;
    const currentPayment = payment;
    setPaymentState({ kind: "opening", payment: currentPayment });
    try {
      await loadCheckoutScript(currentPayment.checkout.checkoutScriptUri);
      if (!window.Razorpay) throw new Error("Checkout is unavailable.");

      const checkout = new window.Razorpay({
        key: currentPayment.checkout.publicKeyId,
        amount: currentPayment.checkout.amountSubunits,
        currency: currentPayment.checkout.currency,
        name: "NoorPath",
        description: `Booking ${booking.bookingReference}`,
        order_id: currentPayment.checkout.providerSessionId,
        handler: async (result) => {
          setPaymentState({
            kind: "waiting",
            payment: currentPayment,
            message: "Verifying the payment response securely…",
          });
          const response = await fetch(
            `/api/v1/payments/${encodeURIComponent(currentPayment.paymentAttemptId)}/checkout-callback`,
            {
              method: "POST",
              credentials: "include",
              headers: requestHeaders(true),
              body: JSON.stringify({
                providerSessionId: result.razorpay_order_id,
                providerPaymentId: result.razorpay_payment_id,
                signature: result.razorpay_signature,
              }),
            },
          );
          if (!response.ok) {
            const problem = (await response.json()) as ProblemDetails;
            setPaymentState({
              kind: "error",
              payment: currentPayment,
              message:
                problem.detail ??
                "The payment response could not be verified. Contact support before retrying.",
            });
            return;
          }
          await loadPayment(currentPayment.paymentAttemptId, true);
        },
        modal: {
          ondismiss: () => {
            setPaymentState({
              kind: "ready",
              payment: currentPayment,
            });
          },
        },
        theme: { color: "#176b50" },
      });
      checkout.on("payment.failed", () => {
        setPaymentState({
          kind: "waiting",
          payment: currentPayment,
          message:
            "The provider reported that payment did not complete. NoorPath is confirming the final state before enabling a safe retry.",
        });
        void loadPayment(currentPayment.paymentAttemptId, true);
      });
      checkout.open();
    } catch {
      setPaymentState({
        kind: "error",
        payment: currentPayment,
        message:
          "Secure checkout could not be opened. No payment was confirmed. Retry safely.",
      });
    }
  };

  return (
    <div className="booking-payment-page">
      <PublicHeader mode="detail" />
      <main id="main-content" className="booking-payment-main">
        <nav className="package-breadcrumbs" aria-label="Breadcrumb">
          <Link href="/">Home</Link>
          <span>/</span>
          {booking ? (
            <Link href={`/packages/${booking.departureId}/plan`}>Your plan</Link>
          ) : (
            <span>Your plan</span>
          )}
          <span>/</span>
          <span aria-current="page">Booking & payment</span>
        </nav>

        {pageState.kind === "loading" ? (
          <PaymentNotice
            tone="neutral"
            title="Loading your booking"
            message="NoorPath is retrieving the exact commercial snapshot you reviewed."
          />
        ) : null}

        {pageState.kind === "unauthenticated" ? (
          <PaymentNotice
            tone="warning"
            title="Sign in to continue"
            message="Your booking and payment details are private to your NoorPath account."
          />
        ) : null}

        {pageState.kind === "not-found" ? (
          <PaymentNotice
            tone="warning"
            title="Booking not found"
            message="This booking is unavailable or does not belong to your account."
          />
        ) : null}

        {pageState.kind === "error" ? (
          <PaymentNotice
            tone="warning"
            title="Booking needs to be reloaded"
            message={pageState.message}
            action={{ label: "Retry safely", onClick: loadBooking }}
          />
        ) : null}

        {booking ? (
          <>
            <section className="booking-payment-hero">
              <div>
                <p className="public-eyebrow">Secure booking step</p>
                <h1>Review your booking before payment</h1>
                <p>
                  Booking reference <strong>{booking.bookingReference}</strong>
                </p>
              </div>
              <div className="booking-payment-status" role="status">
                <Icon name="shield-check" />
                <div>
                  <strong>
                    {booking.state === "PaymentSucceeded"
                      ? "Payment received"
                      : "Not confirmed yet"}
                  </strong>
                  <p>
                    {booking.state === "PaymentSucceeded"
                      ? "NoorPath is moving this booking into confirmation."
                      : "Payment is the next commitment. Confirmation follows only after authenticated settlement and inventory conversion."}
                  </p>
                </div>
              </div>
            </section>

            <div className="booking-payment-layout">
              <section className="booking-review-card" aria-labelledby="booking-review-title">
                <div className="booking-payment-heading-row">
                  <div>
                    <p className="public-eyebrow">Immutable review</p>
                    <h2 id="booking-review-title">Your booking obligation</h2>
                  </div>
                  <span className="booking-reference-chip">
                    {booking.bookingReference}
                  </span>
                </div>

                <dl className="booking-review-facts">
                  <div>
                    <dt>Room</dt>
                    <dd>{occupancyLabel(booking.occupancy)}</dd>
                  </div>
                  <div>
                    <dt>Travellers</dt>
                    <dd>{booking.travellerCount} adults</dd>
                  </div>
                  <div>
                    <dt>Total</dt>
                    <dd>{money(booking.currency, booking.total)}</dd>
                  </div>
                  <div>
                    <dt>Due now</dt>
                    <dd>{dueLabel}</dd>
                  </div>
                  <div>
                    <dt>Remaining</dt>
                    <dd>{money(booking.currency, booking.remaining)}</dd>
                  </div>
                  <div>
                    <dt>Current state</dt>
                    <dd>{booking.state}</dd>
                  </div>
                </dl>

                <div className="booking-traveller-review">
                  <h3>Travellers</h3>
                  <ol>
                    {booking.travellers.map((traveller) => (
                      <li key={traveller.travellerId}>
                        <span>{traveller.position}</span>
                        <div>
                          <strong>{traveller.fullName}</strong>
                          <small>Born {formatDate(traveller.dateOfBirth)}</small>
                        </div>
                      </li>
                    ))}
                  </ol>
                </div>

                {booking.instalments.length > 0 ? (
                  <div className="booking-instalment-review">
                    <h3>Remaining payment schedule</h3>
                    <ol>
                      {booking.instalments.map((instalment) => (
                        <li key={instalment.sequence}>
                          <span>Instalment {instalment.sequence}</span>
                          <time dateTime={instalment.dueDate}>
                            {formatDate(instalment.dueDate)}
                          </time>
                          <strong>
                            {money(booking.currency, instalment.amount)}
                          </strong>
                        </li>
                      ))}
                    </ol>
                  </div>
                ) : null}
              </section>

              <aside className="payment-action-card" aria-labelledby="payment-action-title">
                <div className="payment-action-icon">
                  <Icon name="shield-check" />
                </div>
                <p className="public-eyebrow">Provider-hosted checkout</p>
                <h2 id="payment-action-title">Pay {dueLabel} securely</h2>
                <p>
                  NoorPath creates the payment obligation, but your card or UPI
                  details are entered only in Razorpay Checkout.
                </p>

                <PaymentAction
                  booking={booking}
                  state={paymentState}
                  onStart={() => void startPayment(false)}
                  onRetry={() => void startPayment(true)}
                  onOpen={() => void openCheckout()}
                  onRefresh={() =>
                    payment
                      ? void loadPayment(payment.paymentAttemptId)
                      : void startPayment(false)
                  }
                />

                <div className="payment-safety-note">
                  <Icon name="lock" />
                  <p>
                    NoorPath does not receive or store your card number or CVV.
                    A browser success response is verified, but settlement is
                    accepted only from the authenticated provider event.
                  </p>
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

function PaymentAction({
  booking,
  state,
  onStart,
  onRetry,
  onOpen,
  onRefresh,
}: {
  booking: Booking;
  state: PaymentState;
  onStart: () => void;
  onRetry: () => void;
  onOpen: () => void;
  onRefresh: () => void;
}) {
  if (booking.state === "PaymentSucceeded") {
    return (
      <div className="payment-state-block success" role="status" aria-live="polite">
        <strong>Payment received</strong>
        <p>
          This is not the final booking confirmation. NoorPath will confirm the
          booking after the durable inventory commitment completes.
        </p>
      </div>
    );
  }

  if (state.kind === "idle") {
    return (
      <button className="payment-primary-action" type="button" onClick={onStart}>
        Prepare secure payment
      </button>
    );
  }

  if (state.kind === "starting") {
    return (
      <button className="payment-primary-action" type="button" disabled>
        Preparing secure payment…
      </button>
    );
  }

  if (state.kind === "error") {
    const failed = state.payment?.state === "Failed";
    return (
      <div className="payment-state-block warning" role="alert">
        <strong>Payment needs attention</strong>
        <p>{state.message}</p>
        <button type="button" onClick={failed ? onRetry : onRefresh}>
          {failed ? "Create a fresh payment attempt" : "Retry safely"}
        </button>
      </div>
    );
  }

  const payment = state.payment;
  if (payment.state === "Succeeded") {
    return (
      <div className="payment-state-block success" role="status" aria-live="polite">
        <strong>Payment received</strong>
        <p>NoorPath is moving this booking into confirmation.</p>
      </div>
    );
  }

  if (payment.state === "Failed" || payment.state === "Cancelled") {
    return (
      <div className="payment-state-block warning" role="alert">
        <strong>
          {payment.state === "Failed"
            ? "Payment did not complete"
            : "Payment was cancelled"}
        </strong>
        <p>No payment was confirmed. You can create a fresh attempt safely.</p>
        <button type="button" onClick={onRetry}>
          Create a fresh payment attempt
        </button>
      </div>
    );
  }

  if (state.kind === "waiting") {
    return (
      <div className="payment-state-block pending" role="status" aria-live="polite">
        <strong>Waiting for authenticated settlement</strong>
        <p>{state.message}</p>
        <button type="button" onClick={onRefresh}>
          Refresh payment status
        </button>
      </div>
    );
  }

  if (!payment.checkout) {
    return (
      <div className="payment-state-block pending" role="status">
        <strong>Payment is being prepared</strong>
        <p>Refresh the payment state before continuing.</p>
        <button type="button" onClick={onRefresh}>
          Refresh payment status
        </button>
      </div>
    );
  }

  return (
    <div className="payment-checkout-ready">
      <dl>
        <div>
          <dt>Amount</dt>
          <dd>{money(payment.currency, payment.amount)}</dd>
        </div>
        <div>
          <dt>Provider</dt>
          <dd>Razorpay</dd>
        </div>
      </dl>
      <button
        className="payment-primary-action"
        type="button"
        disabled={state.kind === "opening"}
        onClick={onOpen}
      >
        {state.kind === "opening" ? "Opening secure checkout…" : "Open secure checkout"}
      </button>
      <small>
        Closing checkout does not confirm payment. You can reopen this same
        provider order safely while it remains active.
      </small>
    </div>
  );
}

function PaymentNotice({
  tone,
  title,
  message,
  action,
}: {
  tone: "neutral" | "warning";
  title: string;
  message: string;
  action?: { label: string; onClick: () => void };
}) {
  return (
    <section className={`booking-payment-notice ${tone}`} role="status">
      <Icon name={tone === "warning" ? "info" : "shield-check"} />
      <div>
        <h1>{title}</h1>
        <p>{message}</p>
        {action ? (
          <button type="button" onClick={action.onClick}>
            {action.label}
          </button>
        ) : null}
      </div>
    </section>
  );
}
