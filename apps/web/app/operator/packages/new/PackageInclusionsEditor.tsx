"use client";

import { FormEvent, useMemo, useState } from "react";
import {
  STANDARD_PACKAGE_EXCLUSIONS,
  STANDARD_PACKAGE_INCLUSIONS,
} from "../packageDraftStandards";

type PackageInclusionsEditorProps = {
  inclusions: string[];
  exclusions: string[];
  onToggleInclusion: (item: string) => void;
  onToggleExclusion: (item: string) => void;
};

type GroupKey = "package" | "travel" | "umrah" | "excluded";

type Option = {
  value: string;
  label: string;
  icon: IconName;
};

type IconName =
  | "plane"
  | "visa"
  | "hotel"
  | "mosque"
  | "meal"
  | "bus"
  | "ziyarah"
  | "guide"
  | "tag"
  | "pouch"
  | "id"
  | "sim"
  | "contact"
  | "ihram"
  | "bag"
  | "bottle"
  | "book"
  | "water"
  | "wallet"
  | "excursion"
  | "shield"
  | "baggage"
  | "bed"
  | "laundry"
  | "custom";

const PACKAGE_OPTIONS: Option[] = [
  { value: "Return flights", label: "Return flights", icon: "plane" },
  { value: "Visa included", label: "Visa included", icon: "visa" },
  {
    value: "Makkah accommodation",
    label: "Makkah accommodation",
    icon: "hotel",
  },
  {
    value: "Madinah accommodation",
    label: "Madinah accommodation",
    icon: "mosque",
  },
  {
    value: "Breakfast, lunch and dinner",
    label: "Breakfast, lunch and dinner",
    icon: "meal",
  },
  { value: "Intercity travel", label: "Intercity travel", icon: "bus" },
  {
    value: "Ziyarat transport",
    label: "Ziyarat transport",
    icon: "ziyarah",
  },
  { value: "Umrah guidance", label: "Umrah guidance", icon: "guide" },
];

const TRAVEL_KIT_OPTIONS: Option[] = [
  { value: "Luggage tag", label: "Luggage tag", icon: "tag" },
  {
    value: "Neck pouch / document wallet",
    label: "Neck pouch / document wallet",
    icon: "pouch",
  },
  { value: "ID card", label: "ID card", icon: "id" },
  {
    value: "SIM / eSIM guidance",
    label: "SIM / eSIM guidance",
    icon: "sim",
  },
  {
    value: "Emergency contact card",
    label: "Emergency contact card",
    icon: "contact",
  },
];

const UMRAH_KIT_OPTIONS: Option[] = [
  {
    value: "Ihram for men / prayer essentials option",
    label: "Ihram / prayer essentials",
    icon: "ihram",
  },
  { value: "Drawstring bag", label: "Drawstring bag", icon: "bag" },
  {
    value: "Unscented toiletries",
    label: "Unscented toiletries",
    icon: "bottle",
  },
  { value: "Pocket Dua guide", label: "Pocket Dua guide", icon: "book" },
  {
    value: "Zamzam handling guidance",
    label: "Zamzam handling guidance",
    icon: "water",
  },
];

const EXCLUSION_OPTIONS: Option[] = [
  { value: "Personal expenses", label: "Personal expenses", icon: "wallet" },
  {
    value: "Optional excursions",
    label: "Optional excursions",
    icon: "excursion",
  },
  {
    value: "Travel insurance unless stated",
    label: "Travel insurance unless stated",
    icon: "shield",
  },
  { value: "Extra baggage", label: "Extra baggage", icon: "baggage" },
  { value: "Room upgrade", label: "Room upgrade", icon: "bed" },
  { value: "Laundry", label: "Laundry", icon: "laundry" },
];

const GROUP_COPY: Record<
  GroupKey,
  { title: string; helper: string; mode: "inclusion" | "exclusion" }
> = {
  package: {
    title: "Package includes",
    helper: "Standard NoorPath package items are selected by default.",
    mode: "inclusion",
  },
  travel: {
    title: "Travel kit included",
    helper: "Select only the travel-kit items supplied with this package.",
    mode: "inclusion",
  },
  umrah: {
    title: "Umrah kit included",
    helper: "Add kit items provided by the operator for this journey.",
    mode: "inclusion",
  },
  excluded: {
    title: "Not included",
    helper: "Standard exclusions are selected by default; add or remove as needed.",
    mode: "exclusion",
  },
};

function PackageOptionIcon({ name }: { name: IconName }) {
  const common = {
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
  };

  const paths: Record<IconName, React.ReactNode> = {
    plane: (
      <>
        <path {...common} d="m3 13 7-2 4-7 2 1-2 6 6 2v2l-6 1-2 5-2-1 1-5-6 1-2-3Z" />
      </>
    ),
    visa: (
      <>
        <rect {...common} x="5" y="3" width="12" height="18" rx="2" />
        <circle {...common} cx="11" cy="9" r="2.5" />
        <path {...common} d="M8 15h6M8 18h4M18 14l3 3" />
      </>
    ),
    hotel: (
      <>
        <path {...common} d="M5 21V5h14v16M3 21h18" />
        <path {...common} d="M8 8h2M14 8h2M8 12h2M14 12h2M8 16h2M14 16h2" />
      </>
    ),
    mosque: (
      <>
        <path {...common} d="M4 21h16M6 21v-8h12v8M8 13c0-4 8-4 8 0M12 5v3M10 5h4" />
        <path {...common} d="M10 21v-4h4v4" />
      </>
    ),
    meal: (
      <>
        <path {...common} d="M4 15h16M6 15a6 6 0 0 1 12 0M12 7V5" />
        <path {...common} d="M5 19h14" />
      </>
    ),
    bus: (
      <>
        <rect {...common} x="5" y="3" width="14" height="16" rx="3" />
        <path {...common} d="M7 7h10v5H7zM8 19v2M16 19v2" />
        <circle {...common} cx="9" cy="16" r="1" />
        <circle {...common} cx="15" cy="16" r="1" />
      </>
    ),
    ziyarah: (
      <>
        <path {...common} d="M4 21h16M6 21V10l6-5 6 5v11M9 21v-6h6v6" />
        <path {...common} d="M9 11h6" />
      </>
    ),
    guide: (
      <>
        <circle {...common} cx="12" cy="7" r="3" />
        <path {...common} d="M6 21v-3a6 6 0 0 1 12 0v3M9 13l3 3 3-3" />
      </>
    ),
    tag: (
      <>
        <path {...common} d="M7 4h8l4 4v12H7z" />
        <circle {...common} cx="11" cy="8" r="1" />
      </>
    ),
    pouch: (
      <>
        <path {...common} d="M7 8h10l-1 12H8zM9 8V5h6v3" />
        <path {...common} d="M9 12h6" />
      </>
    ),
    id: (
      <>
        <rect {...common} x="3" y="6" width="18" height="13" rx="2" />
        <circle {...common} cx="8" cy="12" r="2" />
        <path {...common} d="M5.5 16c1-2 4-2 5 0M13 11h5M13 15h4" />
      </>
    ),
    sim: (
      <>
        <path {...common} d="M7 3h8l3 3v15H7z" />
        <rect {...common} x="10" y="12" width="5" height="5" rx="1" />
        <path {...common} d="M18 4c2 1 3 3 3 5M17 7c1 .5 1.5 1.5 1.5 3" />
      </>
    ),
    contact: (
      <>
        <rect {...common} x="6" y="3" width="12" height="18" rx="2" />
        <path {...common} d="M9 8h6M9 12h6M9 16h4" />
      </>
    ),
    ihram: (
      <>
        <path {...common} d="M7 5c2 1 3 3 3 5v11H5V10c0-2 1-4 2-5ZM17 5c-2 1-3 3-3 5v11h5V10c0-2-1-4-2-5Z" />
      </>
    ),
    bag: (
      <>
        <path {...common} d="M7 8h10l2 12H5zM9 8c0-2 1-4 3-4s3 2 3 4" />
      </>
    ),
    bottle: (
      <>
        <path {...common} d="M9 3h6v4l2 3v11H7V10l2-3z" />
        <path {...common} d="M9 14h8M10 6h4" />
      </>
    ),
    book: (
      <>
        <path {...common} d="M5 4h11a3 3 0 0 1 3 3v13H8a3 3 0 0 1-3-3z" />
        <path {...common} d="M8 4v16M11 9h5M11 13h5" />
      </>
    ),
    water: (
      <>
        <path {...common} d="M8 3h6v4l2 3v11H6V10l2-3z" />
        <path {...common} d="M17 14c2-2 4 0 4 2a2 2 0 0 1-4 0c0-1 .5-1.5 1-2" />
      </>
    ),
    wallet: (
      <>
        <path {...common} d="M4 6h15v14H4zM4 9h17v7h-5a2 2 0 0 1 0-4h5" />
      </>
    ),
    excursion: (
      <>
        <path {...common} d="M4 19 9 8l3 5 2-3 6 9z" />
        <path {...common} d="M17 5v4M15 7h4" />
      </>
    ),
    shield: (
      <>
        <path {...common} d="M12 3 19 6v6c0 5-3 8-7 10-4-2-7-5-7-10V6z" />
        <path {...common} d="m9 12 2 2 4-4" />
      </>
    ),
    baggage: (
      <>
        <rect {...common} x="6" y="7" width="12" height="13" rx="2" />
        <path {...common} d="M9 7V4h6v3M9 11v5M15 11v5" />
      </>
    ),
    bed: (
      <>
        <path {...common} d="M4 18V9M4 14h16v4M7 14V9h5a3 3 0 0 1 3 3v2" />
        <path {...common} d="M4 18v2M20 18v2" />
      </>
    ),
    laundry: (
      <>
        <rect {...common} x="6" y="3" width="12" height="18" rx="2" />
        <circle {...common} cx="12" cy="14" r="4" />
        <path {...common} d="M9 7h1M13 7h2" />
      </>
    ),
    custom: (
      <>
        <rect {...common} x="5" y="5" width="14" height="14" rx="3" />
        <path {...common} d="M12 8v8M8 12h8" />
      </>
    ),
  };

  return (
    <svg viewBox="0 0 24 24" aria-hidden="true" focusable="false">
      {paths[name]}
    </svg>
  );
}

export default function PackageInclusionsEditor({
  inclusions,
  exclusions,
  onToggleInclusion,
  onToggleExclusion,
}: PackageInclusionsEditorProps) {
  const [customOptions, setCustomOptions] = useState<
    Record<GroupKey, Option[]>
  >({ package: [], travel: [], umrah: [], excluded: [] });
  const [addingTo, setAddingTo] = useState<GroupKey | null>(null);
  const [customLabel, setCustomLabel] = useState("");

  const groups = useMemo(
    () => [
      { key: "package" as const, options: PACKAGE_OPTIONS },
      { key: "travel" as const, options: TRAVEL_KIT_OPTIONS },
      { key: "umrah" as const, options: UMRAH_KIT_OPTIONS },
      { key: "excluded" as const, options: EXCLUSION_OPTIONS },
    ],
    [],
  );

  const addCustomOption = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!addingTo) return;

    const value = customLabel.trim();
    if (!value) return;

    const normalized = value.toLocaleLowerCase();
    const existing = [
      ...PACKAGE_OPTIONS,
      ...TRAVEL_KIT_OPTIONS,
      ...UMRAH_KIT_OPTIONS,
      ...EXCLUSION_OPTIONS,
      ...Object.values(customOptions).flat(),
    ].some((item) => item.value.toLocaleLowerCase() === normalized);

    if (existing) {
      setCustomLabel("");
      setAddingTo(null);
      return;
    }

    const option: Option = { value, label: value, icon: "custom" };
    setCustomOptions((current) => ({
      ...current,
      [addingTo]: [...current[addingTo], option],
    }));

    if (GROUP_COPY[addingTo].mode === "exclusion") {
      if (!exclusions.includes(value)) onToggleExclusion(value);
    } else if (!inclusions.includes(value)) {
      onToggleInclusion(value);
    }

    setCustomLabel("");
    setAddingTo(null);
  };

  return (
    <div className="package-options-editor">
      {groups.map(({ key, options }) => {
        const copy = GROUP_COPY[key];
        const allOptions = [...options, ...customOptions[key]];
        const selected = copy.mode === "exclusion" ? exclusions : inclusions;
        const toggle =
          copy.mode === "exclusion" ? onToggleExclusion : onToggleInclusion;

        return (
          <section className="package-option-group" key={key}>
            <div className="package-option-group-heading">
              <div>
                <h3>{copy.title}</h3>
                <p>{copy.helper}</p>
              </div>
            </div>

            <div className="package-option-grid">
              {allOptions.map((item) => {
                const checked = selected.includes(item.value);
                return (
                  <label
                    className="package-option-tile"
                    data-selected={checked ? "true" : "false"}
                    key={item.value}
                  >
                    <input
                      type="checkbox"
                      checked={checked}
                      onChange={() => toggle(item.value)}
                    />
                    <span className="package-option-icon">
                      <PackageOptionIcon name={item.icon} />
                    </span>
                    <strong>{item.label}</strong>
                  </label>
                );
              })}

              <button
                className="package-option-add"
                type="button"
                aria-label={`Add another item to ${copy.title}`}
                onClick={() => {
                  setAddingTo(key);
                  setCustomLabel("");
                }}
              >
                <span aria-hidden="true">+</span>
                <strong>Add more</strong>
              </button>
            </div>

            {addingTo === key ? (
              <form className="package-option-custom" onSubmit={addCustomOption}>
                <label>
                  <span>Custom item</span>
                  <input
                    autoFocus
                    value={customLabel}
                    placeholder="e.g. Wheelchair assistance"
                    onChange={(event) => setCustomLabel(event.target.value)}
                  />
                </label>
                <div>
                  <button
                    className="secondary-button"
                    type="button"
                    onClick={() => {
                      setAddingTo(null);
                      setCustomLabel("");
                    }}
                  >
                    Cancel
                  </button>
                  <button
                    className="primary-button"
                    type="submit"
                    disabled={!customLabel.trim()}
                  >
                    Add item
                  </button>
                </div>
              </form>
            ) : null}
          </section>
        );
      })}
    </div>
  );
}

export const DEFAULT_PACKAGE_INCLUSIONS = [...STANDARD_PACKAGE_INCLUSIONS];
export const DEFAULT_PACKAGE_EXCLUSIONS = [...STANDARD_PACKAGE_EXCLUSIONS];
