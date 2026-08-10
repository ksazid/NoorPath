"use client";

import Image from "next/image";
import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import { useCallback, useEffect, useState } from "react";
import AccountIdentityMenu from "../AccountIdentityMenu";

type AccessResponse = {
  accountId: string;
  displayName?: string;
};

type AccessState =
  | { kind: "loading" }
  | { kind: "authorized"; access: AccessResponse }
  | { kind: "unauthenticated" }
  | { kind: "forbidden" }
  | { kind: "error" };

type NavigationItem = {
  label: string;
  href: string;
  match: (pathname: string, hash: string) => boolean;
};

type NavigationGroup = {
  label: string;
  items: NavigationItem[];
};

const navigation: NavigationGroup[] = [
  {
    label: "Workspace",
    items: [
      {
        label: "Overview",
        href: "/admin",
        match: (pathname, hash) =>
          pathname === "/admin" && hash !== "#operators",
      },
      {
        label: "Operators",
        href: "/admin#operators",
        match: (pathname, hash) =>
          pathname === "/admin" && hash === "#operators",
      },
    ],
  },
  {
    label: "Governance",
    items: [
      {
        label: "Publication reviews",
        href: "/platform/publications",
        match: (pathname) =>
          pathname === "/platform/publications" ||
          pathname.startsWith("/platform/publications/"),
      },
    ],
  },
  {
    label: "Account",
    items: [
      {
        label: "Account",
        href: "/account",
        match: (pathname) =>
          pathname === "/account" || pathname.startsWith("/account/"),
      },
    ],
  },
];

function PlatformAdminBrand() {
  return (
    <Link
      className="np-brand np-staff-brand"
      href="/admin"
      aria-label="NoorPath platform administration home"
    >
      <Image
        src="/assets/noorpath-wordmark.svg"
        alt="NoorPath"
        width={252}
        height={100}
        priority
      />
    </Link>
  );
}

function PlatformAdminNavigation({
  pathname,
  hash,
}: {
  pathname: string;
  hash: string;
}) {
  return (
    <nav aria-label="Platform administration navigation">
      {navigation.map((group) => (
        <section className="np-staff-nav-group" key={group.label}>
          <h2>{group.label}</h2>
          {group.items.map((item) => {
            const current = item.match(pathname, hash);
            const currentValue = current ? "page" : undefined;
            return item.href.includes("#") ? (
              <a key={item.href} href={item.href} aria-current={currentValue}>
                {item.label}
              </a>
            ) : (
              <Link
                key={item.href}
                href={item.href}
                aria-current={currentValue}
              >
                {item.label}
              </Link>
            );
          })}
        </section>
      ))}
    </nav>
  );
}

export default function PlatformAdminWorkspaceShell({
  title,
  summary,
  children,
  contentOwnsLandmark = false,
  contentClassName = "",
}: {
  title: string;
  summary: string;
  children: ReactNode;
  contentOwnsLandmark?: boolean;
  contentClassName?: string;
}) {
  const pathname = usePathname();
  const [hash, setHash] = useState("");
  const [state, setState] = useState<AccessState>({ kind: "loading" });

  const loadAccess = useCallback(async () => {
    setState({ kind: "loading" });
    try {
      const response = await fetch("/api/v1/platform/access", {
        cache: "no-store",
        credentials: "same-origin",
      });
      if (response.status === 401) {
        setState({ kind: "unauthenticated" });
        return;
      }
      if (response.status === 403) {
        setState({ kind: "forbidden" });
        return;
      }
      if (!response.ok) {
        setState({ kind: "error" });
        return;
      }

      const access = (await response.json()) as AccessResponse;
      setState({ kind: "authorized", access });
    } catch {
      setState({ kind: "error" });
    }
  }, []);

  useEffect(() => {
    const pending = window.setTimeout(loadAccess, 0);
    return () => window.clearTimeout(pending);
  }, [loadAccess]);

  useEffect(() => {
    const syncHash = () => setHash(window.location.hash);
    syncHash();
    window.addEventListener("hashchange", syncHash);
    return () => window.removeEventListener("hashchange", syncHash);
  }, [pathname]);

  if (state.kind !== "authorized") {
    const heading =
      state.kind === "loading"
        ? "Checking secure access"
        : state.kind === "unauthenticated"
          ? "Sign in to continue"
          : state.kind === "forbidden"
            ? "Platform administrator access required"
            : "We could not verify access";

    return (
      <main className="account-gate" id="main-content">
        <PlatformAdminBrand />
        <section
          className="auth-card"
          aria-live="polite"
          aria-busy={state.kind === "loading"}
        >
          <p className="auth-eyebrow">Platform administration</p>
          <h1>{heading}</h1>
          <p className="auth-intro">
            {state.kind === "loading"
              ? "Please wait while NoorPath verifies your administrator access."
              : state.kind === "unauthenticated"
                ? "Use an authorized NoorPath staff account to continue."
                : state.kind === "forbidden"
                  ? "This workspace is restricted to configured NoorPath platform administrators."
                  : "Check your connection and try again. Your account details are safe."}
          </p>
          {state.kind === "unauthenticated" ? (
            <Link
              className="auth-primary"
              href={`/auth/sign-in?returnUrl=${encodeURIComponent(pathname)}`}
            >
              Sign in securely
            </Link>
          ) : null}
          {state.kind === "forbidden" ? (
            <div className="auth-actions">
              <Link className="auth-secondary" href="/account">
                Return to account
              </Link>
              <Link className="auth-secondary" href="/">
                Return to NoorPath
              </Link>
            </div>
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

  const displayName =
    state.access.displayName?.trim() || "Platform administrator";
  const contentClass = ["np-staff-content", contentClassName]
    .filter(Boolean)
    .join(" ");
  const pageContent = (
    <>
      <header className="np-staff-content__header">
        <p>Protected platform workspace</p>
        <h1>{title}</h1>
        <span className="np-staff-content__summary">{summary}</span>
      </header>
      {children}
    </>
  );

  return (
    <div className="np-staff-shell np-platform-admin-shell">
      <a className="np-skip-link" href="#platform-admin-content">
        Skip to main content
      </a>
      <header className="np-staff-header">
        <PlatformAdminBrand />
        <span>Platform Administration</span>
        <AccountIdentityMenu
          displayName={displayName}
          accountHref="/account"
          settingsHref="/account"
        />
      </header>
      <aside className="np-staff-sidebar">
        <PlatformAdminNavigation pathname={pathname} hash={hash} />
      </aside>
      <details className="np-staff-menu">
        <summary>Platform Admin menu</summary>
        <div className="np-staff-menu__panel">
          <PlatformAdminNavigation pathname={pathname} hash={hash} />
        </div>
      </details>
      <div className="np-platform-admin-content-column">
        {contentOwnsLandmark ? (
          <div
            className={contentClass}
            id="platform-admin-content"
            tabIndex={-1}
          >
            {pageContent}
          </div>
        ) : (
          <main
            className={contentClass}
            id="platform-admin-content"
            tabIndex={-1}
          >
            {pageContent}
          </main>
        )}
        <footer className="np-platform-admin-footer">
          <span>NoorPath Platform Administration</span>
          <nav aria-label="Platform administration footer">
            <Link href="/platform/publications">Publication reviews</Link>
            <Link href="/account">Account</Link>
            <Link href="/">NoorPath home</Link>
          </nav>
        </footer>
      </div>
    </div>
  );
}
