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

For UI/UX analysis, design-system extraction, component design, responsive
behaviour, accessibility review, and visual consistency, use the installed
`.agents/skills/ui-ux-pro-max` skill.

The approved NoorPath Landing page and Package page are the primary visual
source of truth. UI UX Pro Max may improve usability, accessibility,
responsiveness, and consistency, but must not replace NoorPath's established
visual identity with a generic design system.

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

When UI UX Pro Max and Ponytail are both relevant:
- UI UX Pro Max governs UX/design analysis.
- Approved NoorPath design governs visual identity.
- Ponytail governs implementation simplicity.
- This AGENTS.md and the governing sources above remain authoritative.


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
- A screenshot comparison and product-owner acceptance are required before visual work is complete.

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

