"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
  STANDARD_PACKAGE_EXCLUSIONS,
  STANDARD_PACKAGE_INCLUSIONS,
  calculateJourneyDuration,
  suggestPackageTitle,
} from "../packageDraftStandards";
import PackageInclusionsEditor from "./PackageInclusionsEditor";

type IntercityMode = "bus" | "train";
type SaveState = "ready" | "saving" | "error";

function TravelModeIcon({ mode }: { mode: IntercityMode }) {
  const common = {
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
  };

  return mode === "bus" ? (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <rect {...common} x="5" y="3" width="14" height="16" rx="3" />
      <path {...common} d="M7 8h10M8 19v2M16 19v2M8 15h.01M16 15h.01" />
    </svg>
  ) : (
    <svg viewBox="0 0 24 24" aria-hidden="true">
      <path
        {...common}
        d="M7 4h10a2 2 0 0 1 2 2v9a4 4 0 0 1-4 4H9a4 4 0 0 1-4-4V6a2 2 0 0 1 2-2Z"
      />
      <path {...common} d="M7 9h10M8 15h.01M16 15h.01M9 19l-2 2M15 19l2 2" />
    </svg>
  );
}

// prettier-ignore
export default function PackageQuickStart() {
  const router = useRouter();
  const [origin, setOrigin] = useState("");
  const [departureDate, setDepartureDate] = useState("");
  const [returnDate, setReturnDate] = useState("");
  const [makkahHotel, setMakkahHotel] = useState("");
  const [madinahHotel, setMadinahHotel] = useState("");
  const [makkahClassification, setMakkahClassification] = useState("");
  const [madinahClassification, setMadinahClassification] = useState("");
  const [intercityMode, setIntercityMode] =
    useState<IntercityMode>("bus");
  const [inclusions, setInclusions] = useState<string[]>([
    ...STANDARD_PACKAGE_INCLUSIONS,
  ]);
  const [exclusions, setExclusions] = useState<string[]>([
    ...STANDARD_PACKAGE_EXCLUSIONS,
  ]);
  const [state, setState] = useState<SaveState>("ready");
  const [error, setError] = useState("");

  const duration = useMemo(
    () => calculateJourneyDuration(departureDate, returnDate),
    [departureDate, returnDate],
  );
  const title = useMemo(
    () => suggestPackageTitle(origin, departureDate, returnDate),
    [origin, departureDate, returnDate],
  );
  const makkahNights = duration ? Math.ceil(duration.nights / 2) : null;
  const madinahNights = duration ? duration.nights - Math.ceil(duration.nights / 2) : null;

  const toggleInclusion = (item: string) => {
    setInclusions((current) =>
      current.includes(item)
        ? current.filter((value) => value !== item)
        : [...current, item],
    );
  };

  const toggleExclusion = (item: string) => {
    setExclusions((current) =>
      current.includes(item)
        ? current.filter((value) => value !== item)
        : [...current, item],
    );
  };

  const createDraft = async () => {
    if (
      !origin.trim() ||
      !departureDate ||
      !returnDate ||
      !makkahHotel.trim() ||
      !madinahHotel.trim()
    ) {
      setError(
        "Add the origin, dates, and both hotel names to create the draft.",
      );
      return;
    }
    if (!duration || makkahNights === null || madinahNights === null) {
      setError("Return date must be after the departure date.");
      return;
    }

    setState("saving");
    setError("");
    const transportLabel =
      intercityMode === "train"
        ? "Intercity travel by train"
        : "Intercity travel by bus";
    const selectedInclusions = inclusions.map((item) =>
      item === "Intercity travel" ? transportLabel : item,
    );

    try {
      const response = await fetch("/api/v1/operator/departures", {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          packageName: title,
          summary: `${duration.days}-day Umrah journey from ${origin.trim()} with Makkah and Madinah stays, visa included, full-board meals, and ${intercityMode} transfer.`,
          makkah: {
            hotelName: makkahHotel.trim(),
            classification: makkahClassification,
            distanceDisclosure: "",
            nights: makkahNights,
            confirmationState: "pending",
          },
          madinah: {
            hotelName: madinahHotel.trim(),
            classification: madinahClassification,
            distanceDisclosure: "",
            nights: madinahNights,
            confirmationState: "pending",
          },
          travel: {
            routeSummary: `${origin.trim()} → Jeddah → Makkah → Madinah`,
            details: `Intercity transfer by ${intercityMode}. Flight details to be confirmed.`,
            confirmationState: "pending",
          },
          origin: origin.trim(),
          departureDate,
          returnDate,
          inclusions: selectedInclusions,
          exclusions,
        }),
      });

      if (!response.ok) throw new Error();
      const created = (await response.json()) as { departureId: string };
      router.replace(`/operator/departures/${created.departureId}`);
    } catch {
      setState("error");
      setError(
        "The draft could not be created. Your entries are still here; retry safely.",
      );
    }
  };

  return (
    <main className="admin-shell composer-shell">
      <aside className="admin-sidebar composer-sidebar">
        <Link className="brand" href="/operator" aria-label="Operator home">
          <span className="brand-mark" aria-hidden="true">
            ◇
          </span>
          <span>NoorPath</span>
        </Link>
        <nav aria-label="Operator navigation">
          <Link
            className="composer-nav-active"
            href="/operator/packages/new"
            aria-current="page"
          >
            <span className="composer-icon" aria-hidden="true">
              ◈
            </span>
            Create package
          </Link>
          <Link href="/operator/packages">Packages</Link>
          <Link href="/operator/departures">Departures</Link>
        </nav>
      </aside>

      <section className="admin-content composer-content">
        <div className="admin-titlebar">
          <div>
            <span className="eyebrow">Packages · Quick start</span>
            <h1>Create a package draft</h1>
            <p>
              Set the essential journey facts now. Pricing, occupancy,
              milestones and preview follow after the draft is created.
            </p>
          </div>
          <span className="draft-pill">Private draft</span>
        </div>

        {error ? (
          <div className="error-summary" role="alert">
            <strong>Review package details</strong>
            <span>{error}</span>
          </div>
        ) : null}

        <section className="form-card composer-step-card journey-step-card">
          <div className="form-card-heading">
            <span>01</span>
            <div>
              <h2>Journey</h2>
              <p>
                Set the departure city and travel dates. NoorPath calculates the
                duration and customer-facing package heading automatically.
              </p>
            </div>
          </div>
          <div className="form-grid journey-fields">
            <label className="field">
              <span>Departure origin *</span>
              <input
                value={origin}
                placeholder="e.g. Delhi (DEL)"
                autoComplete="off"
                onChange={(event) => setOrigin(event.target.value)}
              />
            </label>
            <label className="field">
              <span>Departure date *</span>
              <input
                type="date"
                value={departureDate}
                onChange={(event) => setDepartureDate(event.target.value)}
              />
            </label>
            <label className="field">
              <span>Return date *</span>
              <input
                type="date"
                value={returnDate}
                min={departureDate || undefined}
                onChange={(event) => setReturnDate(event.target.value)}
              />
            </label>
          </div>
          <div className="journey-summary" aria-live="polite">
            <div>
              <small>Package heading</small>
              <strong>{title}</strong>
            </div>
            <div>
              <small>Journey duration</small>
              <strong>
                {duration
                  ? `${duration.days} Days / ${duration.nights} Nights`
                  : "Choose valid dates"}
              </strong>
            </div>
          </div>
        </section>

        <section className="form-card composer-step-card stay-step-card">
          <div className="form-card-heading">
            <span>02</span>
            <div>
              <h2>Stays & intercity travel</h2>
              <p>
                Add the two hotel stays and choose how travellers move between
                the holy cities. Nights are split automatically from the journey.
              </p>
            </div>
          </div>

          <div className="stay-card-grid">
            <article className="stay-card" aria-labelledby="makkah-stay-title">
              <header>
                <div>
                  <span className="stay-city-kicker">Makkah stay</span>
                  <h3 id="makkah-stay-title">Makkah</h3>
                </div>
                <span className="stay-night-badge">
                  {makkahNights === null ? "Dates first" : `${makkahNights} nights`}
                </span>
              </header>
              <label className="field">
                <span>Hotel name *</span>
                <input
                  value={makkahHotel}
                  placeholder="e.g. Swissotel Makkah"
                  onChange={(event) => setMakkahHotel(event.target.value)}
                />
              </label>
              <label className="field">
                <span>Hotel classification</span>
                <select
                  value={makkahClassification}
                  onChange={(event) => setMakkahClassification(event.target.value)}
                >
                  <option value="">Select if known</option>
                  <option value="3 star">3 star</option>
                  <option value="4 star">4 star</option>
                  <option value="5 star">5 star</option>
                  <option value="Apartment / serviced residence">Apartment / serviced residence</option>
                </select>
              </label>
              <small className="stay-helper">
                Distance and final confirmation can be completed in the full composer.
              </small>
            </article>

            <article className="stay-card" aria-labelledby="madinah-stay-title">
              <header>
                <div>
                  <span className="stay-city-kicker">Madinah stay</span>
                  <h3 id="madinah-stay-title">Madinah</h3>
                </div>
                <span className="stay-night-badge">
                  {madinahNights === null ? "Dates first" : `${madinahNights} nights`}
                </span>
              </header>
              <label className="field">
                <span>Hotel name *</span>
                <input
                  value={madinahHotel}
                  placeholder="e.g. Anwar Al Madinah Mövenpick"
                  onChange={(event) => setMadinahHotel(event.target.value)}
                />
              </label>
              <label className="field">
                <span>Hotel classification</span>
                <select
                  value={madinahClassification}
                  onChange={(event) => setMadinahClassification(event.target.value)}
                >
                  <option value="">Select if known</option>
                  <option value="3 star">3 star</option>
                  <option value="4 star">4 star</option>
                  <option value="5 star">5 star</option>
                  <option value="Apartment / serviced residence">Apartment / serviced residence</option>
                </select>
              </label>
              <small className="stay-helper">
                Distance and final confirmation can be completed in the full composer.
              </small>
            </article>
          </div>

          <fieldset className="intercity-selector">
            <legend>Intercity travel provided by operator</legend>
            <p>Choose the default transfer between Makkah and Madinah.</p>
            <div className="intercity-options">
              <label>
                <input
                  type="radio"
                  name="intercity"
                  checked={intercityMode === "bus"}
                  onChange={() => setIntercityMode("bus")}
                />
                <span className="intercity-icon">
                  <TravelModeIcon mode="bus" />
                </span>
                <span className="intercity-copy">
                  <strong>Bus</strong>
                  <small>Coach or private bus transfer</small>
                </span>
                <span className="intercity-check" aria-hidden="true">✓</span>
              </label>
              <label>
                <input
                  type="radio"
                  name="intercity"
                  checked={intercityMode === "train"}
                  onChange={() => setIntercityMode("train")}
                />
                <span className="intercity-icon">
                  <TravelModeIcon mode="train" />
                </span>
                <span className="intercity-copy">
                  <strong>Train</strong>
                  <small>Haramain or another confirmed rail service</small>
                </span>
                <span className="intercity-check" aria-hidden="true">✓</span>
              </label>
            </div>
          </fieldset>
        </section>

        <section className="form-card package-options-card">
          <div className="form-card-heading">
            <span>03</span>
            <div>
              <h2>Package inclusions & exclusions</h2>
              <p>
                Keep the defaults, untick anything that does not apply, or add
                package-specific items without changing the rest of the draft.
              </p>
            </div>
          </div>
          <PackageInclusionsEditor
            inclusions={inclusions}
            exclusions={exclusions}
            onToggleInclusion={toggleInclusion}
            onToggleExclusion={toggleExclusion}
          />
        </section>
      </section>

      <footer className="admin-sticky-footer composer-savebar">
        <span>
          Next: pricing, occupancy, payment milestones and customer preview
        </span>
        <div>
          <Link className="secondary-button" href="/operator/packages">
            Cancel
          </Link>
          <button
            className="primary-button"
            type="button"
            disabled={state === "saving"}
            onClick={() => void createDraft()}
          >
            {state === "saving"
              ? "Creating…"
              : state === "error"
                ? "Retry create draft"
                : "Create draft & continue"}
          </button>
        </div>
      </footer>
    </main>
  );
}
