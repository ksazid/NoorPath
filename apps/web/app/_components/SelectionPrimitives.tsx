import type {
  InputHTMLAttributes,
  ReactNode,
  SelectHTMLAttributes,
} from "react";

function describedBy(id: string, description?: string, error?: string) {
  return (
    [description ? `${id}-description` : null, error ? `${id}-error` : null]
      .filter(Boolean)
      .join(" ") || undefined
  );
}

export function SelectField({
  description,
  error,
  id,
  label,
  options,
  optional = false,
  ...selectProps
}: Omit<SelectHTMLAttributes<HTMLSelectElement>, "id" | "children"> & {
  description?: string;
  error?: string;
  id: string;
  label: string;
  options: { label: string; value: string }[];
  optional?: boolean;
}) {
  return (
    <div className={`np-field${error ? " np-field--error" : ""}`}>
      <label htmlFor={id}>
        <span>{label}</span>
        {optional ? <small>Optional</small> : null}
      </label>
      <select
        aria-describedby={describedBy(id, description, error)}
        aria-invalid={error ? true : undefined}
        id={id}
        {...selectProps}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {description ? (
        <p className="np-field__description" id={`${id}-description`}>
          {description}
        </p>
      ) : null}
      {error ? (
        <p className="np-field__error" id={`${id}-error`} role="alert">
          {error}
        </p>
      ) : null}
    </div>
  );
}

export function CheckboxField({
  description,
  label,
  ...inputProps
}: Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  description?: string;
  label: string;
}) {
  return (
    <label className="np-check-field">
      <input type="checkbox" {...inputProps} />
      <span>
        <strong>{label}</strong>
        {description ? <small>{description}</small> : null}
      </span>
    </label>
  );
}

export function RadioCard({
  children,
  ...inputProps
}: Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  children: ReactNode;
}) {
  return (
    <label className="np-radio-card">
      <input type="radio" {...inputProps} />
      <span>{children}</span>
    </label>
  );
}

export function ToggleField({
  description,
  label,
  ...inputProps
}: Omit<InputHTMLAttributes<HTMLInputElement>, "type" | "role"> & {
  description?: string;
  label: string;
}) {
  return (
    <label className="np-toggle-field">
      <span>
        <strong>{label}</strong>
        {description ? <small>{description}</small> : null}
      </span>
      <input role="switch" type="checkbox" {...inputProps} />
    </label>
  );
}
