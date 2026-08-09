"use client";

import { useMemo, useState, type DragEvent, type FormEvent } from "react";
import {
  STANDARD_PACKAGE_EXCLUSIONS,
  STANDARD_PACKAGE_INCLUSIONS,
} from "../packageDraftStandards";

type Destination = "included" | "excluded";

type PackageInclusionsEditorProps = {
  inclusions: string[];
  exclusions: string[];
  onToggleInclusion: (item: string) => void;
  onToggleExclusion: (item: string) => void;
};

type IconName =
  | "plane"
  | "visa"
  | "hotel"
  | "mosque"
  | "meal"
  | "bus"
  | "guide"
  | "bag"
  | "book"
  | "water"
  | "wallet"
  | "shield"
  | "baggage"
  | "bed"
  | "laundry"
  | "custom";

type Option = {
  value: string;
  label: string;
  icon: IconName;
  group: string;
};

const OPTIONS: Option[] = [
  { value: "Return flights", label: "Return flights", icon: "plane", group: "Package" },
  { value: "Visa included", label: "Visa included", icon: "visa", group: "Package" },
  {
    value: "Makkah accommodation",
    label: "Makkah accommodation",
    icon: "hotel",
    group: "Package",
  },
  {
    value: "Madinah accommodation",
    label: "Madinah accommodation",
    icon: "mosque",
    group: "Package",
  },
  {
    value: "Breakfast, lunch and dinner",
    label: "Breakfast, lunch and dinner",
    icon: "meal",
    group: "Package",
  },
  { value: "Intercity travel", label: "Intercity travel", icon: "bus", group: "Package" },
  { value: "Ziyarat transport", label: "Ziyarat transport", icon: "mosque", group: "Package" },
  { value: "Umrah guidance", label: "Umrah guidance", icon: "guide", group: "Package" },
  { value: "Luggage tag", label: "Luggage tag", icon: "baggage", group: "Travel kit" },
  {
    value: "Neck pouch / document wallet",
    label: "Document wallet",
    icon: "wallet",
    group: "Travel kit",
  },
  { value: "ID card", label: "ID card", icon: "custom", group: "Travel kit" },
  {
    value: "SIM / eSIM guidance",
    label: "SIM / eSIM guidance",
    icon: "custom",
    group: "Travel kit",
  },
  {
    value: "Emergency contact card",
    label: "Emergency contact card",
    icon: "custom",
    group: "Travel kit",
  },
  {
    value: "Ihram for men / prayer essentials option",
    label: "Ihram / prayer essentials",
    icon: "custom",
    group: "Umrah kit",
  },
  { value: "Drawstring bag", label: "Drawstring bag", icon: "bag", group: "Umrah kit" },
  {
    value: "Unscented toiletries",
    label: "Unscented toiletries",
    icon: "custom",
    group: "Umrah kit",
  },
  { value: "Pocket Dua guide", label: "Pocket Dua guide", icon: "book", group: "Umrah kit" },
  {
    value: "Zamzam handling guidance",
    label: "Zamzam handling guidance",
    icon: "water",
    group: "Umrah kit",
  },
  { value: "Personal expenses", label: "Personal expenses", icon: "wallet", group: "Common exclusions" },
  {
    value: "Optional excursions",
    label: "Optional excursions",
    icon: "custom",
    group: "Common exclusions",
  },
  {
    value: "Travel insurance unless stated",
    label: "Travel insurance unless stated",
    icon: "shield",
    group: "Common exclusions",
  },
  { value: "Extra baggage", label: "Extra baggage", icon: "baggage", group: "Common exclusions" },
  { value: "Room upgrade", label: "Room upgrade", icon: "bed", group: "Common exclusions" },
  { value: "Laundry", label: "Laundry", icon: "laundry", group: "Common exclusions" },
];

const ICON_CHOICES: { value: IconName; label: string }[] = [
  { value: "custom", label: "General" },
  { value: "plane", label: "Flight" },
  { value: "visa", label: "Visa" },
  { value: "hotel", label: "Hotel" },
  { value: "mosque", label: "Ziyarat" },
  { value: "meal", label: "Meals" },
  { value: "bus", label: "Transport" },
  { value: "guide", label: "Guidance" },
  { value: "bag", label: "Kit" },
  { value: "book", label: "Guide book" },
  { value: "water", label: "Zamzam" },
  { value: "shield", label: "Insurance" },
  { value: "baggage", label: "Baggage" },
  { value: "bed", label: "Room" },
  { value: "laundry", label: "Laundry" },
];

function PackageOptionIcon({ name }: { name: IconName }) {
  const common = {
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
  };

  const paths: Record<IconName, React.ReactNode> = {
    plane: <path {...common} d="m3 13 7-2 4-7 2 1-2 6 6 2v2l-6 1-2 5-2-1 1-5-6 1-2-3Z" />,
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
    guide: (
      <>
        <circle {...common} cx="12" cy="7" r="3" />
        <path {...common} d="M6 21v-3a6 6 0 0 1 12 0v3M9 13l3 3 3-3" />
      </>
    ),
    bag: <path {...common} d="M7 8h10l2 12H5zM9 8c0-2 1-4 3-4s3 2 3 4" />,
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
    wallet: <path {...common} d="M4 6h15v14H4zM4 9h17v7h-5a2 2 0 0 1 0-4h5" />,
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

function knownIcon(item: string, customIcons: Record<string, IconName>) {
  return customIcons[item] ?? OPTIONS.find((option) => option.value === item)?.icon ?? "custom";
}

export default function PackageInclusionsEditor({
  inclusions,
  exclusions,
  onToggleInclusion,
  onToggleExclusion,
}: PackageInclusionsEditorProps) {
  const [customIcons, setCustomIcons] = useState<Record<string, IconName>>({});
  const [customLabel, setCustomLabel] = useState("");
  const [customIcon, setCustomIcon] = useState<IconName>("custom");
  const [customDestination, setCustomDestination] = useState<Destination>("included");
  const [adding, setAdding] = useState(false);
  const [dragOver, setDragOver] = useState<Destination | null>(null);
  const [status, setStatus] = useState("");

  const selectedValues = useMemo(
    () => new Set([...inclusions, ...exclusions].map((item) => item.toLocaleLowerCase())),
    [inclusions, exclusions],
  );
  const suggestions = useMemo(
    () => OPTIONS.filter((option) => !selectedValues.has(option.value.toLocaleLowerCase())),
    [selectedValues],
  );
  const suggestionGroups = useMemo(
    () => [...new Set(suggestions.map((option) => option.group))],
    [suggestions],
  );

  const moveItem = (item: string, destination: Destination) => {
    const isIncluded = inclusions.includes(item);
    const isExcluded = exclusions.includes(item);

    if (destination === "included") {
      if (isExcluded) onToggleExclusion(item);
      if (!isIncluded) onToggleInclusion(item);
      setStatus(`${item} moved to Included.`);
      return;
    }

    if (isIncluded) onToggleInclusion(item);
    if (!isExcluded) onToggleExclusion(item);
    setStatus(`${item} moved to Not included.`);
  };

  const startDrag = (event: DragEvent<HTMLElement>, item: string) => {
    event.dataTransfer.setData("text/plain", item);
    event.dataTransfer.effectAllowed = "move";
  };

  const dropItem = (event: DragEvent<HTMLElement>, destination: Destination) => {
    event.preventDefault();
    const item = event.dataTransfer.getData("text/plain");
    setDragOver(null);
    if (item) moveItem(item, destination);
  };

  const addCustomItem = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const value = customLabel.trim();
    if (!value) return;

    const normalized = value.toLocaleLowerCase();
    if (selectedValues.has(normalized) || OPTIONS.some((option) => option.value.toLocaleLowerCase() === normalized)) {
      setStatus(`${value} already exists in this package or suggestions.`);
      return;
    }

    setCustomIcons((current) => ({ ...current, [value]: customIcon }));
    moveItem(value, customDestination);
    setStatus(`${value} added to ${customDestination === "included" ? "Included" : "Not included"}.`);
    setCustomLabel("");
    setCustomIcon("custom");
    setAdding(false);
  };

  const renderBoard = (destination: Destination, items: string[]) => {
    const oppositeLabel = destination === "included" ? "Not included" : "Included";
    const oppositeDestination: Destination = destination === "included" ? "excluded" : "included";
    const title = destination === "included" ? "Included" : "Not included";

    return (
      <section
        className="package-drop-zone"
        data-destination={destination}
        data-drag-over={dragOver === destination ? "true" : "false"}
        onDragEnter={() => setDragOver(destination)}
        onDragLeave={(event) => {
          if (!event.currentTarget.contains(event.relatedTarget as Node | null)) setDragOver(null);
        }}
        onDragOver={(event) => {
          event.preventDefault();
          event.dataTransfer.dropEffect = "move";
        }}
        onDrop={(event) => dropItem(event, destination)}
        key={destination}
      >
        <header className="package-board-heading">
          <div>
            <h3>{title}</h3>
            <p>
              {destination === "included"
                ? "Services the operator confirms are part of this package."
                : "Services the operator explicitly excludes from this package."}
            </p>
          </div>
          <span>{items.length}</span>
        </header>

        <div className="package-board-list" data-testid={`${destination}-board`}>
          {items.length === 0 ? (
            <p className="package-board-empty">Drop an item here or use an Include / Exclude action below.</p>
          ) : (
            items.map((item) => (
              <article
                className="package-selected-item"
                draggable
                onDragStart={(event) => startDrag(event, item)}
                key={item}
                data-item={item}
              >
                <span className="package-option-icon">
                  <PackageOptionIcon name={knownIcon(item, customIcons)} />
                </span>
                <div className="package-selected-copy">
                  <strong>{item}</strong>
                  <small>Drag to the other list or use the move action.</small>
                </div>
                <button
                  className="package-item-move"
                  type="button"
                  onClick={() => moveItem(item, oppositeDestination)}
                >
                  Move to {oppositeLabel}
                </button>
              </article>
            ))
          )}
        </div>
      </section>
    );
  };

  return (
    <div className="package-options-editor">
      <div className="package-board-grid">
        {renderBoard("included", inclusions)}
        {renderBoard("excluded", exclusions)}
      </div>

      <p className="package-options-status" aria-live="polite" role="status">
        {status}
      </p>

      <section className="package-suggestions" aria-labelledby="package-suggestions-title">
        <div className="package-option-group-heading">
          <div>
            <h3 id="package-suggestions-title">Suggested items</h3>
            <p>
              Common Umrah-package items are suggestions only. Confirm each item for this package before adding it.
            </p>
          </div>
          <button className="package-option-add-inline" type="button" onClick={() => setAdding((current) => !current)}>
            Add custom item
          </button>
        </div>

        {suggestionGroups.map((group) => (
          <div className="package-suggestion-group" key={group}>
            <h4>{group}</h4>
            <div className="package-option-grid">
              {suggestions
                .filter((option) => option.group === group)
                .map((option) => (
                  <article className="package-option-tile package-suggestion-tile" key={option.value}>
                    <span className="package-option-icon">
                      <PackageOptionIcon name={option.icon} />
                    </span>
                    <strong>{option.label}</strong>
                    <div className="package-suggestion-actions">
                      <button type="button" onClick={() => moveItem(option.value, "included")}>
                        Include
                      </button>
                      <button type="button" onClick={() => moveItem(option.value, "excluded")}>
                        Exclude
                      </button>
                    </div>
                  </article>
                ))}
            </div>
          </div>
        ))}

        {adding ? (
          <form className="package-option-custom package-custom-composer" onSubmit={addCustomItem}>
            <label className="package-custom-label">
              <span>Custom item</span>
              <input
                autoFocus
                value={customLabel}
                placeholder="e.g. Wheelchair assistance"
                onChange={(event) => setCustomLabel(event.target.value)}
              />
            </label>

            <fieldset className="package-icon-picker">
              <legend>Choose an icon</legend>
              <div>
                {ICON_CHOICES.map((choice) => (
                  <label key={choice.value}>
                    <input
                      type="radio"
                      name="custom-package-icon"
                      value={choice.value}
                      checked={customIcon === choice.value}
                      onChange={() => setCustomIcon(choice.value)}
                    />
                    <span className="package-option-icon">
                      <PackageOptionIcon name={choice.value} />
                    </span>
                    <small>{choice.label}</small>
                  </label>
                ))}
              </div>
            </fieldset>

            <fieldset className="package-custom-destination">
              <legend>Add to</legend>
              <label>
                <input
                  type="radio"
                  name="custom-package-destination"
                  checked={customDestination === "included"}
                  onChange={() => setCustomDestination("included")}
                />
                Included
              </label>
              <label>
                <input
                  type="radio"
                  name="custom-package-destination"
                  checked={customDestination === "excluded"}
                  onChange={() => setCustomDestination("excluded")}
                />
                Not included
              </label>
            </fieldset>

            <div className="package-custom-actions">
              <button
                className="secondary-button"
                type="button"
                onClick={() => {
                  setAdding(false);
                  setCustomLabel("");
                  setCustomIcon("custom");
                }}
              >
                Cancel
              </button>
              <button className="primary-button" type="submit" disabled={!customLabel.trim()}>
                Add item
              </button>
            </div>
          </form>
        ) : null}
      </section>
    </div>
  );
}

export const DEFAULT_PACKAGE_INCLUSIONS = [...STANDARD_PACKAGE_INCLUSIONS];
export const DEFAULT_PACKAGE_EXCLUSIONS = [...STANDARD_PACKAGE_EXCLUSIONS];
