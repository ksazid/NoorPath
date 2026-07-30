# NoorPath Public UI Fidelity Workflow

This workflow applies to every existing and future customer-facing UI.

## Authority

1. Approved PRD and active slice requirements define product behaviour.
2. `design-references/noorpath-landing-reference.png` and
   `design-references/noorpath-package-reference.png` are immutable visual
   specifications for the Landing and Package families.
3. `design-system/MASTER.md` translates the references into shared rules.
4. UI UX Pro Max may improve accessibility and responsive behaviour.
5. Impeccable may detect drift and perform bounded polish.
6. Emil may add purposeful interaction feedback.
7. Ponytail keeps the implementation proportional and dependency-light.

No skill may replace an approved logo, icon family, image treatment, section
order, component shape, button treatment, or layout direction.

## Required process

1. Resolve the applicable approved reference and capture the current UI at the
   same desktop/mobile state.
2. Record a before/after audit covering logo, navigation, typography, spacing,
   colour, imagery, icons, section order, controls, footer, and mobile flow.
3. Reuse supplied assets and approved icon sources. Do not draw replacements
   with CSS, HTML, emoji, or handcrafted inline SVG.
4. Preserve desktop composition. Mobile may stack or reflow the same content,
   but must not remove or replace approved sections.
5. Motion is limited to feedback and continuity: normally 100–250 ms,
   `transform`/`opacity`, interruptible, hover gated to fine pointers, and
   reduced-motion safe.
6. Verify at a representative desktop viewport and 390 × 844 mobile viewport:
   no horizontal overflow, 44 px touch targets, logical headings, keyboard
   focus, meaningful image alternatives, working primary controls, and no
   console errors.
7. Place source and implementation captures together for visual QA. Fix all
   P0–P2 drift before acceptance.
8. Keep the pull request in draft until product-owner visual approval.

## Acceptance evidence

Every changed public surface must include:

- source reference path;
- browser-rendered desktop and mobile captures;
- comparison record and residual differences;
- type/build/lint results;
- interaction, overflow, focus, reduced-motion, and touch-target results; and
- explicit product-owner visual approval before merge.

