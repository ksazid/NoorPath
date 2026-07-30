# NoorPath Engineering Instructions

## Mission

Build a calm, trustworthy Umrah platform that makes operator identity, price, instalments, inclusions, status, refunds, and human support explicit.

## Governing order

1. Approved PRD requirement IDs
2. Approved Pilot TRD and ADRs
3. Approved UI/UX Design Baseline
4. Pilot Execution Backlog and active slice specification
5. This file

If sources conflict, stop and record the conflict. Do not silently invent product policy.

## Agent skills

### UI/UX work

These rules apply to every existing and future customer, operator, and admin
screen. Refinement may correct usability, accessibility, responsiveness,
interaction quality, visual drift, or missing states. It must not silently
redesign an approved screen or change product behaviour.

Before any UI/UX task, verify and read the relevant installed project skills:

- `.agents/skills/ui-ux-pro-max/SKILL.md` for IA, task flow, responsive
  structure, accessibility, forms, feedback, and usability.
- `.agents/skills/impeccable/SKILL.md` for bounded visual refinement,
  hierarchy, spacing, typography, rhythm, consistency, content stress, and
  design-drift detection.
- `.agents/skills/emil-design-eng/SKILL.md` for purposeful interaction,
  feedback, transitions, motion, performance, and reduced-motion equivalents.
- `.agents/skills/ponytail/SKILL.md` in full mode for proportional,
  minimum-change implementation.

Do not claim a skill was used when its project `SKILL.md` is absent or unread.
Install and commit the approved skill source first, or record the task as
blocked.

The approved NoorPath Landing and Package references in `design-references/`
are the primary visual source of truth. `design-system/MASTER.md` and approved
Figma screens/components translate that identity into reusable rules. Skills
may improve usability, accessibility, responsiveness, consistency, craft, and
purposeful feedback, but must not replace NoorPath's established identity with
a generic design system.

### Engineering work

For coding, refactoring, bug fixing, dependency decisions, and implementation
design, use the installed `.agents/skills/ponytail` skill in full mode.

Ponytail's minimalism is subordinate to NoorPath requirements, architecture,
security, accessibility, testing, auditability, financial correctness, and
Definition of Done.

Prefer, in order:
1. Existing NoorPath implementation or component
2. Existing platform/framework capability
3. Existing dependency
4. Minimum new implementation required

Do not remove required validation, security, accessibility, observability,
tests, audit behaviour, or documented product requirements in the name of
simplification.

When multiple UI skills are relevant, apply them in this order:

1. Approved requirement IDs and the active vertical slice define product scope.
2. Approved Landing/Package references and `design-system/MASTER.md` govern
   visual identity.
3. UI UX Pro Max governs UX structure, states, accessibility, and responsive
   behaviour.
4. Approved Figma artifacts govern new screen/component decisions where they
   exist.
5. Impeccable performs a bounded visual-refinement pass; it refines rather than
   redesigns.
6. Emil principles apply selectively where interaction or motion adds feedback,
   state clarity, causality, or spatial continuity.
7. Product-owner approval closes material visual decisions.
8. Ponytail governs implementation simplicity after the design decision.
9. Automated accessibility, responsive, interaction, and screenshot comparison
   verify production against the approved design.

This AGENTS.md and the governing sources above remain authoritative. If a skill
recommendation conflicts with them, follow the governing source and record the
conflict.


## Architecture rules

- Keep a modular monolith. Do not introduce microservices.
- Dependencies point inward: API/UI and infrastructure may depend on application/domain abstractions; domain code depends on neither.
- Organise implementation by module and vertical slice.
- Use commands for state changes and queries for projections when separation adds clarity.
- Do not add MediatR, events, repositories, or abstractions mechanically.
- Do not create generic repositories or a generic unit of work.
- EF Core `DbContext` may be used directly inside application handlers.
- Use focused repositories only where aggregate intent or complex transactional loading justifies one.
- Modules may not query or mutate another module's tables directly.
- Cross-module work uses explicit contracts and transactional/outbox events.

## Security and trust rules

- Deny by default; enforce operator tenant isolation server-side.
- Never log secrets, tokens, payment payloads, passport data, or document URLs.
- Store documents privately and issue short-lived authorised URLs.
- Verify webhook signatures and guarantee idempotency.
- Financial records are append-only; corrections use compensating entries.
- Booking, payment, document, refund, and fulfilment states remain separate.
- Manual overrides require a reason, actor, timestamp, and approver where specified.
- Validate file type, size, content signature, and malware-scan status before access.

## Frontend rules

- Follow the approved NoorPath design tokens; do not introduce arbitrary colours, spacing, shadows, or motion.
- Render package content dynamically. Never assume quad sharing, one hotel, or fixed inclusions.
- Label included, optional, excluded, confirmed, and pending items explicitly.
- Always distinguish total price, amount due now, remaining balance, and due dates.
- Meet WCAG 2.2 AA, keyboard navigation, visible focus, semantic HTML, and reduced-motion behavior.
- Include loading, empty, error, offline, and retry states for every data-driven flow.
- Every changed UI must be compared at approved desktop and mobile viewports against the applicable visual authority.
- Record loading, empty, error, offline, validation, permission, stale/conflict, success, long-content, zoom/reflow, keyboard, and reduced-motion evidence where relevant.
- A screenshot comparison and product-owner acceptance are required before visual work is complete; passing source tests alone is not visual acceptance.

## Loop engineering

- Work on one approved vertical slice at a time.
- Keep implementation tasks between 30 and 90 minutes where practical.
- Start by citing the slice ID and relevant requirement IDs.
- Make the smallest end-to-end change that produces a demonstrable outcome.
- Do not preload or implement future-backlog features.
- Record a new ADR when a durable architectural decision changes.

## Definition of Done

- Acceptance criteria pass.
- Unit, integration, architecture, and E2E tests required by the slice pass.
- Formatting, linting, type checking, builds, migrations, and dependency checks pass.
- Authorization, tenant isolation, privacy, audit, and failure paths are tested.
- Accessibility and responsive behavior are checked.
- Visual QA passes against the approved baseline.
- Observability is sufficient to diagnose the feature.
- Documentation and traceability are updated.

