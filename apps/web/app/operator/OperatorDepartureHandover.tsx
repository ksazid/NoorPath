"use client";

import Link from "next/link";
import { useCallback, useState } from "react";
import { useDeferredInitialLoad } from "../../lib/use-deferred-initial-load";
import OperatorWorkspaceShell from "./OperatorWorkspaceShell";
import styles from "./OperatorDepartureHandover.module.css";

type Handover = {
  departure: {
    id: string;
    packageName: string;
    origin: string;
    departureDate: string;
    returnDate: string;
  };
  summary: {
    travellers: number;
    ready: number;
    blocked: number;
    paymentBlocked: number;
    documentBlocked: number;
    visaBlocked: number;
    accommodationBlocked: number;
  };
  canComplete: boolean;
  handover: {
    isCompleted: boolean;
    finalNote: string | null;
    completedByAccountId: string | null;
    completedAtUtc: string | null;
    version: number;
  };
  audits: Array<{
    action: string;
    note: string;
    actorAccountId: string;
    previousVersion: number;
    resultingVersion: number;
    travellerCount: number;
    blockedCount: number;
    occurredAtUtc: string;
  }>;
};

type LoadState =
  | { kind: "loading" }
  | { kind: "ready"; value: Handover }
  | { kind: "forbidden" }
  | { kind: "not-found" }
  | { kind: "error" };

const blockerLabels: Array<[keyof Handover["summary"], string]> = [
  ["paymentBlocked", "Payment"],
  ["documentBlocked", "Documents"],
  ["visaBlocked", "Visa"],
  ["accommodationBlocked", "Accommodation"],
];

export default function OperatorDepartureHandover({
  departureId,
}: {
  departureId: string;
}) {
  const [state, setState] = useState<LoadState>({ kind: "loading" });
  const [note, setNote] = useState("");
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    setState({ kind: "loading" });
    setError("");
    try {
      const response = await fetch(
        `/api/v1/operator/departures/${departureId}/handover`,
        { credentials: "include", cache: "no-store" },
      );
      if (response.status === 403) return setState({ kind: "forbidden" });
      if (response.status === 404) return setState({ kind: "not-found" });
      if (!response.ok) throw new Error();
      const value = (await response.json()) as Handover;
      setNote(value.handover.finalNote ?? "");
      setState({ kind: "ready", value });
    } catch {
      setState({ kind: "error" });
    }
  }, [departureId]);

  useDeferredInitialLoad(load);

  const complete = async () => {
    if (busy || state.kind !== "ready") return;
    setBusy(true);
    setError("");
    setMessage("");
    try {
      const response = await fetch(
        `/api/v1/operator/departures/${departureId}/handover/complete`,
        {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            finalNote: note,
            expectedVersion: state.value.handover.version,
          }),
        },
      );
      if (response.status === 409) {
        const detail = (await response.json()) as { code?: string };
        setError(
          detail.code === "handover_blocked"
            ? "Final handover is blocked. Resolve every readiness blocker in the pilgrim manifest and retry."
            : "The handover changed in another session. Refresh before retrying.",
        );
        return;
      }
      if (!response.ok) {
        setError("The final handover could not be completed. Review the note and retry.");
        return;
      }
      setMessage("Final departure handover completed.");
      await load();
    } catch {
      setError("The handover service is temporarily unavailable. Retry when connected.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <OperatorWorkspaceShell
      title="Final departure handover"
      summary="Complete the governed operational closeout only after every authoritative readiness gate is clear."
    >
      <div className={styles.workspace}>
        <Link className={styles.backLink} href={`/operator/departures/${departureId}/manifest`}>
          ← Back to pilgrim manifest
        </Link>

        {state.kind === "loading" ? (
          <section className={styles.stateCard} role="status" aria-live="polite">
            <strong>Loading final handover</strong>
            <span>Checking current departure readiness and closeout state.</span>
          </section>
        ) : null}

        {state.kind === "forbidden" ? (
          <section className={styles.stateCard}>
            <strong>Handover access unavailable</strong>
            <span>Your operator account does not have permission to complete this departure.</span>
          </section>
        ) : null}

        {state.kind === "not-found" ? (
          <section className={styles.stateCard}>
            <strong>Departure handover not found</strong>
            <span>This departure is unavailable or belongs to another operator.</span>
          </section>
        ) : null}

        {state.kind === "error" ? (
          <section className={styles.stateCard} role="alert">
            <strong>Handover unavailable</strong>
            <span>Check the connection and retry. No closeout changes were made.</span>
            <button className={styles.secondaryButton} type="button" onClick={load}>
              Retry
            </button>
          </section>
        ) : null}

        {state.kind === "ready" ? (
          <>
            <section aria-label="Departure facts">
              <h2>{state.value.departure.packageName}</h2>
              <p>
                {state.value.departure.origin} · {state.value.departure.departureDate} to{" "}
                {state.value.departure.returnDate}
              </p>
            </section>

            <section className={styles.summaryGrid} aria-label="Final readiness summary">
              <div className={styles.summaryItem}>
                <span>Travellers</span>
                <strong>{state.value.summary.travellers}</strong>
              </div>
              <div className={styles.summaryItem}>
                <span>Ready</span>
                <strong>{state.value.summary.ready}</strong>
              </div>
              <div className={styles.summaryItem}>
                <span>Blocked</span>
                <strong>{state.value.summary.blocked}</strong>
              </div>
              <div className={styles.summaryItem}>
                <span>Status</span>
                <strong>{state.value.handover.isCompleted ? "Completed" : "Open"}</strong>
              </div>
            </section>

            <section className={styles.panel} aria-label="Readiness blockers">
              <h2>Readiness gates</h2>
              <div className={styles.blockers}>
                {blockerLabels.map(([key, label]) => {
                  const count = state.value.summary[key] as number;
                  return (
                    <span
                      key={key}
                      className={`${styles.badge} ${count === 0 ? styles.ready : styles.blocked}`}
                    >
                      {label}: {count === 0 ? "clear" : `${count} blocked`}
                    </span>
                  );
                })}
              </div>
              {!state.value.canComplete && !state.value.handover.isCompleted ? (
                <p>
                  Final handover remains locked until every traveller readiness blocker is resolved.
                </p>
              ) : null}
              <Link className={styles.manifestLink} href={`/operator/departures/${departureId}/manifest`}>
                Review pilgrim manifest
              </Link>
            </section>

            {message ? (
              <div className={styles.notice} role="status" aria-live="polite">
                {message}
              </div>
            ) : null}
            {error ? (
              <div className={styles.error} role="alert">
                {error}
              </div>
            ) : null}

            <section className={styles.panel} aria-label="Complete final handover">
              <h2>{state.value.handover.isCompleted ? "Handover completed" : "Complete handover"}</h2>
              <label className={styles.field}>
                Final operational note
                <textarea
                  maxLength={500}
                  value={note}
                  disabled={state.value.handover.isCompleted}
                  onChange={(event) => setNote(event.target.value)}
                />
              </label>

              {state.value.handover.isCompleted ? (
                <p>
                  Completed {state.value.handover.completedAtUtc} by {state.value.handover.completedByAccountId}.
                  This closeout is read-only.
                </p>
              ) : (
                <div className={styles.actions}>
                  <span>Completion is immutable and recorded in the operational audit trail.</span>
                  <button
                    className={styles.button}
                    type="button"
                    disabled={!state.value.canComplete || busy || note.trim().length === 0}
                    onClick={complete}
                  >
                    {busy ? "Completing…" : "Complete final handover"}
                  </button>
                </div>
              )}
            </section>

            <section aria-label="Handover audit history">
              <h2>Recent handover activity</h2>
              {state.value.audits.length === 0 ? (
                <div className={styles.stateCard}>No handover activity recorded yet.</div>
              ) : (
                <div className={styles.auditList}>
                  {state.value.audits.map((audit, index) => (
                    <article className={styles.auditCard} key={`${audit.occurredAtUtc}-${index}`}>
                      <strong>{audit.action}</strong>
                      <span>{audit.note}</span>
                      <div className={styles.auditMeta}>
                        <span>{audit.actorAccountId}</span>
                        <span>{audit.occurredAtUtc}</span>
                        <span>v{audit.previousVersion} → v{audit.resultingVersion}</span>
                      </div>
                    </article>
                  ))}
                </div>
              )}
            </section>
          </>
        ) : null}
      </div>
    </OperatorWorkspaceShell>
  );
}
