# VS-05 Customer Discovery — Design Notes

## Authority

1. Approved NoorPath Landing reference
2. Approved NoorPath Package reference
3. `design-system/MASTER.md`
4. Existing public UI tokens/components
5. UI UX Pro Max for accessibility, responsive and interaction quality only

VS-05 does not introduce a new visual direction.

## Existing composition retained

The Landing page package section remains structurally unchanged:

- section heading and supporting copy;
- three-column desktop package-card composition with existing responsive reflow;
- image area and factual badge;
- package title;
- Makkah/Madinah night summary;
- compact inclusion highlights;
- two-column departure/availability row;
- commercial summary and `View package` action;
- existing service strip and footer behaviour.

## Authoritative content binding

Placeholder commercial claims are replaced with facts from the VS-05 public discovery contract:

- badge: `Verified operator` because the public query independently re-checks current operator eligibility;
- title: authoritative PackageVersion name;
- stay summary: authoritative Makkah/Madinah night counts;
- inclusion labels: up to three authoritative PackageVersion inclusion items;
- departure: authoritative departure date;
- availability: `Available` only when a matching saleable occupancy exists;
- commercial summary: formatted `From <currency amount>` based on the lowest currently available published occupancy price;
- supporting commercial line: operator display name;
- CTA: existing `View package` route using `departureId`.

No limited-stock, discount, popularity, hotel-star, meal, visa or comfort-tier claim is inferred unless the authoritative source explicitly contains that fact.

## States

### Loading

Keep the package section in place, mark the state `aria-busy`, and use restrained card-shaped placeholders so content below does not jump.

### Empty

Within the existing package section, show a calm message: no published journeys are currently available. Do not fall back to fabricated demo packages.

### Error / offline

Within the existing package section, state that published journeys could not be loaded and provide one clear retry action. Keep the rest of the Landing page usable.

### Populated

Use the existing package-card layout. Preserve the current visual hierarchy and only replace placeholder data with authoritative content.

## Accessibility / responsive review

- full keyboard access to retry and package CTAs;
- visible focus states inherited from the public UI system;
- meaningful image alt text remains generic to the sacred-place artwork and does not imply the image is the exact package hotel;
- price/availability cannot be conveyed by colour alone;
- loading status announced politely;
- no horizontal scroll at 390 px or 360 px;
- price, availability and `View package` remain visible on mobile;
- existing reduced-motion rules remain in force;
- card heights may vary with truthful content; do not truncate factual text purely to preserve a decorative grid.

## Product-owner visual gate

Rendered desktop and mobile evidence must be reviewed before VS-05 is merged. Any change to the approved Landing composition beyond the state/content substitutions described here returns to Product Owner review before implementation proceeds.
