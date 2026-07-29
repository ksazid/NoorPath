# NoorPath V2 Design System & Figma Baseline

Status: Draft baseline
Version: 0.1
Step: 15

## Purpose

This document defines how NoorPath's approved visual identity, UX rules, design tooling, Figma workspace, tokens, components, responsive behaviour, accessibility and design-to-code workflow will operate before implementation resumes.

## 1. Design Authority

1. Approved NoorPath Landing reference
2. Approved NoorPath Package reference
3. design-system/MASTER.md
4. Approved Figma screens/components/variables
5. Design tokens and production components
6. Implementation details that do not contradict the above

The existing NoorPath visual identity is extended, not replaced.

## 2. Design Principle

**Sacred calm + operational confidence.**

Customer experience emphasises trust, clarity, pilgrimage context and reassurance. Operator/admin experience emphasises state, accuracy, exceptions, workflow, risk and auditability.

## 3. Visual DNA

### Makkah
- Kiswah black
- restrained Kiswah-inspired gold
- Haram marble / ivory
- monumental scale and sacred focus
- authentic Makkah / Masjid al-Haram imagery

### Madinah
- Madinah green
- courtyard ivory
- warm stone
- soft daylight and calm
- authentic Masjid an-Nabawi imagery

### Saudi character
- sandstone / limestone warmth
- architectural geometry
- restrained Islamic geometric accents
- Arabic typographic accents only where appropriate
- Saudi identity should be felt rather than decorated excessively

Avoid generic crescent/mosque/gold clichés and misleading trust symbolism.

## 4. Figma Workspace Structure

00 Foundations
01 Brand
02 Tokens
03 Primitives
04 Customer Components
05 Admin Components
06 Patterns
07 Customer Journeys
08 Operator Journeys
09 Edge States
10 Accessibility
11 Approved Screens
12 Explorations / Archive

Only Approved Screens and approved library components are implementation authority.

## 5. Token System

Define semantic tokens for colour, typography, spacing, radius, elevation, borders, containers, breakpoints, motion, icons, focus and status semantics.

Figma variables and code tokens must represent the same semantic system.

## 6. Component Foundations

Shared primitives include buttons, links, form controls, field states, status badges, alerts, dialogs, drawers, tabs, accordions, loading, empty states, pagination and navigation aids.

Customer components include public navigation/footer, package cards, operator trust blocks, price summaries, occupancy selectors, stay cards, inclusions/exclusions, journey progress, travellers, payment schedule, documents, visa/readiness and support.

Admin/operator components include navigation shell, work queues, filters, tables/lists, state badges, validation summaries, review/audit panels, confirmations, exception panels, pricing/inventory patterns and guided package/departure workflows.

## 7. Component State Contract

Relevant components must design default, interaction, disabled, loading, empty, validation-error, system-error, success, permission-denied, stale/conflict, unavailable/expired, long-content and reduced-motion states.

## 8. Responsive & Accessibility

Customer flows are mobile-first. Admin flows are desktop-first for operational density and then explicitly adapted for smaller devices.

Target WCAG 2.2 AA including contrast, focus, keyboard flow, form errors, semantics, target sizes, zoom/reflow, reduced motion, dialogs and data-heavy views.

## 9. Motion

Motion communicates state, hierarchy, causality or feedback. Decorative motion is avoided. Reduced-motion equivalents are mandatory when animation exists.

## 10. Imagery

Use authentic licensed Haramain, Makkah, Madinah, Saudi travel and real package/hotel imagery. Imagery is never proof of operator reliability or religious legitimacy. Avoid inaccurate AI-generated sacred-site imagery in production trust-critical contexts.

## 11. Skill / Tool Responsibilities

### UI UX Pro Max
IA, task flows, cognitive load, responsive structure, accessibility, forms, errors/recovery and admin usability. It cannot replace NoorPath's visual identity.

### Figma
Canonical editable workspace for foundations, variables/tokens, components, responsive screens, journeys, approved screens and developer handoff.

### Impeccable
Visual hierarchy, spacing, typography, rhythm, consistency, content stress testing and design-drift detection. Refines rather than redesigns NoorPath.

### Emil / design-engineering principles
Interaction quality, transitions, feedback, motion choreography and reduced-motion equivalents, selectively where useful.

### Ponytail
Applied during implementation after design approval to keep code/UI implementation simple and proportional to MVP scope. It has no product or visual authority.

## 12. Skill Execution Sequence

1. Product/domain requirement is ready.
2. UI UX Pro Max reviews UX/state/accessibility.
3. Figma creates/updates screens/components.
4. Impeccable performs visual refinement.
5. Emil principles are applied where interaction/motion adds value.
6. Product Owner approves.
7. MASTER/tokens/components update if a reusable rule changed.
8. Ponytail guides implementation simplicity.
9. Playwright/accessibility/visual QA verify production against approved design.

## 13. Availability / Installation Model

Repository-level AI skills live under `.agents/skills/` and are versioned with the repository where installed.

Known project skills:
- `.agents/skills/ui-ux-pro-max/SKILL.md`
- `.agents/skills/ponytail/SKILL.md`

Agents must inspect/read the relevant SKILL.md in the active checkout before relying on it.

Figma is available as a connected design tool rather than a repository skill.

Impeccable and Emil guidance must only be described as installed when corresponding repository/plugin resources are actually verified; until then they are approved methodology/tooling roles rather than assumed installed dependencies.

## 14. Design-to-Code Contract

MASTER -> Figma variables/components -> design tokens -> React/Next.js components -> visual regression.

Code Connect may be added later for approved components once production component APIs stabilise.

## 15. Change Control

Material design-system changes require reason, impacted screens/components, accessibility/responsive evidence, comparison against existing authority and Product Owner approval. MASTER/tokens/components are updated where applicable.

## 16. MVP Restraint

Do not prebuild a huge component library, exhaustive animation system, multiple themes, speculative V2 components or one-off bespoke primitives. Build reusable patterns when a real MVP workflow needs them.

## 17. Exit Criteria

Step 15 is ready when design authority, Figma structure, token/component ownership, customer/admin principles, responsive/accessibility/state rules, skill responsibilities, design-to-code flow and change control are explicit.

Detailed Figma creation occurs against approved MVP slices/journeys rather than designing the entire future product upfront.
