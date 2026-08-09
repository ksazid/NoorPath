"use client";

import Link from "next/link";
import { useCallback, useMemo, useState } from "react";
import { useDeferredInitialLoad } from "../../lib/use-deferred-initial-load";
import OperatorWorkspaceShell from "./OperatorWorkspaceShell";
import styles from "./OperatorDepartureManifest.module.css";

type ManifestOperation = {
  note: string | null;
  isAcknowledged: boolean;
  version: number;
  actorAccountId: string;
  updatedAtUtc: string;
};

type ManifestItem = {
  bookingId: string;
  bookingReference: string;
  travellerId: string;
  position: number;
  fullName: string;
  dateOfBirth: string;
  readiness: "ready" | "blocked";
  blockers: string[];
  payment: { ready: boolean; paid: number; total: number; currency: string };
  documents: { ready: boolean; required: number };
  visa: { ready: boolean; status: string };
  accommodation: {
    ready: boolean;
    makkahAssigned: boolean;
    madinahAssigned: boolean;
  };
  operation: ManifestOperation | null;
};

type Manifest = {
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
  fulfilment: {
    groupLeaderName: string | null;
    version: number;
    isCompleted: boolean;
  };
  items: ManifestItem[];
};

type LoadState =
  | { kind: "loading" }
  | { kind: "ready"; manifest: Manifest }
  | { kind: "forbidden" }
  | { kind: "not-found" }
  | { kind: "error" };

type Draft = { note: string; isAcknowledged: boolean };

const blockerLabels: Record<string, string> = {
  payment: "Payment outstanding",
  documents: "Documents incomplete",
  visa: "Visa not approved",
  accommodation: "Room assignment incomplete",
};

export default function OperatorDepartureManifest({
  departureId,
}: {
  departureId: string;
}) {
  const [state, setState] = useState<LoadState>({ kind: "loading" });
  const [query, setQuery] = useState("");
  const [filter, setFilter] = useState("all");
  const [drafts, setDrafts] = useState<Record<string, Draft>>({});
  const [busy, setBusy] = useState("");
  const [leaderBusy, setLeaderBusy] = useState(false);
  const [groupLeaderName, setGroupLeaderName] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    setState({ kind: "loading" });
    setError("");
    try {
      const response = await fetch(
        `/api/v1/operator/departures/${departureId}/manifest`,
        { credentials: "include", cache: "no-store" },
      );
      if (response.status === 403) return setState({ kind: "forbidden" });
      if (response.status === 404) return setState({ kind: "not-found" });
      if (!response.ok) throw new Error();
      const manifest = (await response.json()) as Manifest;
      setGroupLeaderName(manifest.fulfilment.groupLeaderName ?? "");
      setDrafts(
        Object.fromEntries(
          manifest.items.map((item) => [
            item.travellerId,
            {
              note: item.operation?.note ?? "",
              isAcknowledged: item.operation?.isAcknowledged ?? false,
            },
          ]),
        ),
      );
      setState({ kind: "ready", manifest });
    } catch {
      setState({ kind: "error" });
    }
  }, [departureId]);

  useDeferredInitialLoad(load);

  const visibleItems = useMemo(() => {
    if (state.kind !== "ready") return [];
    const needle = query.trim().toLowerCase();
    return state.manifest.items.filter((item) => {
      const matchesText =
        !needle ||
        item.fullName.toLowerCase().includes(needle) ||
        item.bookingReference.toLowerCase().includes(needle);
      const matchesFilter =
        filter === "all" ||
        item.readiness === filter ||
        item.blockers.includes(filter);
      return matchesText && matchesFilter;
    });
  }, [filter, query, state]);

  const saveOperation = async (item: ManifestItem) => {
    if (busy) return;
    const draft = drafts[item.travellerId] ?? {
      note: "",
      isAcknowledged: false,
    };
    setBusy(item.travellerId);
    setError("");
    setMessage("");
    try {
      const response = await fetch(
        `/api/v1/operator/departures/${departureId}/manifest/travellers/${item.travellerId}/operations`,
        {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            note: draft.note,
            isAcknowledged: draft.isAcknowledged,
            expectedVersion: item.operation?.version ?? 0,
          }),
        },
      );
      if (response.status === 409) {
        setError(
          "This traveller operation changed in another session. Refresh the manifest before retrying.",
        );
        return;
      }
      if (!response.ok) {
        setError(
          "The operational note could not be saved. Review it and retry.",
        );
        return;
      }
      setMessage(`Operational update saved for ${item.fullName}.`);
      await load();
    } catch {
      setError(
        "The manifest is temporarily unavailable. Retry when connected.",
      );
    } finally {
      setBusy("");
    }
  };

  const saveGroupLeader = async (name: string | null) => {
    if (leaderBusy || state.kind !== "ready") return;
    setLeaderBusy(true);
    setError("");
    setMessage("");
    try {
      const response = await fetch(
        `/api/v1/operator/departures/${departureId}/manifest/group-leader`,
        {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            name,
            expectedVersion: state.manifest.fulfilment.version,
          }),
        },
      );
      if (response.status === 409) {
        const detail = (await response.json()) as { code?: string };
        setError(
          detail.code === "handover_completed"
            ? "The final handover is completed, so the accompanying group leader can no longer be changed."
            : "Departure fulfilment changed in another session. Refresh before retrying.",
        );
        return;
      }
      if (!response.ok) {
        setError(
          "The accompanying group leader could not be saved. Review the name and retry.",
        );
        return;
      }
      setMessage(
        name?.trim()
          ? "Accompanying group leader saved."
          : "Accompanying group leader cleared.",
      );
      await load();
    } catch {
      setError(
        "Departure fulfilment is temporarily unavailable. Retry when connected.",
      );
    } finally {
      setLeaderBusy(false);
    }
  };

  return (
    <OperatorWorkspaceShell
      title="Pilgrim manifest"
      summary="Review authoritative departure readiness and record operator follow-up without changing payment, document, visa or accommodation source states."
    >
      <div className={styles.workspace}>
        <Link
          className={styles.backLink}
          href={`/operator/departures/${departureId}`}
        >
          ← Back to departure
        </Link>

        {state.kind === "loading" ? (
          <section
            className={styles.stateCard}
            role="status"
            aria-live="polite"
          >
            <strong>Loading pilgrim manifest</strong>
            <span>Checking the latest operational readiness.</span>
          </section>
        ) : null}

        {state.kind === "forbidden" ? (
          <section className={styles.stateCard}>
            <strong>Manifest access unavailable</strong>
            <span>
              Your operator account does not have permission to view this
              departure.
            </span>
          </section>
        ) : null}

        {state.kind === "not-found" ? (
          <section className={styles.stateCard}>
            <strong>Departure manifest not found</strong>
            <span>
              This departure is unavailable or belongs to another operator.
            </span>
          </section>
        ) : null}

        {state.kind === "error" ? (
          <section className={styles.stateCard} role="alert">
            <strong>Manifest unavailable</strong>
            <span>
              Check the connection and retry. No operational changes were made.
            </span>
            <button
              className={styles.secondaryButton}
              type="button"
              onClick={load}
            >
              Retry
            </button>
          </section>
        ) : null}

        {state.kind === "ready" ? (
          <>
            <section aria-label="Departure summary">
              <div className={styles.actions}>
                <div>
                  <h2>{state.manifest.departure.packageName}</h2>
                </div>
                <Link
                  className={styles.secondaryButton}
                  href={`/operator/departures/${departureId}/handover`}
                >
                  Final handover
                </Link>
              </div>
              <p>
                {state.manifest.departure.origin} ·{" "}
                {state.manifest.departure.departureDate} to{" "}
                {state.manifest.departure.returnDate}
              </p>
            </section>

            <section
              className={styles.fulfilmentPanel}
              aria-label="Departure fulfilment"
            >
              <div className={styles.fulfilmentHeader}>
                <div>
                  <span className={styles.eyebrow}>Departure fulfilment</span>
                  <h2>{state.manifest.departure.packageName}</h2>
                  <p>
                    Keep the package being delivered and the accompanying group
                    leader visible during operations.
                  </p>
                </div>
                <Link
                  className={styles.secondaryButton}
                  href={`/operator/departures/${departureId}/preview`}
                >
                  View package being fulfilled
                </Link>
              </div>
              <div className={styles.leaderRow}>
                <label className={styles.field}>
                  Accompanying group leader
                  <input
                    maxLength={120}
                    value={groupLeaderName}
                    disabled={
                      state.manifest.fulfilment.isCompleted || leaderBusy
                    }
                    onChange={(event) => setGroupLeaderName(event.target.value)}
                    placeholder="Add group leader name"
                  />
                  <span className={styles.fieldHint}>
                    Operational contact only. This does not add a booked
                    traveller.
                  </span>
                </label>
                <div className={styles.leaderActions}>
                  <button
                    className={styles.button}
                    type="button"
                    disabled={
                      state.manifest.fulfilment.isCompleted ||
                      leaderBusy ||
                      !groupLeaderName.trim()
                    }
                    onClick={() => saveGroupLeader(groupLeaderName)}
                  >
                    {leaderBusy
                      ? "Saving…"
                      : state.manifest.fulfilment.groupLeaderName
                        ? "Update group leader"
                        : "Save group leader"}
                  </button>
                  {state.manifest.fulfilment.groupLeaderName ? (
                    <button
                      className={styles.secondaryButton}
                      type="button"
                      disabled={
                        state.manifest.fulfilment.isCompleted || leaderBusy
                      }
                      onClick={() => saveGroupLeader(null)}
                    >
                      Clear
                    </button>
                  ) : null}
                </div>
              </div>
              {state.manifest.fulfilment.isCompleted ? (
                <p className={styles.fieldHint}>
                  Final handover is complete. Departure fulfilment metadata is
                  read-only.
                </p>
              ) : null}
            </section>

            <section
              className={styles.summaryGrid}
              aria-label="Manifest readiness summary"
            >
              <div className={styles.summaryItem}>
                <span>Travellers</span>
                <strong>{state.manifest.summary.travellers}</strong>
              </div>
              <div className={styles.summaryItem}>
                <span>Ready</span>
                <strong>{state.manifest.summary.ready}</strong>
              </div>
              <div className={styles.summaryItem}>
                <span>Blocked</span>
                <strong>{state.manifest.summary.blocked}</strong>
              </div>
              <div className={styles.summaryItem}>
                <span>Room blockers</span>
                <strong>{state.manifest.summary.accommodationBlocked}</strong>
              </div>
            </section>

            <section className={styles.toolbar} aria-label="Manifest filters">
              <div className={styles.filterGroup}>
                <label className={styles.field}>
                  Search travellers
                  <input
                    type="search"
                    value={query}
                    onChange={(event) => setQuery(event.target.value)}
                    placeholder="Name or booking reference"
                  />
                </label>
                <label className={styles.field}>
                  Readiness filter
                  <select
                    value={filter}
                    onChange={(event) => setFilter(event.target.value)}
                  >
                    <option value="all">All travellers</option>
                    <option value="ready">Ready</option>
                    <option value="blocked">Blocked</option>
                    <option value="payment">Payment blocker</option>
                    <option value="documents">Document blocker</option>
                    <option value="visa">Visa blocker</option>
                    <option value="accommodation">Accommodation blocker</option>
                  </select>
                </label>
              </div>
              <button
                className={styles.secondaryButton}
                type="button"
                onClick={load}
              >
                Refresh readiness
              </button>
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

            {visibleItems.length === 0 ? (
              <section className={styles.stateCard}>
                <strong>No travellers match this view</strong>
                <span>Clear the search or change the readiness filter.</span>
              </section>
            ) : (
              <section
                className={styles.list}
                aria-label="Pilgrim manifest travellers"
              >
                {visibleItems.map((item) => {
                  const draft = drafts[item.travellerId] ?? {
                    note: "",
                    isAcknowledged: false,
                  };
                  return (
                    <article className={styles.card} key={item.travellerId}>
                      <div className={styles.cardHeader}>
                        <div>
                          <h2>{item.fullName}</h2>
                          <div className={styles.meta}>
                            <span>{item.bookingReference}</span>
                            <span>DOB {item.dateOfBirth}</span>
                          </div>
                        </div>
                        <span
                          className={`${styles.badge} ${
                            item.readiness === "ready"
                              ? styles.ready
                              : styles.blocked
                          }`}
                        >
                          {item.readiness === "ready"
                            ? "Operationally ready"
                            : "Blocked"}
                        </span>
                      </div>

                      <div
                        className={styles.statusGrid}
                        aria-label={`${item.fullName} readiness`}
                      >
                        {item.readiness === "ready" ? (
                          <span className={`${styles.badge} ${styles.ready}`}>
                            All readiness gates clear
                          </span>
                        ) : (
                          item.blockers.map((blocker) => (
                            <span
                              className={`${styles.badge} ${styles.blocked}`}
                              key={blocker}
                            >
                              {blockerLabels[blocker] ?? blocker}
                            </span>
                          ))
                        )}
                      </div>

                      <div className={styles.statusGrid}>
                        <span
                          className={`${styles.badge} ${item.payment.ready ? styles.ready : styles.neutral}`}
                        >
                          Payment {item.payment.ready ? "ready" : "pending"}
                        </span>
                        <span
                          className={`${styles.badge} ${item.documents.ready ? styles.ready : styles.neutral}`}
                        >
                          Documents {item.documents.ready ? "ready" : "pending"}
                        </span>
                        <span
                          className={`${styles.badge} ${item.visa.ready ? styles.ready : styles.neutral}`}
                        >
                          Visa {item.visa.ready ? "approved" : "pending"}
                        </span>
                        <span
                          className={`${styles.badge} ${item.accommodation.ready ? styles.ready : styles.neutral}`}
                        >
                          Accommodation{" "}
                          {item.accommodation.ready ? "ready" : "pending"}
                        </span>
                      </div>

                      <label className={styles.field}>
                        Operational note
                        <textarea
                          maxLength={500}
                          value={draft.note}
                          onChange={(event) =>
                            setDrafts((current) => ({
                              ...current,
                              [item.travellerId]: {
                                ...draft,
                                note: event.target.value,
                              },
                            }))
                          }
                        />
                      </label>

                      <div className={styles.actions}>
                        <label>
                          <input
                            type="checkbox"
                            checked={draft.isAcknowledged}
                            onChange={(event) =>
                              setDrafts((current) => ({
                                ...current,
                                [item.travellerId]: {
                                  ...draft,
                                  isAcknowledged: event.target.checked,
                                },
                              }))
                            }
                          />{" "}
                          Follow-up acknowledged
                        </label>
                        <button
                          className={styles.button}
                          type="button"
                          disabled={busy === item.travellerId}
                          onClick={() => saveOperation(item)}
                        >
                          {busy === item.travellerId
                            ? "Saving…"
                            : "Save operational update"}
                        </button>
                      </div>
                    </article>
                  );
                })}
              </section>
            )}
          </>
        ) : null}
      </div>
    </OperatorWorkspaceShell>
  );
}
