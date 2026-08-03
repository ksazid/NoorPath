"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

type Kind = "customer" | "operator" | "platform";
type State =
  "loading" | "authorized" | "unauthenticated" | "forbidden" | "error";

const content = {
  customer: {
    label: "Customer account",
    title: "My NoorPath",
    summary:
      "Your bookings, travellers, payments, and readiness in one private place.",
    items: ["My Journey", "Travellers", "Payments", "Account"],
    endpoint: "/api/v1/account/access",
    action: { label: "Browse packages", href: "/" },
  },
  operator: {
    label: "Operator workspace",
    title: "Operator administration",
    summary:
      "Manage approved packages and departures within your operator scope.",
    items: ["Overview", "Packages", "Departures", "Account"],
    endpoint: "/api/v1/operator/access",
    action: { label: "Create new draft", href: "/operator/departures/new" },
  },
  platform: {
    label: "NoorPath administration",
    title: "Platform operations",
    summary: "Review platform work using your explicitly approved permissions.",
    items: ["Work queue", "Operators", "Reviews", "Account"],
    endpoint: "/api/v1/platform/access",
    action: {
      label: "Open publication reviews",
      href: "/platform/publications",
    },
  },
} satisfies Record<
  Kind,
  {
    label: string;
    title: string;
    summary: string;
    items: string[];
    endpoint: string;
    action: { label: string; href: string };
  }
>;

export default function ProtectedAccountShell({ kind }: { kind: Kind }) {
  const details = content[kind];
  const [state, setState] = useState<State>("loading");

  const load = useCallback(async () => {
    setState("loading");
    try {
      const response = await fetch(details.endpoint, {
        cache: "no-store",
        credentials: "same-origin",
      });
      setState(
        response.ok
          ? "authorized"
          : response.status === 401
            ? "unauthenticated"
            : response.status === 403
              ? "forbidden"
              : "error",
      );
    } catch {
      setState("error");
    }
  }, [details.endpoint]);

  useEffect(() => {
    const pending = window.setTimeout(load, 0);
    return () => window.clearTimeout(pending);
  }, [load]);

  if (state !== "authorized") {
    return (
      <main className="account-gate" id="main-content">
        <Link className="auth-brand" href="/">
          NoorPath
        </Link>
        <section
          className="auth-card"
          aria-live="polite"
          aria-busy={state === "loading"}
        >
          <p className="auth-eyebrow">{details.label}</p>
          <h1>
            {state === "loading"
              ? "Checking secure access"
              : state === "unauthenticated"
                ? "Sign in to continue"
                : state === "forbidden"
                  ? "Access unavailable"
                  : "We could not verify access"}
          </h1>
          <p className="auth-intro">
            {state === "loading"
              ? "Please wait while NoorPath verifies your account."
              : state === "forbidden"
                ? "You are signed in, but this account does not have permission to open this workspace."
                : state === "error"
                  ? "Check your connection and try again. Your account details are safe."
                  : "Use your phone or Google account to continue securely."}
          </p>
          {state === "unauthenticated" && (
            <Link
              className="auth-primary"
              href={`/auth/sign-in?returnUrl=${kind === "customer" ? "/account" : kind === "operator" ? "/operator" : "/admin"}`}
            >
              Sign in securely
            </Link>
          )}
          {state === "forbidden" && (
            <Link className="auth-secondary" href="/">
              Return to NoorPath
            </Link>
          )}
          {state === "error" && (
            <button className="auth-primary" type="button" onClick={load}>
              Try again
            </button>
          )}
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
        <span>{details.label}</span>
      </header>
      <aside
        className="account-sidebar"
        aria-label={`${details.label} navigation`}
      >
        <nav>
          {details.items.map((item, index) => (
            <a
              key={item}
              href={
                index === 0
                  ? "#account-content"
                  : `#${item.toLowerCase().replaceAll(" ", "-")}`
              }
              aria-current={index === 0 ? "page" : undefined}
            >
              {item}
            </a>
          ))}
        </nav>
      </aside>
      <main className="account-content" id="account-content">
        <p className="auth-eyebrow">Protected account</p>
        <h1>{details.title}</h1>
        <p>{details.summary}</p>
        <section className="account-welcome">
          <h2>Your secure workspace is ready</h2>
          <p>
            Only information and actions permitted for this account will appear
            here.
          </p>
          <Link className="auth-primary" href={details.action.href}>
            {details.action.label}
          </Link>
          {kind === "customer" ? (
            <Link className="auth-secondary" href="/account/family">
              Manage family travellers
            </Link>
          ) : null}
        </section>
      </main>
    </div>
  );
}
