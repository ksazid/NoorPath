import type { ReactNode, SVGProps } from "react";

export type NoorPathIconName =
  | "arrow-right"
  | "bag"
  | "bus"
  | "calendar"
  | "check"
  | "error"
  | "hotel"
  | "info"
  | "meal"
  | "passport"
  | "payment"
  | "phone"
  | "plane"
  | "search"
  | "shield"
  | "support"
  | "users"
  | "verified"
  | "warning"
  | "whatsapp";

const paths: Record<NoorPathIconName, ReactNode> = {
  "arrow-right": (
    <>
      <path d="M5 12h14" />
      <path d="m13 6 6 6-6 6" />
    </>
  ),
  bag: (
    <>
      <path d="M6 7h12l1 13H5L6 7Z" />
      <path d="M9 7V5a3 3 0 0 1 6 0v2" />
    </>
  ),
  bus: (
    <>
      <rect x="4" y="3" width="16" height="16" rx="3" />
      <path d="M4 11h16M8 19v2m8-2v2" />
      <circle cx="8" cy="15" r="1" />
      <circle cx="16" cy="15" r="1" />
    </>
  ),
  calendar: (
    <>
      <rect x="3" y="5" width="18" height="16" rx="2" />
      <path d="M16 3v4M8 3v4M3 10h18" />
    </>
  ),
  check: <path d="m5 12 4 4L19 6" />,
  error: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="m9 9 6 6m0-6-6 6" />
    </>
  ),
  hotel: (
    <>
      <path d="M4 21V5h12v16M16 9h4v12M8 9h1m3 0h1m-5 4h1m3 0h1m-5 4h1m3 0h1" />
      <path d="M2 21h20" />
    </>
  ),
  info: (
    <>
      <circle cx="12" cy="12" r="9" />
      <path d="M12 11v5" />
      <path d="M12 8h.01" />
    </>
  ),
  meal: (
    <>
      <path d="M7 3v8m-3-8v5a3 3 0 0 0 6 0V3M7 11v10" />
      <path d="M17 3v18M14 3v7a3 3 0 0 0 3 3" />
    </>
  ),
  passport: (
    <>
      <rect x="5" y="3" width="14" height="18" rx="2" />
      <circle cx="12" cy="10" r="3" />
      <path d="M9 10h6M12 7a5 5 0 0 1 0 6M8 16h8" />
    </>
  ),
  payment: (
    <>
      <rect x="3" y="5" width="18" height="14" rx="2" />
      <path d="M3 9h18M7 15h4" />
    </>
  ),
  phone: (
    <path d="M7 3H5a2 2 0 0 0-2 2c0 8.8 7.2 16 16 16a2 2 0 0 0 2-2v-2l-4-1-1 3a16 16 0 0 1-11-11l3-1-1-4Z" />
  ),
  plane: (
    <>
      <path d="M22 2 9 15" />
      <path d="m22 2-7 20-4-9-9-4 20-7Z" />
    </>
  ),
  search: (
    <>
      <circle cx="11" cy="11" r="7" />
      <path d="m20 20-4-4" />
    </>
  ),
  shield: (
    <>
      <path d="M12 3 5 6v5c0 5 3 8 7 10 4-2 7-5 7-10V6l-7-3Z" />
      <path d="m9 12 2 2 4-4" />
    </>
  ),
  support: (
    <>
      <path d="M4 13a8 8 0 0 1 16 0" />
      <path d="M4 13v4a2 2 0 0 0 2 2h2v-7H4Zm16 0v4a2 2 0 0 1-2 2h-2v-7h4Z" />
      <path d="M16 19c0 1-1 2-3 2" />
    </>
  ),
  users: (
    <>
      <circle cx="9" cy="8" r="3" />
      <path d="M3 20a6 6 0 0 1 12 0" />
      <path d="M16 4a3 3 0 0 1 0 6M18 13a5 5 0 0 1 3 5" />
    </>
  ),
  verified: (
    <>
      <path d="m12 2 2.2 2.1 3-.4.8 2.9 2.7 1.4-1.4 2.7.4 3-2.9.8-2.1 2.2-2.1-2.2-2.9-.8.4-3L3.3 8 6 6.6l.8-2.9 3 .4L12 2Z" />
      <path d="m9 10 2 2 4-4" />
    </>
  ),
  warning: (
    <>
      <path d="M12 3 2.5 20h19L12 3Z" />
      <path d="M12 9v4M12 17h.01" />
    </>
  ),
  whatsapp: (
    <>
      <path d="M20 11.5a8 8 0 0 1-11.9 7L3 20l1.5-4.9A8 8 0 1 1 20 11.5Z" />
      <path d="M8.5 8.5c.5 3 2 4.5 5 5" />
    </>
  ),
};

type NoorPathIconProps = Omit<SVGProps<SVGSVGElement>, "children"> & {
  name: NoorPathIconName;
  size?: number;
  title?: string;
};

export function NoorPathIcon({
  name,
  size = 20,
  title,
  ...props
}: NoorPathIconProps) {
  return (
    <svg
      aria-hidden={title ? undefined : true}
      fill="none"
      height={size}
      role={title ? "img" : undefined}
      stroke="currentColor"
      strokeLinecap="round"
      strokeLinejoin="round"
      strokeWidth="1.75"
      viewBox="0 0 24 24"
      width={size}
      {...props}
    >
      {title ? <title>{title}</title> : null}
      {paths[name]}
    </svg>
  );
}
