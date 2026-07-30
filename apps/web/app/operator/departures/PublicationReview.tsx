"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

type ReviewMode = "operator" | "platform";
type ReviewState =
  | "loading"
  | "ready"
  | "submitting"
  | "submitted"
  | "published"
  | "unauthenticated"
  | "forbidden"
  | "not-found"
  | "conflict"
  | "error";

type PublicationCheck = {
  key: string;
  label: string;
  passed: boolean;
  detail: string;
};

type PublicationReviewResponse = {
  departureId: string;
  operatorId: string;
  status: "draft" | "readyForReview" | "published";
  departureVersion: number;
  pricingVersion: number;
  inventoryVersion: number;
  ready: boolean;
  checks: PublicationCheck[];
  package: {
    name: string;
    summary: string;
    origin: string;
    departureDate: string;
    returnDate: string;
    makkah: Stay;
    madinah: Stay;
    travel: {
      routeSummary: string;
      details: string;
      confirmationState: string;
    };
    inclusions: string[];
    exclusions: string[];
  };
  pricing: {
    currency: string;
    version: number;
    occupancies: Array<{ occupancy: string; amount: number }>;
  } | null;
  inventory: {
    version: number;
    pools: Array<{
      occupancy: string;
      capacity: number;
      availableQuantity: number;
    }>;
  } | null;
};

type Stay = {
  hotelName: string;
  classification: string;
  distanceDisclosure: string;
  nights: number;
  confirmationState: string;
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  code?: string;
  correlationId?: string;
};

function requestHeaders(json = false): HeadersInit {
  const headers: Record<string, string> = {};
  if (json) headers["Content-Type"] = "application/json";
  const testIdentity = process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY;
  if (testIdentity) headers["X-NoorPath-Test-Identity"] = testIdentity;
  return headers;
}

function statusLabel(status: PublicationReviewResponse["status"]) {
  if (status === "readyForReview") return "Awaiting platform approval";
  if (status === "published") return "Published";
  return "Private draft";
}

function StatePage({
  state,
  detail,
  retry,
}: {
  state: Exclude<
    ReviewState,
    "ready" | "submitting" | "submitted" | "published"
  >;
  detail: string;
  retry: () => void;
}) {
  const title = {
    loading: "Preparing publication review",
    unauthenticated: "Sign in to continue",
    forbidden: "Publication access unavailable",
    "not-found": "Departure not found",
    conflict: "The departure changed",
    error: "Review temporarily unavailable",
  }[state];

  return (
    <main className="composer-state-page">
      <Link className="brand" href="/" aria-label="NoorPath home">
        <span className="brand-mark" aria-hidden="true">
          ◇
        </span>
        <span>NoorPath</span>
      </Link>
      <section className="composer-state-card" role="status" aria-live="polite">
        <span className="composer-icon" aria-hidden="true">
          {state === "loading" ? "…" : "◇"}
        </span>
        <span className="eyebrow">Review &amp; publish</span>
        <h1>{title}</h1>
        <p>{detail}</p>
        {(state === "conflict" || state === "error") && (
          <button className="primary-button" type="button" onClick={retry}>
            Reload review
          </button>
        )}
      </section>
    </main>
  );
}

function StaySummary({ city, value }: { city: string; value: Stay }) {
  return (
    <article className="publication-summary-card">
      <span>{city}</span>
      <h3>{value.hotelName}</h3>
      <p>
        {value.nights} nights
        {value.classification ? ` · ${value.classification}` : ""}
      </p>
      {value.distanceDisclosure && <small>{value.distanceDisclosure}</small>}
      <small className={`publication-fact ${value.confirmationState}`}>
        {value.confirmationState} fact
      </small>
    </article>
  );
}

export default function PublicationReview({
  departureId,
  mode,
}: {
  departureId: string;
  mode: ReviewMode;
}) {
  const [review, setReview] = useState<PublicationReviewResponse | null>(null);
  const [state, setState] = useState<ReviewState>("loading");
  const [problem, setProblem] = useState("");

  const load = useCallback(async () => {
    setState("loading");
    setProblem("");
    const path =
      mode === "operator"
        ? `/api/v1/operator/departures/${departureId}/publication-review`
        : `/api/v1/platform/publications/${departureId}`;

    try {
      const response = await fetch(path, {
        cache: "no-store",
        credentials: "include",
        headers: requestHeaders(),
      });
      if (response.status === 401) return setState("unauthenticated");
      if (response.status === 403) return setState("forbidden");
      if (response.status === 404) return setState("not-found");
      if (!response.ok) throw new Error("review unavailable");
      const body = (await response.json()) as PublicationReviewResponse;
      setReview(body);
      setState(
        body.status === "published"
          ? "published"
          : body.status === "readyForReview"
            ? "submitted"
            : "ready",
      );
    } catch {
      setProblem("Check your connection and reload this publication review.");
      setState("error");
    }
  }, [departureId, mode]);

  useEffect(() => {
    void load();
  }, [load]);

  const transition = async () => {
    if (!review) return;
    setState("submitting");
    setProblem("");
    const path =
      mode === "operator"
        ? `/api/v1/operator/departures/${departureId}/submit-review`
        : `/api/v1/platform/publications/${departureId}/publish`;

    try {
      const response = await fetch(path, {
        method: "POST",
        credentials: "include",
        headers: requestHeaders(true),
        body: JSON.stringify({
          expectedDepartureVersion: review.departureVersion,
          expectedPricingVersion: review.pricingVersion,
          expectedInventoryVersion: review.inventoryVersion,
        }),
      });
      const body = (await response.json()) as ProblemDetails;
      if (response.status === 401) return setState("unauthenticated");
      if (response.status === 403) {
        setProblem(
          body.detail ??
            "A separately authorized account must approve this publication.",
        );
        return setState("forbidden");
      }
      if (response.status === 409) {
        setProblem(
          body.detail ??
            "Catalogue, pricing, or inventory changed after this review loaded.",
        );
        return setState("conflict");
      }
      if (!response.ok) {
        setProblem(
          body.detail ?? "Resolve each failed check before continuing.",
        );
        return setState("error");
      }
      await load();
    } catch {
      setProblem("The request was not completed. Reload before trying again.");
      setState("error");
    }
  };

  if (
    !review ||
    state === "loading" ||
    state === "unauthenticated" ||
    state === "forbidden" ||
    state === "not-found" ||
    state === "conflict" ||
    state === "error"
  ) {
    return (
      <StatePage
        state={
          state === "submitting" ||
          state === "ready" ||
          state === "submitted" ||
          state === "published"
            ? "loading"
            : state
        }
        detail={
          problem ||
          {
            loading:
              "NoorPath is checking the saved catalogue, pricing, inventory, and operator status.",
            unauthenticated:
              "Use an approved account to open this protected review.",
            forbidden:
              "Your account is not authorized for this publication step.",
            "not-found":
              "This departure is unavailable or outside your permitted scope.",
            conflict: "Reload to review the latest saved versions.",
            error: "No changes were made.",
          }[
            state === "submitting" ||
            state === "ready" ||
            state === "submitted" ||
            state === "published"
              ? "loading"
              : state
          ]
        }
        retry={() => void load()}
      />
    );
  }

  const canAct =
    review.ready &&
    ((mode === "operator" && review.status === "draft") ||
      (mode === "platform" && review.status === "readyForReview"));

  return (
    <main className="admin-shell composer-shell publication-shell">
      <a className="skip-link" href="#publication-main">
        Skip to publication review
      </a>
      <aside className="admin-sidebar composer-sidebar">
        <Link
          className="brand"
          href={mode === "operator" ? "/operator" : "/platform/publications"}
          aria-label={
            mode === "operator" ? "Operator home" : "Publication queue"
          }
        >
          <span className="brand-mark" aria-hidden="true">
            ◇
          </span>
          <span>NoorPath</span>
        </Link>
        <nav aria-label={`${mode} publication navigation`}>
          <Link
            className="composer-nav-active"
            href={
              mode === "operator"
                ? `/operator/departures/${departureId}`
                : "/platform/publications"
            }
          >
            <span className="composer-icon" aria-hidden="true">
              {mode === "operator" ? "◈" : "✓"}
            </span>
            {mode === "operator" ? "Departure draft" : "Approval queue"}
          </Link>
        </nav>
        <div className="composer-access-card">
          <span className="composer-access-badge">
            {mode === "operator" ? "Operator review" : "Platform approval"}
          </span>
          <strong>{review.package.name}</strong>
          <small>{statusLabel(review.status)}</small>
        </div>
      </aside>

      <section
        id="publication-main"
        className="admin-content composer-content"
        aria-busy={state === "submitting"}
        tabIndex={-1}
      >
        <div className="admin-titlebar publication-titlebar">
          <div>
            <span className="eyebrow">
              {mode === "operator"
                ? "Operator · Submit for review"
                : "Platform · Independent approval"}
            </span>
            <h1>Review before publication</h1>
            <p>
              Confirm the exact saved facts below. This step does not redesign
              or rewrite the customer package page.
            </p>
          </div>
          <span className={`draft-pill publication-status ${review.status}`}>
            {statusLabel(review.status)}
          </span>
        </div>

        {(state === "submitted" || state === "published") && (
          <div
            className="composer-notice saved publication-notice"
            role="status"
            aria-live="polite"
          >
            <span className="composer-icon" aria-hidden="true">
              ✓
            </span>
            <div>
              <strong>
                {state === "published"
                  ? "Departure published"
                  : "Submitted for independent approval"}
              </strong>
              <span>
                {state === "published"
                  ? "The approved versions are immutable and ready for public projections."
                  : "Catalogue, pricing, and inventory authoring are now locked."}
              </span>
            </div>
          </div>
        )}

        <section className="form-card publication-readiness">
          <div className="form-card-heading">
            <span>01</span>
            <div>
              <h2>Publication readiness</h2>
              <p>Every rule must pass at the same saved version.</p>
            </div>
          </div>
          <div className="publication-check-grid">
            {review.checks.map((check) => (
              <article
                className={`publication-check ${check.passed ? "passed" : "failed"}`}
                key={check.key}
              >
                <span aria-hidden="true">{check.passed ? "✓" : "!"}</span>
                <div>
                  <strong>{check.label}</strong>
                  <small>{check.detail}</small>
                </div>
              </article>
            ))}
          </div>
        </section>

        <section className="form-card">
          <div className="form-card-heading">
            <span>02</span>
            <div>
              <h2>Customer facts preview</h2>
              <p>
                This is a factual review, not a replacement for the approved
                Package page design.
              </p>
            </div>
          </div>
          <div className="publication-package-heading">
            <div>
              <h3>{review.package.name}</h3>
              <p>{review.package.summary}</p>
            </div>
            <dl>
              <div>
                <dt>Origin</dt>
                <dd>{review.package.origin}</dd>
              </div>
              <div>
                <dt>Travel dates</dt>
                <dd>
                  {review.package.departureDate} — {review.package.returnDate}
                </dd>
              </div>
            </dl>
          </div>
          <div className="publication-stay-grid">
            <StaySummary city="Makkah" value={review.package.makkah} />
            <StaySummary city="Madinah" value={review.package.madinah} />
            <article className="publication-summary-card">
              <span>Travel</span>
              <h3>{review.package.travel.routeSummary}</h3>
              <p>
                {review.package.travel.details ||
                  "No additional travel details recorded."}
              </p>
              <small
                className={`publication-fact ${review.package.travel.confirmationState}`}
              >
                {review.package.travel.confirmationState} fact
              </small>
            </article>
          </div>
          <div className="publication-boundary-grid">
            <div>
              <h3>Included</h3>
              <ul>
                {review.package.inclusions.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </div>
            <div>
              <h3>Not included</h3>
              <ul>
                {review.package.exclusions.map((item) => (
                  <li key={item}>{item}</li>
                ))}
              </ul>
            </div>
          </div>
        </section>

        <section className="form-card">
          <div className="form-card-heading">
            <span>03</span>
            <div>
              <h2>Pricing &amp; capacity snapshot</h2>
              <p>
                Versions {review.pricingVersion} and {review.inventoryVersion}{" "}
                will be bound to this publication.
              </p>
            </div>
          </div>
          <div className="publication-commercial-table" role="table">
            <div className="publication-commercial-row heading" role="row">
              <span role="columnheader">Occupancy</span>
              <span role="columnheader">Price</span>
              <span role="columnheader">Capacity</span>
            </div>
            {(review.pricing?.occupancies ?? []).map((price) => {
              const pool = review.inventory?.pools.find(
                (item) => item.occupancy === price.occupancy,
              );
              return (
                <div
                  className="publication-commercial-row"
                  role="row"
                  key={price.occupancy}
                >
                  <strong role="cell">{price.occupancy}</strong>
                  <span role="cell">
                    {review.pricing?.currency}{" "}
                    {price.amount.toLocaleString("en")}
                  </span>
                  <span role="cell">{pool?.capacity ?? "—"}</span>
                </div>
              );
            })}
          </div>
        </section>
      </section>

      <footer className="admin-sticky-footer composer-savebar">
        <span>
          Saved versions · Catalogue {review.departureVersion} · Pricing{" "}
          {review.pricingVersion} · Inventory {review.inventoryVersion}
        </span>
        <div>
          <Link
            className="secondary-button"
            href={
              mode === "operator"
                ? `/operator/departures/${departureId}`
                : "/platform/publications"
            }
          >
            {mode === "operator" ? "Back to draft" : "Back to queue"}
          </Link>
          <button
            className="primary-button"
            type="button"
            disabled={!canAct || state === "submitting"}
            onClick={() => void transition()}
          >
            {state === "submitting"
              ? "Checking…"
              : mode === "operator"
                ? review.status === "draft"
                  ? "Submit for approval"
                  : "Submitted"
                : review.status === "readyForReview"
                  ? "Approve & publish"
                  : "Published"}
          </button>
        </div>
      </footer>
    </main>
  );
}
