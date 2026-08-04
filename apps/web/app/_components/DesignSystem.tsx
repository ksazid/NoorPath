import type { ButtonHTMLAttributes, HTMLAttributes, ReactNode } from "react";
import { NoorPathIcon, type NoorPathIconName } from "./NoorPathIcon";

export const PACKAGE_DETAIL_SECTION_ORDER = [
  "hero-gallery",
  "verified-operator",
  "package-summary",
  "hotels",
  "room-occupancy-and-pricing",
  "journey-payment-summary",
  "reserve-action",
  "itinerary",
  "package-inclusions",
  "travel-kit",
  "umrah-kit",
  "journey-payment-schedule",
  "service-confirmation",
  "cancellation-policy",
  "help-and-support",
  "sticky-reservation-action",
] as const;

export type PackageDetailSectionId =
  (typeof PACKAGE_DETAIL_SECTION_ORDER)[number];

export type ActionVariant =
  "primary" | "secondary" | "tertiary" | "destructive";

export function ActionButton({
  children,
  className = "",
  variant = "primary",
  pending = false,
  disabled,
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ActionVariant;
  pending?: boolean;
}) {
  return (
    <button
      aria-busy={pending || undefined}
      className={`np-action np-action--${variant} ${className}`.trim()}
      disabled={disabled || pending}
      {...props}
    >
      {pending ? <span className="np-spinner" aria-hidden="true" /> : null}
      <span>{children}</span>
    </button>
  );
}

export function SurfaceCard({
  children,
  className = "",
  ...props
}: HTMLAttributes<HTMLElement>) {
  return (
    <section className={`np-card ${className}`.trim()} {...props}>
      {children}
    </section>
  );
}

export type StatusTone = "success" | "warning" | "danger" | "info" | "neutral";

const statusIcons: Record<StatusTone, NoorPathIconName> = {
  success: "check",
  warning: "warning",
  danger: "error",
  info: "info",
  neutral: "info",
};

export function StatusBadge({
  children,
  tone = "neutral",
}: {
  children: ReactNode;
  tone?: StatusTone;
}) {
  return (
    <span className={`np-status np-status--${tone}`}>
      <NoorPathIcon name={statusIcons[tone]} size={14} />
      <span>{children}</span>
    </span>
  );
}

export function FeatureTile({
  description,
  icon,
  title,
}: {
  description?: string;
  icon: NoorPathIconName;
  title: string;
}) {
  return (
    <article className="np-feature">
      <span className="np-feature__icon" aria-hidden="true">
        <NoorPathIcon name={icon} size={22} />
      </span>
      <div>
        <h3>{title}</h3>
        {description ? <p>{description}</p> : null}
      </div>
    </article>
  );
}

export function OccupancyAvatarGroup({
  count,
  label,
}: {
  count: 2 | 3 | 4;
  label?: string;
}) {
  const accessibleLabel = label ?? `${count} pilgrims sharing one room`;

  return (
    <span className="np-occupancy" aria-label={accessibleLabel} role="img">
      {Array.from({ length: count }, (_, index) => (
        <span className="np-occupancy__avatar" key={index} aria-hidden="true">
          <NoorPathIcon name="users" size={15} />
        </span>
      ))}
    </span>
  );
}

export function OccupancyCard({
  count,
  description,
  selected = false,
  title,
}: {
  count: 2 | 3 | 4;
  description: string;
  selected?: boolean;
  title: string;
}) {
  return (
    <article className={`np-occupancy-card${selected ? " is-selected" : ""}`}>
      <div className="np-occupancy-card__heading">
        <OccupancyAvatarGroup
          count={count}
          label={`${title}: ${description}`}
        />
        {selected ? (
          <StatusBadge tone="success">Recommended</StatusBadge>
        ) : null}
      </div>
      <h3>{title}</h3>
      <p>{description}</p>
    </article>
  );
}

export type StateKind =
  "loading" | "empty" | "error" | "offline" | "unavailable" | "success";

const stateIcons: Record<StateKind, NoorPathIconName> = {
  loading: "info",
  empty: "search",
  error: "error",
  offline: "warning",
  unavailable: "info",
  success: "check",
};

export function StatePanel({
  action,
  description,
  kind,
  title,
}: {
  action?: ReactNode;
  description: string;
  kind: StateKind;
  title: string;
}) {
  return (
    <section
      aria-busy={kind === "loading" || undefined}
      aria-live="polite"
      className={`np-state np-state--${kind}`}
    >
      <span className="np-state__icon" aria-hidden="true">
        {kind === "loading" ? (
          <span className="np-spinner" />
        ) : (
          <NoorPathIcon name={stateIcons[kind]} size={26} />
        )}
      </span>
      <h2>{title}</h2>
      <p>{description}</p>
      {action ? <div className="np-state__action">{action}</div> : null}
    </section>
  );
}

export function TimelineItem({
  children,
  label,
  status,
}: {
  children?: ReactNode;
  label: string;
  status: "completed" | "current" | "upcoming" | "action-required";
}) {
  const icon: NoorPathIconName =
    status === "completed"
      ? "check"
      : status === "action-required"
        ? "warning"
        : "calendar";

  return (
    <li className={`np-timeline__item np-timeline__item--${status}`}>
      <span className="np-timeline__marker" aria-hidden="true">
        <NoorPathIcon name={icon} size={16} />
      </span>
      <div>
        <strong>{label}</strong>
        {children ? (
          <div className="np-timeline__detail">{children}</div>
        ) : null}
      </div>
    </li>
  );
}

export function PackageSection({
  children,
  description,
  id,
  title,
}: {
  children: ReactNode;
  description?: string;
  id: PackageDetailSectionId;
  title: string;
}) {
  return (
    <section className="np-package-section" data-package-section={id}>
      <header className="np-package-section__header">
        <h2>{title}</h2>
        {description ? <p>{description}</p> : null}
      </header>
      {children}
    </section>
  );
}
