"use client";

import Image from "next/image";
import {
  useEffect,
  useMemo,
  useRef,
  useState,
  type ComponentProps,
  type Dispatch,
  type ReactNode,
  type SetStateAction,
} from "react";

type View = "customer" | "admin";
type CustomerStatus = "results" | "loading" | "empty" | "error" | "offline";
type Availability = 0 | 1 | 2 | 3;

type BatchForm = {
  operatorId: string;
  operatorName: string;
  packageName: string;
  summary: string;
  tier: string;
  departureCity: string;
  route: string;
  departureDate: string;
  returnDate: string;
  capacity: string;
  availability: string;
  price: string;
};

type ApiBatch = {
  id: string;
  operatorName: string;
  packageName: string;
  tier: string;
  departureCity: string;
  route: string;
  departureDate: string;
  returnDate: string;
  durationDays: number;
  capacity: number;
  availability: Availability;
  totalStartingPriceInr: number;
  inclusions: string[];
};

type PublishedBatch = {
  id: string;
  operator: string;
  packageName: string;
  tier: string;
  departureCity: string;
  route: string;
  departureDate: string;
  returnDate: string;
  capacity: number;
  seats: number | null;
  availability: Availability;
  price: number;
  image: string;
  inclusions: string[];
};

type DraftResponse = { id: string; version: number; status: string };
type FormErrors = Partial<Record<keyof BatchForm | "form", string>>;
type IconProps = ComponentProps<"svg"> & { size?: number; weight?: string };
function Icon({ size = 20, weight, ...props }: IconProps) {
  void weight;
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      {...props}
    >
      <circle cx="12" cy="12" r="9" />
      <path d="m8 12 2.5 2.5L16 9" />
    </svg>
  );
}

const AirplaneTilt = Icon,
  ArrowRight = Icon,
  Buildings = Icon,
  Bus = Icon,
  CalendarBlank = Icon,
  CaretDown = Icon,
  Check = Icon,
  CheckCircle = Icon,
  CloudSlash = Icon,
  ForkKnife = Icon,
  Headset = Icon,
  MapPin = Icon,
  Mosque = Icon,
  Plus = Icon,
  ShieldCheck = Icon,
  SignOut = Icon,
  SlidersHorizontal = Icon,
  SuitcaseRolling = Icon,
  UserCircle = Icon,
  UsersThree = Icon,
  WarningCircle = Icon,
  X = Icon;

const initialBatch: BatchForm = {
  operatorId: "test-approved-noor",
  operatorName: "Noor International Tours & Travels",
  packageName: "Noor Harmony 12 Nights",
  summary: "A calm Makkah and Madinah journey with guided human support.",
  tier: "Comfort",
  departureCity: "Delhi (DEL)",
  route: "Jeddah → Makkah → Madinah",
  departureDate: "2026-10-10",
  returnDate: "2026-10-22",
  capacity: "24",
  availability: "Exact seats",
  price: "94500",
};

const formatCurrency = (value: string | number | null) =>
  new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: "INR",
    maximumFractionDigits: 0,
  }).format(Number(value) || 0);

const formatDate = (value: string) =>
  value
    ? new Intl.DateTimeFormat("en-GB", {
        day: "2-digit",
        month: "short",
        year: "numeric",
      }).format(new Date(`${value}T00:00:00`))
    : "";

function Brand() {
  return (
    <a className="brand" href="#top" aria-label="NoorPath home">
      <span className="brand-mark">
        <Mosque size={27} weight="light" aria-hidden="true" />
      </span>
      <span>NoorPath</span>
    </a>
  );
}

function Header({
  view,
  setView,
}: {
  view: View;
  setView: Dispatch<SetStateAction<View>>;
}) {
  return (
    <header className="topbar" id="top">
      <Brand />
      <nav aria-label="Primary navigation">
        <button
          className={view === "customer" ? "nav-link active" : "nav-link"}
          onClick={() => setView("customer")}
        >
          Packages
        </button>
        <button className="nav-link" type="button">
          Destinations
        </button>
        <button className="nav-link" type="button">
          About us
        </button>
        <button className="nav-link" type="button">
          Support
        </button>
      </nav>
      <div className="header-actions">
        <button className="support-button" type="button">
          <Headset size={19} aria-hidden="true" />
          Human support
        </button>
        <button
          className={view === "admin" ? "admin-button active" : "admin-button"}
          onClick={() => setView(view === "admin" ? "customer" : "admin")}
        >
          <UserCircle size={20} aria-hidden="true" />
          {view === "admin" ? "Exit admin" : "Admin preview"}
        </button>
      </div>
    </header>
  );
}

function StatusSwitcher({
  state,
  onChange,
}: {
  state: CustomerStatus;
  onChange: Dispatch<SetStateAction<CustomerStatus>>;
}) {
  return (
    <div className="state-switcher" aria-label="Preview customer states">
      <span>Review state</span>
      {(["results", "loading", "empty", "error", "offline"] as const).map(
        (item) => (
          <button
            className={state === item ? "selected" : ""}
            key={item}
            onClick={() => onChange(item)}
          >
            {item}
          </button>
        ),
      )}
    </div>
  );
}

function TrustStrip() {
  return (
    <div className="trust-strip" aria-label="NoorPath trust commitments">
      <div>
        <ShieldCheck size={24} aria-hidden="true" />
        <span>
          <strong>Verified operators</strong>
          <small>Approval checked by NoorPath</small>
        </span>
      </div>
      <div>
        <Buildings size={24} aria-hidden="true" />
        <span>
          <strong>Truthful package details</strong>
          <small>What is included stays explicit</small>
        </span>
      </div>
      <div>
        <Headset size={24} aria-hidden="true" />
        <span>
          <strong>Human support</strong>
          <small>Help before, during and after</small>
        </span>
      </div>
    </div>
  );
}

function PackageCard({ batch }: { batch: PublishedBatch }) {
  const iconMap: Record<string, typeof Icon> = {
    "Return flights": AirplaneTilt,
    "4★ hotels": Buildings,
    "3★ hotels": Buildings,
    Breakfast: ForkKnife,
    "Group visa": ShieldCheck,
    "Airport transfer": Bus,
  };

  return (
    <article className="package-card">
      <div className="package-image-wrap">
        <Image
          src={batch.image}
          alt=""
          fill
          sizes="(max-width: 900px) 100vw, 33vw"
        />
        <span className="tier">{batch.tier}</span>
        <span className="availability">
          {batch.availability === 0 ? (
            <>
              <strong>{batch.seats}</strong> / {batch.capacity} seats available
            </>
          ) : (
            [
              "Exact seats",
              "Limited availability",
              "Waitlist only",
              "Unavailable",
            ][batch.availability]
          )}
        </span>
      </div>
      <div className="package-body">
        <div className="verified">
          <CheckCircle size={18} weight="fill" aria-hidden="true" />
          Verified operator
        </div>
        <p className="operator">{batch.operator}</p>
        <h3>{batch.packageName}</h3>
        <p className="route">{batch.route}</p>
        <div className="inclusions" aria-label="Package inclusion highlights">
          {batch.inclusions.map((item) => {
            const InclusionIcon = iconMap[item] || Check;
            return (
              <span key={item}>
                <InclusionIcon size={17} aria-hidden="true" />
                {item}
              </span>
            );
          })}
        </div>
        <dl className="package-meta">
          <div>
            <dt>Departure</dt>
            <dd>{batch.departureDate}</dd>
          </div>
          <div>
            <dt>From</dt>
            <dd>{batch.departureCity}</dd>
          </div>
        </dl>
        <div className="price-row">
          <div>
            <small>Total starting price</small>
            <strong>{formatCurrency(batch.price)}</strong>
            <span>per person</span>
          </div>
          <span className="published-label">Published journey</span>
        </div>
      </div>
    </article>
  );
}

function CustomerState({
  state,
  onRetry,
  published,
}: {
  state: CustomerStatus;
  onRetry: () => Promise<void>;
  published: PublishedBatch[];
}) {
  if (state === "loading") {
    return (
      <div className="packages-grid" aria-live="polite" aria-busy="true">
        {[1, 2, 3].map((item) => (
          <div className="package-card skeleton-card" key={item}>
            <div className="skeleton image-skeleton" />
            <div className="package-body">
              <div className="skeleton line short" />
              <div className="skeleton line medium" />
              <div className="skeleton line" />
              <div className="skeleton blocks" />
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (state !== "results") {
    const states = {
      empty: {
        icon: Mosque,
        title: "New journeys are being prepared",
        body: "There are no published packages right now. Speak with our support team and we’ll help you plan.",
        action: "Request a callback",
      },
      error: {
        icon: WarningCircle,
        title: "We couldn’t load packages",
        body: "Your details are safe. Please try again, or contact our support team if this continues.",
        action: "Try again",
      },
      offline: {
        icon: CloudSlash,
        title: "You appear to be offline",
        body: "Reconnect to see the latest published packages and live availability.",
        action: "Check connection",
      },
    };
    const current = states[state];
    const StateIcon = current.icon;
    return (
      <div className="customer-state" role="status">
        <span className="state-icon">
          <StateIcon size={31} aria-hidden="true" />
        </span>
        <h3>{current.title}</h3>
        <p>{current.body}</p>
        <button onClick={onRetry}>{current.action}</button>
      </div>
    );
  }

  return (
    <div className="packages-grid">
      {published.map((batch) => (
        <PackageCard batch={batch} key={batch.id} />
      ))}
    </div>
  );
}

function CustomerView({ refreshKey }: { refreshKey: number }) {
  const [state, setState] = useState<CustomerStatus>("loading");
  const [published, setPublished] = useState<PublishedBatch[]>([]);
  const load = async () => {
    setState(navigator.onLine ? "loading" : "offline");
    if (!navigator.onLine) return;
    try {
      const response = await fetch("/api/v1/batches", { cache: "no-store" });
      if (!response.ok) throw new Error("catalogue unavailable");
      const batches = (await response.json()) as ApiBatch[];
      setPublished(
        batches.map((batch) => ({
          id: batch.id,
          operator: batch.operatorName,
          packageName: batch.packageName,
          tier: batch.tier,
          departureCity: batch.departureCity,
          route: `${batch.durationDays} nights · ${batch.route}`,
          departureDate: formatDate(batch.departureDate),
          returnDate: formatDate(batch.returnDate),
          capacity: batch.capacity,
          seats: batch.availability === 0 ? batch.capacity : null,
          availability: batch.availability,
          price: batch.totalStartingPriceInr,
          image: "/assets/kaaba-morning.png",
          inclusions: batch.inclusions,
        })),
      );
      setState(batches.length ? "results" : "empty");
    } catch {
      setState("error");
    }
  };
  useEffect(() => {
    const pending = window.setTimeout(load, 0);
    return () => window.clearTimeout(pending);
  }, [refreshKey]);
  return (
    <main className="customer-page">
      <section className="discovery-hero">
        <Image
          className="hero-art"
          src="/assets/kaaba-morning.png"
          alt=""
          fill
          priority
          sizes="100vw"
          aria-hidden="true"
        />
        <div className="hero-copy">
          <span className="eyebrow">Journeys you can trust</span>
          <h1>Your Umrah, thoughtfully planned.</h1>
          <p>
            Compare published packages from verified operators, with clear
            pricing and honest availability.
          </p>
        </div>
        <div className="search-panel" aria-label="Package discovery summary">
          <div>
            <MapPin size={20} aria-hidden="true" />
            <span>
              <small>Departure city</small>
              <strong>All cities</strong>
            </span>
            <CaretDown size={16} aria-hidden="true" />
          </div>
          <div>
            <CalendarBlank size={20} aria-hidden="true" />
            <span>
              <small>Travel period</small>
              <strong>Oct – Dec 2026</strong>
            </span>
            <CaretDown size={16} aria-hidden="true" />
          </div>
          <button type="button">
            <SlidersHorizontal size={18} aria-hidden="true" />
            Refine
          </button>
        </div>
        <TrustStrip />
      </section>

      <section className="packages-section">
        <div className="section-heading">
          <div>
            <span className="eyebrow">Published journeys</span>
            <h2>Find your path to the Haramain</h2>
            <p>Every price shown is the total starting price per person.</p>
          </div>
          <StatusSwitcher state={state} onChange={setState} />
        </div>
        <CustomerState state={state} published={published} onRetry={load} />
      </section>
      <footer className="customer-footer">
        <Brand />
        <p>
          Trusted packages, verified operators, and human support for your Umrah
          journey.
        </p>
        <a href="mailto:support@noorpath.example">Contact human support</a>
      </footer>
    </main>
  );
}

function Field({
  label,
  children,
  hint,
  error,
  required = true,
}: {
  label: string;
  children: ReactNode;
  hint?: string;
  error?: string;
  required?: boolean;
}) {
  return (
    <label className={error ? "field field-error" : "field"}>
      <span>
        {label} {required && <em aria-hidden="true">*</em>}
      </span>
      {children}
      {hint && <small>{hint}</small>}
      {error && <small className="error-text">{error}</small>}
    </label>
  );
}

function AdminView({
  onPublish,
}: {
  onPublish: (batch?: null, switchToCustomer?: boolean) => void;
}) {
  const [form, setForm] = useState<BatchForm>(initialBatch);
  const [inclusions, setInclusions] = useState([
    "Return flights",
    "4★ hotels",
    "Breakfast",
    "Group visa",
  ]);
  const [newInclusion, setNewInclusion] = useState("");
  const [errors, setErrors] = useState<FormErrors>({});
  const [step, setStep] = useState<"edit" | "confirm" | "success">("edit");
  const [publishedId, setPublishedId] = useState("");
  const [draft, setDraft] = useState<DraftResponse | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const reviewButton = useRef<HTMLButtonElement>(null);

  const update = <K extends keyof BatchForm>(key: K, value: BatchForm[K]) => {
    setDraft(null);
    setForm((current) => ({ ...current, [key]: value }));
  };

  const duration = useMemo(() => {
    if (!form.departureDate || !form.returnDate) return 0;
    return Math.round(
      (new Date(form.returnDate).getTime() -
        new Date(form.departureDate).getTime()) /
        86400000,
    );
  }, [form.departureDate, form.returnDate]);

  const validate = () => {
    const next: FormErrors = {};
    if (!form.packageName.trim())
      next.packageName = "Enter a public package name.";
    if (!form.departureDate) next.departureDate = "Choose a departure date.";
    if (!form.returnDate || duration <= 0)
      next.returnDate = "Return date must be after departure.";
    if (Number(form.capacity) <= 0)
      next.capacity = "Capacity must be greater than zero.";
    if (Number(form.price) <= 0) next.price = "Enter a total starting price.";
    setErrors(next);
    if (Object.keys(next).length) {
      requestAnimationFrame(() =>
        document.querySelector<HTMLElement>(".error-summary")?.focus(),
      );
      return false;
    }
    return true;
  };

  const addInclusion = () => {
    const clean = newInclusion.trim();
    if (clean && !inclusions.includes(clean))
      setInclusions([...inclusions, clean]);
    setNewInclusion("");
  };

  const createDraft = async () => {
    if (!validate()) return null;
    setSubmitting(true);
    try {
      const response = await fetch("/api/v1/admin/batches", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "X-NoorPath-Admin": "s02-pilot-admin",
        },
        body: JSON.stringify({
          operatorId: form.operatorId,
          operatorName: form.operatorName,
          packageName: form.packageName,
          summary: form.summary,
          tier: form.tier,
          departureCity: form.departureCity,
          route: form.route,
          departureDate: form.departureDate,
          returnDate: form.returnDate,
          capacity: Number(form.capacity),
          availability: [
            "Exact seats",
            "Limited availability",
            "Waitlist only",
            "Unavailable",
          ].indexOf(form.availability),
          totalPriceInr: Number(form.price),
          inclusions,
        }),
      });
      const body = await response.json();
      if (!response.ok) {
        setErrors(
          body.errors ?? { form: body.detail ?? "Draft could not be saved." },
        );
        return null;
      }
      setDraft(body);
      return body;
    } catch {
      setErrors({
        form: "Draft could not be saved. Check the connection and retry.",
      });
      return null;
    } finally {
      setSubmitting(false);
    }
  };

  const closeConfirmation = () => {
    setStep("edit");
    requestAnimationFrame(() => reviewButton.current?.focus());
  };

  const confirm = async () => {
    const saved = draft ?? (await createDraft());
    if (saved) setStep("confirm");
  };

  const publish = async () => {
    if (!draft) return;
    setSubmitting(true);
    try {
      const response = await fetch(
        `/api/v1/admin/batches/${draft.id}/publish`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            "X-NoorPath-Admin": "s02-pilot-admin",
          },
          body: JSON.stringify({
            expectedVersion: draft.version,
            operatorId: form.operatorId,
          }),
        },
      );
      const body = await response.json();
      if (!response.ok) {
        setErrors({ form: body.detail ?? "Publication was rejected." });
        setStep("edit");
        return;
      }
      setPublishedId(body.id);
      setStep("success");
      onPublish();
    } catch {
      setErrors({ form: "Publication could not be completed. Retry safely." });
      setStep("edit");
    } finally {
      setSubmitting(false);
    }
  };

  if (step === "success") {
    return (
      <main className="admin-shell success-shell">
        <aside className="admin-sidebar">
          <Brand />
        </aside>
        <section className="success-panel" role="status">
          <span className="success-icon">
            <Check size={32} weight="bold" aria-hidden="true" />
          </span>
          <span className="eyebrow">Published successfully</span>
          <h1>{form.packageName} is now visible</h1>
          <p>
            The public customer view now includes this batch. No booking or seat
            reservation has been created.
          </p>
          <dl>
            <div>
              <dt>Batch ID</dt>
              <dd>{publishedId}</dd>
            </div>
            <div>
              <dt>Status</dt>
              <dd>Published</dd>
            </div>
          </dl>
          <div className="success-actions">
            <button
              className="primary-button"
              onClick={() => onPublish(null, true)}
            >
              View customer page <ArrowRight size={18} aria-hidden="true" />
            </button>
            <button
              className="secondary-button"
              onClick={() => setStep("edit")}
            >
              Back to draft
            </button>
          </div>
        </section>
      </main>
    );
  }

  return (
    <main className="admin-shell">
      <aside className="admin-sidebar">
        <Brand />
        <nav aria-label="Admin navigation">
          <button className="active">
            <SuitcaseRolling size={20} aria-hidden="true" />
            Departure batches
          </button>
          <button>
            <Buildings size={20} aria-hidden="true" />
            Operators
          </button>
          <button>
            <UsersThree size={20} aria-hidden="true" />
            Team access
          </button>
        </nav>
        <div className="sidebar-user">
          <UserCircle size={32} aria-hidden="true" />
          <span>
            <strong>Sazid Khan</strong>
            <small>NoorPath admin</small>
          </span>
          <SignOut size={18} aria-hidden="true" />
        </div>
      </aside>

      <section className="admin-content">
        <div className="admin-titlebar">
          <div>
            <span className="eyebrow">Catalogue · New departure batch</span>
            <h1>Create a publishable journey</h1>
            <p>Only approved test operators are available in this pilot.</p>
          </div>
          <span className="draft-pill">Draft · Not public</span>
        </div>

        {Object.keys(errors).length > 0 && (
          <div className="error-summary" role="alert" tabIndex={-1}>
            <WarningCircle size={22} weight="fill" aria-hidden="true" />
            <div>
              <strong>
                Review {Object.keys(errors).length} highlighted field(s)
              </strong>
              <span>Your valid entries have been preserved.</span>
            </div>
          </div>
        )}

        <form onSubmit={(event) => event.preventDefault()}>
          <section className="form-card">
            <div className="form-card-heading">
              <span>01</span>
              <div>
                <h2>Operator and package</h2>
                <p>Public identity and summary shown to customers.</p>
              </div>
            </div>
            <div className="form-grid">
              <Field label="Approved operator">
                <select
                  value={form.operatorId}
                  onChange={(event) => {
                    const option = event.target.selectedOptions[0];
                    setForm((current) => ({
                      ...current,
                      operatorId: event.target.value,
                      operatorName: option.text,
                    }));
                    setDraft(null);
                  }}
                >
                  <option value="test-approved-noor">
                    Noor International Tours & Travels
                  </option>
                  <option value="test-approved-rahma">
                    Rahma Pilgrimage Services
                  </option>
                </select>
                <span className="verified-field">
                  <ShieldCheck size={16} weight="fill" aria-hidden="true" />
                  Approved test operator
                </span>
              </Field>
              <Field label="Package tier">
                <select
                  value={form.tier}
                  onChange={(event) => update("tier", event.target.value)}
                >
                  <option>Essential</option>
                  <option>Comfort</option>
                  <option>Premium</option>
                </select>
              </Field>
              <Field label="Public package name" error={errors.packageName}>
                <input
                  value={form.packageName}
                  onChange={(event) =>
                    update("packageName", event.target.value)
                  }
                />
              </Field>
              <Field label="Short public summary">
                <input
                  value={form.summary}
                  onChange={(event) => update("summary", event.target.value)}
                />
              </Field>
            </div>
          </section>

          <section className="form-card">
            <div className="form-card-heading">
              <span>02</span>
              <div>
                <h2>Departure and availability</h2>
                <p>Dates, route and truthful capacity disclosure.</p>
              </div>
            </div>
            <div className="form-grid three">
              <Field label="Departure city">
                <select
                  value={form.departureCity}
                  onChange={(event) =>
                    update("departureCity", event.target.value)
                  }
                >
                  <option>Delhi (DEL)</option>
                  <option>Lucknow (LKO)</option>
                  <option>Mumbai (BOM)</option>
                </select>
              </Field>
              <Field label="Route">
                <input
                  value={form.route}
                  onChange={(event) => update("route", event.target.value)}
                />
              </Field>
              <Field label="Availability display">
                <select
                  value={form.availability}
                  onChange={(event) =>
                    update("availability", event.target.value)
                  }
                >
                  <option>Exact seats</option>
                  <option>Limited availability</option>
                  <option>Waitlist only</option>
                  <option>Unavailable</option>
                </select>
              </Field>
              <Field label="Departure date" error={errors.departureDate}>
                <input
                  type="date"
                  value={form.departureDate}
                  onChange={(event) =>
                    update("departureDate", event.target.value)
                  }
                />
              </Field>
              <Field label="Return date" error={errors.returnDate}>
                <input
                  type="date"
                  value={form.returnDate}
                  onChange={(event) => update("returnDate", event.target.value)}
                />
              </Field>
              <Field label="Total capacity" error={errors.capacity}>
                <input
                  type="number"
                  min="1"
                  value={form.capacity}
                  onChange={(event) => update("capacity", event.target.value)}
                />
              </Field>
            </div>
          </section>

          <section className="form-card">
            <div className="form-card-heading">
              <span>03</span>
              <div>
                <h2>Price and inclusions</h2>
                <p>No hidden “from” price—show the total starting price.</p>
              </div>
            </div>
            <div className="price-inclusions">
              <Field
                label="Total starting price per person"
                hint={`Customer sees ${formatCurrency(form.price)} total`}
                error={errors.price}
              >
                <div className="currency-input">
                  <span>₹</span>
                  <input
                    type="number"
                    min="1"
                    value={form.price}
                    onChange={(event) => update("price", event.target.value)}
                  />
                </div>
              </Field>
              <div className="field">
                <span>Dynamic inclusion highlights</span>
                <div className="tag-list">
                  {inclusions.map((item) => (
                    <span key={item}>
                      {item}
                      <button
                        type="button"
                        aria-label={`Remove ${item}`}
                        onClick={() =>
                          setInclusions(
                            inclusions.filter((value) => value !== item),
                          )
                        }
                      >
                        <X size={13} aria-hidden="true" />
                      </button>
                    </span>
                  ))}
                </div>
                <div className="add-inclusion">
                  <input
                    value={newInclusion}
                    placeholder="Add another inclusion"
                    onChange={(event) => setNewInclusion(event.target.value)}
                    onKeyDown={(event) => {
                      if (event.key === "Enter") {
                        event.preventDefault();
                        addInclusion();
                      }
                    }}
                  />
                  <button type="button" onClick={addInclusion}>
                    <Plus size={17} aria-hidden="true" />
                    Add
                  </button>
                </div>
              </div>
            </div>
          </section>
        </form>

        <div className="admin-sticky-footer">
          <span>
            <ShieldCheck size={19} aria-hidden="true" />
            Server validation and audit recording apply at publication.
          </span>
          <div>
            <button
              className="secondary-button"
              type="button"
              onClick={createDraft}
              disabled={submitting || Boolean(draft)}
            >
              {draft ? "Draft saved" : submitting ? "Saving…" : "Save draft"}
            </button>
            <button
              className="primary-button"
              type="button"
              onClick={confirm}
              ref={reviewButton}
            >
              {submitting ? "Saving…" : "Review and publish"}{" "}
              <ArrowRight size={18} aria-hidden="true" />
            </button>
          </div>
        </div>
      </section>

      {step === "confirm" && (
        <div
          className="modal-backdrop"
          role="presentation"
          onKeyDown={(event) => {
            if (event.key === "Escape") closeConfirmation();
          }}
        >
          <section
            className="publish-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="publish-title"
          >
            <span className="modal-icon">
              <ShieldCheck size={28} aria-hidden="true" />
            </span>
            <span className="eyebrow">Explicit publication</span>
            <h2 id="publish-title">Make this journey public?</h2>
            <p>
              Customers will immediately see this approved operator, total
              price, dates, inclusions and exact availability.
            </p>
            <dl>
              <div>
                <dt>Package</dt>
                <dd>{form.packageName}</dd>
              </div>
              <div>
                <dt>Total price</dt>
                <dd>{formatCurrency(form.price)} per person</dd>
              </div>
              <div>
                <dt>Capacity</dt>
                <dd>{form.capacity} seats</dd>
              </div>
            </dl>
            <div className="modal-actions">
              <button
                className="secondary-button"
                onClick={closeConfirmation}
                autoFocus
              >
                Keep as draft
              </button>
              <button
                className="primary-button"
                onClick={publish}
                disabled={submitting}
              >
                <Check size={18} weight="bold" aria-hidden="true" />
                Confirm publication
              </button>
            </div>
          </section>
        </div>
      )}
    </main>
  );
}

export default function App() {
  const [view, setView] = useState<View>("customer");
  const [refreshKey, setRefreshKey] = useState(0);

  const handlePublish = (_batch?: null, switchToCustomer = false) => {
    setRefreshKey((current) => current + 1);
    if (switchToCustomer) setView("customer");
  };

  return (
    <div className="app">
      {view === "customer" && <Header view={view} setView={setView} />}
      {view === "customer" ? (
        <CustomerView refreshKey={refreshKey} />
      ) : (
        <AdminView onPublish={handlePublish} />
      )}
    </div>
  );
}
