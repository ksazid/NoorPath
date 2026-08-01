"use client";
import Link from "next/link";
import { useCallback, useState } from "react";
import { PublicHeader, PublicFooter } from "../../public-ui";
type Item = {
  id: string;
  bookingId: string;
  travellerId: string;
  kind: string;
  createdAtUtc: string;
  version: number;
};
const h = (json = false): HeadersInit => ({
  ...(json ? { "Content-Type": "application/json" } : {}),
  ...(process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY
    ? {
        "X-NoorPath-Test-Identity":
          process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY,
      }
    : {}),
});
export default function ReviewPage() {
  const [state, setState] = useState<
    | { kind: "loading" }
    | { kind: "denied" }
    | { kind: "error" }
    | { kind: "ready"; items: Item[] }
  >({ kind: "loading" });
  const load = useCallback(async () => {
    try {
      const r = await fetch("/api/v1/operator/documents", {
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
  async function review(
    i: Item,
    decision: "Approve" | "RequestCorrection" | "Reject",
  ) {
    const reason =
      decision === "Approve"
        ? null
        : window.prompt("Give the customer a clear reason");
    if (decision !== "Approve" && !reason) return;
    const r = await fetch(`/api/v1/operator/documents/${i.id}/review`, {
      method: "POST",
      credentials: "include",
      headers: h(true),
      body: JSON.stringify({ decision, reason, version: i.version }),
    });
    if (r.status === 409)
      alert("This submission changed. The queue will refresh.");
    await load();
  }
  async function view(i: Item) {
    const r = await fetch(`/api/v1/operator/documents/${i.id}/access`, {
      credentials: "include",
      headers: h(),
    });
    if (r.ok) {
      const b = (await r.json()) as { accessUrl: string };
      window.open(b.accessUrl, "_blank", "noopener,noreferrer");
    }
  }
  return (
    <div className="journey-page">
      <PublicHeader mode="detail" />
      <main id="main-content" className="journey-main">
        <nav className="package-breadcrumbs" aria-label="Breadcrumb">
          <Link href="/operator">Operator</Link>
          <span>/</span>
          <span aria-current="page">Document review</span>
        </nav>
        <p className="public-eyebrow">Authorized review</p>
        <h1>Safe documents</h1>
        <p className="journey-intro">
          Only files that passed type, signature and malware checks appear here.
        </p>
        <div aria-live="polite">
          {state.kind === "loading" ? "Loading review queue…" : null}
          {state.kind === "denied"
            ? "You do not have document review permission."
            : null}
          {state.kind === "error" ? (
            <>
              <p>Review queue temporarily unavailable.</p>
              <button className="documents-action" onClick={() => void load()}>
                Retry
              </button>
            </>
          ) : null}
        </div>
        {state.kind === "ready" && state.items.length === 0 ? (
          <section className="journey-state">
            <h2>No documents await review</h2>
            <p>The queue is up to date.</p>
          </section>
        ) : null}
        {state.kind === "ready" ? (
          <div className="documents-list">
            {state.items.map((i) => (
              <article className="documents-card" key={i.id}>
                <h2>{i.kind}</h2>
                <p>Booking {i.bookingId}</p>
                <div className="document-row">
                  <button onClick={() => void view(i)}>View safely</button>
                  <span>
                    <button onClick={() => void review(i, "Approve")}>
                      Approve
                    </button>{" "}
                    <button onClick={() => void review(i, "RequestCorrection")}>
                      Request correction
                    </button>{" "}
                    <button onClick={() => void review(i, "Reject")}>
                      Reject
                    </button>
                  </span>
                </div>
              </article>
            ))}
          </div>
        ) : null}
      </main>
      <PublicFooter />
    </div>
  );
}
