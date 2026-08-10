"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import styles from "./TravelFactsEditor.module.css";

type ConfirmationState = "pending" | "confirmed";

type FlightLeg = {
  airlineName: string;
  airlineCode: string;
  flightNumber: string;
  departureAirportName: string;
  departureAirportCode: string;
  arrivalAirportName: string;
  arrivalAirportCode: string;
  confirmationState: ConfirmationState;
};

type TravelFactsResponse = {
  departureId: string;
  version: number;
  editable: boolean;
  legs: FlightLeg[];
};

type ProblemDetails = {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
};

type EditorState =
  | "loading"
  | "ready"
  | "saving"
  | "saved"
  | "conflict"
  | "unauthenticated"
  | "forbidden"
  | "not-found"
  | "error";

type FieldErrors = Record<string, string>;

const emptyLeg = (): FlightLeg => ({
  airlineName: "",
  airlineCode: "",
  flightNumber: "",
  departureAirportName: "",
  departureAirportCode: "",
  arrivalAirportName: "",
  arrivalAirportCode: "",
  confirmationState: "pending",
});

function requestHeaders(json = false): HeadersInit {
  const headers: Record<string, string> = {};
  if (json) headers["Content-Type"] = "application/json";
  const testIdentity = process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY;
  if (testIdentity) headers["X-NoorPath-Test-Identity"] = testIdentity;
  return headers;
}

function toFieldErrors(problem: ProblemDetails): FieldErrors {
  return Object.fromEntries(
    Object.entries(problem.errors ?? {}).map(([key, values]) => [
      key,
      values[0] ?? "Review this field.",
    ]),
  );
}

function Field({
  id,
  label,
  error,
  hint,
  children,
}: {
  id: string;
  label: string;
  error?: string;
  hint?: string;
  children: React.ReactNode;
}) {
  return (
    <label
      className={`${styles.field} ${error ? styles.fieldError : ""}`}
      htmlFor={id}
    >
      <span>{label}</span>
      {children}
      {error ? (
        <small className={styles.errorText}>{error}</small>
      ) : (
        hint && <small>{hint}</small>
      )}
    </label>
  );
}

export function TravelFactsLaunchLink({ departureId }: { departureId: string }) {
  return (
    <Link
      className={styles.launchLink}
      href={`/operator/departures/${departureId}/travel-facts`}
    >
      Airline & airport facts
    </Link>
  );
}

export default function TravelFactsEditor({
  departureId,
}: {
  departureId: string;
}) {
  const [legs, setLegs] = useState<FlightLeg[]>([]);
  const [version, setVersion] = useState(1);
  const [editable, setEditable] = useState(false);
  const [state, setState] = useState<EditorState>("loading");
  const [errors, setErrors] = useState<FieldErrors>({});
  const [problem, setProblem] = useState("");
  const [dirty, setDirty] = useState(false);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      try {
        const response = await fetch(
          `/api/v1/operator/departures/${departureId}/travel-facts`,
          {
            cache: "no-store",
            credentials: "include",
            headers: requestHeaders(),
          },
        );
        if (cancelled) return;
        if (response.status === 401) return setState("unauthenticated");
        if (response.status === 403) return setState("forbidden");
        if (response.status === 404) return setState("not-found");
        if (!response.ok) throw new Error("travel facts unavailable");

        const body = (await response.json()) as TravelFactsResponse;
        setLegs(body.legs ?? []);
        setVersion(body.version);
        setEditable(body.editable);
        setDirty(false);
        setState("ready");
      } catch {
        if (!cancelled) {
          setProblem(
            "We couldn’t load the airline and airport facts. Check the connection and retry.",
          );
          setState("error");
        }
      }
    };

    void load();
    return () => {
      cancelled = true;
    };
  }, [departureId]);

  useEffect(() => {
    if (!dirty) return;
    const warn = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      event.returnValue = "";
    };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);

  const updateLeg = (index: number, patch: Partial<FlightLeg>) => {
    setLegs((current) =>
      current.map((leg, legIndex) =>
        legIndex === index ? { ...leg, ...patch } : leg,
      ),
    );
    setErrors((current) => {
      const next = { ...current };
      Object.keys(patch).forEach((key) => delete next[`legs[${index}].${key}`]);
      return next;
    });
    setProblem("");
    setDirty(true);
    if (state === "saved") setState("ready");
  };

  const addLeg = () => {
    setLegs((current) => [...current, emptyLeg()]);
    setDirty(true);
    setState("ready");
  };

  const removeLeg = (index: number) => {
    setLegs((current) => current.filter((_, legIndex) => legIndex !== index));
    setErrors({});
    setDirty(true);
    setState("ready");
  };

  const save = async () => {
    setState("saving");
    setProblem("");
    setErrors({});

    try {
      const response = await fetch(
        `/api/v1/operator/departures/${departureId}/travel-facts`,
        {
          method: "PUT",
          credentials: "include",
          headers: requestHeaders(true),
          body: JSON.stringify({ expectedVersion: version, legs }),
        },
      );
      const body = (await response.json()) as TravelFactsResponse & ProblemDetails;

      if (response.status === 401) return setState("unauthenticated");
      if (response.status === 403) return setState("forbidden");
      if (response.status === 404) return setState("not-found");
      if (response.status === 409) {
        setProblem(
          body.detail ??
            "These travel facts changed elsewhere. Reload before saving again.",
        );
        return setState("conflict");
      }
      if (response.status === 422) {
        setErrors(toFieldErrors(body));
        setProblem(body.detail ?? "Review the highlighted flight facts.");
        setState("ready");
        requestAnimationFrame(() =>
          document.querySelector<HTMLElement>("[data-travel-facts-errors]")?.focus(),
        );
        return;
      }
      if (!response.ok)
        throw new Error(body.detail ?? body.title ?? "travel facts save failed");

      setLegs(body.legs ?? []);
      setVersion(body.version);
      setEditable(body.editable);
      setDirty(false);
      setState("saved");
    } catch {
      setProblem(
        "We couldn’t save these facts. Your entries are still here; retry safely.",
      );
      setState("error");
    }
  };

  if (["loading", "unauthenticated", "forbidden", "not-found"].includes(state)) {
    const message = {
      loading: "Loading airline and airport facts…",
      unauthenticated: "Sign in with an operator staff account to continue.",
      forbidden: "Your account does not have operator authoring access.",
      "not-found": "This departure is unavailable or belongs to another operator.",
    }[state as "loading" | "unauthenticated" | "forbidden" | "not-found"];

    return (
      <div className={styles.notice} role="status" aria-live="polite">
        <strong>Travel facts</strong>
        <p>{message}</p>
      </div>
    );
  }

  const locked = !editable || state === "conflict";
  const busy = state === "saving";

  return (
    <div className={styles.workspace}>
      <div className={styles.toolbar}>
        <div className={styles.toolbarLinks}>
          <Link className={styles.link} href={`/operator/departures/${departureId}`}>
            Back to package draft
          </Link>
          <Link
            className={styles.link}
            href={`/operator/departures/${departureId}/preview`}
          >
            Preview package
          </Link>
        </div>
        <button
          className={styles.secondaryButton}
          type="button"
          disabled={locked || busy || legs.length >= 8}
          onClick={addLeg}
        >
          Add flight leg
        </button>
      </div>

      <div className={styles.notice}>
        <strong>Operator-authored facts only</strong>
        <p>
          Enter airline, flight and airport facts you can support. External airline
          or airport lookup is not configured in this slice; leave uncertain facts
          Pending rather than guessing.
        </p>
      </div>

      {!editable && (
        <div className={styles.notice} role="status">
          <strong>Travel facts are read-only</strong>
          <p>This departure is already awaiting review or published.</p>
        </div>
      )}

      {(problem || Object.keys(errors).length > 0 || state === "conflict") && (
        <div
          className={styles.errorNotice}
          role="alert"
          tabIndex={-1}
          data-travel-facts-errors
        >
          <strong>
            {state === "conflict" ? "Travel facts changed elsewhere" : "Review travel facts"}
          </strong>
          <p>
            {problem ||
              `${Object.keys(errors).length} field(s) need attention before saving.`}
          </p>
          {state === "conflict" && (
            <button
              className={styles.secondaryButton}
              type="button"
              onClick={() => window.location.reload()}
            >
              Reload latest facts
            </button>
          )}
        </div>
      )}

      {state === "saved" && (
        <div className={styles.successNotice} role="status" aria-live="polite">
          <strong>Travel facts saved</strong>
          <p>Fact version {version} is stored with this private package draft.</p>
        </div>
      )}

      {legs.length === 0 ? (
        <div className={styles.emptyState}>
          <strong>No flight facts recorded yet</strong>
          <p>
            This is a truthful empty state. Add a leg when the operator has airline
            or airport facts to record.
          </p>
        </div>
      ) : (
        <div className={styles.legList}>
          {legs.map((leg, index) => {
            const key = `legs[${index}]`;
            const fieldId = (name: keyof FlightLeg) => `flight-${index}-${name}`;
            const fieldError = (name: keyof FlightLeg) => errors[`${key}.${name}`];

            return (
              <fieldset className={styles.legCard} disabled={locked || busy} key={index}>
                <div className={styles.legHeader}>
                  <div>
                    <h2>Flight leg {index + 1}</h2>
                    <p>
                      Record each connection independently so confirmation is never
                      implied across the whole journey.
                    </p>
                  </div>
                  <button
                    className={styles.dangerButton}
                    type="button"
                    onClick={() => removeLeg(index)}
                  >
                    Remove leg {index + 1}
                  </button>
                </div>

                <div className={styles.grid}>
                  <Field
                    id={fieldId("airlineName")}
                    label="Airline name"
                    error={fieldError("airlineName")}
                  >
                    <input
                      id={fieldId("airlineName")}
                      maxLength={120}
                      value={leg.airlineName}
                      placeholder="e.g. Saudia"
                      onChange={(event) =>
                        updateLeg(index, { airlineName: event.target.value })
                      }
                    />
                  </Field>
                  <Field
                    id={fieldId("airlineCode")}
                    label="Airline code"
                    error={fieldError("airlineCode")}
                    hint="Optional while pending."
                  >
                    <input
                      id={fieldId("airlineCode")}
                      maxLength={8}
                      value={leg.airlineCode}
                      placeholder="e.g. SV"
                      onChange={(event) =>
                        updateLeg(index, { airlineCode: event.target.value })
                      }
                    />
                  </Field>
                  <Field
                    id={fieldId("flightNumber")}
                    label="Flight number"
                    error={fieldError("flightNumber")}
                  >
                    <input
                      id={fieldId("flightNumber")}
                      maxLength={16}
                      value={leg.flightNumber}
                      placeholder="e.g. SV759"
                      onChange={(event) =>
                        updateLeg(index, { flightNumber: event.target.value })
                      }
                    />
                  </Field>
                </div>

                <div className={styles.grid}>
                  <Field
                    id={fieldId("departureAirportName")}
                    label="Departure airport"
                    error={fieldError("departureAirportName")}
                  >
                    <input
                      id={fieldId("departureAirportName")}
                      maxLength={160}
                      value={leg.departureAirportName}
                      placeholder="Airport name"
                      onChange={(event) =>
                        updateLeg(index, { departureAirportName: event.target.value })
                      }
                    />
                  </Field>
                  <Field
                    id={fieldId("departureAirportCode")}
                    label="Departure airport code"
                    error={fieldError("departureAirportCode")}
                  >
                    <input
                      id={fieldId("departureAirportCode")}
                      maxLength={8}
                      value={leg.departureAirportCode}
                      placeholder="e.g. BOM"
                      onChange={(event) =>
                        updateLeg(index, { departureAirportCode: event.target.value })
                      }
                    />
                  </Field>
                  <Field
                    id={fieldId("arrivalAirportName")}
                    label="Arrival airport"
                    error={fieldError("arrivalAirportName")}
                  >
                    <input
                      id={fieldId("arrivalAirportName")}
                      maxLength={160}
                      value={leg.arrivalAirportName}
                      placeholder="Airport name"
                      onChange={(event) =>
                        updateLeg(index, { arrivalAirportName: event.target.value })
                      }
                    />
                  </Field>
                  <Field
                    id={fieldId("arrivalAirportCode")}
                    label="Arrival airport code"
                    error={fieldError("arrivalAirportCode")}
                  >
                    <input
                      id={fieldId("arrivalAirportCode")}
                      maxLength={8}
                      value={leg.arrivalAirportCode}
                      placeholder="e.g. JED"
                      onChange={(event) =>
                        updateLeg(index, { arrivalAirportCode: event.target.value })
                      }
                    />
                  </Field>
                </div>

                <div className={styles.statusRow}>
                  <Field
                    id={fieldId("confirmationState")}
                    label="Flight fact status"
                    error={fieldError("confirmationState")}
                    hint="Confirmed requires airline, flight number and both airports."
                  >
                    <select
                      id={fieldId("confirmationState")}
                      value={leg.confirmationState}
                      onChange={(event) =>
                        updateLeg(index, {
                          confirmationState: event.target.value as ConfirmationState,
                        })
                      }
                    >
                      <option value="pending">Pending — still being verified</option>
                      <option value="confirmed">Confirmed — operator verified</option>
                    </select>
                  </Field>
                </div>
              </fieldset>
            );
          })}
        </div>
      )}

      <div className={styles.actions}>
        <p>
          {dirty
            ? "Unsaved flight-fact changes are present."
            : `Travel fact version ${version} is current.`}
        </p>
        <button
          className={styles.primaryButton}
          type="button"
          disabled={locked || busy || !dirty}
          onClick={() => void save()}
        >
          {busy ? "Saving…" : "Save travel facts"}
        </button>
      </div>
    </div>
  );
}
