---
name: design-taste-frontend
description: Conditional anti-slop frontend direction for NoorPath landing, package, marketing, and explicitly approved redesign work. Not the primary skill for customer workflows, operator/admin screens, dashboards, queues, forms, or data tables.
source: https://github.com/ksazid/taste-skill/tree/main/skills/taste-skill
upstream: https://github.com/Leonxlnx/taste-skill
license: MIT
---

# Design Taste Frontend for NoorPath

Use this skill only to strengthen NoorPath's approved visual identity on landing, package, campaign, editorial, or explicitly approved redesign surfaces.

## Governing authority

The approved NoorPath Landing and Package references, `design-system/MASTER.md`, approved Figma artifacts, active requirement IDs, and active slice remain authoritative. This skill must never replace NoorPath's established identity or product behavior.

## Use when

- Extending the approved landing/package language into a new marketing or editorial surface.
- Refining layout, typography, spacing, visual hierarchy, or art direction without changing product behavior.
- Conducting an explicitly approved redesign audit.
- Preventing generic template-like output on customer-facing marketing pages.

## Do not use as the primary skill when

- Building My Journey, booking, payment, document, visa, refund, support, or account workflows.
- Building operator/admin queues, dashboards, data tables, forms, or multi-step processes.
- The task is limited to accessibility, responsiveness, interaction states, or implementation quality.
- The proposed design conflicts with approved NoorPath references or tokens.

## Required workflow

1. Read active requirement IDs and slice scope.
2. Read the applicable approved visual references and `design-system/MASTER.md`.
3. State a one-line design read for the surface and audience.
4. Infer restrained design variance, motion, and density appropriate for a calm, trustworthy Umrah product.
5. Reuse NoorPath components, tokens, imagery, icons, header/footer, and spacing patterns before introducing anything new.
6. Use UI UX Pro Max for workflow structure, accessibility, forms, responsive behavior, and states.
7. Use Impeccable only for bounded polish; use Emil only where motion improves feedback or continuity.
8. Use Ponytail for minimum-change React/Next.js implementation.
9. Verify desktop/mobile screenshots, keyboard access, reduced motion, content stress, and approved visual parity.

## NoorPath defaults

- Calm and trustworthy over experimental.
- Preserve the approved Saudi/Makkah/Madinah-inspired palette and established iconography.
- Keep customer-facing footer consistency where applicable.
- Avoid generic AI gradients, glassmorphism, arbitrary icon-family changes, and redesign-by-default.
- Do not introduce a new design system when NoorPath's own system already covers the need.

## Canonical installation/update

```bash
npx skills add https://github.com/ksazid/taste-skill --skill "design-taste-frontend"
```

The upstream v2 skill is experimental; NoorPath's project-local constraints in this file always take precedence.
