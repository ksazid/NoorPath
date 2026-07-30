# NoorPath public UI fidelity QA

## Source visual truth

- Landing: `design-references/noorpath-landing-reference.png`
- Package:
  `design-references/noorpath-package-reference.png`
- Local review copies:
  `/workspace/scratch/6ad22844876b/upload/landing(1).jpeg` and
  `/workspace/scratch/6ad22844876b/upload/package(1).jpeg`

The approved source images are 1536 × 1024 composite boards. Their desktop
surfaces were normalized to 988 × 1100 (Landing) and 967 × 900 (Package) for
qualitative side-by-side review. They are not raw browser captures, so exact
CSS viewport density cannot be inferred from the source.

## Browser-rendered implementation evidence

- `docs/design/evidence/public-pages/landing-implementation.jpg` — 1348 × 926
  capture, desktop browser viewport
  1363 × 936, device scale factor 1.
- `docs/design/evidence/public-pages/landing-bottom.jpg` — focused Landing
  cards/service-strip capture at the same viewport.
- `docs/design/evidence/public-pages/package-implementation.jpg` — 1348 × 926
  capture, desktop browser viewport 1363 × 936, device scale factor 1.
- `docs/design/evidence/public-pages/mobile-implementation.jpg` — two
  browser-rendered 390 × 844 iframes in a 1363 × 936 QA viewport.
- `docs/design/evidence/public-pages/landing-comparison.jpg` and
  `docs/design/evidence/public-pages/package-comparison.jpg` — source and
  implementation placed together for the final comparison.

State: default public Landing and first published Package route.

## Comparison history

### Pass 1 — blocked

- P1: Landing headline wrapped to three lines rather than the approved two.
- P1: Package used a journey-summary replacement instead of payment,
  instalment, kit, status, cancellation, and booking composition.
- P1: Generic repeated checkmarks replaced the approved pictogram system.
- P2: Landing used a full-width hero and generic service strip rather than the
  approved framed composition and desert finish.
- P2: Package operator name and gallery crop changed the approved hierarchy.

Fixes: restored the framed Landing composition, exact section order, separate
approved wordmarks, real Phosphor source icons, payment/instalment structure,
itinerary pictograms, kit grids, status/cancellation panels, sticky booking
bar, two-line hero, corrected gallery crop, and real desert image asset.

### Pass 2 — passed

- Fonts/typography: editorial serif and compact sans hierarchy now match the
  source direction; the Landing headline holds its approved two lines.
- Spacing/layout: framed Landing proportions and three-column Package
  composition match; mobile preserves content order without horizontal
  overflow.
- Colours/tokens: ivory, white, black, Madinah green, and restrained gold map
  consistently to the approved palette.
- Image quality: real raster/photo assets are used; no CSS/HTML illustrations
  or placeholder art remain.
- Icons: visible functional icons come from one Phosphor source family;
  approved page-specific logos are retained.
- Copy/content: exact package names may differ from the static approved board
  without changing the visual contract. Commercial values remain absent until
  a later approved slice provides authoritative data; the same approved visual
  slots instead show clear confirmation states.

Residual P3: the approved composite does not provide isolated licensed source
photos, so implementation imagery matches subject, palette, crop direction,
and density rather than being the exact original photography.

## Interaction and accessibility evidence

- TypeScript and production build passed.
- Search selects remain labelled and the search action reaches `#packages`.
- Package links resolve to the published package route.
- Desktop and both 390 px frames have no horizontal overflow.
- Mobile navigation, selects, search, callback, booking, and package controls
  meet the 44 px target after the final correction.
- Heading order is one H1 followed by H2/H3 content headings.
- Meaningful photography has descriptive alt text; decorative icons are hidden
  from assistive technology.
- Focus treatment is visible.
- Hover motion is gated to fine pointers; press feedback is 160 ms; reduced
  motion removes effective transition duration.
- No application console errors or warnings were observed. Browser-extension
  metadata errors were excluded as non-application noise.

## Final result

final result: passed
