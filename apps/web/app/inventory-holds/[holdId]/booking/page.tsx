npm warn Unknown env config "http-proxy". This will stop working in the next major version of npm.
"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Icon, PublicFooter, PublicHeader } from "../../../public-ui";

type BookingResponse = {
  bookingId: string;
  bookingReference: string;
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  code?: string;
  bookingId?: string;
};

type TransitionState =
  | { kind: "preparing" }
  | { kind: "unauthenticated" }
  | { kind: "not-found" }
  | { kind: "error"; message: string };

function requestHeaders(additional: Record<string, string> = {}): HeadersInit {
  const headers: Record<string, string> = { ...additional };
  const testIdentity = process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY;
  if (testIdentity) {
    headers["X-NoorPath-Test-Identity"] = testIdentity;
  }
  return headers;
}

function storageKey(holdId: string) {
  return `noorpath:booking:key:${holdId}`;
}

function getOrCreateBookingKey(holdId: string) {
  const key = storageKey(holdId);
  const existing = window.sessionStorage.getItem(key);
  if (existing) {
    return existing;
  }
  const created = `booking-${window.crypto.randomUUID()}`;
  window.sessionStorage.setItem(key, created);
  return created;
}

export default function HoldBookingTransitionPage() {
  const params = useParams<{ holdId: string }>();
  const router = useRouter();
  const holdId = params.holdId;
  const [state, setState] = useState<TransitionState>({ kind: "preparing" });

  const createOrRecover = useCallback(async () => {
    setState({ kind: "preparing" });
    try {
      const response = await fetch(
        `/api/v1/inventory-holds/${encodeURIComponent(holdId)}/bookings`,
        {
          method: "POST",
          credentials: "include",
          headers: requestHeaders({
            "Idempotency-Key": getOrCreateBookingKey(holdId),
          }),
        },
      );
      const body = (await response.json()) as BookingResponse & ProblemDetails;
      if (response.status === 401) {
        setState({ kind: "unauthenticated" });
        return;
      }
      if (response.status === 404) {
        setState({ kind: "not-found" });
        return;
      }
      if (
        response.status === 409 &&
        body.code === "booking_exists_for_hold" &&
        body.bookingId
      ) {
        router.replace(`/bookings/${body.bookingId}/payment`);
        return;
      }
      if (!response.ok || !body.bookingId) {
        setState({
          kind: "error",
          message:
            body.detail ??
            "NoorPath could not create the booking safely. No payment was started.",
        });
        return;
      }
      router.replace(`/bookings/${body.bookingId}/payment`);
    } catch {
      setState({
        kind: "error",
        message:
          "NoorPath could not prepare the booking. Check your connection and retry safely.",
      });
    }
  }, [holdId, router]);

  useEffect(() => {
    const timer = window.setTimeout(() => void createOrRecover(), 0);
    return () => window.clearTimeout(timer);
  }, [createOrRecover]);

  return (
    <div className="booking-payment-page booking-transition-page">
      <PublicHeader mode="detail" />
      <main id="main-content" className="booking-payment-main">
        <nav className="package-breadcrumbs" aria-label="Breadcrumb">
          <Link href="/">Home</Link>
          <span>/</span>
          <span aria-current="page">Prepare booking</span>
        </nav>
        <section className="booking-transition-card" aria-live="polite">
          <div className="payment-action-icon">
            <Icon name="shield-check" />
          </div>
          {state.kind === "preparing" ? (
            <>
              <p className="public-eyebrow">Secure hand-off</p>
              <h1>Preparing your booking</h1>
              <p>
                NoorPath is copying the exact quote, travellers, occupancy and
                payment schedule you reviewed. No payment has started yet.
              </p>
              <div className="booking-transition-progress" role="status">
                <span aria-hidden="true" />
                Creating or recovering one booking safely…
              </div>
            </>
          ) : null}

          {state.kind === "unauthenticated" ? (
            <>
              <p className="public-eyebrow">Private booking</p>
              <h1>Sign in to continue</h1>
              <p>
                The secured hold and booking are private to the NoorPath account
                that created them.
              </p>
            </>
          ) : null}

          {state.kind === "not-found" ? (
            <>
              <p className="public-eyebrow">Hold unavailable</p>
              <h1>This secured availability could not be found</h1>
              <p>
                Return to your plan and secure availability again before
                creating a booking.
              </p>
              <Link className="payment-primary-action" href="/packages">
                Browse packages
              </Link>
            </>
          ) : null}

          {state.kind === "error" ? (
            <>
              <p className="public-eyebrow">Safe retry</p>
              <h1>Booking needs your attention</h1>
              <p>{state.message}</p>
              <button
                className="payment-primary-action"
                type="button"
                onClick={() => void createOrRecover()}
              >
                Retry safely
              </button>
            </>
          ) : null}
        </section>
      </main>
      <PublicFooter />
    </div>
  );
}
npm notice
npm notice New minor version of npm available! 11.9.0 -> 11.19.0
npm notice Changelog: https://github.com/npm/cli/releases/tag/v11.19.0
npm notice To update run: npm install -g npm@11.19.0
npm notice
