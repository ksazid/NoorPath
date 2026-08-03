"use client";

import Link from "next/link";
import { FormEvent, useCallback, useState } from "react";
import { useDeferredInitialLoad } from "../../../lib/use-deferred-initial-load";
import { Icon, PublicFooter, PublicHeader } from "../../public-ui";

type Refund = {
  refundId: string;
  currency: string;
  entitledAmount: number;
  refundedAmount: number;
  state: string;
  failureCode?: string | null;
  version: number;
  updatedAtUtc: string;
};

type QueueItem = {
  cancellationId: string;
  bookingId: string;
  state: string;
  customerStatus: string;
  currency: string;
  settledAmount: number;
  percentageFee: number;
  nonRefundableAmount: number;
  refundableAmount: number;
  policyVersion: string;
  version: number;
  requestedAtUtc: string;
  updatedAtUtc: string;
  failureCode?: string | null;
  refund?: Refund | null;
};

type AllowedAction = {
  code: string;
  label: string;
  refundId?: string;
};

type CaseDetail = {
  booking: {
    id: string;
    reference: string;
    state: string;
    currency: string;
    total: number;
    cancelledAtUtc?: string | null;
  };
  cancellation: {
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
  };
  calculation: {
    policyVersion: string;
    policyTimeZoneId: string;
    departureAtUtc: string;
    daysBeforeDeparture: number;
    windowMinimumDaysBeforeDeparture: number;
    feeBasisPoints: number;
    currency: string;
    settledAmount: number;
    percentageFee: number;
    nonRefundableAmount: number;
    refundableAmount: number;
    refundProcessingBusinessDays: number;
  };
  refund?: Refund | null;
  allowedActions: AllowedAction[];
  audit: {
    action: string;
    reason?: string | null;
    actorAccountId: string;
    occurredAtUtc: string;
  }[];
};

type ListState =
  | { kind: "loading" }
  | { kind: "denied" }
  | { kind: "error" }
  | { kind: "ready"; items: QueueItem[] };

function requestHeaders(json = false): HeadersInit {
  const headers: Record<string, string> = {};
  if (json) headers["Content-Type"] = "application/json";

  const identity = process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY;
  if (identity) headers["X-NoorPath-Test-Identity"] = identity;
  return headers;
}

export default function CancellationReviewPage() {
  const [state, setState] = useState<ListState>({ kind: "loading" });
  const [filter, setFilter] = useState("");
  const [detail, setDetail] = useState<CaseDetail | null>(null);
  const [reason, setReason] = useState("");
  const [feedback, setFeedback] = useState("");
  const [working, setWorking] = useState(false);

  const load = useCallback(async (selectedState = "") => {
    setState({ kind: "loading" });
    const params = new URLSearchParams();
    if (selectedState) params.set("state", selectedState);

    try {
      const response = await fetch(`/api/v1/operator/cancellations?${params}`, {
        credentials: "include",
        cache: "no-store",
        headers: requestHeaders(),
      });
      if (response.status === 403) {
        setState({ kind: "denied" });
        return;
      }
      if (!response.ok) throw new Error();

      const payload = (await response.json()) as { items: QueueItem[] };
      setState({ kind: "ready", items: payload.items });
    } catch {
      setState({ kind: "error" });
    }
  }, []);

  useDeferredInitialLoad(load);

  async function applyFilter(event: FormEvent) {
    event.preventDefault();
    setFeedback("");
    setDetail(null);
    await load(filter);
  }

  async function openCase(cancellationId: string) {
    setReason("");
    try {
      const response = await fetch(
        `/api/v1/operator/cancellations/${encodeURIComponent(cancellationId)}`,
        {
          credentials: "include",
          cache: "no-store",
          headers: requestHeaders(),
        },
      );
      if (!response.ok) throw new Error();
      setDetail((await response.json()) as CaseDetail);
    } catch {
      setFeedback(
        "The cancellation case could not be loaded. Refresh the queue and try again.",
      );
    }
  }

  async function runAction(action: AllowedAction) {
    if (!detail || working) return;
    if (reason.trim().length < 5) {
      setFeedback(
        "Record a reason of at least five characters before continuing.",
      );
      return;
    }

    setWorking(true);
    setFeedback("");
    const endpoint =
      action.code === "execute_refund" && action.refundId
        ? `/api/v1/operator/refunds/${encodeURIComponent(action.refundId)}/execute`
        : `/api/v1/operator/cancellations/${encodeURIComponent(
            detail.cancellation.id,
          )}/${action.code}`;
    const expectedVersion =
      action.code === "execute_refund"
        ? detail.refund?.version
        : detail.cancellation.version;

    try {
      const response = await fetch(endpoint, {
        method: "POST",
        credentials: "include",
        headers: requestHeaders(true),
        body: JSON.stringify({ expectedVersion, reason: reason.trim() }),
      });
      if (response.status === 409) {
        setFeedback(
          "This case changed while you were reviewing it. The latest state has been loaded.",
        );
        await openCase(detail.cancellation.id);
        await load(filter);
        return;
      }
      if (response.status === 503) {
        setFeedback(
          "The refund remains authorized, but production execution is not enabled. No payment fact was changed.",
        );
        await openCase(detail.cancellation.id);
        return;
      }
      if (!response.ok && response.status !== 202) throw new Error();

      setFeedback(actionSuccess(action.code));
      await openCase(detail.cancellation.id);
      await load(filter);
    } catch {
      setFeedback(
        "The governed action could not be completed. No manual amount or historical payment fact was changed.",
      );
    } finally {
      setWorking(false);
    }
  }

  return (
    <div className="journey-page">
      <PublicHeader mode="detail" />
      <main id="main-content" className="journey-main">
        <nav className="package-breadcrumbs" aria-label="Breadcrumb">
          <Link href="/operator">Operator</Link>
          <span>/</span>
          <span aria-current="page">Cancellation review</span>
        </nav>
        <p className="public-eyebrow">Governed financial operations</p>
        <h1>Cancellation &amp; refund review</h1>
        <p className="journey-intro">
          Review the immutable policy calculation, approve or reject the whole
          booking request, and execute only the system-authorized refund.
        </p>

        <form
          className="journey-panel"
          onSubmit={(event) => void applyFilter(event)}
        >
          <label>
            Cancellation state
            <select
              value={filter}
              onChange={(event) => setFilter(event.target.value)}
            >
              <option value="">All states</option>
              <option value="Requested">Requested</option>
              <option value="Applied">Applied</option>
              <option value="Rejected">Rejected</option>
              <option value="Exception">Recovery required</option>
            </select>
          </label>
          <button type="submit">Apply filter</button>
        </form>

        <div aria-live="polite">
          {feedback ? <p role="alert">{feedback}</p> : null}
          {state.kind === "loading" ? <p>Loading cancellation cases…</p> : null}
          {state.kind === "denied" ? (
            <p>You do not have cancellation review permission.</p>
          ) : null}
          {state.kind === "error" ? (
            <section className="journey-state">
              <h2>Cancellation queue temporarily unavailable</h2>
              <button type="button" onClick={() => void load(filter)}>
                Retry
              </button>
            </section>
          ) : null}
        </div>

        {state.kind === "ready" && state.items.length === 0 ? (
          <section className="journey-state">
            <h2>No matching cancellation cases</h2>
            <p>The operator-scoped queue is clear for this filter.</p>
          </section>
        ) : null}

        {state.kind === "ready" ? (
          <div className="documents-list" aria-label="Cancellation cases">
            {state.items.map((item) => (
              <article className="documents-card" key={item.cancellationId}>
                <p className="public-eyebrow">{item.customerStatus}</p>
                <h2>Cancellation request</h2>
                <p>
                  Requested{" "}
                  {new Date(item.requestedAtUtc).toLocaleString("en-IN")}
                </p>
                <p>
                  Maximum authorized entitlement:{" "}
                  <strong>{money(item.currency, item.refundableAmount)}</strong>
                </p>
                <p>
                  Policy {item.policyVersion} · Version {item.version}
                </p>
                {item.failureCode ? (
                  <p>Recovery code: {item.failureCode}</p>
                ) : null}
                <button
                  type="button"
                  onClick={() => {
                    setFeedback("");
                    void openCase(item.cancellationId);
                  }}
                >
                  Review case
                </button>
              </article>
            ))}
          </div>
        ) : null}

        {detail ? (
          <section
            className="journey-panel"
            aria-labelledby="cancellation-case-title"
          >
            <p className="public-eyebrow">Booking {detail.booking.reference}</p>
            <h2 id="cancellation-case-title">
              {detail.cancellation.customerStatus}
            </h2>
            <p>
              Customer reason: {humanize(detail.cancellation.reasonCategory)}
            </p>

            <dl className="journey-money">
              <div>
                <dt>Settled payments</dt>
                <dd>
                  {money(
                    detail.calculation.currency,
                    detail.calculation.settledAmount,
                  )}
                </dd>
              </div>
              <div>
                <dt>Policy deductions</dt>
                <dd>
                  {money(
                    detail.calculation.currency,
                    detail.calculation.percentageFee +
                      detail.calculation.nonRefundableAmount,
                  )}
                </dd>
              </div>
              <div>
                <dt>Authorized refund</dt>
                <dd>
                  {money(
                    detail.calculation.currency,
                    detail.calculation.refundableAmount,
                  )}
                </dd>
              </div>
            </dl>
            <p>
              Policy {detail.calculation.policyVersion} ·{" "}
              {detail.calculation.daysBeforeDeparture} days before departure ·{" "}
              {detail.calculation.policyTimeZoneId}
            </p>
            <p className="document-help">
              <Icon name="shield-check" /> Amounts are read-only and derived
              from authoritative settled-payment facts. Operators cannot enter a
              replacement refund value.
            </p>

            {detail.refund ? (
              <p>
                Refund: {detail.refund.state} ·{" "}
                {money(detail.refund.currency, detail.refund.refundedAmount)} of{" "}
                {money(detail.refund.currency, detail.refund.entitledAmount)}{" "}
                recorded
              </p>
            ) : null}

            {detail.allowedActions.length ? (
              <div className="support-actions">
                <label>
                  Decision or recovery reason
                  <textarea
                    value={reason}
                    onChange={(event) => setReason(event.target.value)}
                    minLength={5}
                    maxLength={500}
                    rows={4}
                    required
                  />
                </label>
                <div className="document-row" aria-label="Governed actions">
                  {detail.allowedActions.map((action) => (
                    <button
                      type="button"
                      key={action.code}
                      disabled={working}
                      onClick={() => void runAction(action)}
                    >
                      {working ? "Working…" : action.label}
                    </button>
                  ))}
                </div>
              </div>
            ) : (
              <p>No operator action is currently available for this case.</p>
            )}

            <h3>Audit history</h3>
            <ol className="journey-instalments">
              {detail.audit.map((entry, index) => (
                <li key={`${entry.occurredAtUtc}-${index}`}>
                  <span>
                    {humanize(entry.action)}
                    <small>{entry.reason || "System transition"}</small>
                  </span>
                  <time dateTime={entry.occurredAtUtc}>
                    {new Date(entry.occurredAtUtc).toLocaleString("en-IN")}
                  </time>
                </li>
              ))}
            </ol>
          </section>
        ) : null}
      </main>
      <PublicFooter />
    </div>
  );
}

function money(currency: string, amount: number) {
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(amount);
}

function humanize(value: string) {
  return value
    .replaceAll(/([A-Z_])/g, " $1")
    .replaceAll("_", " ")
    .trim();
}

function actionSuccess(code: string) {
  switch (code) {
    case "approve":
      return "The cancellation was approved and the governed cancellation workflow was applied.";
    case "reject":
      return "The cancellation request was rejected and the confirmed booking remains unchanged.";
    case "recover":
      return "The governed cancellation recovery was retried.";
    case "execute_refund":
      return "The authorized refund execution was processed.";
    default:
      return "The governed action completed.";
  }
}
