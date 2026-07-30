"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

type QueueItem = {
  departureId: string;
  operatorId: string;
  packageName: string;
  origin: string;
  departureDate: string;
  returnDate: string;
  departureVersion: number;
  submittedAtUtc: string;
};

type QueueState =
  | "loading"
  | "ready"
  | "unauthenticated"
  | "forbidden"
  | "error";

function requestHeaders(): HeadersInit {
  const testIdentity = process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY;
  return testIdentity ? { "X-NoorPath-Test-Identity": testIdentity } : {};
}

export default function PublicationQueue() {
  const [items, setItems] = useState<QueueItem[]>([]);
  const [state, setState] = useState<QueueState>("loading");

  const load = useCallback(async () => {
    setState("loading");
    try {
      const response = await fetch("/api/v1/platform/publications", {
        cache: "no-store",
        credentials: "include",
        headers: requestHeaders(),
      });
      if (response.status === 401) return setState("unauthenticated");
      if (response.status === 403) return setState("forbidden");
      if (!response.ok) throw new Error("queue unavailable");
      const body = (await response.json()) as { items: QueueItem[] };
      setItems(body.items);
      setState("ready");
    } catch {
      setState("error");
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <main className="publication-queue-page">
      <a className="skip-link" href="#publication-queue">
        Skip to publication queue
      </a>
      <header className="publication-queue-header">
        <Link className="brand" href="/" aria-label="NoorPath home">
          <span className="brand-mark" aria-hidden="true">
            ◇
          </span>
          <span>NoorPath</span>
        </Link>
        <span>Platform publication approval</span>
      </header>
      <section id="publication-queue" className="publication-queue-content">
        <div className="admin-titlebar">
          <div>
            <span className="eyebrow">Independent approval queue</span>
            <h1>Review submitted departures</h1>
            <p>
              Publication approvers verify the exact submitted versions before
              making a departure public.
            </p>
          </div>
          {state === "ready" && (
            <span className="draft-pill">{items.length} awaiting approval</span>
          )}
        </div>

        {state === "loading" && (
          <div className="publication-queue-state" role="status">
            Checking the approval queue…
          </div>
        )}
        {state === "unauthenticated" && (
          <div className="publication-queue-state" role="alert">
            <strong>Sign in required</strong>
            <span>Use an authorized platform publication account.</span>
          </div>
        )}
        {state === "forbidden" && (
          <div className="publication-queue-state" role="alert">
            <strong>Publication access unavailable</strong>
            <span>Your account is not configured as a platform approver.</span>
          </div>
        )}
        {state === "error" && (
          <div className="publication-queue-state" role="alert">
            <strong>Queue temporarily unavailable</strong>
            <button type="button" onClick={() => void load()}>
              Try again
            </button>
          </div>
        )}
        {state === "ready" && items.length === 0 && (
          <div className="publication-queue-state" role="status">
            <strong>No departures are waiting</strong>
            <span>New operator submissions will appear here.</span>
          </div>
        )}
        {state === "ready" && items.length > 0 && (
          <div className="publication-queue-list">
            {items.map((item) => (
              <article
                className="publication-queue-item"
                key={item.departureId}
              >
                <div>
                  <span>{item.operatorId}</span>
                  <h2>{item.packageName}</h2>
                  <p>
                    {item.origin} · {item.departureDate} — {item.returnDate}
                  </p>
                </div>
                <div>
                  <small>
                    Submitted{" "}
                    {new Date(item.submittedAtUtc).toLocaleDateString()}
                  </small>
                  <Link
                    className="primary-button"
                    href={`/platform/publications/${item.departureId}`}
                  >
                    Open review
                  </Link>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </main>
  );
}
