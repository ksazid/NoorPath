import Link from "next/link";
import type { InputHTMLAttributes } from "react";
import { NoorPathIcon, type NoorPathIconName } from "./NoorPathIcon";

export function TextField({
  description,
  error,
  id,
  label,
  optional = false,
  ...inputProps
}: Omit<InputHTMLAttributes<HTMLInputElement>, "id"> & {
  description?: string;
  error?: string;
  id: string;
  label: string;
  optional?: boolean;
}) {
  const descriptionId = description ? `${id}-description` : undefined;
  const errorId = error ? `${id}-error` : undefined;
  const describedBy = [descriptionId, errorId].filter(Boolean).join(" ") || undefined;

  return (
    <div className={`np-field${error ? " np-field--error" : ""}`}>
      <label htmlFor={id}>
        <span>{label}</span>
        {optional ? <small>Optional</small> : null}
      </label>
      <input
        aria-describedby={describedBy}
        aria-invalid={error ? true : undefined}
        id={id}
        {...inputProps}
      />
      {description ? (
        <p className="np-field__description" id={descriptionId}>
          {description}
        </p>
      ) : null}
      {error ? (
        <p className="np-field__error" id={errorId} role="alert">
          {error}
        </p>
      ) : null}
    </div>
  );
}

export function SupportAction({
  description,
  href,
  icon,
  title,
}: {
  description: string;
  href: string;
  icon: Extract<NoorPathIconName, "phone" | "support" | "whatsapp">;
  title: string;
}) {
  return (
    <Link className="np-support-action" href={href}>
      <span className="np-support-action__icon" aria-hidden="true">
        <NoorPathIcon name={icon} size={22} />
      </span>
      <span>
        <strong>{title}</strong>
        <small>{description}</small>
      </span>
      <NoorPathIcon name="arrow-right" size={18} />
    </Link>
  );
}

export function SkeletonBlock({
  height = "1rem",
  label = "Loading content",
  width = "100%",
}: {
  height?: string;
  label?: string;
  width?: string;
}) {
  return (
    <span
      aria-label={label}
      className="np-skeleton"
      role="status"
      style={{ height, width }}
    />
  );
}
