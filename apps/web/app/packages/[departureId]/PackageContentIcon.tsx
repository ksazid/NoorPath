type PackageContentIconName =
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

export function PackageContentIcon({ name }: { name: PackageContentIconName }) {
  const common = {
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.8,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
  };

  const paths: Record<PackageContentIconName, React.ReactNode> = {
    plane: (
      <path
        {...common}
        d="m3 13 7-2 4-7 2 1-2 6 6 2v2l-6 1-2 5-2-1 1-5-6 1-2-3Z"
      />
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
        <path
          {...common}
          d="M4 21h16M6 21v-8h12v8M8 13c0-4 8-4 8 0M12 5v3M10 5h4"
        />
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
    wallet: (
      <path {...common} d="M4 6h15v14H4zM4 9h17v7h-5a2 2 0 0 1 0-4h5" />
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

export type { PackageContentIconName };
