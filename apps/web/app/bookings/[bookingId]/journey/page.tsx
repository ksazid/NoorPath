"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useState } from "react";
import { useDeferredInitialLoad } from "../../../../lib/use-deferred-initial-load";
import { Icon, PublicFooter, PublicHeader } from "../../../public-ui";

type Journey = {
  bookingId: string;
  bookingReference: string;
  state: string;
  occupancy: string;
  confirmedAtUtc: string;
  journey: {
    packageName: string;
    origin: string;
    departureDate: string;
    returnDate: string;
    makkahHotelName: string;
    makkahNights: number;
    madinahHotelName: string;
    madinahNights: number;
    travelRouteSummary: string;
  };
  travellers: { travellerId: string; fullName: string }[];
  family: null | {
    familyPartyId: string;
    partyName: string;
    policyVersion: string;
    partyVersion: number;
    travellers: { travellerId: string; fullName: string }[];
    mahramLinks: {
      protectedTravellerId: string;
      protectedTravellerName: string;
      mahramTravellerId: string;
      mahramTravellerName: string;
      relationshipType: string;
    }[];
    disclaimer: string;
  };
  commercial: {
    currency: string;
    total: number;
    paid: number;
    remaining: number;
  };
  payment: {
    state: string;
    instalments: {
      sequence: number;
      dueDate: string;
      amount: number;
      status: string;
    }[];
  };
  readiness: { documents: string; visa: string };
  support: { bookingReference: string; correlationId: string };
};

export default function JourneyPage() {
  const { bookingId } = useParams<{ bookingId: string }>();
  const [state, setState] = useState<
    | { kind: "loading" }
    | { kind: "ready"; journey: Journey }
    | { kind: "delayed"; correlationId?: string }
    | { kind: "error" }
  >({ kind: "loading" });
  const load = useCallback(async () => {
    setState({ kind: "loading" });
    try {
      const response = await fetch(
        `/api/v1/journeys/${encodeURIComponent(bookingId)}`,
        {
          cache: "no-store",
          credentials: "include",
          headers: process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY
            ? {
                "X-NoorPath-Test-Identity":
                  process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY,
              }
            : {},
        },
      );
      if (response.status === 503) {
        const problem = (await response.json()) as { correlationId?: string };
        setState({ kind: "delayed", correlationId: problem.correlationId });
      } else if (!response.ok) setState({ kind: "error" });
      else
        setState({
          kind: "ready",
          journey: (await response.json()) as Journey,
        });
    } catch {
      setState({ kind: "error" });
    }
  }, [bookingId]);
  useDeferredInitialLoad(load);

  return (
    <div className="journey-page">
      <PublicHeader mode="detail" />
      <main id="main-content" className="journey-main journey-detail-main">
        <nav className="package-breadcrumbs" aria-label="Breadcrumb">
          <Link href="/journeys">My Journey</Link>
          <span>/</span>
          <span aria-current="page">Booking</span>
        </nav>
        <div aria-live="polite">
          {state.kind === "loading" ? (
            <JourneyNotice title="Checking your journey" icon="clock">
              Loading the latest booking and payment facts.
            </JourneyNotice>
          ) : null}
          {state.kind === "delayed" ? (
            <JourneyNotice
              title="Your journey is still being prepared"
              icon="clock"
            >
              Your booking remains confirmed. Please retry shortly.
              {state.correlationId ? (
                <small> Support reference: {state.correlationId}</small>
              ) : null}
              <button onClick={() => void load()}>Retry</button>
            </JourneyNotice>
          ) : null}
          {state.kind === "error" ? (
            <JourneyNotice
              title="Journey temporarily unavailable"
              icon="headset"
            >
              We could not load this journey. Check that you are signed into the
              account that booked it.
              <button onClick={() => void load()}>Retry</button>
            </JourneyNotice>
          ) : null}
        </div>
        {state.kind === "ready" ? <Dashboard journey={state.journey} /> : null}
      </main>
      <PublicFooter />
    </div>
  );
}

function Dashboard({ journey: j }: { journey: Journey }) {
  const m = (value: number) => money(j.commercial.currency, value);
  return (
    <>
      <section className="journey-hero">
        <div>
          <p className="public-eyebrow">Confirmed · {j.bookingReference}</p>
          <h1>{j.journey.packageName}</h1>
          <p>{j.journey.travelRouteSummary}</p>
        </div>
        <span className="journey-confirmed">
          <Icon name="seal-check" /> Confirmed
        </span>
      </section>
      <section className="journey-grid" aria-label="Journey overview">
        <article>
          <p className="public-eyebrow">Departure</p>
          <h2>{date(j.journey.departureDate)}</h2>
          <p>
            From {j.journey.origin} · Return {date(j.journey.returnDate)}
          </p>
        </article>
        <article>
          <p className="public-eyebrow">Stay</p>
          <h2>{j.journey.makkahNights + j.journey.madinahNights} nights</h2>
          <p>
            {j.journey.makkahNights} nights at {j.journey.makkahHotelName}
          </p>
          <p>
            {j.journey.madinahNights} nights at {j.journey.madinahHotelName}
          </p>
        </article>
        <article>
          <p className="public-eyebrow">Travellers · {j.occupancy}</p>
          <h2>{j.travellers.length} confirmed</h2>
          <ul>
            {j.travellers.map((t) => (
              <li key={t.travellerId}>{t.fullName}</li>
            ))}
          </ul>
        </article>
      </section>
      {j.family ? (
        <section className="journey-panel" aria-labelledby="family-title">
          <p className="public-eyebrow">Booked family snapshot</p>
          <h2 id="family-title">{j.family.partyName}</h2>
          <p>
            This read-only composition is the exact validated family snapshot
            retained for checkout under policy {j.family.policyVersion}.
          </p>
          <div className="journey-readiness">
            <div>
              <Icon name="users" />
              <h3>{j.family.travellers.length} family travellers</h3>
              <ul>
                {j.family.travellers.map((traveller) => (
                  <li key={traveller.travellerId}>{traveller.fullName}</li>
                ))}
              </ul>
            </div>
            <div>
              <Icon name="link" />
              <h3>Declared Mahram relationships</h3>
              {j.family.mahramLinks.length ? (
                <ul>
                  {j.family.mahramLinks.map((link) => (
                    <li
                      key={`${link.protectedTravellerId}-${link.mahramTravellerId}`}
                    >
                      {link.protectedTravellerName} → {link.mahramTravellerName} (
                      {humanize(link.relationshipType)})
                    </li>
                  ))}
                </ul>
              ) : (
                <p>No Mahram relationship was included in the booked snapshot.</p>
              )}
            </div>
          </div>
          <p>{j.family.disclaimer}</p>
        </section>
      ) : null}
      <section className="journey-panel" aria-labelledby="payment-title">
        <div>
          <p className="public-eyebrow">Payment schedule</p>
          <h2 id="payment-title">Your commercial commitment</h2>
        </div>
        <dl className="journey-money">
          <div>
            <dt>Total price</dt>
            <dd>{m(j.commercial.total)}</dd>
          </div>
          <div>
            <dt>Paid now</dt>
            <dd>{m(j.commercial.paid)}</dd>
          </div>
          <div>
            <dt>Remaining balance</dt>
            <dd>{m(j.commercial.remaining)}</dd>
          </div>
        </dl>
        {j.payment.instalments.length ? (
          <ol className="journey-instalments">
            {j.payment.instalments.map((i) => (
              <li key={i.sequence}>
                <span>
                  Instalment {i.sequence}
                  <small>Due {date(i.dueDate)}</small>
                </span>
                <strong>{m(i.amount)}</strong>
                <span className="journey-scheduled">{i.status}</span>
              </li>
            ))}
          </ol>
        ) : (
          <p className="journey-paid">
            <Icon name="seal-check" /> Payment complete — no remaining
            instalments.
          </p>
        )}
      </section>
      <section className="journey-panel">
        <p className="public-eyebrow">Travel readiness</p>
        <h2>What happens next</h2>
        <div className="journey-readiness">
          <div>
            <Icon name="identification-card" />
            <h3>Documents</h3>
            <p>
              Upload each traveller&apos;s required passport documents and
              follow their review status.
            </p>
            <Link href={`/bookings/${j.bookingId}/documents`}>
              Manage documents
            </Link>
          </div>
          <div>
            <Icon name="file-text" />
            <h3>Visa</h3>
            <p>
              See each traveller&apos;s current visa progress and any action
              needed.
            </p>
            <Link href={`/bookings/${j.bookingId}/visa`}>View visa status</Link>
          </div>
        </div>
      </section>
      <aside className="journey-support">
        <Icon name="headset" />
        <div>
          <h2>Need human support?</h2>
          <p>
            Share booking reference{" "}
            <strong>{j.support.bookingReference}</strong>. Do not send passport
            or payment details by email.
          </p>
          <a
            href={`mailto:support@noorpath.example?subject=${encodeURIComponent(`Booking ${j.support.bookingReference}`)}&body=${encodeURIComponent(`Booking reference: ${j.support.bookingReference}\nSupport reference: ${j.support.correlationId}`)}`}
          >
            Contact support
          </a>
        </div>
      </aside>
    </>
  );
}

function JourneyNotice({
  title,
  icon,
  children,
}: {
  title: string;
  icon: string;
  children: React.ReactNode;
}) {
  return (
    <section className="journey-state">
      <Icon name={icon} />
      <h1>{title}</h1>
      <div>{children}</div>
    </section>
  );
}
function date(value: string) {
  return new Date(`${value}T00:00:00Z`).toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
    timeZone: "UTC",
  });
}
function money(currency: string, amount: number) {
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(amount);
}
function humanize(value: string) {
  return value.replaceAll(/([A-Z])/g, " $1").trim();
}
