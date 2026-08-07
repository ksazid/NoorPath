"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useMemo, useState } from "react";
import { useDeferredInitialLoad } from "../../lib/use-deferred-initial-load";
import OperatorWorkspaceShell from "./OperatorWorkspaceShell";

type BookingDetail = {
  bookingId: string;
  reference: string;
  packageName: string;
  state: string;
  occupancy: string;
  travellerCount: number;
  payment: {
    currency: string;
    total: number;
    paid: number;
    outstanding: number;
  };
  travellers: Array<{
    travellerId: string;
    fullName: string;
    dateOfBirth: string;
  }>;
};

type TravellerDraft = {
  travellerId: string;
  fullName: string;
  dateOfBirth: string;
};

type Financials = {
  currency: string;
  unitPrice: number;
  total: number;
  dueNow: number;
  remaining: number;
  instalments: Array<{
    sequence: number;
    dueDate: string;
    amount: number;
  }>;
};

type Preview = {
  bookingId: string;
  reference: string;
  bookingVersion: number;
  current: {
    occupancy: string;
    travellers: TravellerDraft[];
    financials: Financials;
  };
  proposed: {
    occupancy: string;
    travellers: TravellerDraft[];
    financials: Financials;
    priceVersionId: string;
  };
  priceDelta: number;
  changesMoney: boolean;
  previewToken: string;
  expiresAtUtc: string;
};

type LoadState =
  | { kind: "loading" }
  | { kind: "ready"; detail: BookingDetail }
  | { kind: "forbidden" }
  | { kind: "not-found" }
  | { kind: "error" };

type ActionState = "idle" | "previewing" | "confirming";

const emptyTravellerId = "00000000-0000-0000-0000-000000000000";
const requiredTravellerCount: Record<string, number> = {
  double: 2,
  triple: 3,
  quad: 4,
};
const moneyFormatter = new Intl.NumberFormat("en-IN", {
  maximumFractionDigits: 0,
});

function money(currency: string, value: number) {
  return `${currency} ${moneyFormatter.format(value)}`;
}

function occupancyLabel(value: string) {
  return `${value.charAt(0).toUpperCase()}${value.slice(1)} sharing`;
}

function readProblemMessage(payload: unknown, fallback: string) {
  if (!payload || typeof payload !== "object") return fallback;
  const candidate = payload as {
    message?: string;
    title?: string;
    errors?: Record<string, string[]>;
  };
  if (candidate.message) return candidate.message;
  if (candidate.errors) {
    const first = Object.values(candidate.errors).flat()[0];
    if (first) return first;
  }
  return candidate.title ?? fallback;
}

export default function OperatorBookingAmendment({
  bookingId,
}: {
  bookingId: string;
}) {
  const router = useRouter();
  const [loadState, setLoadState] = useState<LoadState>({ kind: "loading" });
  const [occupancy, setOccupancy] = useState("double");
  const [travellers, setTravellers] = useState<TravellerDraft[]>([]);
  const [reason, setReason] = useState("");
  const [preview, setPreview] = useState<Preview | null>(null);
  const [actionState, setActionState] = useState<ActionState>("idle");
  const [error, setError] = useState("");
  const [confirmed, setConfirmed] = useState(false);

  const load = useCallback(async () => {
    setLoadState({ kind: "loading" });
    setError("");
    try {
      const response = await fetch(`/api/v1/operator/bookings/${bookingId}`, {
        credentials: "include",
        cache: "no-store",
      });
      if (response.status === 403) {
        setLoadState({ kind: "forbidden" });
        return;
      }
      if (response.status === 404) {
        setLoadState({ kind: "not-found" });
        return;
      }
      if (!response.ok) throw new Error();
      const detail = (await response.json()) as BookingDetail;
      setLoadState({ kind: "ready", detail });
      setOccupancy(detail.occupancy);
      setTravellers(
        detail.travellers.map((traveller) => ({
          travellerId: traveller.travellerId,
          fullName: traveller.fullName,
          dateOfBirth: traveller.dateOfBirth,
        })),
      );
    } catch {
      setLoadState({ kind: "error" });
    }
  }, [bookingId]);

  useDeferredInitialLoad(load);

  const requiredCount = requiredTravellerCount[occupancy] ?? 0;
  const canPreview = useMemo(
    () =>
      loadState.kind === "ready" &&
      loadState.detail.state === "confirmed" &&
      requiredCount > 0 &&
      travellers.length === requiredCount &&
      travellers.every(
        (traveller) => traveller.fullName.trim() && traveller.dateOfBirth,
      ) &&
      reason.trim().length > 0 &&
      actionState === "idle",
    [loadState, requiredCount, travellers, reason, actionState],
  );

  const changeOccupancy = (next: string) => {
    setOccupancy(next);
    setPreview(null);
    setConfirmed(false);
    setError("");
    const nextCount = requiredTravellerCount[next] ?? 0;
    setTravellers((current) => {
      const resized = current.slice(0, nextCount);
      while (resized.length < nextCount) {
        resized.push({
          travellerId: emptyTravellerId,
          fullName: "",
          dateOfBirth: "",
        });
      }
      return resized;
    });
  };

  const updateTraveller = (
    index: number,
    field: "fullName" | "dateOfBirth",
    value: string,
  ) => {
    setPreview(null);
    setConfirmed(false);
    setTravellers((current) =>
      current.map((traveller, travellerIndex) =>
        travellerIndex === index ? { ...traveller, [field]: value } : traveller,
      ),
    );
  };

  const createPreview = async () => {
    if (!canPreview) return;
    setActionState("previewing");
    setPreview(null);
    setConfirmed(false);
    setError("");
    try {
      const response = await fetch(
        `/api/v1/operator/bookings/${bookingId}/amendments/preview`,
        {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            occupancy,
            travellers,
            reason: reason.trim(),
          }),
        },
      );
      const payload = (await response.json().catch(() => null)) as unknown;
      if (!response.ok) {
        setError(
          readProblemMessage(
            payload,
            response.status === 409
              ? "This booking changed or is not currently amendable. Refresh and try again."
              : "The amendment preview could not be created. Review the proposed changes and retry.",
          ),
        );
        return;
      }
      setPreview(payload as Preview);
    } catch {
      setError(
        "The amendment preview is temporarily unavailable. Your edits are still here; retry safely.",
      );
    } finally {
      setActionState("idle");
    }
  };

  const confirmAmendment = async () => {
    if (!preview || !confirmed || actionState !== "idle") return;
    setActionState("confirming");
    setError("");
    try {
      const response = await fetch(
        `/api/v1/operator/bookings/${bookingId}/amendments/confirm`,
        {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            previewToken: preview.previewToken,
            confirmed: true,
          }),
        },
      );
      const payload = (await response.json().catch(() => null)) as unknown;
      if (!response.ok) {
        setPreview(null);
        setConfirmed(false);
        setError(
          readProblemMessage(
            payload,
            response.status === 409
              ? "The preview is stale. Refresh the booking and create a new preview before confirming."
              : "The amendment could not be confirmed. No partial change was applied.",
          ),
        );
        return;
      }
      router.replace(`/operator/bookings/${bookingId}?amended=1`);
      router.refresh();
    } catch {
      setError(
        "The amendment could not be confirmed. Refresh the booking before retrying so you do not act on stale information.",
      );
      setPreview(null);
      setConfirmed(false);
    } finally {
      setActionState("idle");
    }
  };

  return (
    <OperatorWorkspaceShell
      title="Amend booking"
      summary="Change the booked traveller snapshot or occupancy only after reviewing a server-authoritative commercial impact. Payments, documents, visa and cancellation remain separate governed workflows."
    >
      <section className="operator-amendment" aria-live="polite">
        {loadState.kind === "loading" ? (
          <div className="operator-booking-state">
            Loading booking amendment…
          </div>
        ) : null}

        {loadState.kind === "forbidden" ? (
          <div className="operator-booking-state">
            <strong>
              You do not have access to amend this operator booking.
            </strong>
            <Link className="auth-secondary" href="/operator/bookings">
              Back to bookings
            </Link>
          </div>
        ) : null}

        {loadState.kind === "not-found" ? (
          <div className="operator-booking-state">
            <strong>Booking not found.</strong>
            <p>
              This booking is unavailable or does not belong to your operator
              account.
            </p>
            <Link className="auth-secondary" href="/operator/bookings">
              Back to bookings
            </Link>
          </div>
        ) : null}

        {loadState.kind === "error" ? (
          <div className="operator-booking-state">
            <strong>Booking amendment is temporarily unavailable.</strong>
            <button className="auth-secondary" type="button" onClick={load}>
              Retry
            </button>
          </div>
        ) : null}

        {loadState.kind === "ready" ? (
          <>
            <div className="operator-amendment__back">
              <Link href={`/operator/bookings/${bookingId}`}>
                ← Back to booking {loadState.detail.reference}
              </Link>
            </div>

            <article className="operator-amendment__hero">
              <div>
                <p className="auth-eyebrow">
                  Booking {loadState.detail.reference}
                </p>
                <h2>{loadState.detail.packageName}</h2>
                <p>
                  Current booking: {occupancyLabel(loadState.detail.occupancy)}{" "}
                  · {loadState.detail.travellerCount} travellers
                </p>
              </div>
              <span className="operator-booking-badge good">
                {loadState.detail.state === "confirmed"
                  ? "Confirmed"
                  : loadState.detail.state}
              </span>
            </article>

            {loadState.detail.state !== "confirmed" ? (
              <div className="operator-amendment__notice" role="status">
                <strong>
                  This booking cannot be amended in its current state.
                </strong>
                <p>
                  VS-25 permits amendments only after the booking is confirmed.
                  Continue in the owning workflow until the booking becomes
                  eligible.
                </p>
              </div>
            ) : (
              <>
                {error ? (
                  <div className="operator-amendment__error" role="alert">
                    <strong>Review amendment</strong>
                    <span>{error}</span>
                  </div>
                ) : null}

                <section
                  className="operator-amendment__section"
                  aria-labelledby="amendment-details-heading"
                >
                  <div>
                    <p className="auth-eyebrow">Step 1</p>
                    <h2 id="amendment-details-heading">
                      Proposed booking snapshot
                    </h2>
                    <p>
                      Change only the operational booking snapshot. Passport,
                      document and visa facts are not edited here.
                    </p>
                  </div>

                  <label className="operator-amendment__field">
                    <span>Occupancy</span>
                    <select
                      value={occupancy}
                      onChange={(event) => changeOccupancy(event.target.value)}
                    >
                      <option value="double">
                        Double sharing · 2 travellers
                      </option>
                      <option value="triple">
                        Triple sharing · 3 travellers
                      </option>
                      <option value="quad">Quad sharing · 4 travellers</option>
                    </select>
                  </label>

                  <div className="operator-amendment__travellers">
                    {travellers.map((traveller, index) => (
                      <fieldset key={`${traveller.travellerId}-${index}`}>
                        <legend>Traveller {index + 1}</legend>
                        <label className="operator-amendment__field">
                          <span>Full name</span>
                          <input
                            value={traveller.fullName}
                            autoComplete="off"
                            maxLength={120}
                            onChange={(event) =>
                              updateTraveller(
                                index,
                                "fullName",
                                event.target.value,
                              )
                            }
                          />
                        </label>
                        <label className="operator-amendment__field">
                          <span>Date of birth</span>
                          <input
                            type="date"
                            value={traveller.dateOfBirth}
                            onChange={(event) =>
                              updateTraveller(
                                index,
                                "dateOfBirth",
                                event.target.value,
                              )
                            }
                          />
                        </label>
                      </fieldset>
                    ))}
                  </div>

                  <label className="operator-amendment__field">
                    <span>Reason for amendment</span>
                    <textarea
                      value={reason}
                      maxLength={500}
                      rows={4}
                      placeholder="Explain why the booked traveller snapshot or occupancy needs to change."
                      onChange={(event) => {
                        setReason(event.target.value);
                        setPreview(null);
                        setConfirmed(false);
                      }}
                    />
                    <small>{reason.trim().length}/500 characters</small>
                  </label>

                  <button
                    className="auth-primary operator-amendment__primary"
                    type="button"
                    disabled={!canPreview}
                    onClick={createPreview}
                  >
                    {actionState === "previewing"
                      ? "Preparing price impact…"
                      : "Preview amendment impact"}
                  </button>
                </section>

                {preview ? (
                  <section
                    className="operator-amendment__section"
                    aria-labelledby="price-impact-heading"
                  >
                    <div>
                      <p className="auth-eyebrow">Step 2</p>
                      <h2 id="price-impact-heading">Review price impact</h2>
                      <p>
                        This preview is authoritative for booking version{" "}
                        {preview.bookingVersion} and expires automatically.
                      </p>
                    </div>

                    <div
                      className="operator-amendment__comparison"
                      aria-label="Current and proposed financial snapshots"
                    >
                      <article>
                        <span>Current total</span>
                        <strong>
                          {money(
                            preview.current.financials.currency,
                            preview.current.financials.total,
                          )}
                        </strong>
                        <small>
                          {occupancyLabel(preview.current.occupancy)} ·{" "}
                          {preview.current.travellers.length} travellers
                        </small>
                      </article>
                      <article>
                        <span>Proposed total</span>
                        <strong>
                          {money(
                            preview.proposed.financials.currency,
                            preview.proposed.financials.total,
                          )}
                        </strong>
                        <small>
                          {occupancyLabel(preview.proposed.occupancy)} ·{" "}
                          {preview.proposed.travellers.length} travellers
                        </small>
                      </article>
                      <article>
                        <span>Price delta</span>
                        <strong>
                          {preview.priceDelta === 0
                            ? "No change"
                            : `${preview.priceDelta > 0 ? "+" : "−"}${money(
                                preview.proposed.financials.currency,
                                Math.abs(preview.priceDelta),
                              )}`}
                        </strong>
                        <small>
                          {preview.priceDelta > 0
                            ? "Additional collection stays in Payments."
                            : preview.priceDelta < 0
                              ? "Any credit/refund stays in Refunds."
                              : "No payment follow-up required."}
                        </small>
                      </article>
                    </div>

                    <div className="operator-amendment__financial-detail">
                      <span>
                        Due now after amendment:{" "}
                        <strong>
                          {money(
                            preview.proposed.financials.currency,
                            preview.proposed.financials.dueNow,
                          )}
                        </strong>
                      </span>
                      <span>
                        Remaining scheduled balance:{" "}
                        <strong>
                          {money(
                            preview.proposed.financials.currency,
                            preview.proposed.financials.remaining,
                          )}
                        </strong>
                      </span>
                    </div>

                    <label className="operator-amendment__confirm">
                      <input
                        type="checkbox"
                        checked={confirmed}
                        onChange={(event) => setConfirmed(event.target.checked)}
                      />
                      <span>
                        I reviewed the traveller, occupancy and financial impact
                        and want to apply this exact preview.
                      </span>
                    </label>

                    <div className="operator-amendment__actions">
                      <button
                        className="auth-primary operator-amendment__primary"
                        type="button"
                        disabled={!confirmed || actionState !== "idle"}
                        onClick={confirmAmendment}
                      >
                        {actionState === "confirming"
                          ? "Applying amendment…"
                          : "Confirm amendment"}
                      </button>
                      <button
                        className="auth-secondary"
                        type="button"
                        disabled={actionState !== "idle"}
                        onClick={() => {
                          setPreview(null);
                          setConfirmed(false);
                        }}
                      >
                        Edit proposal
                      </button>
                    </div>
                  </section>
                ) : null}
              </>
            )}
          </>
        ) : null}
      </section>
    </OperatorWorkspaceShell>
  );
}
