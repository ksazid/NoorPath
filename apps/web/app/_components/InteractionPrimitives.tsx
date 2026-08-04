import type { ButtonHTMLAttributes, ReactNode } from "react";
import { NoorPathIcon, type NoorPathIconName } from "./NoorPathIcon";

export function IconButton({
  className = "",
  icon,
  label,
  ...buttonProps
}: Omit<ButtonHTMLAttributes<HTMLButtonElement>, "aria-label" | "children"> & {
  icon: NoorPathIconName;
  label: string;
}) {
  return (
    <button
      aria-label={label}
      className={`np-icon-button ${className}`.trim()}
      title={label}
      {...buttonProps}
    >
      <NoorPathIcon name={icon} size={20} />
    </button>
  );
}

export function MetricCard({
  description,
  label,
  value,
}: {
  description?: string;
  label: string;
  value: ReactNode;
}) {
  return (
    <article className="np-metric-card">
      <span>{label}</span>
      <strong>{value}</strong>
      {description ? <p>{description}</p> : null}
    </article>
  );
}

export function StickyActionBar({
  action,
  summary,
  support,
}: {
  action: ReactNode;
  summary: ReactNode;
  support?: ReactNode;
}) {
  return (
    <aside className="np-sticky-action" aria-label="Current journey action">
      <div className="np-sticky-action__summary">{summary}</div>
      <div className="np-sticky-action__controls">
        {support}
        {action}
      </div>
    </aside>
  );
}
