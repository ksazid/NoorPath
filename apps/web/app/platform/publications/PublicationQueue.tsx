"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";

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

const dateFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  year: "numeric",
});

const submittedFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  hour: "2-digit",
  minute: "2-digit",
});

function formatDate(value: string) {
  return dateFormatter.format(new Date(`${value}T00:00:00Z`));
}

function formatSubmitted(value: string) {
  return submittedFormatter.format(new Date(value));
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
    const pending = window.setTimeout(() => void load(), 0);
    return () => window.clearTimeout(pending);
  }, [load]);

  const orderedItems = useMemo(
    () =>
      [...items].sort(
        (left, right) =>
          new Date(left.submittedAtUtc).getTime() -
          new Date(right.submittedAtUtc).getTime(),
      ),
    [items],
  );

  return (
    <main className="publication-queue-page platform-approval-page">
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
        <div className="platform-approval-header-copy">
          <strong>Platform Administrator</strong>
          <span>Independent publication approval</span>
        </div>
      </header>

      <section id="publication-queue" className="publication-queue-content">
        <div className="admin-titlebar platform-approval-titlebar">
          <div>
            <span className="eyebrow">Independent approval queue</span>
            <h1>Review packages before they go live</h1>
            <p>
              Verify the operator submission, saved commercial versions and
              publication checks before approving the package for customers.
            </p>
          </div>
          {state === "ready" && (
            <span className="draft-pill platform-awaiting-pill">
              {items.length} awaiting approval
            </span>
          )}
        </div>

        <div className="platform-approval-principles" role="note">
          <article>
            <span aria-hidden="true">✓</span>
            <div>
              <strong>Independent approval</strong>
              <small>Operators cannot publish their own submitted package.</small>
            </div>
          </article>
          <article>
            <span aria-hidden="true">◇</span>
            <div>
              <strong>Exact saved versions</strong>
              <small>Catalogue, pricing and inventory are checked together.</small>
            </div>
          </article>
          <article>
            <span aria-hidden="true">↗</span>
            <div>
              <strong>Customer-visible action</strong>
              <small>Approval publishes only after every readiness check passes.</small>
            </div>
          </article>
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
            <span>No publication action was taken.</span>
            <button type="button" onClick={() => void load()}>
              Try again
            </button>
          </div>
        )}
        {state === "ready" && items.length === 0 && (
          <div className="publication-queue-state platform-empty-state" role="status">
            <span className="platform-empty-icon" aria-hidden="true">
              ✓
            </span>
            <strong>No packages are waiting</strong>
            <span>New operator submissions will appear here for independent review.</span>
          </div>
        )}
        {state === "ready" && orderedItems.length > 0 && (
          <div className="publication-queue-list platform-approval-list">
            {orderedItems.map((item, index) => (
              <article
                className="publication-queue-item platform-approval-item"
                key={item.departureId}
              >
                <div className="platform-approval-item-main">
                  <div className="platform-approval-item-meta">
                    <span className="platform-queue-number">
                      {String(index + 1).padStart(2, "0")}
                    </span>
                    <span className="platform-review-status">Awaiting review</span>
                  </div>
                  <h2>{item.packageName}</h2>
                  <p>
                    {item.origin} · {formatDate(item.departureDate)} — {formatDate(item.returnDate)}
                  </p>
                  <dl className="platform-approval-facts">
                    <div>
                      <dt>Operator</dt>
                      <dd>{item.operatorId}</dd>
                    </div>
                    <div>
                      <dt>Catalogue version</dt>
                      <dd>v{item.departureVersion}</dd>
                    </div>
                    <div>
                      <dt>Submitted</dt>
                      <dd>{formatSubmitted(item.submittedAtUtc)}</dd>
                    </div>
                  </dl>
                </div>
                <div className="platform-approval-item-action">
                  <small>Review all readiness checks before publishing.</small>
                  <Link
                    className="primary-button"
                    href={`/platform/publications/${item.departureId}`}
                  >
                    Review for publication
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
