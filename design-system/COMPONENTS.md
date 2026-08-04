# NoorPath Component Contracts

## Authority

These contracts implement the approved Landing and Package visual language without changing product behaviour. Existing approved components remain valid; new or changed shared components should use the semantic tokens exported by `@noorpath/design-tokens`.

Related governance:

- [`../PRODUCT_PRINCIPLES.md`](../PRODUCT_PRINCIPLES.md)
- [`../PR_UX_CHECKLIST.md`](../PR_UX_CHECKLIST.md)
- [`MASTER.md`](MASTER.md)

## Shared primitives

### `ActionButton`

Variants:

- `primary` — one dominant action per screen or decision area.
- `secondary` — supporting action with equal clarity but lower emphasis.
- `tertiary` — low-emphasis navigation or disclosure.
- `destructive` — irreversible or materially risky action only.

Rules:

- Minimum interactive height: 44 pixels.
- Pending state disables repeat submission and exposes `aria-busy`.
- Labels describe the outcome: `Reserve Your Seats`, not `Continue`.
- Press feedback uses a restrained transform and is disabled for reduced motion.

### `IconButton`

Used only when the icon meaning is conventional and the control has a required accessible label. The component always exposes that label through `aria-label` and `title` while using the governed NoorPath icon registry.

### `StatusBadge`

Approved tones: success, warning, danger, info and neutral.

Every status includes an icon and text. Colour never carries meaning alone. Product-specific wording remains owned by the relevant slice or domain policy.

### `FeatureTile`

Used for stable package inclusions and other concise feature summaries. Operators control approved content values, not icon, colour, spacing or layout.

### `MetricCard`

Used for concise customer or staff facts such as total price, amount due now, remaining balance, capacity or queue count. Numeric values use tabular figures. The label and basis must remain explicit.

### `OccupancyAvatarGroup` and `OccupancyCard`

Approved occupancy counts are two, three and four people for the current product boundary. Customer labels use:

- Double Sharing — 2 pilgrims sharing one room
- Triple Sharing — 3 pilgrims sharing one room
- Quad Sharing — 4 pilgrims sharing one room

The avatar group has one accessible label and hides decorative repeated icons from assistive technology.

### `StatePanel`

Approved states:

- loading
- empty
- error
- offline
- unavailable
- success

The message must explain what happened, whether data is safe, and the next available action. Retry is shown only where it can actually retry the failed operation.

### `TimelineItem`

Approved presentation states:

- completed
- current
- upcoming
- action required

Domain status remains authoritative. The component only controls presentation.

### `PackageSection`

Uses a typed section identifier from `PACKAGE_DETAIL_SECTION_ORDER`. It does not allow operator-controlled section ordering.

### `TextField` and `SelectField`

The shared fields own their visible label, helper text, field-adjacent error, `aria-describedby` and `aria-invalid` wiring. They keep a minimum 48-pixel control height and inherit the shared focus token. Product slices still own validation rules, options and submitted values.

### `CheckboxField`, `RadioCard` and `ToggleField`

These use native inputs and preserve keyboard and assistive-technology behaviour. Labels provide the full interactive target. A product slice chooses the appropriate control:

- checkbox — independent choice;
- radio card — one choice from a mutually exclusive group;
- switch — immediate on/off preference, never a form submission substitute.

### `SupportAction`

Used for approved WhatsApp Support, Request a Callback and general assisted-support entries. The icon mapping is fixed; the action includes a visible title and description and meets the minimum target size.

### `SkeletonBlock`

Represents a known loading shape without inventing content. Skeleton motion is removed under `prefers-reduced-motion`, while the loading state remains exposed to assistive technology.

### `StickyActionBar`

Provides a reusable safe-area-aware customer action bar. It must show the current commercial summary and one primary action. Support may be present as a subordinate control. Product slices decide whether sticky behaviour is appropriate and must reserve content space where required.

## Shell primitives

### `CustomerShell`

Modes:

- `public`
- `authenticated`
- `transactional`

Footer modes:

- `full`
- `compact`
- `none`

The shell provides the governed customer header, native responsive menu, skip link, content landmark and approved footer contracts. A product slice chooses the correct mode; the shell does not implement authentication or reservation behaviour.

### `StaffShell`

The staff shell provides grouped, caller-supplied navigation plus search and header-action slots. The caller supplies only items already permitted for the authorized role. Hidden navigation remains presentation-only; server authorization is still mandatory.

## Form adoption

Existing form controls remain in place until migrated by the slice that owns them. New or changed controls should use these primitives or match their accessible contracts. All controls require visible labels where the context does not already provide an unambiguous accessible name, field-adjacent validation, minimum 16-pixel mobile text and the shared focus token.

## Adoption rule

Do not perform broad mechanical rewrites. A component is adopted when a product slice changes the relevant surface or when an isolated migration is explicitly approved and visually verified.
