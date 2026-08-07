"use client";

import Link from "next/link";
import { useCallback, useMemo, useState } from "react";
import { useDeferredInitialLoad } from "../../lib/use-deferred-initial-load";
import OperatorWorkspaceShell from "./OperatorWorkspaceShell";

type Traveller = {
  travellerId: string;
  position: number;
  fullName: string;
};

type Room = {
  roomId: string;
  stay: "makkah" | "madinah";
  roomType: "double" | "triple" | "quad";
  label: string;
  capacity: number;
  version: number;
  isLocked: boolean;
  occupants: string[];
};

type Workspace = {
  bookingId: string;
  reference: string;
  bookingState: string;
  bookingOccupancy: string;
  travellers: Traveller[];
  rooms: Room[];
  unassigned: Array<{ stay: string; travellerIds: string[] }>;
  history: Array<{
    auditId: string;
    travellerId?: string | null;
    previousRoomId?: string | null;
    roomId?: string | null;
    stay: string;
    action: string;
    reason: string;
    occurredAtUtc: string;
  }>;
};

type LoadState =
  | { kind: "loading" }
  | { kind: "ready"; workspace: Workspace }
  | { kind: "forbidden" }
  | { kind: "not-found" }
  | { kind: "error" };

type CreateRoomDraft = {
  stay: "makkah" | "madinah";
  roomType: "double" | "triple" | "quad";
  label: string;
};

const stayLabels = { makkah: "Makkah", madinah: "Madinah" } as const;
const roomTypeLabels = {
  double: "Double sharing",
  triple: "Triple sharing",
  quad: "Quad sharing",
} as const;

function readProblem(payload: unknown, fallback: string) {
  if (!payload || typeof payload !== "object") return fallback;
  const value = payload as {
    message?: string;
    title?: string;
    errors?: Record<string, string[]>;
    code?: string;
  };
  if (value.message) return value.message;
  const first = value.errors
    ? Object.values(value.errors).flat()[0]
    : undefined;
  return first ?? value.title ?? fallback;
}

export default function OperatorAccommodation({
  bookingId,
}: {
  bookingId: string;
}) {
  const [state, setState] = useState<LoadState>({ kind: "loading" });
  const [roomDraft, setRoomDraft] = useState<CreateRoomDraft>({
    stay: "makkah",
    roomType: "double",
    label: "",
  });
  const [selectedTraveller, setSelectedTraveller] = useState<
    Record<string, string>
  >({});
  const [reasonByRoom, setReasonByRoom] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  const load = useCallback(async () => {
    setState({ kind: "loading" });
    setError("");
    try {
      const response = await fetch(
        `/api/v1/operator/bookings/${bookingId}/accommodation`,
        {
          credentials: "include",
          cache: "no-store",
        },
      );
      if (response.status === 403) return setState({ kind: "forbidden" });
      if (response.status === 404) return setState({ kind: "not-found" });
      if (!response.ok) throw new Error();
      setState({
        kind: "ready",
        workspace: (await response.json()) as Workspace,
      });
    } catch {
      setState({ kind: "error" });
    }
  }, [bookingId]);

  useDeferredInitialLoad(load);

  const travellersById = useMemo(() => {
    if (state.kind !== "ready") return new Map<string, Traveller>();
    return new Map(
      state.workspace.travellers.map((traveller) => [
        traveller.travellerId,
        traveller,
      ]),
    );
  }, [state]);

  const mutate = async (
    key: string,
    request: () => Promise<Response>,
    success: string,
  ) => {
    if (busy) return;
    setBusy(key);
    setError("");
    setMessage("");
    try {
      const response = await request();
      const payload = (await response.json().catch(() => null)) as unknown;
      if (!response.ok) {
        setError(
          readProblem(
            payload,
            response.status === 409
              ? "The room allocation changed. Refresh and try again."
              : "The accommodation change could not be saved.",
          ),
        );
        return;
      }
      setMessage(success);
      await load();
    } catch {
      setError(
        "Accommodation is temporarily unavailable. Refresh before retrying so you do not act on stale room data.",
      );
    } finally {
      setBusy("");
    }
  };

  const createRoom = async () => {
    const label = roomDraft.label.trim();
    if (!label) {
      setError("Enter a room label before creating the allocation.");
      return;
    }
    await mutate(
      "create-room",
      () =>
        fetch(`/api/v1/operator/bookings/${bookingId}/accommodation/rooms`, {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ ...roomDraft, label }),
        }),
      "Room allocation created.",
    );
    setRoomDraft((current) => ({ ...current, label: "" }));
  };

  const assign = async (room: Room) => {
    if (state.kind !== "ready") return;
    const travellerId = selectedTraveller[room.roomId] ?? "";
    const reason = (reasonByRoom[room.roomId] ?? "").trim();
    if (!travellerId || !reason) {
      setError(
        "Choose a traveller and enter an operational reason before assigning a room.",
      );
      return;
    }
    const previousRoom = state.workspace.rooms.find(
      (candidate) =>
        candidate.stay === room.stay &&
        candidate.occupants.includes(travellerId),
    );
    await mutate(
      `assign-${room.roomId}`,
      () =>
        fetch(
          `/api/v1/operator/bookings/${bookingId}/accommodation/rooms/${room.roomId}/assign`,
          {
            method: "POST",
            credentials: "include",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              travellerId,
              reason,
              expectedRoomVersion: room.version,
              expectedPreviousRoomVersion: previousRoom?.version ?? null,
            }),
          },
        ),
      previousRoom ? "Traveller reassigned." : "Traveller assigned.",
    );
  };

  const unassign = async (room: Room, travellerId: string) => {
    const reason = (reasonByRoom[room.roomId] ?? "").trim();
    if (!reason) {
      setError(
        "Enter an operational reason before removing a traveller from a room.",
      );
      return;
    }
    await mutate(
      `unassign-${room.roomId}-${travellerId}`,
      () =>
        fetch(
          `/api/v1/operator/bookings/${bookingId}/accommodation/rooms/${room.roomId}/unassign`,
          {
            method: "POST",
            credentials: "include",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              travellerId,
              reason,
              expectedRoomVersion: room.version,
            }),
          },
        ),
      "Traveller removed from the room.",
    );
  };

  const setLock = async (room: Room) => {
    const reason = (reasonByRoom[room.roomId] ?? "").trim();
    if (!reason) {
      setError(
        `Enter a reason before ${room.isLocked ? "unlocking" : "locking"} this room.`,
      );
      return;
    }
    await mutate(
      `lock-${room.roomId}`,
      () =>
        fetch(
          `/api/v1/operator/bookings/${bookingId}/accommodation/rooms/${room.roomId}/lock`,
          {
            method: "POST",
            credentials: "include",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              locked: !room.isLocked,
              reason,
              expectedRoomVersion: room.version,
            }),
          },
        ),
      room.isLocked ? "Room allocation unlocked." : "Room allocation locked.",
    );
  };

  return (
    <OperatorWorkspaceShell
      title="Accommodation"
      summary="Assign confirmed travellers to Makkah and Madinah rooms without changing the commercial booking occupancy or price. Every move is capacity-checked, versioned and auditable."
    >
      <section className="operator-accommodation" aria-live="polite">
        {state.kind === "loading" ? (
          <div className="operator-booking-state">Loading accommodation…</div>
        ) : null}
        {state.kind === "forbidden" ? (
          <div className="operator-booking-state">
            <strong>
              You do not have access to this booking accommodation.
            </strong>
            <Link className="auth-secondary" href="/operator/bookings">
              Back to bookings
            </Link>
          </div>
        ) : null}
        {state.kind === "not-found" ? (
          <div className="operator-booking-state">
            <strong>Booking accommodation not found.</strong>
            <p>The booking is unavailable or belongs to another operator.</p>
            <Link className="auth-secondary" href="/operator/bookings">
              Back to bookings
            </Link>
          </div>
        ) : null}
        {state.kind === "error" ? (
          <div className="operator-booking-state">
            <strong>Accommodation is temporarily unavailable.</strong>
            <button className="auth-secondary" type="button" onClick={load}>
              Retry
            </button>
          </div>
        ) : null}

        {state.kind === "ready" ? (
          <>
            <div className="operator-accommodation__back">
              <Link href={`/operator/bookings/${bookingId}`}>
                ← Back to booking
              </Link>
            </div>
            <article className="operator-accommodation__hero">
              <div>
                <p className="auth-eyebrow">
                  Booking {state.workspace.reference}
                </p>
                <h2>Room allocation</h2>
                <p>
                  {state.workspace.travellers.length} travellers ·{" "}
                  {state.workspace.bookingOccupancy} commercial occupancy
                </p>
              </div>
              <span className="operator-booking-badge progress">
                Operational
              </span>
            </article>

            {error ? (
              <div className="operator-accommodation__error" role="alert">
                <strong>Action not saved</strong>
                <span>{error}</span>
              </div>
            ) : null}
            {message ? (
              <div className="operator-accommodation__success" role="status">
                {message}
              </div>
            ) : null}

            <section
              className="operator-accommodation__section"
              aria-labelledby="create-room-heading"
            >
              <div>
                <p className="auth-eyebrow">Room plan</p>
                <h2 id="create-room-heading">Add room allocation</h2>
                <p>
                  Create only the operational room labels needed for this
                  booking. This does not create supplier inventory or change
                  package pricing.
                </p>
              </div>
              <div className="operator-accommodation__create">
                <label>
                  <span>Stay</span>
                  <select
                    value={roomDraft.stay}
                    onChange={(event) =>
                      setRoomDraft((current) => ({
                        ...current,
                        stay: event.target.value as CreateRoomDraft["stay"],
                      }))
                    }
                  >
                    <option value="makkah">Makkah</option>
                    <option value="madinah">Madinah</option>
                  </select>
                </label>
                <label>
                  <span>Room type</span>
                  <select
                    value={roomDraft.roomType}
                    onChange={(event) =>
                      setRoomDraft((current) => ({
                        ...current,
                        roomType: event.target
                          .value as CreateRoomDraft["roomType"],
                      }))
                    }
                  >
                    <option value="double">Double</option>
                    <option value="triple">Triple</option>
                    <option value="quad">Quad</option>
                  </select>
                </label>
                <label>
                  <span>Room label</span>
                  <input
                    value={roomDraft.label}
                    maxLength={80}
                    placeholder="e.g. Makkah 201"
                    onChange={(event) =>
                      setRoomDraft((current) => ({
                        ...current,
                        label: event.target.value,
                      }))
                    }
                  />
                </label>
                <button
                  className="auth-primary"
                  type="button"
                  disabled={Boolean(busy)}
                  onClick={createRoom}
                >
                  {busy === "create-room" ? "Creating…" : "Add room"}
                </button>
              </div>
            </section>

            <div className="operator-accommodation__stays">
              {(["makkah", "madinah"] as const).map((stay) => {
                const rooms = state.workspace.rooms.filter(
                  (room) => room.stay === stay,
                );
                const unassigned =
                  state.workspace.unassigned.find((item) => item.stay === stay)
                    ?.travellerIds.length ?? state.workspace.travellers.length;
                return (
                  <section
                    className="operator-accommodation__section"
                    key={stay}
                    aria-labelledby={`${stay}-heading`}
                  >
                    <div className="operator-accommodation__section-head">
                      <div>
                        <p className="auth-eyebrow">{stayLabels[stay]} stay</p>
                        <h2 id={`${stay}-heading`}>{stayLabels[stay]} rooms</h2>
                      </div>
                      <strong>{unassigned} unassigned</strong>
                    </div>
                    {rooms.length === 0 ? (
                      <p className="operator-accommodation__empty">
                        No room allocations yet. Add the first{" "}
                        {stayLabels[stay]} room above.
                      </p>
                    ) : (
                      <div className="operator-accommodation__rooms">
                        {rooms.map((room) => (
                          <article
                            className="operator-accommodation__room"
                            key={room.roomId}
                          >
                            <header>
                              <div>
                                <h3>{room.label}</h3>
                                <p>
                                  {roomTypeLabels[room.roomType]} ·{" "}
                                  {room.occupants.length}/{room.capacity} places
                                </p>
                              </div>
                              <span
                                className={`operator-booking-badge ${room.isLocked ? "muted" : "good"}`}
                              >
                                {room.isLocked ? "Locked" : "Open"}
                              </span>
                            </header>
                            <ul
                              className="operator-accommodation__occupants"
                              aria-label={`${room.label} occupants`}
                            >
                              {room.occupants.length === 0 ? (
                                <li className="operator-accommodation__empty">
                                  No travellers assigned.
                                </li>
                              ) : (
                                room.occupants.map((travellerId) => (
                                  <li key={travellerId}>
                                    <span>
                                      {travellersById.get(travellerId)
                                        ?.fullName ?? "Traveller"}
                                    </span>
                                    <button
                                      type="button"
                                      className="auth-secondary"
                                      disabled={room.isLocked || Boolean(busy)}
                                      onClick={() =>
                                        unassign(room, travellerId)
                                      }
                                    >
                                      Remove
                                    </button>
                                  </li>
                                ))
                              )}
                            </ul>
                            <label className="operator-accommodation__field">
                              <span>Traveller to assign or move</span>
                              <select
                                disabled={room.isLocked || Boolean(busy)}
                                value={selectedTraveller[room.roomId] ?? ""}
                                onChange={(event) =>
                                  setSelectedTraveller((current) => ({
                                    ...current,
                                    [room.roomId]: event.target.value,
                                  }))
                                }
                              >
                                <option value="">Choose traveller</option>
                                {state.workspace.travellers.map((traveller) => (
                                  <option
                                    key={traveller.travellerId}
                                    value={traveller.travellerId}
                                  >
                                    {traveller.fullName}
                                  </option>
                                ))}
                              </select>
                            </label>
                            <label className="operator-accommodation__field">
                              <span>Operational reason</span>
                              <input
                                maxLength={500}
                                value={reasonByRoom[room.roomId] ?? ""}
                                placeholder="Required for assignment, move, removal or lock"
                                onChange={(event) =>
                                  setReasonByRoom((current) => ({
                                    ...current,
                                    [room.roomId]: event.target.value,
                                  }))
                                }
                              />
                            </label>
                            <div className="operator-accommodation__actions">
                              <button
                                className="auth-primary"
                                type="button"
                                disabled={
                                  room.isLocked ||
                                  Boolean(busy) ||
                                  room.occupants.length >= room.capacity
                                }
                                onClick={() => assign(room)}
                              >
                                {busy === `assign-${room.roomId}`
                                  ? "Saving…"
                                  : "Assign / move"}
                              </button>
                              <button
                                className="auth-secondary"
                                type="button"
                                disabled={Boolean(busy)}
                                onClick={() => setLock(room)}
                              >
                                {busy === `lock-${room.roomId}`
                                  ? "Saving…"
                                  : room.isLocked
                                    ? "Unlock room"
                                    : "Lock room"}
                              </button>
                            </div>
                          </article>
                        ))}
                      </div>
                    )}
                  </section>
                );
              })}
            </div>

            <section
              className="operator-accommodation__section"
              aria-labelledby="history-heading"
            >
              <div>
                <p className="auth-eyebrow">Audit trail</p>
                <h2 id="history-heading">Recent allocation changes</h2>
              </div>
              {state.workspace.history.length === 0 ? (
                <p className="operator-accommodation__empty">
                  No allocation changes recorded yet.
                </p>
              ) : (
                <ol className="operator-accommodation__history">
                  {state.workspace.history.map((item) => (
                    <li key={item.auditId}>
                      <div>
                        <strong>
                          {item.travellerId
                            ? (travellersById.get(item.travellerId)?.fullName ??
                              "Traveller")
                            : "Room allocation"}
                        </strong>
                        <span>
                          {item.action} ·{" "}
                          {stayLabels[item.stay as keyof typeof stayLabels] ??
                            item.stay}
                        </span>
                      </div>
                      <p>{item.reason}</p>
                    </li>
                  ))}
                </ol>
              )}
            </section>
          </>
        ) : null}
      </section>
    </OperatorWorkspaceShell>
  );
}
