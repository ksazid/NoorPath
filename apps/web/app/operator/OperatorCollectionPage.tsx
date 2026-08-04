"use client";

import Link from "next/link";
import { useCallback, useMemo, useState } from "react";
import { useDeferredInitialLoad } from "../../lib/use-deferred-initial-load";
import OperatorWorkspaceShell from "./OperatorWorkspaceShell";

type CatalogueItem = {
  departureId: string;
  packageTemplateId: string;
  packageVersionId: string;
  packageName: string;
  summary: string;
  origin: string;
  departureDate: string;
  returnDate: string;
  status: string;
  version: number;
  updatedAtUtc: string;
};

type CatalogueState =
  | { kind: "loading" }
  | { kind: "error" }
  | { kind: "ready"; items: CatalogueItem[] };

const dateFormatter = new Intl.DateTimeFormat("en-IN", {
  day: "numeric",
  month: "short",
  year: "numeric",
});

function displayDate(value: string) {
  return dateFormatter.format(new Date(`${value}T00:00:00Z`));
}

function statusLabel(value: string) {
  return value.replace(/([A-Z])/g, " $1").trim();
}

export default function OperatorCollectionPage({
  mode,
}: {
  mode: "packages" | "departures";
}) {
  const [state, setState] = useState<CatalogueState>({ kind: "loading" });

  const load = useCallback(async () => {
    setState({ kind: "loading" });
    try {
      const response = await fetch("/api/v1/operator/catalogue", {
        credentials: "include",
        cache: "no-store",
      });
      if (!response.ok) throw new Error();
      const body = (await response.json()) as { items: CatalogueItem[] };
      setState({ kind: "ready", items: body.items });
    } catch {
      setState({ kind: "error" });
    }
  }, []);

  useDeferredInitialLoad(load);

  const packages = useMemo(() => {
    if (state.kind !== "ready") return [];
    const grouped = new Map<
      string,
      {
        packageTemplateId: string;
        packageName: string;
        summary: string;
        departures: CatalogueItem[];
      }
    >();

    for (const item of state.items) {
      const existing = grouped.get(item.packageTemplateId);
      if (existing) existing.departures.push(item);
      else
        grouped.set(item.packageTemplateId, {
          packageTemplateId: item.packageTemplateId,
          packageName: item.packageName,
          summary: item.summary,
          departures: [item],
        });
    }

    return [...grouped.values()].sort((a, b) =>
      a.packageName.localeCompare(b.packageName),
    );
  }, [state]);

  const title = mode === "packages" ? "Packages" : "Departures";
  const summary =
    mode === "packages"
      ? "Review the package content and linked departures owned by your operator."
      : "Manage draft, review-ready, and published departures within your operator scope.";

  return (
    <OperatorWorkspaceShell title={title} summary={summary}>
      <section className="operator-section" aria-live="polite">
        <div className="operator-section-heading">
          <div>
            <p className="auth-eyebrow">Operator catalogue</p>
            <h2>
              {mode === "packages" ? "Package library" : "Departure schedule"}
            </h2>
          </div>
          <Link className="auth-primary" href="/operator/departures/new">
            Create new draft
          </Link>
        </div>

        {state.kind === "loading" ? <p>Loading {mode}…</p> : null}
        {state.kind === "error" ? (
          <div className="operator-inline-state">
            <p>{title} are temporarily unavailable.</p>
            <button
              className="auth-secondary"
              type="button"
              onClick={load}
            >
              Retry
            </button>
          </div>
        ) : null}

        {state.kind === "ready" && state.items.length === 0 ? (
          <div className="operator-empty-state">
            <h3>No {mode} yet</h3>
            <p>Create a draft to start building the operator catalogue.</p>
          </div>
        ) : null}

        {mode === "packages" && packages.length > 0 ? (
          <div className="operator-list">
            {packages.map((item) => {
              const departures = [...item.departures].sort((a, b) =>
                a.departureDate.localeCompare(b.departureDate),
              );
              const next = departures[0];
              const origins = [...new Set(departures.map((x) => x.origin))];
              return (
                <article
                  className="operator-card"
                  key={item.packageTemplateId}
                >
                  <div>
                    <p className="auth-eyebrow">Package</p>
                    <h3>{item.packageName}</h3>
                    <p>{item.summary}</p>
                  </div>
                  <dl className="operator-card-facts">
                    <div>
                      <dt>Departures</dt>
                      <dd>{departures.length}</dd>
                    </div>
                    <div>
                      <dt>Origins</dt>
                      <dd>{origins.join(", ")}</dd>
                    </div>
                    <div>
                      <dt>Next departure</dt>
                      <dd>{displayDate(next.departureDate)}</dd>
                    </div>
                  </dl>
                  <Link
                    className="auth-secondary"
                    href={`/operator/departures/${next.departureId}`}
                  >
                    Open package departure
                  </Link>
                </article>
              );
            })}
          </div>
        ) : null}

        {mode === "departures" &&
        state.kind === "ready" &&
        state.items.length > 0 ? (
          <div className="operator-list">
            {state.items.map((item) => (
              <article className="operator-card" key={item.departureId}>
                <div className="operator-card-heading">
                  <div>
                    <p className="auth-eyebrow">{item.origin}</p>
                    <h3>{item.packageName}</h3>
                  </div>
                  <span className="operator-status-badge">
                    {statusLabel(item.status)}
                  </span>
                </div>
                <p>{item.summary}</p>
                <dl className="operator-card-facts">
                  <div>
                    <dt>Departure</dt>
                    <dd>{displayDate(item.departureDate)}</dd>
                  </div>
                  <div>
                    <dt>Return</dt>
                    <dd>{displayDate(item.returnDate)}</dd>
                  </div>
                  <div>
                    <dt>Version</dt>
                    <dd>{item.version}</dd>
                  </div>
                </dl>
                <Link
                  className="auth-secondary"
                  href={`/operator/departures/${item.departureId}`}
                >
                  Open departure
                </Link>
              </article>
            ))}
          </div>
        ) : null}
      </section>
    </OperatorWorkspaceShell>
  );
}
