"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { ReactNode, useCallback, useEffect, useState } from "react";

type AccessResponse = {
  accountId: string;
  operator: { id: string; displayName: string };
  permissions: string[];
};

type AccessState =
  | { kind: "loading" }
  | { kind: "authorized"; access: AccessResponse }
  | { kind: "unauthenticated" }
  | { kind: "platform-administrator" }
  | { kind: "forbidden" }
  | { kind: "error" };

const navigation = [
  { label: "Overview", href: "/operator" },
  { label: "Packages", href: "/operator/packages" },
  { label: "Departures", href: "/operator/departures" },
  { label: "Bookings", href: "/operator/bookings" },
  { label: "Visa", href: "/operator/visa" },
  { label: "Support", href: "/operator/support" },
  { label: "Cancellations", href: "/operator/cancellations" },
  { label: "Account", href: "/operator/account" },
] as const;

function isCurrentPath(pathname: string, href: string) {
  return href === "/operator"
    ? pathname === href
    : pathname === href || pathname.startsWith(`${href}/`);
}

export default function OperatorWorkspaceShell({
  title,
  summary,
  children,
}: {
  title: string;
  summary: string;
  children: ReactNode;
}) {
  const pathname = usePathname();
  const [state, setState] = useState<AccessState>({ kind: "loading" });

  const loadAccess = useCallback(async () => {
    setState({ kind: "loading" });
    const request: RequestInit = {
      cache: "no-store",
      credentials: "same-origin",
    };

    try {
      const response = await fetch("/api/v1/operator/access", request);
      if (response.ok) {
        setState({
          kind: "authorized",
          access: (await response.json()) as AccessResponse,
        });
        return;
      }

      if (response.status === 401) {
        setState({ kind: "unauthenticated" });
        return;
      }

      if (response.status === 403) {
        const platformResponse = await fetch(
          "/api/v1/platform/access",
          request,
        );
        setState(
          platformResponse.ok
            ? { kind: "platform-administrator" }
            : { kind: "forbidden" },
        );
        return;
      }

      setState({ kind: "error" });
    } catch {
      setState({ kind: "error" });
    }
  }, []);

  useEffect(() => {
    const pending = window.setTimeout(loadAccess, 0);
    return () => window.clearTimeout(pending);
  }, [loadAccess]);

  if (state.kind !== "authorized") {
    const heading =
      state.kind === "loading"
        ? "Checking secure access"
        : state.kind === "unauthenticated"
          ? "Sign in to continue"
          : state.kind === "platform-administrator"
            ? "Use NoorPath administration"
            : state.kind === "forbidden"
              ? "Access unavailable"
              : "We could not verify access";

    return (
      <main className="account-gate" id="main-content">
        <Link className="auth-brand" href="/">
          NoorPath
        </Link>
        <section
          className="auth-card"
          aria-live="polite"
          aria-busy={state.kind === "loading"}
        >
          <p className="auth-eyebrow">Operator workspace</p>
          <h1>{heading}</h1>
          <p className="auth-intro">
            {state.kind === "loading"
              ? "Please wait while NoorPath verifies your account."
              : state.kind === "platform-administrator"
                ? "You are signed in as a Platform Administrator. Operator workspaces require an approved operator membership."
                : state.kind === "forbidden"
                  ? "You are signed in, but this account does not have permission to open this workspace."
                  : state.kind === "error"
                    ? "Check your connection and try again. Your account details are safe."
                    : "Use your phone or Google account to continue securely."}
          </p>
          {state.kind === "unauthenticated" ? (
            <Link
              className="auth-primary"
              href={`/auth/sign-in?returnUrl=${encodeURIComponent(pathname)}`}
            >
              Sign in securely
            </Link>
          ) : null}
          {state.kind === "platform-administrator" ? (
            <div className="auth-actions">
              <Link className="auth-primary" href="/admin">
                Open admin workspace
              </Link>
              <Link className="auth-secondary" href="/">
                Return to NoorPath
              </Link>
            </div>
          ) : null}
          {state.kind === "forbidden" ? (
            <Link className="auth-secondary" href="/">
              Return to NoorPath
            </Link>
          ) : null}
          {state.kind === "error" ? (
            <button className="auth-primary" type="button" onClick={loadAccess}>
              Try again
            </button>
          ) : null}
        </section>
      </main>
    );
  }

  return (
    <div className="account-shell">
      <a className="skip-link" href="#account-content">
        Skip to main content
      </a>
      <header className="account-header">
        <Link className="auth-brand" href="/">
          NoorPath
        </Link>
        <span>{state.access.operator.displayName}</span>
      </header>
      <aside className="account-sidebar" aria-label="Operator navigation">
        <nav>
          {navigation.map((item) => {
            const current = isCurrentPath(pathname, item.href);
            return (
              <Link
                key={item.href}
                href={item.href}
                aria-current={current ? "page" : undefined}
              >
                {item.label}
              </Link>
            );
          })}
        </nav>
      </aside>
      <main className="account-content" id="account-content">
        <p className="auth-eyebrow">Protected operator account</p>
        <h1>{title}</h1>
        <p>{summary}</p>
        {children}
      </main>
    </div>
  );
}
