import Link from "next/link";

export default function AccountIdentityMenu({
  displayName,
  accountHref,
  settingsHref,
  helpHref = "/support",
}: {
  displayName: string;
  accountHref: string;
  settingsHref: string;
  helpHref?: string;
}) {
  return (
    <details className="np-account-menu">
      <summary aria-label={`Account menu for ${displayName}`}>
        <span className="np-account-menu__avatar" aria-hidden="true">
          {displayName.trim().charAt(0).toUpperCase() || "N"}
        </span>
        <span className="np-account-menu__name">{displayName}</span>
      </summary>
      <div className="np-account-menu__panel">
        <strong>{displayName}</strong>
        <nav aria-label="Account options">
          <Link href={accountHref}>My account</Link>
          <Link href={settingsHref}>Settings</Link>
          <Link href={helpHref}>Help</Link>
          <a href="/api/auth/sign-out">Log out</a>
        </nav>
      </div>
    </details>
  );
}
