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

