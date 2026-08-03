"use client";

import { FormEvent, useCallback, useState } from "react";
import { useDeferredInitialLoad } from "../../../../lib/use-deferred-initial-load";
import { Icon } from "../../../public-ui";

type FeeComponent = {
  code: string;
  label: string;
  amount: number;
};

type CancellationPolicy = {
  available: boolean;
  canRequest: boolean;
  code: string;
  message: string;
  version?: string | null;
  timeZoneId?: string | null;
  daysBeforeDeparture?: number | null;
  refundProcessingBusinessDays?: number | null;
  currency: string;
  settledAmount: number;
  percentageFee: number;
  nonRefundableAmount: number;
  refundableAmount: number;
  requiresOperatorApproval: boolean;
  feeComponents: FeeComponent[];
};

type CancellationRequest = {
  id: string;
  state: string;
  customerStatus: string;
  reasonCategory: string;
  policyVersion: string;
  version: number;
  currency: string;
  settledAmount: number;
  percentageFee: number;
  nonRefundableAmount: number;
  refundableAmount: number;
  refundProcessingBusinessDays: number;
  decisionReason?: string | null;
  failureCode?: string | null;
  requestedAtUtc: string;
  updatedAtUtc: string;
  decidedAtUtc?: string | null;
  appliedAtUtc?: string | null;
  refundId?: string | null;
  refundState?: string | null;
  refundFailureCode?: string | null;
  refundedAtUtc?: string | null;
};

type Projection = {
  bookingId: string;
  bookingState: string;
  policy: CancellationPolicy;
  request?: CancellationRequest | null;
  reasonCategories: string[];
};

type State =
  | { kind: "loading" }
  | { kind: "ready"; projection: Projection }
  | { kind: "error"; message: string };

export function CancellationPanel({ bookingId }: { bookingId: string }) {
  const [state, setState] = useState<State>({ kind: "loading" });
  const [reasonCategory, setReasonCategory] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [feedback, setFeedback] = useState("");

  const load = useCallback(async () => {
    setState({ kind: "loading" });
    try {
      const response = await fetch(
        `/api/v1/bookings/${encodeURIComponent(bookingId)}/cancellation`,
        {
          cache: "no-store",
          credentials: "include",
          headers: testHeaders(),
        },
      );
      if (!response.ok) throw new Error();
      const projection = (await response.json()) as Projection;
      setState({ kind: "ready", projection });
      setReasonCategory(
        (current) => current || projection.reasonCategories.at(0) || "",
      );
    } catch {
      setState({
        kind: "error",
        message:
          "Cancellation information is temporarily unavailable. Your booking has not changed.",
      });
    }
  }, [bookingId]);

  useDeferredInitialLoad(load);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!reasonCategory || submitting) return;
    setSubmitting(true);
    setFeedback("");
    try {
      const response = await fetch(
        `/api/v1/bookings/${encodeURIComponent(bookingId)}/cancellation-requests`,
        {
          method: "POST",
          credentials: "include",
          headers: testHeaders(true, {
            "Idempotency-Key": cancellationKey(bookingId),
          }),
          body: JSON.stringify({ reasonCategory }),
        },
      );
      if (response.ok) {
        setState({
          kind: "ready",
          projection: (await response.json()) as Projection,
        });
        setFeedback(
          "Your cancellation request was submitted for operator review.",
        );
        return;
      }

      const problem = (await response.json()) as {
        code?: string;
        title?: string;
      };
      if (
        problem.code === "active_cancellation_exists" ||
        problem.code === "stale_cancellation_version"
      ) {
        await load();
      }
      setFeedback(
        problem.title ||
          "The cancellation request could not be submitted. Your booking has not changed.",
      );
    } catch {
      setFeedback(
        "We could not confirm whether the request was received. Refresh safely before trying again.",
      );
    } finally {
      setSubmitting(false);
    }
  }

  if (state.kind === "loading") {
    return (
      <section className="journey-panel" aria-busy="true">
        <p className="public-eyebrow">Cancellation</p>
        <h2>Checking your cancellation options</h2>
        <p>Your booking remains unchanged while NoorPath loads the policy.</p>
      </section>
    );
  }

  if (state.kind === "error") {
    return (
      <section className="journey-panel">
        <p className="public-eyebrow">Cancellation</p>
        <h2>Cancellation information unavailable</h2>
        <p role="alert">{state.message}</p>
        <button type="button" onClick={() => void load()}>
          Retry
        </button>
      </section>
    );
  }

  const { policy, request, reasonCategories } = state.projection;
  const m = (amount: number) => money(policy.currency, amount);

  return (
    <section className="journey-panel" aria-labelledby="cancellation-title">
      <p className="public-eyebrow">Cancellation &amp; refund</p>
      <h2 id="cancellation-title">
        {request
          ? statusTitle(request.customerStatus)
          : "Review before requesting"}
      </h2>

      {request ? (
        <div className="cancellation-status" aria-live="polite">
          <p>
            <strong>Status:</strong> {statusLabel(request.customerStatus)}
          </p>
          <p>
            Submitted {dateTime(request.requestedAtUtc)} under policy{" "}
            {request.policyVersion}.
          </p>
          <dl className="journey-money">
            <div>
              <dt>Paid and assessed</dt>
              <dd>{m(request.settledAmount)}</dd>
            </div>
            <div>
              <dt>Cancellation deductions</dt>
              <dd>{m(request.percentageFee + request.nonRefundableAmount)}</dd>
            </div>
            <div>
              <dt>Maximum refund entitlement</dt>
              <dd>{m(request.refundableAmount)}</dd>
            </div>
          </dl>
          {request.decisionReason ? (
            <p>
              <strong>Review note:</strong> {request.decisionReason}
            </p>
          ) : null}
          {request.failureCode || request.refundFailureCode ? (
            <p role="alert">
              NoorPath support is reviewing a processing exception. Do not
              submit another cancellation or payment.
            </p>
          ) : null}
          {request.customerStatus === "RefundPending" ? (
            <p>
              An authorized refund is pending. The configured expectation is up
              to {request.refundProcessingBusinessDays} business days after
              execution begins.
            </p>
          ) : null}
          {request.refundedAtUtc ? (
            <p>
              Refund recorded {dateTime(request.refundedAtUtc)}. Your original
              payment remains preserved in the financial history.
            </p>
          ) : null}
        </div>
      ) : (
        <>
          <p>{policy.message}</p>
          {policy.available ? (
            <>
              <dl className="journey-money">
                <div>
                  <dt>Settled payments assessed</dt>
                  <dd>{m(policy.settledAmount)}</dd>
                </div>
                <div>
                  <dt>Estimated deductions</dt>
                  <dd>
                    {m(policy.percentageFee + policy.nonRefundableAmount)}
                  </dd>
                </div>
                <div>
                  <dt>Estimated maximum refund</dt>
                  <dd>{m(policy.refundableAmount)}</dd>
                </div>
              </dl>
              {policy.feeComponents.length ? (
                <ul
                  className="journey-instalments"
                  aria-label="Estimated deductions"
                >
                  {policy.feeComponents.map((component) => (
                    <li key={component.code}>
                      <span>{component.label}</span>
                      <strong>{m(component.amount)}</strong>
                    </li>
                  ))}
                </ul>
              ) : (
                <p>
                  No configured cancellation deduction applies to this estimate.
                </p>
              )}
              <p className="document-help">
                <Icon name="info" /> This is a server-calculated estimate under
                policy {policy.version}. Every request requires operator review;
                submitting does not cancel the booking immediately.
              </p>
            </>
          ) : null}

          {policy.canRequest ? (
            <form onSubmit={(event) => void submit(event)}>
              <label>
                Main reason for cancellation
                <select
                  value={reasonCategory}
                  onChange={(event) => setReasonCategory(event.target.value)}
                  required
                >
                  {reasonCategories.map((reason) => (
                    <option value={reason} key={reason}>
                      {humanize(reason)}
                    </option>
                  ))}
                </select>
              </label>
              <button type="submit" disabled={submitting}>
                {submitting
                  ? "Submitting review request…"
                  : "Request cancellation review"}
              </button>
            </form>
          ) : null}
        </>
      )}

      {feedback ? <p role="alert">{feedback}</p> : null}
    </section>
  );
}

function cancellationKey(bookingId: string) {
  const storageKey = `noorpath:cancellation:key:${bookingId}`;
  const existing = window.sessionStorage.getItem(storageKey);
  if (existing) return existing;
  const created = `cancel-${window.crypto.randomUUID()}`;
  window.sessionStorage.setItem(storageKey, created);
  return created;
}

function testHeaders(
  json = false,
  additional: Record<string, string> = {},
): HeadersInit {
  const headers: Record<string, string> = { ...additional };
  if (json) headers["Content-Type"] = "application/json";
  const identity = process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY;
  if (identity) headers["X-NoorPath-Test-Identity"] = identity;
  return headers;
}

function money(currency: string, amount: number) {
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(amount);
}

function dateTime(value: string) {
  return new Date(value).toLocaleString("en-IN", {
    dateStyle: "medium",
    timeStyle: "short",
  });
}

function humanize(value: string) {
  return value.replaceAll(/([A-Z])/g, " $1").trim();
}

function statusLabel(status: string) {
  return status
    .replaceAll(/([A-Z])/g, " $1")
    .trim()
    .replace(/^./, (letter) => letter.toUpperCase());
}

function statusTitle(status: string) {
  switch (status) {
    case "UnderReview":
      return "Cancellation request under review";
    case "Approved":
      return "Cancellation approved";
    case "Rejected":
      return "Cancellation request declined";
    case "RefundPending":
      return "Refund authorization pending execution";
    case "PartiallyRefunded":
      return "Refund partially completed";
    case "Refunded":
      return "Refund completed";
    case "RecoveryRequired":
      return "NoorPath support action required";
    default:
      return "Booking cancelled";
  }
}
