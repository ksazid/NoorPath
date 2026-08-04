import Link from "next/link";

export default function PrivacyPage() {
  return (
    <main className="np-route-information" id="main-content">
      <nav className="package-breadcrumbs" aria-label="Breadcrumb">
        <Link href="/">Home</Link>
        <span aria-hidden="true">›</span>
        <span aria-current="page">Privacy</span>
      </nav>
      <p className="public-eyebrow">Privacy</p>
      <h1>How NoorPath handles journey information</h1>
      <p>
        NoorPath limits customer, traveller, booking and document information to
        the people and services that need it to operate the requested journey.
      </p>
      <section>
        <h2>Account and booking access</h2>
        <p>
          Customer records remain account-isolated. Operator and administrator
          access requires explicit membership and permission.
        </p>
      </section>
      <section>
        <h2>Sensitive documents</h2>
        <p>
          Passport and visa documents must use approved private storage and
          time-limited access. Do not send sensitive documents through ordinary
          support email.
        </p>
      </section>
      <section>
        <h2>Questions</h2>
        <p>
          Contact <Link href="/support">NoorPath support</Link> for privacy or
          account-access questions.
        </p>
      </section>
    </main>
  );
}
