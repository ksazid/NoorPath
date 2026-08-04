"use client";

import Link from "next/link";
import { FormEvent, useCallback, useState } from "react";
import { useDeferredInitialLoad } from "../../../lib/use-deferred-initial-load";
import { PublicFooter, PublicHeader } from "../../public-ui";

type SupportItem = {
  bookingId: string;
  bookingReference: string;
  category: string;
  title: string;
  code: string;
  updatedAtUtc: string;
  actionLabel: string;
  actionTarget?: string;
};

type Detail = {
  booking: {
    id: string;
    reference: string;
    state: string;
    confirmationExceptionCode?: string;
    updatedAtUtc: string;
  };
  payment?: { state: string; failureCode?: string; updatedAtUtc: string };
  cancellation?: {
    id: string;
    state: string;
    reasonCategory: string;
    policyVersion: string;
    refundableAmount: number;
    currency: string;
    failureCode?: string;
    version: number;
    updatedAtUtc: string;
  };
  refund?: {
    id: string;
    state: string;
    amount: number;
    refundedAmount: number;
    currency: string;
    failureCode?: string;
    version: number;
    updatedAtUtc: string;
  };
  documents: {
    kind: string;
    state: string;
    malwareStatus: string;
    reviewReason?: string;
    version?: number;
  }[];
  visa: {
    travellerId: string;
    status: string;
    customerAction?: string;
    version: number;
    updatedAtUtc: string;
  }[];
  allowedActions: { code: string; label: string; target: string }[];
  navigationActions: { code: string; label: string; target: string }[];
};

const headers = (json = false): HeadersInit => ({
  ...(json && { "Content-Type": "application/json" }),
  ...(process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY
    ? {
        "X-NoorPath-Test-Identity":
          process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY,
      }
    : {}),
});

export default function OperationalSupportPage() {
  const [state, setState] = useState<
    | { kind: "loading" }
    | { kind: "denied" }
    | { kind: "error" }
    | { kind: "ready"; items: SupportItem[] }
  >({ kind: "loading" });
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("");
  const [detail, setDetail] = useState<Detail | null>(null);
  const [message, setMessage] = useState("");

  const load = useCallback(async (term = "", selectedCategory = "") => {
    setState({ kind: "loading" });
    setMessage("");
    try {
      const params = new URLSearchParams();
      if (term.trim()) params.set("search", term.trim());
      if (selectedCategory) params.set("category", selectedCategory);
      const response = await fetch(`/api/v1/operator/support?${params}`, {
        credentials: "include",
        cache: "no-store",
        headers: headers(),
      });
      if (response.status === 403) {
        setState({ kind: "denied" });
        return;
      }
      if (!response.ok) throw new Error();
      const payload = (await response.json()) as { items: SupportItem[] };
      setState({ kind: "ready", items: payload.items });
    } catch {
      setState({ kind: "error" });
    }
  }, []);

  useDeferredInitialLoad(load);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setDetail(null);
    await load(search, category);
  }

  async function openCase(item: SupportItem) {
    setMessage("");
    const response = await fetch(
      `/api/v1/operator/support/bookings/${item.bookingId}`,
      {
        credentials: "include",
        cache: "no-store",
        headers: headers(),
      },
    );
    if (response.ok) setDetail((await response.json()) as Detail);
    else
      setMessage(
        "The support case could not be loaded. Refresh and try again.",
      );
  }

  async function runAction(action: Detail["allowedActions"][number]) {
    if (action.code !== "retry_confirmation" || !detail) return;
    const reason = window.prompt(
      "Record the operational reason for retrying confirmation",
    );
    if (!reason) return;
    const response = await fetch(action.target, {
      method: "POST",
      credentials: "include",
      headers: headers(true),
      body: JSON.stringify({ reason }),
    });
    if (response.status === 409) {
      setMessage(
        "The booking changed and can no longer be recovered from this state.",
      );
      await openCase({ bookingId: detail.booking.id } as SupportItem);
      return;
    }
    if (!response.ok) {
      setMessage("The recovery command failed. Check the reason and retry.");
      return;
    }
    setMessage("Confirmation recovery was requested and audited.");
    setDetail(null);
    await load(search, category);
  }

  return (
    <div className="journey-page">
      <PublicHeader mode="detail" />
      <main id="main-content" className="journey-main">
        <nav className="package-breadcrumbs" aria-label="Breadcrumb">
          <Link href="/operator">Operator</Link>
          <span>/</span>
          <span aria-current="page">Operational support</span>
        </nav>
        <p className="public-eyebrow">Exception-first operations</p>
        <h1>Operational support</h1>
        <p className="journey-intro">
          Find booking exceptions, review module-owned facts, and use only
          governed recovery actions.
        </p>

        <form
          className="journey-panel"
          onSubmit={(event) => void submit(event)}
        >
          <label>
            Booking reference
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search by booking reference"
            />
          </label>
          <label>
            Exception category
            <select
              value={category}
              onChange={(event) => setCategory(event.target.value)}
            >
              <option value="">All categories</option>
              <option value="confirmation">Confirmation</option>
              <option value="payment">Payment</option>
              <option value="documents">Documents</option>
              <option value="visa">Visa</option>
              <option value="cancellation">Cancellation</option>
              <option value="refund">Refund</option>
            </select>
          </label>
          <button type="submit">Search exceptions</button>
        </form>

        <div aria-live="polite">
          {message ? <p role="alert">{message}</p> : null}
          {state.kind === "loading" ? (
            <p>Loading operational exceptions…</p>
          ) : null}
          {state.kind === "denied" ? (
            <p>You do not have operational support permission.</p>
          ) : null}
          {state.kind === "error" ? (
            <section className="journey-state">
              <h2>Support queue temporarily unavailable</h2>
              <button onClick={() => void load(search, category)}>Retry</button>
            </section>
          ) : null}
        </div>

        {state.kind === "ready" && state.items.length === 0 ? (
          <section className="journey-state">
            <h2>No matching exceptions</h2>
            <p>The current operator-scoped queue is clear for these filters.</p>
          </section>
        ) : null}

        {state.kind === "ready" ? (
          <div className="documents-list" aria-label="Operational exceptions">
            {state.items.map((item) => (
              <article
                className="documents-card"
                key={`${item.category}-${item.bookingId}-${item.code}`}
              >
                <p className="public-eyebrow">{item.category}</p>
                <h2>{item.title}</h2>
                <p>Booking {item.bookingReference}</p>
                <p>Exception code: {item.code}</p>
                <time dateTime={item.updatedAtUtc}>
                  Updated {new Date(item.updatedAtUtc).toLocaleString("en-IN")}
                </time>
                <div className="document-row support-actions">
                  <button type="button" onClick={() => void openCase(item)}>
                    Review case
                  </button>
                  {isNavigationTarget(item.actionTarget) ? (
                    <Link href={item.actionTarget}>{item.actionLabel}</Link>
                  ) : null}
                </div>
              </article>
            ))}
          </div>
        ) : null}

        {detail ? (
          <section
            className="journey-panel"
            aria-labelledby="support-case-title"
          >
            <p className="public-eyebrow">Booking {detail.booking.reference}</p>
            <h2 id="support-case-title">Case state: {detail.booking.state}</h2>
            {detail.booking.confirmationExceptionCode ? (
              <p>
                Confirmation exception:{" "}
                {detail.booking.confirmationExceptionCode}
              </p>
            ) : null}

            <h3>Payment</h3>
            <p>
              {detail.payment
                ? `${detail.payment.state}${
                    detail.payment.failureCode
                      ? ` — ${detail.payment.failureCode}`
                      : ""
                  }`
                : "No payment attempt recorded."}
            </p>

            <h3>Cancellation</h3>
            {detail.cancellation ? (
              <div>
                <p>
                  {detail.cancellation.state} · policy{" "}
                  {detail.cancellation.policyVersion}
                </p>
                <p>
                  Maximum entitlement:{" "}
                  {money(
                    detail.cancellation.currency,
                    detail.cancellation.refundableAmount,
                  )}
                </p>
                {detail.cancellation.failureCode ? (
                  <p>Recovery code: {detail.cancellation.failureCode}</p>
                ) : null}
              </div>
            ) : (
              <p>No cancellation request recorded.</p>
            )}

            <h3>Refund</h3>
            {detail.refund ? (
              <div>
                <p>
                  {detail.refund.state} ·{" "}
                  {money(detail.refund.currency, detail.refund.refundedAmount)}{" "}
                  of {money(detail.refund.currency, detail.refund.amount)}{" "}
                  recorded
                </p>
                {detail.refund.failureCode ? (
                  <p>Recovery code: {detail.refund.failureCode}</p>
                ) : null}
              </div>
            ) : (
              <p>No refund record recorded.</p>
            )}

            <h3>Documents</h3>
            {detail.documents.length ? (
              <ul className="journey-instalments">
                {detail.documents.map((document) => (
                  <li key={`${document.kind}-${document.version ?? "missing"}`}>
                    <span>{document.kind}</span>
                    <span>
                      {document.state} · {document.malwareStatus}
                    </span>
                  </li>
                ))}
              </ul>
            ) : (
              <p>No document requirements recorded.</p>
            )}

            <h3>Visa</h3>
            {detail.visa.length ? (
              <ul className="journey-instalments">
                {detail.visa.map((visa) => (
                  <li key={visa.travellerId}>
                    <span>Traveller {visa.travellerId}</span>
                    <span>{visa.status}</span>
                  </li>
                ))}
              </ul>
            ) : (
              <p>No visa cases recorded.</p>
            )}

            <div
              className="document-row support-actions"
              aria-label="Approved recovery and navigation actions"
            >
              {detail.allowedActions.map((action) => (
                <button
                  type="button"
                  key={action.code}
                  onClick={() => void runAction(action)}
                >
                  {action.label}
                </button>
              ))}
              {detail.navigationActions.map((action) => (
                <Link key={action.code} href={action.target}>
                  {action.label}
                </Link>
              ))}
              <Link href="/operator/documents">Open document review</Link>
              <Link href="/operator/visa">Open visa processing</Link>
            </div>
          </section>
        ) : null}
      </main>
      <PublicFooter />
    </div>
  );
}

function isNavigationTarget(target?: string): target is string {
  return Boolean(target?.startsWith("/") && !target.startsWith("/api/"));
}

function money(currency: string, amount: number) {
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(amount);
}
