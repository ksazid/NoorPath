"use client";
import Link from "next/link";
import { useCallback, useState } from "react";
import { useDeferredInitialLoad } from "../../../lib/use-deferred-initial-load";
import { PublicFooter, PublicHeader } from "../../public-ui";
type Item = {
  caseId: string;
  bookingId: string;
  travellerId: string;
  status: string;
  updatedAtUtc: string;
  version: number;
};
type Detail = Item & {
  customerAction?: string;
  allowedTransitions: string[];
  history: {
    previousStatus: string;
    newStatus: string;
    reason?: string;
    version: number;
    occurredAtUtc: string;
  }[];
};
const h = (json = false): HeadersInit => ({
  ...(json && { "Content-Type": "application/json" }),
  ...(process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY
    ? {
        "X-NoorPath-Test-Identity":
          process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY,
      }
    : {}),
});
export default function VisaQueue() {
  const [state, setState] = useState<
    | { kind: "loading" }
    | { kind: "denied" }
    | { kind: "error" }
    | { kind: "ready"; items: Item[] }
  >({ kind: "loading" });
  const [detail, setDetail] = useState<Detail | null>(null);
  const [message, setMessage] = useState("");
  const load = useCallback(async () => {
    setState({ kind: "loading" });
    try {
      const r = await fetch("/api/v1/operator/visa", {
        credentials: "include",
        cache: "no-store",
        headers: h(),
      });
      if (r.status === 403) {
        setState({ kind: "denied" });
        return;
      }
      if (!r.ok) throw new Error();
      setState({
        kind: "ready",
        items: ((await r.json()) as { items: Item[] }).items,
      });
    } catch {
      setState({ kind: "error" });
    }
  }, []);
  useDeferredInitialLoad(load);
  async function open(i: Item) {
    const r = await fetch(`/api/v1/operator/visa/${i.caseId}`, {
      credentials: "include",
      headers: h(),
    });
    if (r.ok) setDetail((await r.json()) as Detail);
  }
  async function transition(next: string) {
    if (!detail) return;
    const required = next === "ActionRequired" || next === "Rejected";
    const reason = required
      ? window.prompt("Give a clear customer-safe reason")
      : null;
    if (required && !reason) return;
    const r = await fetch(
      `/api/v1/operator/visa/${detail.caseId}/transitions`,
      {
        method: "POST",
        credentials: "include",
        headers: h(true),
        body: JSON.stringify({ status: next, reason, version: detail.version }),
      },
    );
    if (r.status === 409) {
      setMessage(
        "This case changed or is not ready. Refresh before trying again.",
      );
      await open(detail);
      return;
    }
    if (!r.ok) {
      setMessage(
        "The transition could not be saved. Check the required reason and retry.",
      );
      return;
    }
    setMessage("Visa status saved with audit history.");
    setDetail(null);
    await load();
  }
  return (
    <div className="journey-page">
      <PublicHeader mode="detail" />
      <main id="main-content" className="journey-main">
        <nav className="package-breadcrumbs" aria-label="Breadcrumb">
          <Link href="/operator">Operator</Link>
          <span>/</span>
          <span aria-current="page">Visa processing</span>
        </nav>
        <p className="public-eyebrow">Authorized operations</p>
        <h1>Visa processing queue</h1>
        <p className="journey-intro">
          Review one traveller at a time. Every status change is governed and
          audited.
        </p>
        <div aria-live="polite">
          {message}
          {state.kind === "loading" ? "Loading visa queue…" : null}
          {state.kind === "denied"
            ? "You do not have visa processing permission."
            : null}
          {state.kind === "error" ? (
            <>
              <p>Visa queue temporarily unavailable.</p>
              <button onClick={() => void load()}>Retry</button>
            </>
          ) : null}
        </div>
        {state.kind === "ready" && state.items.length === 0 ? (
          <section className="journey-state">
            <h2>No actionable visa cases</h2>
            <p>The queue is up to date.</p>
          </section>
        ) : null}
        {state.kind === "ready" ? (
          <div className="documents-list">
            {state.items.map((i) => (
              <article className="documents-card" key={i.caseId}>
                <p className="public-eyebrow">{i.status}</p>
                <h2>Booking {i.bookingId}</h2>
                <p>Traveller {i.travellerId}</p>
                <button onClick={() => void open(i)}>Open case</button>
              </article>
            ))}
          </div>
        ) : null}
        {detail ? (
          <section className="journey-panel" aria-labelledby="visa-case-title">
            <h2 id="visa-case-title">Case status: {detail.status}</h2>
            <div
              className="document-row"
              aria-label="Allowed status transitions"
            >
              {detail.allowedTransitions.map((next) => (
                <button key={next} onClick={() => void transition(next)}>
                  Move to {next.replace(/([A-Z])/g, " $1").trim()}
                </button>
              ))}
            </div>
            <h3>Audit history</h3>
            {detail.history.length ? (
              <ol className="journey-instalments">
                {detail.history.map((x) => (
                  <li key={x.version}>
                    <span>
                      {x.previousStatus} → {x.newStatus}
                      <small>
                        <time dateTime={x.occurredAtUtc}>
                          {new Date(x.occurredAtUtc).toLocaleString("en-IN")}
                        </time>
                      </small>
                    </span>
                    <span>{x.reason ?? "Status advanced"}</span>
                  </li>
                ))}
              </ol>
            ) : (
              <p>No transitions recorded yet.</p>
            )}
          </section>
        ) : null}
      </main>
      <PublicFooter />
    </div>
  );
}
