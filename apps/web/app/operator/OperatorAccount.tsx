"use client";

import { useCallback, useState } from "react";
import { useDeferredInitialLoad } from "../../lib/use-deferred-initial-load";
import OperatorWorkspaceShell from "./OperatorWorkspaceShell";

type AccessResponse = {
  accountId: string;
  operator: { id: string; displayName: string };
  permissions: string[];
};

type AccountState =
  | { kind: "loading" }
  | { kind: "error" }
  | { kind: "ready"; access: AccessResponse };

function permissionLabel(permission: string) {
  return permission
    .replace(/^operator\./, "")
    .replaceAll(".", " ")
    .replace(/(^|\s)\S/g, (value) => value.toUpperCase());
}

export default function OperatorAccount() {
  const [state, setState] = useState<AccountState>({ kind: "loading" });

  const load = useCallback(async () => {
    setState({ kind: "loading" });
    try {
      const response = await fetch("/api/v1/operator/access", {
        credentials: "same-origin",
        cache: "no-store",
      });
      if (!response.ok) throw new Error();
      setState({
        kind: "ready",
        access: (await response.json()) as AccessResponse,
      });
    } catch {
      setState({ kind: "error" });
    }
  }, []);

  useDeferredInitialLoad(load);

  return (
    <OperatorWorkspaceShell
      title="Account access"
      summary="Review the operator identity and explicit permissions attached to this signed-in account."
    >
      <section className="operator-section" aria-live="polite">
        <p className="auth-eyebrow">Authorization</p>
        <h2>Current operator membership</h2>
        {state.kind === "loading" ? <p>Loading account access…</p> : null}
        {state.kind === "error" ? (
          <div className="operator-inline-state">
            <p>Account details are temporarily unavailable.</p>
            <button className="auth-secondary" type="button" onClick={load}>
              Retry
            </button>
          </div>
        ) : null}
        {state.kind === "ready" ? (
          <>
            <dl className="operator-account-details">
              <div>
                <dt>Operator</dt>
                <dd>{state.access.operator.displayName}</dd>
              </div>
              <div>
                <dt>Operator ID</dt>
                <dd>{state.access.operator.id}</dd>
              </div>
              <div>
                <dt>Account ID</dt>
                <dd>{state.access.accountId}</dd>
              </div>
            </dl>
            <h3>Granted permissions</h3>
            <ul className="operator-permission-list">
              {state.access.permissions.map((permission) => (
                <li key={permission}>
                  <strong>{permissionLabel(permission)}</strong>
                  <code>{permission}</code>
                </li>
              ))}
            </ul>
          </>
        ) : null}
      </section>
    </OperatorWorkspaceShell>
  );
}
