# Public UI reference-alignment QA

## Scope

- Landing page: `apps/web/app/page.tsx`
- Package page: `apps/web/app/packages/[departureId]/page.tsx`
- Shared public UI and responsive styles
- Reference images:
  - `design-references/noorpath-landing-reference.png`
  - `design-references/noorpath-package-reference.png`

The reference images are the visual source of truth. NoorPath's design system,
UI UX Pro Max, Impeccable, and Emil Design Engineering were used only to refine
responsive behavior, accessibility, interaction, and polish.

## Evidence

- Desktop landing implementation:
  `docs/design/evidence/public-pages/landing-implementation.svg`
- Desktop package implementation:
  `docs/design/evidence/public-pages/package-implementation.svg`
- Mobile landing and package implementation:
  `docs/design/evidence/public-pages/mobile-implementation.svg`

Captured states and viewports:

- Landing default state at 1348 px width; full-page capture.
- Package default state at 1348 × 926.
- Mobile landing and package at 390 × 844 within the mobile QA canvas.

## Findings and fixes

1. **P1 — original public pages diverged from the approved logo, header,
   information hierarchy, and page composition.**
   Rebuilt both pages around the approved structure and restored the intended
   warm-white, green, and gold visual language.
2. **P2 — initial package restoration was too tall and did not align closely
   enough with the approved desktop proportions.**
   Reduced the gallery height, tightened the summary cards, and balanced the
   three-column layout.
3. **P2 — mobile accommodation content stacked differently from the approved
   package layout.**
   Restored a compact two-column accommodation summary and the approved
   menu/logo/action header pattern.
4. **P2 — several mobile interactive targets were below 44 px.**
   Increased all navigation and action targets to at least 44 px.

## Verification

- TypeScript: passed (`tsc --noEmit`)
- Production build: passed (`next build`)
- Primary actions: package search, package navigation, and mobile menu passed
- Responsive overflow at 390 px: none
- Keyboard focus: visible 3 px outline; skip link reachable
- Impeccable detector: no violations
- Application console: no application errors

Commercial data that the current product does not expose—prices, seat counts,
payment schedules, and booking claims—was intentionally not invented. The
corresponding reference areas use truthful journey status and support content.

## Final result

passed
