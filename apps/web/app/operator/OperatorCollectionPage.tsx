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

type PackageStatus =
  "draft" | "readyForReview" | "published" | "closed" | "other";

type PackageGroup = {
  packageTemplateId: string;
  packageName: string;
  summary: string;
  departures: CatalogueItem[];
  lifecycle: PackageStatus;
};

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

function normalizeStatus(value: string): PackageStatus {
  const status = value.toLowerCase();
  if (status === "draft") return "draft";
  if (status === "readyforreview" || status === "ready_for_review") {
    return "readyForReview";
  }
  if (status === "published") return "published";
  if (status === "closed" || status === "archived") return "closed";
  return "other";
}

function packageLifecycle(departures: CatalogueItem[]): PackageStatus {
  const statuses = departures.map((item) => normalizeStatus(item.status));
  if (statuses.includes("draft")) return "draft";
  if (statuses.includes("readyForReview")) return "readyForReview";
  if (statuses.includes("published")) return "published";
  if (statuses.every((status) => status === "closed")) return "closed";
  return "other";
}

function packageStatusLabel(value: PackageStatus) {
  if (value === "readyForReview") return "Awaiting approval";
  if (value === "published") return "Published";
  if (value === "closed") return "Closed";
  if (value === "draft") return "Draft";
  return "Active";
}

function packagePrimaryAction(item: CatalogueItem) {
  const status = normalizeStatus(item.status);
  if (status === "readyForReview") {
    return {
      href: `/operator/departures/${item.departureId}/review`,
      label: "View approval status",
    };
  }
  if (status === "published") {
    return {
      href: `/packages/${item.departureId}`,
      label: "View customer page",
    };
  }
  return {
    href: `/operator/departures/${item.departureId}`,
    label: "Continue setup",
  };
}

export default function OperatorCollectionPage({
  mode,
}: {
  mode: "packages" | "departures";
}) {
  const [state, setState] = useState<CatalogueState>({ kind: "loading" });
  const [query, setQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<"all" | PackageStatus>(
    "all",
  );

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

  const packages = useMemo<PackageGroup[]>(() => {
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

    return [...grouped.values()]
      .map((item) => ({
        ...item,
        lifecycle: packageLifecycle(item.departures),
      }))
      .sort((a, b) => a.packageName.localeCompare(b.packageName));
  }, [state]);

  const filteredPackages = useMemo(() => {
    const needle = query.trim().toLowerCase();
    return packages.filter((item) => {
      if (statusFilter !== "all" && item.lifecycle !== statusFilter)
        return false;
      if (!needle) return true;
      return (
        item.packageName.toLowerCase().includes(needle) ||
        item.summary.toLowerCase().includes(needle) ||
        item.departures.some((departure) =>
          departure.origin.toLowerCase().includes(needle),
        )
      );
    });
  }, [packages, query, statusFilter]);

  const packageCounts = useMemo(() => {
    return {
      total: packages.length,
      draft: packages.filter((item) => item.lifecycle === "draft").length,
      awaiting: packages.filter((item) => item.lifecycle === "readyForReview")
        .length,
      published: packages.filter((item) => item.lifecycle === "published")
        .length,
    };
  }, [packages]);

  const title = mode === "packages" ? "Packages" : "Departures";
  const summary =
    mode === "packages"
      ? "Create, continue, preview and monitor package publication from one operator workspace."
      : "Manage draft, review-ready, and published departures within your operator scope.";
  const createHref =
    mode === "packages" ? "/operator/packages/new" : "/operator/departures/new";

  return (
    <OperatorWorkspaceShell title={title} summary={summary}>
      <section className="operator-section" aria-live="polite">
        <div className="operator-section-heading">
          <div>
            <p className="auth-eyebrow">Operator catalogue</p>
            <h2>
              {mode === "packages"
                ? "Package management"
                : "Departure schedule"}
            </h2>
          </div>
          <Link className="auth-primary" href={createHref}>
            {mode === "packages" ? "Create package draft" : "Create departure"}
          </Link>
        </div>

        {state.kind === "loading" ? <p>Loading {mode}…</p> : null}
        {state.kind === "error" ? (
          <div className="operator-inline-state">
            <p>{title} are temporarily unavailable.</p>
            <button className="auth-secondary" type="button" onClick={load}>
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
          <>
            <div
              className="operator-package-metrics"
              aria-label="Package status summary"
            >
              <div>
                <strong>{packageCounts.total}</strong>
                <span>All packages</span>
              </div>
              <div>
                <strong>{packageCounts.draft}</strong>
                <span>Draft</span>
              </div>
              <div>
                <strong>{packageCounts.awaiting}</strong>
                <span>Awaiting approval</span>
              </div>
              <div>
                <strong>{packageCounts.published}</strong>
                <span>Published</span>
              </div>
            </div>

            <div className="operator-package-toolbar">
              <label>
                <span>Search packages</span>
                <input
                  type="search"
                  value={query}
                  placeholder="Package name or origin"
                  onChange={(event) => setQuery(event.target.value)}
                />
              </label>
              <label>
                <span>Status</span>
                <select
                  value={statusFilter}
                  onChange={(event) =>
                    setStatusFilter(event.target.value as "all" | PackageStatus)
                  }
                >
                  <option value="all">All statuses</option>
                  <option value="draft">Draft</option>
                  <option value="readyForReview">Awaiting approval</option>
                  <option value="published">Published</option>
                  <option value="closed">Closed</option>
                </select>
              </label>
            </div>

            {filteredPackages.length === 0 ? (
              <div className="operator-empty-state">
                <h3>No packages match this view</h3>
                <p>Change the search or status filter to see other packages.</p>
              </div>
            ) : (
              <div className="operator-list operator-package-list">
                {filteredPackages.map((item) => {
                  const departures = [...item.departures].sort((a, b) =>
                    a.departureDate.localeCompare(b.departureDate),
                  );
                  const next = departures[0];
                  const origins = [...new Set(departures.map((x) => x.origin))];
                  const action = packagePrimaryAction(next);
                  const publishedCount = departures.filter(
                    (departure) =>
                      normalizeStatus(departure.status) === "published",
                  ).length;
                  return (
                    <article
                      className="operator-card operator-package-card"
                      key={item.packageTemplateId}
                    >
                      <div className="operator-card-heading">
                        <div>
                          <p className="auth-eyebrow">Package</p>
                          <h3>{item.packageName}</h3>
                          <p>{item.summary}</p>
                        </div>
                        <span
                          className={`operator-status-badge is-${item.lifecycle}`}
                        >
                          {packageStatusLabel(item.lifecycle)}
                        </span>
                      </div>

                      <dl className="operator-card-facts operator-package-facts">
                        <div>
                          <dt>Departures</dt>
                          <dd>{departures.length}</dd>
                        </div>
                        <div>
                          <dt>Published</dt>
                          <dd>{publishedCount}</dd>
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

                      <div className="operator-package-departures">
                        {departures.slice(0, 3).map((departure) => (
                          <div key={departure.departureId}>
                            <span>{displayDate(departure.departureDate)}</span>
                            <strong>{departure.origin}</strong>
                            <small>{statusLabel(departure.status)}</small>
                          </div>
                        ))}
                        {departures.length > 3 ? (
                          <span className="operator-package-more">
                            +{departures.length - 3} more departure
                            {departures.length - 3 === 1 ? "" : "s"}
                          </span>
                        ) : null}
                      </div>

                      <div className="operator-card-actions operator-package-actions">
                        <Link className="auth-primary" href={action.href}>
                          {action.label}
                        </Link>
                        <Link
                          className="auth-secondary"
                          href={`/operator/departures/${next.departureId}/preview`}
                        >
                          Preview
                        </Link>
                        <Link
                          className="auth-secondary"
                          href={`/operator/packages/new?cloneFrom=${next.departureId}`}
                        >
                          Duplicate package
                        </Link>
                      </div>
                    </article>
                  );
                })}
              </div>
            )}
          </>
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
                <div className="operator-card-actions">
                  <Link
                    className="auth-secondary"
                    href={`/operator/departures/${item.departureId}`}
                  >
                    Open departure
                  </Link>
                  <Link
                    className="auth-secondary"
                    href={`/operator/departures/${item.departureId}/manifest`}
                  >
                    Pilgrim manifest
                  </Link>
                </div>
              </article>
            ))}
          </div>
        ) : null}
      </section>
    </OperatorWorkspaceShell>
  );
}
