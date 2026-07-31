"use client";

import Link from "next/link";
import CommercialEditor from "./CommercialEditor";
import PaymentPlanEditor from "./PaymentPlanEditor";
import {
  useEffect,
  useMemo,
  useState,
  type MouseEvent,
  type ReactNode,
} from "react";

type ConfirmationState = "pending" | "confirmed";
type ComposerState =
  | "loading"
  | "ready"
  | "saving"
  | "saved"
  | "conflict"
  | "unauthenticated"
  | "forbidden"
  | "not-found"
  | "load-error"
  | "error";

type AccommodationForm = {
  hotelName: string;
  classification: string;
  distanceDisclosure: string;
  nights: string;
  confirmationState: ConfirmationState;
};

type TravelForm = {
  routeSummary: string;
  details: string;
  confirmationState: ConfirmationState;
};

type DraftForm = {
  packageName: string;
  summary: string;
  makkah: AccommodationForm;
  madinah: AccommodationForm;
  travel: TravelForm;
  origin: string;
  departureDate: string;
  returnDate: string;
  inclusions: string[];
  exclusions: string[];
};

type FieldErrors = Record<string, string>;

type DraftResponse = {
  packageTemplateId: string;
  packageVersionId: string;
  departureId: string;
  version: number;
  status: "draft" | "readyForReview" | "published";
  packageName?: string;
  summary?: string;
  makkah?: AccommodationResponse;
  madinah?: AccommodationResponse;
  travel?: TravelResponse;
  origin?: string;
  departureDate?: string;
  returnDate?: string;
  inclusions?: string[];
  exclusions?: string[];
};

type AccommodationResponse = {
  hotelName: string;
  classification: string;
  distanceDisclosure: string;
  nights: number;
  confirmationState: ConfirmationState;
};

type TravelResponse = {
  routeSummary: string;
  details: string;
  confirmationState: ConfirmationState;
};

type OperatorAccess = {
  displayName: string;
  permissions: string[];
};

type ProblemDetails = {
  detail?: string;
  title?: string;
  errors?: Record<string, string[]>;
};

const emptyAccommodation = (): AccommodationForm => ({
  hotelName: "",
  classification: "",
  distanceDisclosure: "",
  nights: "",
  confirmationState: "pending",
});

export const createEmptyDraft = (): DraftForm => ({
  packageName: "",
  summary: "",
  makkah: emptyAccommodation(),
  madinah: emptyAccommodation(),
  travel: { routeSummary: "", details: "", confirmationState: "pending" },
  origin: "",
  departureDate: "",
  returnDate: "",
  inclusions: [],
  exclusions: [],
});

export function validateDraft(form: DraftForm): FieldErrors {
  const errors: FieldErrors = {};
  if (!form.packageName.trim()) errors.packageName = "Enter a package name.";
  if (!form.summary.trim()) errors.summary = "Add a factual package summary.";
  if (!form.makkah.hotelName.trim())
    errors["makkah.hotelName"] = "Add the Makkah hotel or stay name.";
  if (!form.madinah.hotelName.trim())
    errors["madinah.hotelName"] = "Add the Madinah hotel or stay name.";
  if (!form.travel.routeSummary.trim())
    errors["travel.routeSummary"] = "Add the travel route summary.";
  if (!form.origin.trim()) errors.origin = "Add the departure origin.";
  if (!form.departureDate) errors.departureDate = "Choose a departure date.";
  if (!form.returnDate) errors.returnDate = "Choose a return date.";
  if (
    form.departureDate &&
    form.returnDate &&
    form.returnDate <= form.departureDate
  )
    errors.returnDate = "Return date must be after departure.";

  const makkahNights = Number(form.makkah.nights || 0);
  const madinahNights = Number(form.madinah.nights || 0);
  if (makkahNights < 0) errors["makkah.nights"] = "Nights cannot be negative.";
  if (madinahNights < 0)
    errors["madinah.nights"] = "Nights cannot be negative.";
  if (makkahNights + madinahNights <= 0)
    errors.stays = "Add at least one night across Makkah and Madinah.";

  return errors;
}

function Brand({
  href = "/",
  label = "NoorPath home",
  onNavigate,
}: {
  href?: string;
  label?: string;
  onNavigate?: (event: MouseEvent<HTMLAnchorElement>) => void;
}) {
  return (
    <Link className="brand" href={href} aria-label={label} onClick={onNavigate}>
      <span className="brand-mark" aria-hidden="true">
        ◇
      </span>
      <span>NoorPath</span>
    </Link>
  );
}

function Icon({ children }: { children: ReactNode }) {
  return (
    <span className="composer-icon" aria-hidden="true">
      {children}
    </span>
  );
}

function Field({
  label,
  error,
  hint,
  required = true,
  children,
}: {
  label: string;
  error?: string;
  hint?: string;
  required?: boolean;
  children: ReactNode;
}) {
  return (
    <label className={error ? "field field-error" : "field"}>
      <span>
        {label} {required && <em aria-hidden="true">*</em>}
      </span>
      {children}
      {error ? (
        <small className="error-text">{error}</small>
      ) : (
        hint && <small>{hint}</small>
      )}
    </label>
  );
}

function ConfirmationField({
  name,
  label,
  value,
  onChange,
}: {
  name: string;
  label: string;
  value: ConfirmationState;
  onChange: (value: ConfirmationState) => void;
}) {
  return (
    <fieldset className="confirmation-field">
      <legend>{label}</legend>
      <label>
        <input
          name={name}
          type="radio"
          checked={value === "pending"}
          onChange={() => onChange("pending")}
        />
        <span>
          <strong>Pending</strong>
          <small>Still awaiting supplier confirmation</small>
        </span>
      </label>
      <label>
        <input
          name={name}
          type="radio"
          checked={value === "confirmed"}
          onChange={() => onChange("confirmed")}
        />
        <span>
          <strong>Confirmed</strong>
          <small>Fact has been verified for this draft</small>
        </span>
      </label>
    </fieldset>
  );
}

function AccommodationSection({
  number,
  city,
  value,
  errors,
  onChange,
}: {
  number: string;
  city: "Makkah" | "Madinah";
  value: AccommodationForm;
  errors: FieldErrors;
  onChange: (patch: Partial<AccommodationForm>) => void;
}) {
  const key = city.toLowerCase();
  return (
    <section className="form-card">
      <div className="form-card-heading">
        <span>{number}</span>
        <div>
          <h2>{city} stay</h2>
          <p>
            {city === "Makkah"
              ? "Keep hotel, distance and confirmation state explicit."
              : "Record Madinah independently; do not infer it from Makkah."}
          </p>
        </div>
      </div>
      <div className="form-grid">
        <Field label="Hotel or stay name" error={errors[`${key}.hotelName`]}>
          <input
            required
            maxLength={160}
            value={value.hotelName}
            onChange={(event) => onChange({ hotelName: event.target.value })}
          />
        </Field>
        <Field label="Classification" required={false}>
          <input
            maxLength={80}
            value={value.classification}
            placeholder="e.g. 4 star"
            onChange={(event) =>
              onChange({ classification: event.target.value })
            }
          />
        </Field>
        <Field label="Distance disclosure" required={false}>
          <input
            maxLength={120}
            value={value.distanceDisclosure}
            placeholder={
              city === "Makkah"
                ? "e.g. 850 m from Masjid al-Haram"
                : "e.g. 450 m from Al-Masjid an-Nabawi"
            }
            onChange={(event) =>
              onChange({ distanceDisclosure: event.target.value })
            }
          />
        </Field>
        <Field label="Nights" error={errors[`${key}.nights`]} required={false}>
          <input
            min="0"
            type="number"
            value={value.nights}
            onChange={(event) => onChange({ nights: event.target.value })}
          />
        </Field>
      </div>
      <ConfirmationField
        name={`${key}-confirmation-state`}
        label={`${city} fact status`}
        value={value.confirmationState}
        onChange={(confirmationState) => onChange({ confirmationState })}
      />
    </section>
  );
}

function TagEditor({
  label,
  values,
  placeholder,
  onChange,
}: {
  label: string;
  values: string[];
  placeholder: string;
  onChange: (values: string[]) => void;
}) {
  const [nextValue, setNextValue] = useState("");
  const add = () => {
    const value = nextValue.trim();
    if (!value) return;
    if (!values.some((item) => item.toLowerCase() === value.toLowerCase()))
      onChange([...values, value]);
    setNextValue("");
  };

  return (
    <div className="composer-tag-editor">
      <span className="composer-field-label">{label}</span>
      {values.length > 0 && (
        <div className="tag-list" aria-label={`${label} list`}>
          {values.map((value) => (
            <span key={value}>
              {value}
              <button
                type="button"
                aria-label={`Remove ${value}`}
                onClick={() =>
                  onChange(values.filter((item) => item !== value))
                }
              >
                ×
              </button>
            </span>
          ))}
        </div>
      )}
      <div className="add-inclusion">
        <input
          aria-label={`Add ${label.toLowerCase()} item`}
          maxLength={120}
          placeholder={placeholder}
          value={nextValue}
          onChange={(event) => setNextValue(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter") {
              event.preventDefault();
              add();
            }
          }}
        />
        <button type="button" onClick={add}>
          Add
        </button>
      </div>
    </div>
  );
}

function requestHeaders(json = false): HeadersInit {
  const headers: Record<string, string> = {};
  if (json) headers["Content-Type"] = "application/json";
  const testIdentity = process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY;
  if (testIdentity) headers["X-NoorPath-Test-Identity"] = testIdentity;
  return headers;
}

function buildRequest(form: DraftForm) {
  return {
    ...form,
    makkah: { ...form.makkah, nights: Number(form.makkah.nights || 0) },
    madinah: { ...form.madinah, nights: Number(form.madinah.nights || 0) },
  };
}

function toDraftForm(response: DraftResponse): DraftForm {
  const accommodation = (value?: AccommodationResponse): AccommodationForm => ({
    hotelName: value?.hotelName ?? "",
    classification: value?.classification ?? "",
    distanceDisclosure: value?.distanceDisclosure ?? "",
    nights: value ? String(value.nights) : "",
    confirmationState: value?.confirmationState ?? "pending",
  });

  return {
    packageName: response.packageName ?? "",
    summary: response.summary ?? "",
    makkah: accommodation(response.makkah),
    madinah: accommodation(response.madinah),
    travel: {
      routeSummary: response.travel?.routeSummary ?? "",
      details: response.travel?.details ?? "",
      confirmationState: response.travel?.confirmationState ?? "pending",
    },
    origin: response.origin ?? "",
    departureDate: response.departureDate ?? "",
    returnDate: response.returnDate ?? "",
    inclusions: response.inclusions ?? [],
    exclusions: response.exclusions ?? [],
  };
}

export default function DepartureComposer({
  initialDepartureId,
}: {
  initialDepartureId?: string;
}) {
  const [form, setForm] = useState<DraftForm>(createEmptyDraft);
  const [departureId, setDepartureId] = useState(initialDepartureId ?? "");
  const [version, setVersion] = useState<number | null>(null);
  const [draftStatus, setDraftStatus] =
    useState<DraftResponse["status"]>("draft");
  const [operator, setOperator] = useState<OperatorAccess | null>(null);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [problem, setProblem] = useState("");
  const [state, setState] = useState<ComposerState>("loading");
  const [isDirty, setIsDirty] = useState(false);
  const [commercialDirty, setCommercialDirty] = useState(false);
  const [commercialBusy, setCommercialBusy] = useState(false);
  const [paymentPlanDirty, setPaymentPlanDirty] = useState(false);
  const [paymentPlanBusy, setPaymentPlanBusy] = useState(false);
  const hasUnsavedChanges = isDirty || commercialDirty || paymentPlanDirty;
  const isLocked = draftStatus !== "draft";

  const duration = useMemo(() => {
    if (!form.departureDate || !form.returnDate) return 0;
    const start = new Date(`${form.departureDate}T00:00:00Z`).getTime();
    const end = new Date(`${form.returnDate}T00:00:00Z`).getTime();
    return Math.max(0, Math.round((end - start) / 86400000));
  }, [form.departureDate, form.returnDate]);

  useEffect(() => {
    if (!hasUnsavedChanges) return;

    const handleBeforeUnload = (event: BeforeUnloadEvent) => {
      event.preventDefault();
      event.returnValue = "";
    };

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [hasUnsavedChanges]);

  useEffect(() => {
    let cancelled = false;

    const load = async () => {
      try {
        const accessResponse = await fetch("/api/v1/operator/access", {
          cache: "no-store",
          credentials: "include",
          headers: requestHeaders(),
        });
        if (cancelled) return;
        if (accessResponse.status === 401) return setState("unauthenticated");
        if (accessResponse.status === 403) return setState("forbidden");
        if (!accessResponse.ok) throw new Error("operator access unavailable");
        setOperator((await accessResponse.json()) as OperatorAccess);

        if (!initialDepartureId) {
          setState("ready");
          return;
        }

        const response = await fetch(
          `/api/v1/operator/departures/${initialDepartureId}`,
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
        if (!response.ok) throw new Error("draft unavailable");

        const draft = (await response.json()) as DraftResponse;
        setForm(toDraftForm(draft));
        setDepartureId(draft.departureId);
        setVersion(draft.version);
        setDraftStatus(draft.status);
        setIsDirty(false);
        setState("ready");
      } catch {
        if (!cancelled) {
          setProblem(
            "We couldn’t load this workspace. Check the connection and retry.",
          );
          setState("load-error");
        }
      }
    };

    void load();
    return () => {
      cancelled = true;
    };
  }, [initialDepartureId]);

  const guardNavigation = (event: MouseEvent<HTMLAnchorElement>) => {
    if (state === "saving" || commercialBusy || paymentPlanBusy) {
      event.preventDefault();
      return;
    }

    if (
      hasUnsavedChanges &&
      !window.confirm("Discard unsaved changes and leave this draft?")
    ) {
      event.preventDefault();
    }
  };

  const markDirty = () => {
    if (["saved", "error"].includes(state)) setState("ready");
    if (state !== "conflict") setProblem("");
    setIsDirty(true);
  };

  const change = <K extends keyof DraftForm>(key: K, value: DraftForm[K]) => {
    setForm((current) => ({ ...current, [key]: value }));
    setErrors((current) => {
      const next = { ...current };
      delete next[key];
      return next;
    });
    markDirty();
  };

  const changeAccommodation = (
    city: "makkah" | "madinah",
    patch: Partial<AccommodationForm>,
  ) => {
    setForm((current) => ({
      ...current,
      [city]: { ...current[city], ...patch },
    }));
    setErrors((current) => {
      const next = { ...current };
      Object.keys(patch).forEach((key) => delete next[`${city}.${key}`]);
      delete next.stays;
      return next;
    });
    markDirty();
  };

  const changeTravel = (patch: Partial<TravelForm>) => {
    setForm((current) => ({
      ...current,
      travel: { ...current.travel, ...patch },
    }));
    setErrors((current) => {
      const next = { ...current };
      Object.keys(patch).forEach((key) => delete next[`travel.${key}`]);
      return next;
    });
    markDirty();
  };

  const save = async () => {
    const validation = validateDraft(form);
    setErrors(validation);
    if (Object.keys(validation).length > 0) {
      requestAnimationFrame(() =>
        document.querySelector<HTMLElement>(".error-summary")?.focus(),
      );
      return;
    }

    setState("saving");
    setProblem("");
    try {
      const request = buildRequest(form);
      const response = await fetch(
        departureId
          ? `/api/v1/operator/departures/${departureId}`
          : "/api/v1/operator/departures",
        {
          method: departureId ? "PUT" : "POST",
          credentials: "include",
          headers: requestHeaders(true),
          body: JSON.stringify(
            departureId
              ? { expectedVersion: version, draft: request }
              : request,
          ),
        },
      );
      const body = (await response.json()) as DraftResponse & ProblemDetails;

      if (response.status === 401) return setState("unauthenticated");
      if (response.status === 403) return setState("forbidden");
      if (response.status === 409) {
        setProblem(
          body.detail ??
            "This draft changed in another session. Reload the latest version before continuing.",
        );
        setState("conflict");
        return;
      }
      if (response.status === 422) {
        setErrors(
          Object.fromEntries(
            Object.entries(body.errors ?? {}).map(([key, value]) => [
              key,
              value[0] ?? "Review this field.",
            ]),
          ),
        );
        setState("ready");
        requestAnimationFrame(() =>
          document.querySelector<HTMLElement>(".error-summary")?.focus(),
        );
        return;
      }
      if (!response.ok)
        throw new Error(body.detail ?? body.title ?? "save failed");

      setDepartureId(body.departureId);
      setVersion(body.version);
      setDraftStatus(body.status);
      setIsDirty(false);
      setState("saved");
      if (!initialDepartureId)
        window.history.replaceState(
          null,
          "",
          `/operator/departures/${body.departureId}`,
        );
    } catch {
      setProblem(
        "We couldn’t save the draft. Your entries are still here; retry safely.",
      );
      setState("error");
    }
  };

  if (
    [
      "loading",
      "unauthenticated",
      "forbidden",
      "not-found",
      "load-error",
    ].includes(state)
  ) {
    const copy = {
      loading: [
        "Loading workspace",
        "Checking your operator access and latest draft state.",
      ],
      unauthenticated: [
        "Sign in required",
        "Sign in with an operator staff account to author catalogue drafts.",
      ],
      forbidden: [
        "Operator access unavailable",
        "Your account does not have permission to author operator catalogue drafts.",
      ],
      "not-found": [
        "Draft not found",
        "This draft is unavailable or belongs to another operator.",
      ],
      "load-error": ["Workspace unavailable", problem],
    } as const;
    const [title, detail] = copy[state as keyof typeof copy];

    return (
      <main className="composer-state-page">
        <Brand />
        <section
          className="composer-state-card"
          role="status"
          aria-live="polite"
        >
          <Icon>{state === "loading" ? "…" : "◇"}</Icon>
          <span className="eyebrow">Operator catalogue</span>
          <h1>{title}</h1>
          <p>{detail}</p>
          {state === "not-found" && (
            <Link className="primary-button" href="/operator/departures/new">
              Start a new draft
            </Link>
          )}
          {state === "load-error" && (
            <button
              className="primary-button"
              type="button"
              onClick={() => window.location.reload()}
            >
              Retry
            </button>
          )}
        </section>
      </main>
    );
  }

  return (
    <main className="admin-shell composer-shell">
      <a className="skip-link" href="#composer-main">
        Skip to authoring form
      </a>
      <aside className="admin-sidebar composer-sidebar">
        <Brand
          href="/operator"
          label="Operator home"
          onNavigate={guardNavigation}
        />
        <nav aria-label="Operator navigation">
          <Link
            className="composer-nav-active"
            href="/operator/departures/new"
            aria-current={departureId ? undefined : "page"}
            onClick={guardNavigation}
          >
            <Icon>◈</Icon>
            New package draft
          </Link>
        </nav>
        <div className="composer-access-card">
          <span className="composer-access-badge">Verified scope</span>
          <strong>{operator?.displayName ?? "Operator workspace"}</strong>
          <small>
            Your operator is resolved from staff access. It cannot be selected
            in this form.
          </small>
        </div>
      </aside>

      <section
        id="composer-main"
        className="admin-content composer-content"
        aria-busy={state === "saving" || commercialBusy || paymentPlanBusy}
        tabIndex={-1}
      >
        <div className="admin-titlebar">
          <div>
            <span className="eyebrow">
              Catalogue · Package & departure authoring
            </span>
            <h1>
              {departureId ? "Edit draft journey" : "Create a draft journey"}
            </h1>
            <p>
              Record truthful Makkah, Madinah, travel and departure facts.
              Configure pricing and capacity separately once the draft is saved.
              Publication comes later.
            </p>
          </div>
          <span className="draft-pill">
            {draftStatus === "readyForReview"
              ? "Awaiting approval"
              : draftStatus === "published"
                ? "Published"
                : `Draft · ${
                    hasUnsavedChanges
                      ? "Unsaved changes"
                      : version
                        ? `Version ${version}`
                        : "Not saved"
                  }`}
          </span>
        </div>

        {isLocked && (
          <div className="composer-notice saved" role="status">
            <Icon>✓</Icon>
            <div>
              <strong>
                {draftStatus === "published"
                  ? "This departure is published"
                  : "This departure is awaiting approval"}
              </strong>
              <span>
                Catalogue, pricing, and inventory are locked at the reviewed
                versions.
              </span>
            </div>
          </div>
        )}

        {(Object.keys(errors).length > 0 ||
          state === "error" ||
          state === "conflict") && (
          <div
            className={
              state === "conflict"
                ? "composer-notice conflict"
                : "error-summary"
            }
            role="alert"
            tabIndex={-1}
          >
            <Icon>!</Icon>
            <div>
              <strong>
                {state === "conflict"
                  ? "This draft changed elsewhere"
                  : state === "error"
                    ? "Draft not saved"
                    : `Review ${Object.keys(errors).length} highlighted field(s)`}
              </strong>
              <span>
                {problem ||
                  "Your valid entries have been preserved. Correct the highlighted facts and save again."}
              </span>
              {Object.keys(errors).length > 0 && (
                <ul className="composer-error-list">
                  {Object.entries(errors).map(([key, message]) => (
                    <li key={key}>{message}</li>
                  ))}
                </ul>
              )}
              {state === "conflict" && (
                <button
                  className="composer-inline-action"
                  type="button"
                  onClick={() => window.location.reload()}
                >
                  Reload latest draft
                </button>
              )}
              {state === "error" && (
                <button
                  className="composer-inline-action"
                  type="button"
                  onClick={() => void save()}
                >
                  Retry save
                </button>
              )}
            </div>
          </div>
        )}

        {state === "saved" && (
          <div
            className="composer-notice saved"
            role="status"
            aria-live="polite"
          >
            <Icon>✓</Icon>
            <div>
              <strong>Draft saved</strong>
              <span>
                Version {version} is safely stored and remains private.
              </span>
            </div>
          </div>
        )}

        <form onSubmit={(event) => event.preventDefault()}>
          <fieldset
            className="composer-form-fieldset"
            disabled={state === "saving" || state === "conflict" || isLocked}
          >
            <section className="form-card">
              <div className="form-card-heading">
                <span>01</span>
                <div>
                  <h2>Package basics</h2>
                  <p>Internal draft identity and factual journey summary.</p>
                </div>
              </div>
              <div className="composer-stack">
                <Field label="Package name" error={errors.packageName}>
                  <input
                    required
                    maxLength={120}
                    placeholder="e.g. Noor Harmony 12 Nights"
                    value={form.packageName}
                    onChange={(event) =>
                      change("packageName", event.target.value)
                    }
                  />
                </Field>
                <Field label="Journey summary" error={errors.summary}>
                  <textarea
                    required
                    maxLength={600}
                    rows={4}
                    placeholder="Describe the journey using only facts you can support."
                    value={form.summary}
                    onChange={(event) => change("summary", event.target.value)}
                  />
                </Field>
              </div>
            </section>

            <AccommodationSection
              number="02"
              city="Makkah"
              value={form.makkah}
              errors={errors}
              onChange={(patch) => changeAccommodation("makkah", patch)}
            />

            <AccommodationSection
              number="03"
              city="Madinah"
              value={form.madinah}
              errors={errors}
              onChange={(patch) => changeAccommodation("madinah", patch)}
            />
            {errors.stays && (
              <p className="composer-section-error">{errors.stays}</p>
            )}

            <section className="form-card">
              <div className="form-card-heading">
                <span>04</span>
                <div>
                  <h2>Travel & departure</h2>
                  <p>
                    Dates, origin and route facts for this specific departure.
                  </p>
                </div>
              </div>
              <div className="form-grid">
                <Field label="Departure origin" error={errors.origin}>
                  <input
                    required
                    maxLength={120}
                    placeholder="e.g. Delhi (DEL)"
                    value={form.origin}
                    onChange={(event) => change("origin", event.target.value)}
                  />
                </Field>
                <Field
                  label="Route summary"
                  error={errors["travel.routeSummary"]}
                >
                  <input
                    required
                    maxLength={200}
                    placeholder="e.g. Delhi → Jeddah → Makkah → Madinah"
                    value={form.travel.routeSummary}
                    onChange={(event) =>
                      changeTravel({ routeSummary: event.target.value })
                    }
                  />
                </Field>
                <Field label="Departure date" error={errors.departureDate}>
                  <input
                    required
                    type="date"
                    value={form.departureDate}
                    onChange={(event) =>
                      change("departureDate", event.target.value)
                    }
                  />
                </Field>
                <Field
                  label="Return date"
                  error={errors.returnDate}
                  hint={duration ? `${duration} day journey` : undefined}
                >
                  <input
                    required
                    type="date"
                    value={form.returnDate}
                    onChange={(event) =>
                      change("returnDate", event.target.value)
                    }
                  />
                </Field>
              </div>
              <div className="composer-stack composer-travel-detail">
                <Field label="Travel detail" required={false}>
                  <textarea
                    maxLength={600}
                    rows={3}
                    placeholder="Add flight or transfer detail only when it is known."
                    value={form.travel.details}
                    onChange={(event) =>
                      changeTravel({ details: event.target.value })
                    }
                  />
                </Field>
                <ConfirmationField
                  name="travel-confirmation-state"
                  label="Travel fact status"
                  value={form.travel.confirmationState}
                  onChange={(confirmationState) =>
                    changeTravel({ confirmationState })
                  }
                />
              </div>
            </section>

            <section className="form-card">
              <div className="form-card-heading">
                <span>05</span>
                <div>
                  <h2>Included & excluded</h2>
                  <p>
                    Keep the commercial boundary clear without adding pricing.
                  </p>
                </div>
              </div>
              <div className="composer-content-grid">
                <TagEditor
                  label="Included"
                  values={form.inclusions}
                  placeholder="e.g. Return flights"
                  onChange={(values) => change("inclusions", values)}
                />
                <TagEditor
                  label="Not included"
                  values={form.exclusions}
                  placeholder="e.g. Personal expenses"
                  onChange={(values) => change("exclusions", values)}
                />
              </div>
            </section>
          </fieldset>
        </form>

        {isLocked ? (
          <section className="form-card commercial-locked-card">
            <div className="form-card-heading">
              <span>06</span>
              <div>
                <h2>Pricing &amp; inventory locked</h2>
                <p>
                  Submitted commercial facts remain visible in the publication
                  review and cannot be edited here.
                </p>
              </div>
            </div>
            <Link
              className="secondary-button"
              href={`/operator/departures/${departureId}/review`}
            >
              Open publication review
            </Link>
          </section>
        ) : (
          <>
            <CommercialEditor
              departureId={departureId || undefined}
              onDirtyChange={setCommercialDirty}
              onBusyChange={setCommercialBusy}
            />
            <PaymentPlanEditor
              departureId={departureId || undefined}
              onDirtyChange={setPaymentPlanDirty}
              onBusyChange={setPaymentPlanBusy}
            />
          </>
        )}
      </section>

      <footer className="admin-sticky-footer composer-savebar">
        <span>
          <Icon>◇</Icon>
          Private draft · Catalogue, pricing and inventory save independently
        </span>
        <div>
          {departureId && !isLocked && (
            <Link
              className="secondary-button"
              href="/operator/departures/new"
              onClick={guardNavigation}
            >
              New draft
            </Link>
          )}
          {departureId && !hasUnsavedChanges && (
            <Link
              className="secondary-button"
              href={`/operator/departures/${departureId}/review`}
            >
              Review publication
            </Link>
          )}
          <button
            className="primary-button"
            type="button"
            disabled={
              state === "saving" ||
              state === "conflict" ||
              Boolean(departureId && !isDirty) ||
              isLocked
            }
            onClick={() => void save()}
          >
            {state === "saving"
              ? "Saving…"
              : departureId && !isDirty
                ? "No changes"
                : departureId
                  ? "Save changes"
                  : "Save draft"}
          </button>
        </div>
      </footer>
    </main>
  );
}
