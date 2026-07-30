import Image from "next/image";
import Link from "next/link";

export function NoorPathBrand() {
  return (
    <Link className="public-brand" href="/" aria-label="NoorPath home">
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

export function PublicHeader({
  mode = "detail",
}: {
  mode?: "landing" | "detail";
}) {
  return (
    <header className={`public-topbar public-topbar-${mode}`}>
      <a className="skip-link" href="#main-content">
        Skip to content
      </a>
      <NoorPathBrand />

      <nav aria-label="Primary navigation" className="public-nav">
        <Link href="/#packages">
          {mode === "landing" ? "Explore" : "Packages"}
        </Link>
        <Link href="/#packages">
          {mode === "landing" ? "Journey" : "Destinations"}
        </Link>
        <Link href="/#trust">{mode === "landing" ? "Family" : "About us"}</Link>
        <a href="mailto:support@noorpath.example">
          {mode === "landing" ? "Help" : "Support"}
        </a>
      </nav>

      {mode === "landing" ? (
        <a
          className="header-profile-button"
          href="mailto:support@noorpath.example"
          aria-label="Contact NoorPath support"
        >
          <UserIcon />
        </a>
      ) : (
        <div className="public-header-actions">
          <a
            className="header-action header-action-secondary"
            href="tel:+0000000000"
          >
            <PhoneIcon /> Request a callback
          </a>
          <a
            className="header-action header-action-primary"
            href="mailto:support@noorpath.example"
          >
            <MessageIcon /> WhatsApp support
          </a>
        </div>
      )}

      <details className="public-mobile-menu">
        <summary aria-label="Open navigation">
          <MenuIcon />
        </summary>
        <nav aria-label="Mobile navigation">
          <Link href="/#packages">Packages</Link>
          <Link href="/#trust">Why NoorPath</Link>
          <a href="mailto:support@noorpath.example">Support</a>
        </nav>
      </details>

      <a
        className="public-mobile-contact"
        href={
          mode === "landing"
            ? "mailto:support@noorpath.example"
            : "tel:+0000000000"
        }
        aria-label={
          mode === "landing" ? "Contact NoorPath support" : "Request a callback"
        }
      >
        {mode === "landing" ? <UserIcon /> : <PhoneIcon />}
      </a>
    </header>
  );
}

export function PublicFooter() {
  return (
    <footer className="public-footer">
      <NoorPathBrand />
      <p>
        Calm, factual Umrah journey information with operator accountability and
        human support.
      </p>
      <nav className="public-footer-links" aria-label="Footer navigation">
        <Link href="/#packages">Explore packages</Link>
        <a href="mailto:support@noorpath.example">Contact support</a>
      </nav>
    </footer>
  );
}

export function ConfirmationBadge({
  state,
}: {
  state: "confirmed" | "pending";
}) {
  const confirmed = state === "confirmed";
  return (
    <span
      className={
        confirmed
          ? "public-fact-state public-fact-state-confirmed"
          : "public-fact-state public-fact-state-pending"
      }
    >
      <span aria-hidden="true" className="public-fact-dot" />
      {confirmed ? "Confirmed" : "Pending"}
    </span>
  );
}

function UserIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24">
      <circle cx="12" cy="8" r="3.25" />
      <path d="M5.75 19c.65-3.25 2.75-5 6.25-5s5.6 1.75 6.25 5" />
    </svg>
  );
}

function PhoneIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24">
      <path d="M8.2 4.5 5.8 6.2c.4 5.9 6.1 11.6 12 12l1.7-2.4-3.7-2.3-1.6 1.6c-2.1-.9-4.4-3.2-5.3-5.3l1.6-1.6-2.3-3.7Z" />
    </svg>
  );
}

function MessageIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24">
      <path d="M20 11.5a8 8 0 0 1-11.7 7L4 19.7l1.2-4.1A8 8 0 1 1 20 11.5Z" />
      <path d="M9 9.5c1 2 2 3 4.2 4" />
    </svg>
  );
}

function MenuIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24">
      <path d="M4 7h16M4 12h16M4 17h11" />
    </svg>
  );
}
