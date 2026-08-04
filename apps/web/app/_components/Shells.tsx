import Link from "next/link";
import type { ReactNode } from "react";

export type CustomerShellMode = "public" | "authenticated" | "transactional";
export type CustomerFooterMode = "full" | "compact" | "none";

type NavigationItem = {
  href: string;
  label: string;
};

const publicNavigation: NavigationItem[] = [
  { href: "/#packages", label: "Packages" },
  { href: "/#how-it-works", label: "How It Works" },
  { href: "/support", label: "Talk to Us" },
  { href: "/account", label: "My Journey" },
];

const authenticatedNavigation: NavigationItem[] = [
  { href: "/#packages", label: "Packages" },
  { href: "/account", label: "My Journey" },
  { href: "/support", label: "Help" },
  { href: "/support", label: "Talk to Us" },
];

const footerGroups = [
  {
    title: "NoorPath",
    links: [
      { href: "/#how-it-works", label: "How It Works" },
      { href: "/#packages", label: "Packages" },
    ],
  },
  {
    title: "Explore",
    links: [
      { href: "/account", label: "My Journey" },
      { href: "/support", label: "Travel Support" },
    ],
  },
  {
    title: "Support",
    links: [
      { href: "/support", label: "WhatsApp Support" },
      { href: "/support", label: "Request a Callback" },
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

function CustomerNavigation({
  activePath,
  items,
}: {
  activePath?: string;
  items: NavigationItem[];
}) {
  return (
    <nav aria-label="Customer navigation">
      {items.map((item) => (
        <Link
          aria-current={activePath === item.href ? "page" : undefined}
          href={item.href}
          key={`${item.href}-${item.label}`}
        >
          {item.label}
        </Link>
      ))}
    </nav>
  );
}

export function CustomerShell({
  activePath,
  children,
  footer = "full",
  mode = "public",
  profileLabel = "Profile",
}: {
  activePath?: string;
  children: ReactNode;
  footer?: CustomerFooterMode;
  mode?: CustomerShellMode;
  profileLabel?: string;
}) {
  const navigation =
    mode === "authenticated" ? authenticatedNavigation : publicNavigation;

  return (
    <div className={`np-customer-shell np-customer-shell--${mode}`}>
      <a className="np-skip-link" href="#main-content">
        Skip to main content
      </a>
      <header className="np-customer-header">
        <Link className="np-brand" href="/" aria-label="NoorPath home">
          NoorPath
        </Link>
        {mode === "transactional" ? (
          <div className="np-transactional-context">
            <strong>Secure Reservation</strong>
            <Link href="/support">Talk to Us</Link>
          </div>
        ) : (
          <>
            <div className="np-customer-navigation--desktop">
              <CustomerNavigation activePath={activePath} items={navigation} />
              {mode === "authenticated" ? (
                <Link className="np-profile-link" href="/account">
                  {profileLabel}
                </Link>
              ) : null}
            </div>
            <details className="np-customer-menu">
              <summary>Menu</summary>
              <div className="np-customer-menu__panel">
                <CustomerNavigation activePath={activePath} items={navigation} />
                {mode === "authenticated" ? (
                  <Link className="np-profile-link" href="/account">
                    {profileLabel}
                  </Link>
                ) : null}
              </div>
            </details>
          </>
        )}
      </header>
      <main id="main-content">{children}</main>
      {footer === "full" ? (
        <footer className="np-customer-footer">
          <div className="np-customer-footer__brand">
            <Link className="np-brand" href="/">
              NoorPath
            </Link>
            <p>A calm, transparent path for your Umrah journey.</p>
          </div>
          {footerGroups.map((group) => (
            <section key={group.title} aria-labelledby={`footer-${group.title}`}>
              <h2 id={`footer-${group.title}`}>{group.title}</h2>
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
      ) : null}
      {footer === "compact" ? (
        <footer className="np-customer-footer np-customer-footer--compact">
          <span>Secure NoorPath reservation</span>
          <nav aria-label="Reservation support and legal links">
            <Link href="/support">Support</Link>
            <Link href="/privacy">Privacy</Link>
            <Link href="/terms">Terms</Link>
          </nav>
        </footer>
      ) : null}
    </div>
  );
}

export type StaffNavigationGroup = {
  label: string;
  items: NavigationItem[];
};

export function StaffShell({
  activePath,
  children,
  headerActions,
  navigation,
  operatorName,
  search,
  title,
}: {
  activePath?: string;
  children: ReactNode;
  headerActions?: ReactNode;
  navigation: StaffNavigationGroup[];
  operatorName: string;
  search?: ReactNode;
  title: string;
}) {
  const navigationContent = (
    <nav aria-label="Staff navigation">
      {navigation.map((group) => (
        <section className="np-staff-nav-group" key={group.label}>
          <h2>{group.label}</h2>
          {group.items.map((item) => (
            <Link
              aria-current={activePath === item.href ? "page" : undefined}
              href={item.href}
              key={`${item.href}-${item.label}`}
            >
              {item.label}
            </Link>
          ))}
        </section>
      ))}
    </nav>
  );

  return (
    <div className="np-staff-shell">
      <a className="np-skip-link" href="#staff-content">
        Skip to main content
      </a>
      <header className="np-staff-header">
        <Link className="np-brand" href="/operator">
          NoorPath Portal
        </Link>
        <div className="np-staff-header__tools">
          {search ? <div className="np-staff-header__search">{search}</div> : null}
          <span>{operatorName}</span>
          {headerActions}
        </div>
      </header>
      <aside className="np-staff-sidebar">{navigationContent}</aside>
      <details className="np-staff-menu">
        <summary>Workspace navigation</summary>
        <div className="np-staff-menu__panel">{navigationContent}</div>
      </details>
      <main className="np-staff-content" id="staff-content">
        <header className="np-staff-content__header">
          <p>Protected workspace</p>
          <h1>{title}</h1>
        </header>
        {children}
      </main>
    </div>
  );
}
