import Link from "next/link";

function MarkIcon() {
  return (
    <svg
      aria-hidden="true"
      fill="none"
      height="22"
      viewBox="0 0 24 24"
      width="22"
    >
      <path
        d="M12 3 18.5 9.5 12 21 5.5 9.5 12 3Z"
        stroke="currentColor"
        strokeLinejoin="round"
        strokeWidth="1.6"
      />
      <path d="M8.5 10h7" stroke="currentColor" strokeWidth="1.6" />
    </svg>
  );
}

export function NoorPathBrand() {
  return (
    <Link className="public-brand" href="/" aria-label="NoorPath home">
      <span className="public-brand-mark">
        <MarkIcon />
      </span>
      <span>NoorPath</span>
    </Link>
  );
}

export function PublicHeader() {
  return (
    <header className="public-topbar">
      <NoorPathBrand />
      <nav aria-label="Primary navigation" className="public-nav">
        <Link href="/#packages">Packages</Link>
        <Link href="/#trust">Why NoorPath</Link>
        <a href="mailto:support@noorpath.example">Support</a>
      </nav>
      <a
        className="public-support-button"
        href="mailto:support@noorpath.example"
      >
        Human support
      </a>
    </header>
  );
}

export function PublicFooter() {
  return (
    <footer className="public-footer">
      <div>
        <NoorPathBrand />
        <p>
          Calm, factual Umrah journey information with operator accountability
          and human support.
        </p>
      </div>
      <div className="public-footer-links" aria-label="Footer navigation">
        <Link href="/#packages">Explore packages</Link>
        <Link href="/#trust">Trust & clarity</Link>
        <a href="mailto:support@noorpath.example">Contact human support</a>
      </div>
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
      {confirmed ? "Confirmed fact" : "Pending confirmation"}
    </span>
  );
}
