import Link from "next/link";

export default function SupportPage() {
  return (
    <main className="np-route-information" id="main-content">
      <nav className="package-breadcrumbs" aria-label="Breadcrumb">
        <Link href="/">Home</Link>
        <span aria-hidden="true">›</span>
        <span aria-current="page">Travel support</span>
      </nav>
      <p className="public-eyebrow">Human support</p>
      <h1>Talk to NoorPath</h1>
      <p>
        Get help understanding a package, your booking commitments, documents,
        visa readiness or the next action in your Umrah journey.
      </p>
      <section aria-labelledby="support-email-title">
        <h2 id="support-email-title">Email support</h2>
        <p>Include your booking reference only when discussing an existing journey.</p>
        <a className="auth-primary" href="mailto:support@noorpath.example">
          Email NoorPath support
        </a>
      </section>
      <section id="callback" aria-labelledby="callback-title">
        <h2 id="callback-title">Request a callback</h2>
        <p>
          Callback scheduling will be connected to an approved support provider
          in a later slice. Do not share passport or payment details by email.
        </p>
      </section>
    </main>
  );
}
