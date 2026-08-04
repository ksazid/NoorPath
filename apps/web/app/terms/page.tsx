import Link from "next/link";

export default function TermsPage() {
  return (
    <main className="np-route-information" id="main-content">
      <nav className="package-breadcrumbs" aria-label="Breadcrumb">
        <Link href="/">Home</Link>
        <span aria-hidden="true">›</span>
        <span aria-current="page">Terms</span>
      </nav>
      <p className="public-eyebrow">Journey terms</p>
      <h1>Understand every commitment before booking</h1>
      <p>
        The authoritative package facts, traveller-specific quote, payment
        schedule, cancellation entitlement and refund terms are shown before a
        customer makes a booking or payment commitment.
      </p>
      <section>
        <h2>Published package facts</h2>
        <p>
          Availability, operator, stay, travel and pricing information may change
          until a place is successfully held and the booking is confirmed.
        </p>
      </section>
      <section>
        <h2>Payments and cancellations</h2>
        <p>
          A customer must review the exact total, due-now amount, remaining
          balance, due dates and applicable cancellation/refund terms before
          confirming.
        </p>
      </section>
      <section>
        <h2>Need clarification?</h2>
        <p>
          Use <Link href="/support">NoorPath support</Link> before committing if
          any journey term is unclear.
        </p>
      </section>
    </main>
  );
}
