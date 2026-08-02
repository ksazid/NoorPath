"use client";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useState } from "react";
import { useDeferredInitialLoad } from "../../../../lib/use-deferred-initial-load";
import { PublicFooter, PublicHeader } from "../../../public-ui";
type Traveller = {
  travellerId: string;
  fullName: string;
  status: string;
  code: string;
  updatedAtUtc: string;
  requiredAction?: string;
};
const headers = (): HeadersInit =>
  process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY
    ? {
        "X-NoorPath-Test-Identity":
          process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY,
      }
    : {};
export default function VisaPage() {
  const { bookingId } = useParams<{ bookingId: string }>();
  const [state, setState] = useState<
    | { kind: "loading" }
    | { kind: "error" }
    | { kind: "ready"; travellers: Traveller[] }
  >({ kind: "loading" });
  const load = useCallback(async () => {
    setState({ kind: "loading" });
    try {
      const r = await fetch(`/api/v1/bookings/${bookingId}/visa`, {
        credentials: "include",
        cache: "no-store",
        headers: headers(),
      });
      if (!r.ok) throw new Error();
      setState({
        kind: "ready",
        travellers: ((await r.json()) as { travellers: Traveller[] })
          .travellers,
      });
    } catch {
      setState({ kind: "error" });
    }
  }, [bookingId]);
  useDeferredInitialLoad(load);
  return (
    <div className="journey-page">
      <PublicHeader mode="detail" />
      <main id="main-content" className="journey-main">
        <nav className="package-breadcrumbs" aria-label="Breadcrumb">
          <Link href={`/bookings/${bookingId}/journey`}>My Journey</Link>
          <span>/</span>
          <span aria-current="page">Visa status</span>
        </nav>
        <p className="public-eyebrow">Traveller readiness</p>
        <h1>Visa progress</h1>
        <p className="journey-intro">
          Truthful status from your operator. Approval appears only after it is
          recorded.
        </p>
        <div aria-live="polite">
          {state.kind === "loading" ? (
            <section className="journey-state">
              <h2>Checking visa progress</h2>
              <p>Loading the latest operator updates.</p>
            </section>
          ) : null}
          {state.kind === "error" ? (
            <section className="journey-state">
              <h2>Visa status temporarily unavailable</h2>
              <p>Your case is unchanged. Please retry.</p>
              <button onClick={() => void load()}>Retry</button>
            </section>
          ) : null}
        </div>
        {state.kind === "ready" && state.travellers.length === 0 ? (
          <section className="journey-state">
            <h2>No travellers found</h2>
            <p>Contact support with your booking reference.</p>
          </section>
        ) : null}
        {state.kind === "ready" ? (
          <div className="documents-list">
            {state.travellers.map((t) => (
              <article className="documents-card" key={t.travellerId}>
                <p className="public-eyebrow">{t.status}</p>
                <h2>{t.fullName}</h2>
                <p>
                  Updated{" "}
                  <time dateTime={t.updatedAtUtc}>
                    {new Date(t.updatedAtUtc).toLocaleString("en-IN")}
                  </time>
                </p>
                {t.requiredAction ? (
                  <aside className="document-help">
                    <strong>Action required</strong>
                    <p>{t.requiredAction}</p>
                    <Link href={`/bookings/${bookingId}/documents`}>
                      Manage documents
                    </Link>
                  </aside>
                ) : null}
                {t.code === "Rejected" ? (
                  <p>
                    Contact NoorPath support for clear next steps. This status
                    is not legal advice.
                  </p>
                ) : null}
              </article>
            ))}
          </div>
        ) : null}
      </main>
      <PublicFooter />
    </div>
  );
}
