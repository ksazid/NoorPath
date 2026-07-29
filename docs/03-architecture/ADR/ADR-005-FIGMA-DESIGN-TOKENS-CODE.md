# ADR-005 — Figma, Design Tokens and Code Synchronization

Status: Accepted for V2 foundation
Date: 2026-07-29

## Decision
NoorPath uses a controlled design-to-code pipeline so approved visual decisions remain traceable and implementation cannot silently invent or drift from the product design language.

Authority order:

1. Approved NoorPath Landing reference
2. Approved NoorPath Package reference
3. `design-system/MASTER.md`
4. Approved Figma variables/components/screens
5. Repository design tokens and component contracts
6. Production implementation

If sources conflict, implementation stops until the conflict is resolved. Production code is never allowed to become the accidental source of design truth.

## Figma role
Figma is the canonical editable workspace for:
- semantic variables/tokens
- shared components
- responsive layouts
- customer and admin journey screens
- edge/error/loading/empty/conflict states
- approved implementation references

Explorations are not implementation authority until explicitly approved.

Recommended Figma page model:
- 00 Foundations
- 01 Brand
- 02 Tokens
- 03 Primitives
- 04 Customer Components
- 05 Admin Components
- 06 Patterns
- 07 Customer Journeys
- 08 Operator Journeys
- 09 Edge States
- 10 Accessibility
- 11 Approved Screens
- 12 Explorations / Archive

## Token model
The repository keeps executable semantic tokens under `packages/design-tokens`.

Token categories include:
- colour
- typography
- spacing
- radius
- border
- elevation
- container/layout
- breakpoints
- icon sizing
- focus styles
- motion duration/easing
- status semantics

Use semantic names such as `surface/default`, `text/primary`, `action/primary`, `status/success` rather than raw colour names in application code.

## Synchronization contract
Figma variables and repository tokens represent the same semantic contract.

Changes follow:

Design decision
→ update Figma variable/component
→ review against Landing/Package/MASTER
→ Product Owner approval where required
→ update repository token/component contract
→ implementation
→ visual/accessibility regression verification

No automated one-way sync tool is required for MVP. Manual controlled synchronization is preferred initially because it is easier to audit and less likely to propagate accidental Figma changes directly into production.

A token automation/export tool may be introduced later if repeated manual synchronization becomes measurable friction.

## Component ownership
Shared components must consume semantic tokens rather than embedding arbitrary design values.

Application screens may compose shared primitives/patterns but should not create new local visual systems.

Customer and admin experiences share foundations but can have separate higher-level component patterns where their workflows differ.

## Design skills and review sequence
For each UI-bearing vertical slice:

1. UI UX Pro Max — IA, usability, accessibility, responsive behaviour, states and flow review.
2. Figma — canonical editable screen/component design.
3. Impeccable — visual hierarchy, spacing, typography, consistency and design-drift refinement.
4. Emil/design-engineering principles — interaction, feedback and motion only where useful; reduced-motion support required.
5. Product Owner approval.
6. MASTER/tokens/component contracts updated if the approved design changes the system.
7. Ponytail — implementation simplicity and avoidance of unnecessary abstraction.
8. Playwright/accessibility/visual QA against approved references.

Skills are advisory tools. They may improve usability or craft but cannot replace NoorPath's established visual identity or product rules.

## Installation/verification rule
Before the first design-bearing implementation slice:
- verify `.agents/skills/ui-ux-pro-max/SKILL.md` is available;
- verify `.agents/skills/ponytail/SKILL.md` is available;
- install and verify Impeccable before it is used as a formal refinement step;
- install/verify the selected Emil/design-engineering resource before formal motion/interaction review;
- confirm the Figma connector/workspace is accessible.

Do not claim a skill is installed until its actual repository/plugin resource is verified.

## Responsive and accessibility contract
Approved Figma work must cover the states/breakpoints relevant to the slice rather than only a desktop happy path.

Required design consideration where applicable:
- mobile/tablet/desktop behaviour
- keyboard/focus behaviour
- WCAG 2.2 AA
- loading
- empty
- validation error
- system error
- offline/retry
- permission denied
- stale/conflict state
- success
- long content/text expansion
- reduced motion

## Visual regression
Critical approved states receive screenshot/visual regression coverage. The goal is to catch meaningful design drift, not to make every pixel change impossible.

Visual test updates require an intentional reason and design approval when the expected UI has changed.

## Existing UI disposition
Existing S01/S02 screens/components may be reused only after they pass V2 design-contract review against:
- approved Landing/Package references
- MASTER
- approved Figma
- current token contract

Existing code is not grandfathered into V2 merely because it already works.

## Rejected alternatives
- Code-first styling as the design source of truth.
- Fully automatic Figma-to-production code generation for MVP.
- Introducing a new generic design system that replaces NoorPath identity.
- Tailwind or another styling framework solely to create consistency when current CSS/tokens can already enforce it.
- Allowing design tools/skills to make unapproved product-policy decisions.

## VS-00 verification
Before VS-00 closes:
- Figma access/workspace confirmed;
- Landing + Package + MASTER authority documented;
- repository token package reconciled to current approved semantic foundations;
- minimum shared primitives required by VS-01 can consume tokens;
- design skill availability is verified, with Impeccable/Emil installed or explicitly queued before the first slice that uses them;
- visual/accessibility verification path is runnable in CI or clearly staged for the first UI-bearing slice.

## Consequence
This ADR completes the pre-development architectural decisions required for VS-00. Further design decisions are made slice-by-slice rather than through another broad planning phase.
