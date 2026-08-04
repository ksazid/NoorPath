# NoorPath Motion Standard

## Principle

Motion exists to explain state, confirm input or preserve spatial continuity. It is not decorative and must never delay a customer or staff task.

## Durations

- Fast: `120ms` — press, hover and compact state feedback.
- Standard: `200ms` — dropdown, disclosure and small content replacement.
- Slow: `320ms` — occasional drawer or modal transition only.

Use the shared easing tokens. Entrances should feel immediate and use an ease-out curve. Repeated keyboard-driven actions should not animate.

## Allowed uses

- Button press feedback.
- Hover or focus colour change.
- Disclosure open and close where the relationship would otherwise be unclear.
- Drawer or modal entry and exit.
- Loading indicator when an operation exceeds immediate feedback.
- Crossfade for content replaced within the same container.

## Prohibited uses

- Decorative page-entry sequences.
- Animation that blocks interaction.
- `transition: all`.
- Animating width, height, top or left when transform or opacity can express the same change.
- Bouncy motion in booking, payment, document, visa, refund or operational workflows.
- Auto-playing parallax or continuous ambient motion.

## Press feedback

Pressable shared actions may use a subtle `scale(0.97)` response. The effect is removed when reduced motion is requested.

## Loading

- Show immediate button feedback for asynchronous actions.
- Use a skeleton for content expected to take longer than approximately one second and where the eventual shape is known.
- Do not show a spinner without explanatory text for page-level waits.
- Pending controls must prevent duplicate submission.

## Reduced motion

The design-token package includes a global `prefers-reduced-motion` rule. Components must preserve equivalent status and completion feedback when animation is removed. A static icon, text label or state change must remain sufficient to understand the result.
