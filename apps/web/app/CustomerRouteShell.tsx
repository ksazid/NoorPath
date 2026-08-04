"use client";

import Image from "next/image";
import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";

export type CustomerRouteMode = "public" | "authenticated" | "transactional";

type NavigationItem = {
  href: string;
  label: string;
  match: (pathname: string) => boolean;
};

const publicNavigation: NavigationItem[] = [
  {
    href: "/#packages",
    label: "Packages",
    match: (pathname) => pathname.startsWith("/packages/"),
  },
  {
    href: "/#plan-ahead",
    label: "How It Works",
    match: () => false,
  },
  {
    href: "/support",
    label: "Talk to Us",
    match: (pathname) => pathname === "/support",
  },
  {
    href: "/journeys",
    label: "My Journey",
    match: (pathname) =>
      pathname === "/journeys" ||
      pathname.startsWith("/account") ||
      pathname.startsWith("/bookings/"),
  },
];

const authenticatedNavigation: NavigationItem[] = [
  {
    href: "/#packages",
    label: "Packages",
    match: (pathname) => pathname.startsWith("/packages/"),
  },
  {
    href: "/journeys",
    label: "My Journey",
    match: (pathname) =>
      pathname === "/journeys" || pathname.startsWith("/bookings/"),
  },
  {
    href: "/support",
    label: "Help",
    match: (pathname) => pathname === "/support",
  },
  {
    href: "/support",
    label: "Talk to Us",
    match: (pathname) => pathname === "/support",
  },
  {
    href: "/account",
    label: "Profile",
    match: (pathname) => pathname.startsWith("/account"),
  },
];

const footerGroups = [
  {
    title: "NoorPath",
    links: [
      { href: "/#plan-ahead", label: "How It Works" },
      { href: "/#packages", label: "Packages" },
    ],
  },
  {
    title: "Explore",
    links: [
      { href: "/journeys", label: "My Journey" },
      { href: "/account/family", label: "Family Travellers" },
    ],
  },
  {
    title: "Support",
    links: [
      { href: "/support", label: "Travel Support" },
      { href: "/support#callback", label: "Request a Callback" },
    ],
  },
  {
    title: "Legal",
    links: [
      { href: "/privacy", label: "Privacy" },
      { href: "/terms", label: "Terms" },
    ],
  },
] as const;

export function classifyCustomerRoute(
  pathname: string,
): CustomerRouteMode | null {
  if (
    pathname.startsWith("/operator") ||
    pathname.startsWith("/admin") ||
    pathname.startsWith("/platform") ||
    pathname.startsWith("/design-system") ||
    pathname.startsWith("/api")
  ) {
    return null;
  }

  if (
    pathname.startsWith("/auth/sign-in") ||
    pathname.startsWith("/inventory-holds/")
  ) {
    return "transactional";
  }

  if (pathname.startsWith("/bookings/")) {
    return /\/(payment|confirmation|documents)(\/|$)/.test(pathname)
      ? "transactional"
      : "authenticated";
  }

  if (pathname === "/journeys" || pathname.startsWith("/account")) {
    return "authenticated";
  }

  if (
    pathname === "/" ||
    pathname.startsWith("/packages/") ||
    pathname === "/support" ||
    pathname === "/privacy" ||
    pathname === "/terms"
  ) {
    return "public";
  }

  return null;
}

function Brand() {
  return (
    <Link
      className="np-brand np-route-brand"
      href="/"
      aria-label="NoorPath home"
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

function Navigation({
  items,
  pathname,
}: {
  items: NavigationItem[];
  pathname: string;
}) {
  return (
    <nav aria-label="Customer navigation">
      {items.map((item) => (
        <Link
          aria-current={item.match(pathname) ? "page" : undefined}
          href={item.href}
          key={`${item.href}-${item.label}`}
        >
          {item.label}
        </Link>
      ))}
    </nav>
  );
}

function Header({
  mode,
  pathname,
}: {
  mode: CustomerRouteMode;
  pathname: string;
}) {
  const navigation =
    mode === "authenticated" ? authenticatedNavigation : publicNavigation;

  return (
    <header className="np-customer-header">
      <Brand />
      {mode === "transactional" ? (
        <div className="np-transactional-context">
          <strong>Secure NoorPath journey</strong>
          <Link href="/support">Talk to Us</Link>
        </div>
      ) : (
        <>
          <div className="np-customer-navigation--desktop">
            <Navigation items={navigation} pathname={pathname} />
          </div>
          <details className="np-customer-menu">
            <summary>Menu</summary>
            <div className="np-customer-menu__panel">
              <Navigation items={navigation} pathname={pathname} />
            </div>
          </details>
        </>
      )}
    </header>
  );
}

function FullFooter() {
  return (
    <footer className="np-customer-footer">
      <div className="np-customer-footer__brand">
        <Brand />
        <p>A calm, transparent path for your Umrah journey.</p>
      </div>
      {footerGroups.map((group) => (
        <section
          key={group.title}
          aria-labelledby={`route-footer-${group.title.toLowerCase()}`}
        >
          <h2 id={`route-footer-${group.title.toLowerCase()}`}>
            {group.title}
          </h2>
          <nav aria-label={`${group.title} links`}>
            {group.links.map((link) => (
              <Link href={link.href} key={`${link.href}-${link.label}`}>
                {link.label}
              </Link>
            ))}
          </nav>
        </section>
      ))}
    </footer>
  );
}

function CompactFooter() {
  return (
    <footer className="np-customer-footer np-customer-footer--compact">
      <span>Secure NoorPath journey</span>
      <nav aria-label="Journey support and legal links">
        <Link href="/support">Support</Link>
        <Link href="/privacy">Privacy</Link>
        <Link href="/terms">Terms</Link>
      </nav>
    </footer>
  );
}

export default function CustomerRouteShell({
  children,
}: {
  children: ReactNode;
}) {
  const pathname = usePathname();
  const mode = classifyCustomerRoute(pathname);

  if (!mode) return children;

  return (
    <div
      className={`np-customer-shell np-route-shell np-route-shell--${mode}`}
      data-customer-shell={mode}
    >
      <a className="np-skip-link" href="#customer-route-content">
        Skip to main content
      </a>
      <Header mode={mode} pathname={pathname} />
      <div
        className="np-route-shell__content"
        id="customer-route-content"
        tabIndex={-1}
      >
        {children}
      </div>
      {mode === "transactional" ? <CompactFooter /> : <FullFooter />}
    </div>
  );
}
