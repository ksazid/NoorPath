"use client";

import Link from "next/link";
import { useCallback, useMemo, useState } from "react";
import { useDeferredInitialLoad } from "../../lib/use-deferred-initial-load";
import OperatorWorkspaceShell from "./OperatorWorkspaceShell";

type CatalogueItem = {
  departureId: string;
  packageTemplateId: string;
  packageName: string;
  origin: string;
  status: string;
};

type VisaItem = {
  caseId: string;
  bookingId: string;
  travellerId: string;
  status: string;
};

type OverviewState =
  | { kind: "loading" }
  | { kind: "error" }
  | { kind: "ready"; catalogue: CatalogueItem[]; visa: VisaItem[] };

export default function OperatorOverview() {
  const [state, setState] = useState<OverviewState>({ kind: "loading" });

  const load = useCallback(async () => {
    setState({ kind: "loading" });
    try {
      const [catalogueResponse, visaResponse] = await Promise.all([
        fetch("/api/v1/operator/catalogue", {
          credentials: "include",
          cache: "no-store",
        }),
        fetch("/api/v1/operator/visa", {
          credentials: "include",
          cache: "no-store",
        }),
      ]);

      if (!catalogueResponse.ok || !visaResponse.ok) throw new Error();

      const catalogue = (await catalogueResponse.json()) as {
        items: CatalogueItem[];
      };
      const visa = (await visaResponse.json()) as { items: VisaItem[] };
      setState({
        kind: "ready",
        catalogue: catalogue.items,
        visa: visa.items,
      });
    } catch {
      setState({ kind: "error" });
    }
  }, []);

  useDeferredInitialLoad(load);

  const metrics = useMemo(() => {
    if (state.kind !== "ready") return null;
    return [
      {
        label: "Packages",
        value: new Set(
          state.catalogue.map((item) => item.packageTemplateId),
        ).size,
        href: "/operator/packages",
      },
      {
        label: "Departures",
        value: state.catalogue.length,
        href: "/operator/departures",
      },
      {
        label: "Bookings in visa flow",
        value: new Set(state.visa.map((item) => item.bookingId)).size,
        href: "/operator/visa",
      },
      {
        label: "Open visa cases",
        value: state.visa.length,
        href: "/operator/visa",
      },
    ];
  }, [state]);

  return (
    <OperatorWorkspaceShell
      title="Operator administration"
      summary="Manage packages, departures, traveller readiness, and operational work within your approved operator scope."
    >
      <section
        className="account-welcome"
        aria-labelledby="operator-ready-title"
      >
        <h2 id="operator-ready-title">Your secure workspace is ready</h2>
        <p>
          Live operator-scoped information now appears here. Use the navigation
          to open the full workflow behind each metric.
        </p>
        <div className="operator-primary-actions">
          <Link className="auth-primary" href="/operator/departures/new">
            Create new draft
          </Link>
          <Link className="auth-secondary" href="/operator/visa">
            Open visa queue
          </Link>
        </div>
      </section>

      <section
        className="operator-section"
        aria-labelledby="operator-metrics-title"
      >
        <div className="operator-section-heading">
          <div>
            <p className="auth-eyebrow">Live workload</p>
            <h2 id="operator-metrics-title">Operational overview</h2>
          </div>
          {state.kind === "error" ? (
            <button
              type="button"
              className="auth-secondary"
              onClick={load}
            >
              Retry
            </button>
          ) : null}
        </div>

        <div aria-live="polite">
          {state.kind === "loading" ? <p>Loading operator activity…</p> : null}
          {state.kind === "error" ? (
            <p>Operator activity is temporarily unavailable.</p>
          ) : null}
        </div>

        {metrics ? (
          <div className="operator-metric-grid">
            {metrics.map((metric) => (
              <Link
                className="operator-metric-card"
                href={metric.href}
                key={metric.label}
              >
                <strong>{metric.value}</strong>
                <span>{metric.label}</span>
              </Link>
            ))}
          </div>
        ) : null}
      </section>

      <section
        className="operator-section"
        aria-labelledby="operator-work-title"
      >
        <p className="auth-eyebrow">Work areas</p>
        <h2 id="operator-work-title">Continue operational work</h2>
        <div className="operator-action-grid">
          <Link href="/operator/packages">
            <strong>Packages</strong>
            <span>Review the package content behind your departures.</span>
          </Link>
          <Link href="/operator/departures">
            <strong>Departures</strong>
            <span>Open published journeys or continue authoring a draft.</span>
          </Link>
          <Link href="/operator/visa">
            <strong>Visa processing</strong>
            <span>Work through traveller cases with governed transitions.</span>
          </Link>
          <Link href="/operator/support">
            <strong>Operational support</strong>
            <span>Resolve booking, payment, document, and visa exceptions.</span>
          </Link>
          <Link href="/operator/cancellations">
            <strong>Cancellations &amp; refunds</strong>
            <span>Review requests without enabling provider execution.</span>
          </Link>
          <Link href="/operator/account">
            <strong>Account access</strong>
            <span>See the operator identity and permissions in this session.</span>
          </Link>
        </div>
      </section>
    </OperatorWorkspaceShell>
  );
}
