"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState } from "react";
import AccountIdentityMenu from "../AccountIdentityMenu";
import "../account.css";
import "../account-identity-menu.css";
import "./admin.css";

type AccessResponse = {
  accountId: string;
  displayName?: string;
};

type OperatorItem = {
  id: string;
  displayName: string;
  state: string;
  version: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  allowedTransitions: string[];
};

type SummaryResponse = {
  total: number;
  pendingApproval: number;
  approved: number;
  suspended: number;
  rejected: number;
  deactivated: number;
};

type OperatorListResponse = {
  items: OperatorItem[];
};

type HistoryItem = {
  id: string;
  fromState: string;
  toState: string;
  actorAccountId: string;
  reason?: string | null;
  operatorVersion: number;
  timestamp: string;
};

type OperatorDetailResponse = {
  operator: OperatorItem;
  history: HistoryItem[];
};

type DecisionDraft = {
  targetState: string;
  reason: string;
};

const adverseStates = new Set(["rejected", "suspended", "deactivated"]);

const stateLabels: Record<string, string> = {
  draft: "Draft",
  pendingApproval: "Pending approval",
  approved: "Approved",
  rejected: "Rejected",
  suspended: "Suspended",
  deactivated: "Deactivated",
};

export default function PlatformAdminWorkspace() {
  const [access, setAccess] = useState<AccessResponse | null>(null);
  const [summary, setSummary] = useState<SummaryResponse | null>(null);
  const [operators, setOperators] = useState<OperatorItem[]>([]);
  const [status, setStatus] = useState<
    "loading" | "ready" | "forbidden" | "error"
  >("loading");
  const [drafts, setDrafts] = useState<Record<string, DecisionDraft>>({});
  const [busyOperator, setBusyOperator] = useState<string | null>(null);
  const [feedback, setFeedback] = useState("");
  const [history, setHistory] = useState<Record<string, HistoryItem[]>>({});
  const [historyLoading, setHistoryLoading] = useState<string | null>(null);

  const loadWorkspace = useCallback(async () => {
    try {
      const accessResponse = await fetch("/api/v1/platform/access", {
        cache: "no-store",
      });
      if (accessResponse.status === 401) {
        window.location.assign("/auth/sign-in?returnUrl=/admin");
        return;
      }
      if (!accessResponse.ok) {
        setStatus(accessResponse.status === 403 ? "forbidden" : "error");
        return;
      }

      const [summaryResponse, operatorsResponse] = await Promise.all([
        fetch("/api/v1/platform/operators/summary", { cache: "no-store" }),
        fetch("/api/v1/platform/operators", { cache: "no-store" }),
      ]);
      if (!summaryResponse.ok || !operatorsResponse.ok) {
        setStatus("error");
        return;
      }

      setAccess((await accessResponse.json()) as AccessResponse);
      setSummary((await summaryResponse.json()) as SummaryResponse);
      const list = (await operatorsResponse.json()) as OperatorListResponse;
      setOperators(list.items);
      setDrafts((current) => {
        const next = { ...current };
        for (const item of list.items) {
          if (!next[item.id]) {
            next[item.id] = {
              targetState: item.allowedTransitions[0] ?? "",
              reason: "",
            };
          }
        }
        return next;
      });
      setStatus("ready");
    } catch {
      setStatus("error");
    }
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      void loadWorkspace();
    }, 0);

    return () => window.clearTimeout(timer);
  }, [loadWorkspace]);

  const pendingFirst = useMemo(
    () =>
      [...operators].sort((left, right) => {
        const leftPending = left.state === "pendingApproval" ? 0 : 1;
        const rightPending = right.state === "pendingApproval" ? 0 : 1;
        return leftPending - rightPending;
      }),
    [operators],
  );

  async function submitDecision(item: OperatorItem) {
    const draft = drafts[item.id] ?? { targetState: "", reason: "" };
    if (!draft.targetState) return;
    if (adverseStates.has(draft.targetState) && !draft.reason.trim()) {
      setFeedback(
        `Add a reason before marking ${item.displayName} as ${stateLabels[draft.targetState]}.`,
      );
      return;
    }

    setBusyOperator(item.id);
    setFeedback("");
    try {
      const response = await fetch(
        `/api/v1/platform/operators/${item.id}/state`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            targetState: draft.targetState,
            expectedVersion: item.version,
            reason: draft.reason.trim() || null,
          }),
        },
      );

      if (response.status === 409) {
        setFeedback(
          "This operator changed since you opened the queue. The latest state has been reloaded.",
        );
        await loadWorkspace();
        return;
      }
      if (!response.ok) {
        const problem = (await response.json().catch(() => null)) as {
          detail?: string;
          title?: string;
        } | null;
        setFeedback(
          problem?.detail ??
            problem?.title ??
            "The operator decision could not be saved.",
        );
        return;
      }

      setFeedback(
        `${item.displayName} is now ${stateLabels[draft.targetState] ?? draft.targetState}.`,
      );
      setHistory((current) => {
        const next = { ...current };
        delete next[item.id];
        return next;
      });
      await loadWorkspace();
    } catch {
      setFeedback("The operator decision could not be saved. Try again.");
    } finally {
      setBusyOperator(null);
    }
  }

  async function toggleHistory(item: OperatorItem) {
    if (history[item.id]) {
      setHistory((current) => {
        const next = { ...current };
        delete next[item.id];
        return next;
      });
      return;
    }

    setHistoryLoading(item.id);
    try {
      const response = await fetch(`/api/v1/platform/operators/${item.id}`, {
        cache: "no-store",
      });
      if (!response.ok) {
        setFeedback("Operator history could not be loaded.");
        return;
      }
      const detail = (await response.json()) as OperatorDetailResponse;
      setHistory((current) => ({ ...current, [item.id]: detail.history }));
    } catch {
      setFeedback("Operator history could not be loaded.");
    } finally {
      setHistoryLoading(null);
    }
  }

  if (status === "loading") {
    return (
      <main className="account-gate" aria-live="polite">
        <div className="auth-card">
          <p className="auth-eyebrow">Platform administration</p>
          <h1>Loading platform operations</h1>
          <p>
            Checking administrator access and the latest operator lifecycle
            state.
          </p>
        </div>
      </main>
    );
  }

  if (status === "forbidden") {
    return (
      <main className="account-gate">
        <div className="auth-card">
          <p className="auth-eyebrow">Access unavailable</p>
          <h1>Platform administrator access required</h1>
          <p>
            This workspace is restricted to approved NoorPath platform
            administrators.
          </p>
          <Link className="auth-secondary" href="/account">
            Return to account
          </Link>
        </div>
      </main>
    );
  }

  if (status === "error" || !summary || !access) {
    return (
      <main className="account-gate">
        <div className="auth-card">
          <p className="auth-eyebrow">Platform administration</p>
          <h1>Platform operations are temporarily unavailable</h1>
          <p>Your access was not changed. Reload the workspace to try again.</p>
          <button
            className="auth-primary"
            type="button"
            onClick={() => void loadWorkspace()}
          >
            Reload workspace
          </button>
        </div>
      </main>
    );
  }

  return (
    <div className="account-shell platform-admin-shell">
      <header className="account-header">
        <Link className="auth-brand" href="/">
          NoorPath
        </Link>
        <AccountIdentityMenu
          displayName={access.displayName || "Platform administrator"}
          accountHref="/admin"
          settingsHref="/admin"
        />
      </header>

      <aside className="account-sidebar">
        <nav aria-label="Platform administration">
          <a href="#overview" aria-current="page">
            Overview
          </a>
          <a href="#operators">Operators</a>
          <Link href="/platform/publications">Publication reviews</Link>
          <Link href="/account">Account</Link>
        </nav>
      </aside>

      <main className="account-content platform-admin-content">
        <section id="overview" aria-labelledby="platform-title">
          <p className="auth-eyebrow">Platform administration</p>
          <h1 id="platform-title">Platform operations</h1>
          <p>
            Review operator access centrally. Approval decisions are
            version-checked, deny-by-default, and recorded in operator history.
          </p>

          <div
            className="platform-metrics"
            aria-label="Operator lifecycle summary"
          >
            <article>
              <strong>{summary.pendingApproval}</strong>
              <span>Pending approval</span>
            </article>
            <article>
              <strong>{summary.approved}</strong>
              <span>Approved</span>
            </article>
            <article>
              <strong>{summary.suspended}</strong>
              <span>Suspended</span>
            </article>
            <article>
              <strong>{summary.total}</strong>
              <span>Total operators</span>
            </article>
          </div>
        </section>

        <section
          id="operators"
          className="platform-operators"
          aria-labelledby="operators-title"
        >
          <div className="platform-section-heading">
            <div>
              <p className="auth-eyebrow">Governed access</p>
              <h2 id="operators-title">Operator lifecycle</h2>
            </div>
            <button
              className="platform-secondary-button"
              type="button"
              onClick={() => void loadWorkspace()}
            >
              Refresh
            </button>
          </div>

          <p className="platform-feedback" aria-live="polite">
            {feedback}
          </p>

          {pendingFirst.length === 0 ? (
            <div className="account-welcome">
              <h2>No operators need attention</h2>
              <p>The operator lifecycle queue is currently empty.</p>
            </div>
          ) : (
            <div className="platform-operator-list">
              {pendingFirst.map((item) => {
                const draft = drafts[item.id] ?? {
                  targetState: item.allowedTransitions[0] ?? "",
                  reason: "",
                };
                const audit = history[item.id];
                return (
                  <article className="platform-operator-card" key={item.id}>
                    <div className="platform-operator-summary">
                      <div>
                        <span
                          className={`platform-status platform-status--${item.state}`}
                        >
                          {stateLabels[item.state] ?? item.state}
                        </span>
                        <h3>{item.displayName}</h3>
                        <p>
                          Operator ID {item.id} · Version {item.version}
                        </p>
                      </div>
                      <button
                        className="platform-history-button"
                        type="button"
                        aria-expanded={Boolean(audit)}
                        onClick={() => void toggleHistory(item)}
                        disabled={historyLoading === item.id}
                      >
                        {historyLoading === item.id
                          ? "Loading history…"
                          : audit
                            ? "Hide history"
                            : "View history"}
                      </button>
                    </div>

                    {item.allowedTransitions.length > 0 ? (
                      <div className="platform-decision-grid">
                        <label>
                          Decision
                          <select
                            value={draft.targetState}
                            onChange={(event) =>
                              setDrafts((current) => ({
                                ...current,
                                [item.id]: {
                                  ...draft,
                                  targetState: event.target.value,
                                },
                              }))
                            }
                          >
                            {item.allowedTransitions.map((target) => (
                              <option value={target} key={target}>
                                {stateLabels[target] ?? target}
                              </option>
                            ))}
                          </select>
                        </label>
                        <label>
                          Reason{" "}
                          {adverseStates.has(draft.targetState)
                            ? "(required)"
                            : "(optional)"}
                          <textarea
                            rows={2}
                            maxLength={500}
                            value={draft.reason}
                            onChange={(event) =>
                              setDrafts((current) => ({
                                ...current,
                                [item.id]: {
                                  ...draft,
                                  reason: event.target.value,
                                },
                              }))
                            }
                          />
                        </label>
                        <button
                          className="platform-primary-button"
                          type="button"
                          disabled={busyOperator === item.id}
                          onClick={() => void submitDecision(item)}
                        >
                          {busyOperator === item.id
                            ? "Saving decision…"
                            : "Apply decision"}
                        </button>
                      </div>
                    ) : (
                      <p className="platform-terminal-note">
                        No further lifecycle transitions are available from this
                        state.
                      </p>
                    )}

                    {audit ? (
                      <div className="platform-history">
                        <h4>Decision history</h4>
                        {audit.length === 0 ? (
                          <p>No lifecycle decisions have been recorded yet.</p>
                        ) : (
                          <ol>
                            {audit.map((entry) => (
                              <li key={entry.id}>
                                <strong>
                                  {stateLabels[entry.fromState] ??
                                    entry.fromState}{" "}
                                  →{" "}
                                  {stateLabels[entry.toState] ?? entry.toState}
                                </strong>
                                <span>
                                  {new Date(entry.timestamp).toLocaleString()}
                                </span>
                                {entry.reason ? <p>{entry.reason}</p> : null}
                              </li>
                            ))}
                          </ol>
                        )}
                      </div>
                    ) : null}
                  </article>
                );
              })}
            </div>
          )}
        </section>
      </main>
    </div>
  );
}
