"use client";

import Link from "next/link";
import { useCallback, useState } from "react";
import { Icon, PublicFooter, PublicHeader } from "../public-ui";

type Journey = {
  bookingId: string;
  bookingReference: string;
  travellerCount: number;
  currency: string;
  total: number;
  confirmedAtUtc: string;
};

export default function JourneysPage() {
  const [state, setState] = useState<
    | { kind: "loading" }
    | { kind: "ready"; items: Journey[] }
    | { kind: "error" }
  >({ kind: "loading" });
  const load = useCallback(async () => {
    setState({ kind: "loading" });
    try {
      const response = await fetch("/api/v1/journeys", {
        cache: "no-store",
        credentials: "include",
        headers: testHeaders(),
      });
      if (!response.ok) throw new Error();
      const body = (await response.json()) as { items: Journey[] };
      setState({ kind: "ready", items: body.items });
    } catch {
      setState({ kind: "error" });
    }
  }, []);

  useDeferredInitialLoad(load);

  return (
    <div className="journey-page">
      <PublicHeader mode="detail" />
      <main id="main-content" className="journey-main">
        <p className="public-eyebrow">Your account</p>
        <h1>My Journey</h1>
        <p className="journey-intro">
          Your confirmed Umrah bookings, payment commitments and next steps in
          one trusted place.
        </p>
        <div aria-live="polite">
          {state.kind === "loading" ? (
            <JourneyState icon="clock" title="Loading your journeys">
              We are checking your latest confirmed bookings.
            </JourneyState>
          ) : null}
          {state.kind === "error" ? (
            <JourneyState
              icon="headset"
              title="Journeys temporarily unavailable"
            >
              Your bookings are unchanged. Please retry.
              <button onClick={() => void load()}>Retry</button>
            </JourneyState>
          ) : null}
          {state.kind === "ready" && state.items.length === 0 ? (
            <JourneyState icon="map-trifold" title="No confirmed journeys yet">
              When a booking is confirmed, it will appear here with its payment
              plan and travel facts.
              <Link href="/#packages">Explore packages</Link>
            </JourneyState>
          ) : null}
          {state.kind === "ready" && state.items.length > 0 ? (
            <ul className="journey-list" aria-label="Confirmed journeys">
              {state.items.map((item) => (
                <li key={item.bookingId}>
                  <p className="public-eyebrow">Confirmed booking</p>
                  <h2>{item.bookingReference}</h2>
                  <p>
                    {item.travellerCount} traveller
                    {item.travellerCount === 1 ? "" : "s"} ·{" "}
                    {money(item.currency, item.total)} total
                  </p>
                  <Link href={`/bookings/${item.bookingId}/journey`}>
                    View journey <span aria-hidden="true">→</span>
                  </Link>
                </li>
              ))}
            </ul>
          ) : null}
        </div>
      </main>
      <PublicFooter />
    </div>
  );
}

function JourneyState({
  icon,
  title,
  children,
}: {
  icon: string;
  title: string;
  children: React.ReactNode;
}) {
  return (
    <section className="journey-state">
      <Icon name={icon} />
      <h2>{title}</h2>
      <div>{children}</div>
    </section>
  );
}

function testHeaders(): HeadersInit {
  return process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY
    ? {
        "X-NoorPath-Test-Identity":
          process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY,
      }
    : {};
}

function money(currency: string, amount: number) {
  return new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(amount);
}
