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

// prettier-ignore
export default function PackageQuickStart() {
  const router = useRouter();
  const [origin, setOrigin] = useState("");
  const [departureDate, setDepartureDate] = useState("");
  const [returnDate, setReturnDate] = useState("");
  const [makkahHotel, setMakkahHotel] = useState("");
  const [madinahHotel, setMadinahHotel] = useState("");
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
    if (!duration) {
      setError("Return date must be after the departure date.");
      return;
    }

    setState("saving");
    setError("");
    const makkahNights = Math.ceil(duration.nights / 2);
    const madinahNights = duration.nights - makkahNights;
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
            classification: "",
            distanceDisclosure: "",
            nights: makkahNights,
            confirmationState: "pending",
          },
          madinah: {
            hotelName: madinahHotel.trim(),
            classification: "",
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

        <section className="form-card">
          <div className="form-card-heading">
            <span>01</span>
            <div>
              <h2>Journey</h2>
              <p>
                Dates automatically calculate the customer-facing duration and
                title.
              </p>
            </div>
          </div>
          <div className="form-grid">
            <label className="field">
              <span>Departure origin *</span>
              <input
                value={origin}
                placeholder="e.g. Delhi (DEL)"
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
                onChange={(event) => setReturnDate(event.target.value)}
              />
            </label>
          </div>
          <div className="operator-inline-state" aria-live="polite">
            <div>
              <small>Suggested heading</small>
              <strong>{title}</strong>
            </div>
            <div>
              <small>Calculated duration</small>
              <strong>
                {duration
                  ? `${duration.days} Days / ${duration.nights} Nights`
                  : "Choose valid dates"}
              </strong>
            </div>
          </div>
        </section>

        <section className="form-card">
          <div className="form-card-heading">
            <span>02</span>
            <div>
              <h2>Stays & intercity travel</h2>
              <p>
                The nights are split automatically and remain editable in the
                full composer.
              </p>
            </div>
          </div>
          <div className="form-grid">
            <label className="field">
              <span>Makkah hotel *</span>
              <input
                value={makkahHotel}
                onChange={(event) => setMakkahHotel(event.target.value)}
              />
            </label>
            <label className="field">
              <span>Madinah hotel *</span>
              <input
                value={madinahHotel}
                onChange={(event) => setMadinahHotel(event.target.value)}
              />
            </label>
          </div>
          <fieldset className="confirmation-field">
            <legend>Intercity travel provided by operator</legend>
            <label>
              <input
                type="radio"
                name="intercity"
                checked={intercityMode === "bus"}
                onChange={() => setIntercityMode("bus")}
              />
              <span>
                <strong>Bus</strong>
                <small>Coach or private bus transfer</small>
              </span>
            </label>
            <label>
              <input
                type="radio"
                name="intercity"
                checked={intercityMode === "train"}
                onChange={() => setIntercityMode("train")}
              />
              <span>
                <strong>Train</strong>
                <small>Haramain or another confirmed rail service</small>
              </span>
            </label>
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
