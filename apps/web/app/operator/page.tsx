"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

type Access = {
  accountId: string;
  operator: { id: string; displayName: string };
  permissions: string[];
};

type State =
  | { kind: "loading" }
  | { kind: "authorized"; access: Access }
  | { kind: "unauthenticated" }
  | { kind: "forbidden" }
  | { kind: "error"; correlationId?: string };

export default function OperatorPage() {
  const [state, setState] = useState<State>({ kind: "loading" });

  const load = async () => {
    setState({ kind: "loading" });
    try {
      const response = await fetch("/api/v1/operator/access", {
        cache: "no-store",
        credentials: "same-origin",
      });
      if (response.status === 401) return setState({ kind: "unauthenticated" });
      if (response.status === 403) return setState({ kind: "forbidden" });
      const body = await response.json();
      if (!response.ok)
        return setState({
          kind: "error",
          correlationId: body.correlationId,
        });
      setState({ kind: "authorized", access: body });
    } catch {
      setState({ kind: "error" });
    }
  };

  useEffect(() => {
    const pending = window.setTimeout(load, 0);
    return () => window.clearTimeout(pending);
  }, []);

  return (
    <div className="operator-canvas">
      <a className="skip-link" href="#operator-main">
        Skip to main content
      </a>
      <header className="operator-header">
        <Link className="operator-brand" href="/" aria-label="NoorPath home">
          NoorPath
        </Link>
        <span>Operator administration</span>
      </header>
      <main id="operator-main" className="operator-main">
        {state.kind === "loading" && (
          <section
            className="operator-state"
            aria-live="polite"
            aria-busy="true"
          >
            <p className="operator-eyebrow">Checking secure access</p>
            <h1>Preparing your workspace</h1>
            <p>Please wait while NoorPath verifies your operator access.</p>
          </section>
        )}

        {state.kind === "authorized" && (
          <section className="operator-state">
            <p className="operator-eyebrow">Approved operator</p>
            <h1>{state.access.operator.displayName}</h1>
            <p>
              Your secure administration workspace is ready. Approved
              capabilities will appear here as they become available.
            </p>
            <div className="operator-empty" role="status">
              <h2>No administration tasks yet</h2>
              <p>
                There is nothing you need to complete in this foundation
                release.
              </p>
            </div>
          </section>
        )}

        {state.kind === "unauthenticated" && (
          <section className="operator-state">
            <p className="operator-eyebrow">Secure operator area</p>
            <h1>Sign in to continue</h1>
            <p>
              Use your approved operator account to access NoorPath
              administration.
            </p>
            <a
              className="operator-primary"
              href="/api/auth/sign-in?returnUrl=/operator"
            >
              Sign in securely
            </a>
          </section>
        )}

        {state.kind === "forbidden" && (
          <section className="operator-state" role="alert">
            <p className="operator-eyebrow">Access unavailable</p>
            <h1>This account cannot open operator administration</h1>
            <p>
              Your account is signed in, but it does not currently have the
              required operator access. Contact your NoorPath administrator if
              this seems wrong.
            </p>
            <Link className="operator-secondary" href="/">
              Return to NoorPath
            </Link>
          </section>
        )}

        {state.kind === "error" && (
          <section className="operator-state" role="alert">
            <p className="operator-eyebrow">Temporary interruption</p>
            <h1>We could not verify access</h1>
            <p>
              Your account details are safe. Check your connection and try
              again.
            </p>
            {state.correlationId && (
              <p className="operator-reference">
                Reference: {state.correlationId}
              </p>
            )}
            <button className="operator-primary" onClick={load} type="button">
              Try again
            </button>
          </section>
        )}
      </main>
    </div>
  );
}
