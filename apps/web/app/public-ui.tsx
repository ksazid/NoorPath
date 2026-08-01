import Image from "next/image";
import Link from "next/link";

type HeaderMode = "landing" | "detail";

export function NoorPathBrand({ mode = "landing" }: { mode?: HeaderMode }) {
  const packageBrand = mode === "detail";

  return (
    <Link
      className={`public-brand public-brand-${mode}`}
      href="/"
      aria-label="NoorPath home"
    >
      <Image
        src={
          packageBrand
            ? "/assets/noorpath-package-wordmark.webp"
            : "/assets/noorpath-wordmark.svg"
        }
        alt="NoorPath"
        width={packageBrand ? 278 : 252}
        height={packageBrand ? 100 : 100}
        priority
      />
    </Link>
  );
}

export function PublicHeader({ mode = "detail" }: { mode?: HeaderMode }) {
  return (
    <header className={`public-topbar public-topbar-${mode}`}>
      <a className="skip-link" href="#main-content">
        Skip to content
      </a>
      <NoorPathBrand mode={mode} />

      <nav aria-label="Primary navigation" className="public-nav">
        <Link href="/#packages">
          {mode === "landing" ? "Explore" : "Packages"}
        </Link>
        <Link href={mode === "landing" ? "/#plan-ahead" : "/#packages"}>
          {mode === "landing" ? "Plan ahead" : "Destinations"}
        </Link>
        <Link href="/#trust">
          {mode === "landing" ? "Why NoorPath" : "About Us"}
        </Link>
        {mode === "detail" ? <Link href="/journeys">My Journey</Link> : null}
        <a href="mailto:support@noorpath.example">
          {mode === "landing" ? "Help" : "Support"}
        </a>
      </nav>

      {mode === "landing" ? (
        <Link
          className="header-profile-button"
          href="/operator"
          aria-label="Open operator portal"
        >
          <Icon name="user-circle" />
        </Link>
      ) : (
        <div className="public-header-actions">
          <a
            className="header-action header-action-secondary"
            href="tel:+0000000000"
          >
            <Icon name="phone" /> Request a callback
          </a>
          <a
            className="header-action header-action-primary"
            href="mailto:support@noorpath.example"
          >
            <Icon name="whatsapp-logo" /> WhatsApp support
          </a>
        </div>
      )}

      <details className="public-mobile-menu">
        <summary aria-label="Open navigation">
          <span />
          <span />
          <span />
        </summary>
        <nav aria-label="Mobile navigation">
          <Link href="/#packages">Packages</Link>
          <Link href="/#plan-ahead">Plan ahead</Link>
          <Link href="/#trust">Why NoorPath</Link>
          <Link href="/journeys">My Journey</Link>
          <a href="mailto:support@noorpath.example">Support</a>
        </nav>
      </details>

      <a
        className="public-mobile-contact"
        href={mode === "landing" ? "/operator" : "tel:+0000000000"}
        aria-label={
          mode === "landing" ? "Open operator portal" : "Request a callback"
        }
      >
        <Icon name={mode === "landing" ? "user-circle" : "phone"} />
      </a>
    </header>
  );
}

export function PublicFooter() {
  return (
    <footer className="public-footer">
      <NoorPathBrand mode="landing" />
      <p>
        Plan your Umrah early, understand every commitment, and stay supported
        from booking through travel readiness.
      </p>
      <nav className="public-footer-links" aria-label="Footer navigation">
        <Link href="/#packages">Explore packages</Link>
        <Link href="/#plan-ahead">Plan ahead</Link>
        <a href="mailto:support@noorpath.example">Contact support</a>
      </nav>
    </footer>
  );
}

export function Icon({
  name,
  className = "",
}: {
  name: string;
  className?: string;
}) {
  return (
    <Image
      className={`public-icon ${className}`}
      src={`/assets/icons/${name}.svg`}
      alt=""
      aria-hidden="true"
      width={24}
      height={24}
    />
  );
}
