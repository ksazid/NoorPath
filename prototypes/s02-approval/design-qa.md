# NoorPath S02 Design QA

- Date: 2026-07-28
- Source visual truth:
  - `public/assets/reference-landing.png`
  - `public/assets/reference-package.png`
- Implementation:
  - Local Vite approval prototype at `/`
  - Combined live comparison at `public/qa-compare.html`
  - True mobile review frame at `public/mobile-review.html`
- Browser-rendered evidence: cloud-browser captures produced inline during this
  QA run. The browser surface does not expose a persistent filesystem path for
  its captured image.

## Viewports and normalization

- Source images: 1536 × 1024 px each.
- Outer cloud-browser viewport: 1363 × 936 CSS px.
- Desktop implementation frame: 1280 × 900 CSS px, displayed at 0.5 scale
  beside the desktop source solely for comparison.
- Mobile implementation frame: 390 × 844 CSS px at device scale factor 1.
- Mobile density normalization: the live app renders inside a real 390 px
  iframe viewport; it is not a scaled desktop capture.
- Comparison state: customer discovery results, light theme, signed-out public
  content, realistic pilot data.

## Full-view comparison evidence

The browser-rendered combined comparison shows the two approved source designs
and the live implementation together. The implementation preserves:

- the calm ivory canvas and bright photographic Haramain imagery;
- restrained Kiswah-gold accents and Madinah-green verification/actions;
- editorial serif display hierarchy with compact sans-serif interface copy;
- light borders, restrained radii, and minimal elevation;
- explicit operator verification, total starting price, dates, inclusions, and
  honest seat availability;
- desktop editorial spacing and a mobile composition that brings the first
  package image into the initial 390 × 844 viewport.

The package-detail and payment content visible in the source reference is
intentionally not recreated because it belongs to S03 and later slices. The
reference is used here for visual-system and trust-density fidelity only.

## Focused comparison evidence

- Desktop admin form: separately captured at 1363 × 936. Numbered sections,
  approved-operator disclosure, draft status, structured fields, and fixed
  publication actions remain clear without clipping.
- Publication modal: separately captured at 1363 × 936. The modal repeats the
  public package name, total price, and capacity before the irreversible
  prototype action.
- Mobile customer view: captured inside a true 390 × 844 viewport. Header,
  editorial hero, filters, state controls, and the first package image are
  visible without horizontal overflow.
- No additional crop was required because the combined comparison and the
  dedicated admin/modal/mobile captures make the important type, spacing,
  imagery, and controls readable.

## Required fidelity surfaces

- Fonts and typography: passed. Georgia/system serif gives the approved
  editorial character; interface text uses a compact system sans-serif with
  clear hierarchy and wrapping at desktop and mobile.
- Spacing and layout rhythm: passed. Desktop grids, admin form sections,
  customer cards, 390 px mobile stacking, borders, radii, and vertical rhythm
  are consistent with the references.
- Colors and visual tokens: passed. Ivory, gold, deep green, muted ink, semantic
  danger, and border colors are consistently mapped through CSS custom
  properties.
- Image quality and asset fidelity: passed. Generated Makkah and Madinah raster
  assets are sharp, correctly cropped, and visually coherent with the source
  art direction. Phosphor supplies the compact line-icon system; no placeholder
  art or handcrafted SVG substitutes are used.
- Copy and content: passed. Trust language is explicit, calm, and within S02
  scope. No urgency manipulation, booking, payment, child pricing, documents,
  or notification behavior is introduced.

## Interaction and accessibility checks

- Admin draft → review → explicit confirmation → published success → customer
  discovery passed.
- The newly published batch appears first with the approved operator, total
  starting price, dates, dynamic inclusions, and exact availability.
- Loading, empty, error, offline, retry, and results states passed.
- Form labels, semantic headings, dialog semantics, keyboard focus styles,
  reduced-motion behavior, and practical mobile tap targets are present.
- No application-origin console errors were observed. Repeated console entries
  came only from the cloud-browser extension and not from the prototype.
- `npm run build` and `npm run test:sites` passed.

## Comparison history

### Earlier blocked finding

- P2 mobile evidence gap: the previous QA run could not render the app at the
  required small viewport, so mobile layout fidelity remained unverified.

### Fixes made

- Added a true 390 × 844 mobile review frame.
- Compacted the mobile hero, filter controls, section spacing, and heading scale
  so discovery content enters the initial viewport.
- Added a combined source-and-live comparison surface for direct visual
  judgment.

### Post-fix evidence

- The 390 px frame renders without horizontal overflow.
- The first package image is visible in the initial mobile viewport.
- The combined capture shows consistent type, palette, imagery, spacing, and
  trust treatment across both source references and the implementation.

## Findings

- P0: none.
- P1: none.
- P2: none.
- P3: the prototype uses a system serif rather than the exact source typeface;
  this avoids an external runtime font dependency while preserving the
  approved editorial character.

## Final result

final result: passed
