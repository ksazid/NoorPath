# NoorPath

**NoorPath is a trusted, operator-backed Umrah booking and journey-management platform designed for Indian Muslim families.**

The product aims to make Umrah planning clearer, safer, and more manageable by bringing package discovery, transparent pricing, instalment-based booking, traveller management, document processing, visa progress, operational support, and journey milestones into one connected experience.

NoorPath is being developed as a governed pilot rather than a generic travel marketplace. Every implementation must trace back to approved product requirements, technical decisions, design references, security policies, and an active vertical slice.

## Product vision

Planning Umrah can involve fragmented communication, unclear inclusions, uncertain payment expectations, manual document exchange, limited visibility into visa processing, and dependence on informal support channels.

NoorPath addresses these problems by giving customers and operators a shared source of truth for the complete journey.

The intended customer experience is:

- Calm and trustworthy rather than sales-heavy.
- Transparent about price, instalments, inclusions, exclusions, and due dates.
- Explicit about the responsible Umrah operator.
- Accessible to families from Indian Tier-2 and Tier-3 cities.
- Easy to understand on mobile devices.
- Supported by clear human escalation and assistance.
- Honest about confirmed, pending, blocked, optional, and unavailable items.

## Who NoorPath serves

### Customers and families

Customers can discover suitable Umrah departures, understand the complete package, book for themselves or family members, manage travellers, follow payment milestones, submit required documents, and monitor journey readiness.

### Umrah operators

Operators can manage batches, capacity, customer records, documentation, visa processing, operational exceptions, and fulfilment workflows through governed role-based tools.

### Platform administrators

Platform administrators manage authorised operators, privileged access, system-level policies, auditability, and operational oversight.

## Core product capabilities

### Package and batch discovery

- Browse available Umrah groups by departure city, date, duration, and package type.
- View clear package inclusions, exclusions, hotels, transport, meals, ziyarah, and occupancy options.
- Distinguish confirmed information from provisional or pending details.
- Present economy and premium package choices without hiding commercial differences.
- Show available seats and relevant booking deadlines where approved.

### Transparent pricing and instalments

- Show total package price and traveller-level breakdowns.
- Clearly separate the amount due now, remaining balance, and future due dates.
- Support milestone-based instalment collection.
- Prevent misleading or incomplete payment presentation.
- Maintain financial state separately from booking, visa, document, and fulfilment state.

### Family and traveller management

- Support bookings for multiple family members.
- Maintain traveller identity and relationship information.
- Support Mahram linking and other approved traveller rules.
- Keep traveller records isolated to the authorised customer and operator context.

### Document centre

- Provide a customer-facing document checklist.
- Support passport and required-document uploads.
- Validate file type, size, and content signature.
- Keep documents private and expose them only through short-lived authorised access.
- Support malware scanning, quarantine, operator review, corrections, rejection, retention, legal holds, and governed deletion.
- Maintain auditable document transitions and reviewer actions.

### Visa processing tracker

- Create one governed visa case for every eligible traveller.
- Expose safe customer-facing statuses without revealing internal operational details.
- Support operator processing queues and controlled status transitions.
- Require reasons for correction, rejection, or other material changes.
- Protect terminal states and reject stale updates through optimistic concurrency.
- Preserve append-oriented history and audited operator actions.

### My Journey

- Give authenticated customers a single view of confirmed bookings.
- Show departure, stay, traveller, occupancy, payment, document, and visa milestones.
- Present loading, empty, delayed, unavailable, action-required, rejected, approved, offline, retry, and stale states clearly.
- Provide support access using a safe booking reference.

### Operator and administration tools

- Create, review, and publish Umrah batches.
- Manage seats, travellers, documentation, visa cases, and operational queues.
- Apply permission-based and operator-isolated access.
- Record manual overrides with actor, reason, timestamp, and approval where required.
- Export or report operational data only through authorised workflows.

## Pilot scope

The approved pilot focuses on the complete customer and operator journey required to validate the business safely.

### Must-have pilot capabilities

- Group and batch discovery.
- Package detail and transparent instalment presentation.
- Seat lock and first-payment flow.
- Customer authentication and protected account shell.
- Family booking and Mahram linking.
- Customer My Journey dashboard.
- Passport and document submission workflow.
- Operator document review workflow.
- Visa status tracking.
- Room-sharing selection.
- Cancellation and refund rules.
- Administrative batch, seat, customer, and document controls.

### Later or conditional capabilities

The broader roadmap may include food preferences, orientation content, group communication, WhatsApp notifications, additional payment methods, reminders, add-ons, multilingual support, ziyarah schedules, referrals, loyalty, agent distribution, dynamic pricing, and other approved product extensions.

Future capabilities must not be preloaded into active pilot slices.

## Product principles

- **Trust before conversion:** operator identity, price, responsibility, and support must be explicit.
- **No hidden commercial state:** included, optional, excluded, pending, and confirmed items must be labelled.
- **Human support remains available:** automation must not remove meaningful customer assistance.
- **Mobile-first clarity:** critical journeys must work for customers using smaller screens and slower connections.
- **Deny-by-default access:** customers, operators, and administrators see only authorised data.
- **Auditable operations:** sensitive transitions require actor, timestamp, reason, and history.
- **Financial correctness:** financial records are append-oriented and corrected through governed compensating actions.
- **No silent policy invention:** unresolved product, religious, financial, legal, security, or operational decisions block implementation.

## Experience and design direction

The approved NoorPath Landing page and Package page are the primary visual source of truth for customer-facing experiences.

The rest of the application must feel like a natural extension of those references rather than a separate generic dashboard product.

Design characteristics include:

- Calm, contemporary, trust-first presentation.
- Visual language inspired by the spiritual and architectural context of Makkah and Madinah without decorative excess.
- Consistent typography, spacing, cards, controls, buttons, navigation, header, and footer patterns.
- Responsive layouts with strong mobile usability.
- Clear status presentation and failure recovery.
- WCAG 2.2 AA accessibility targets.
- Purposeful, restrained motion with reduced-motion equivalents.

### UI skills and governance

Project-local skills guide implementation, but they remain subordinate to approved requirements, design references, the design system, Figma artifacts, and Product Owner decisions.

- **Taste Skill** is used conditionally for landing, package, campaign, editorial, and explicitly approved redesign work.
- **UI UX Pro Max** governs information architecture, task flow, forms, states, accessibility, and responsive behaviour.
- **Impeccable** performs bounded visual refinement without replacing NoorPath's identity.
- **Emil Design Engineering** applies purposeful interaction and motion where it improves feedback or spatial continuity.
- **Ponytail** governs minimum-change frontend implementation and maintainable code.

Taste Skill is not the primary skill for My Journey, payments, documents, visa operations, admin queues, dense data tables, or other multi-step product workflows.

## Current engineering architecture

NoorPath uses a deliberately restrained architecture intended to support the pilot safely without premature microservices.

- Next.js customer and operations PWA.
- ASP.NET Core on .NET 10.
- Modular monolith organised by business module and vertical slice.
- Clean Architecture dependency direction.
- Selective CQRS where command/query separation improves clarity.
- PostgreSQL as the system of record.
- Entity Framework Core for persistence and migrations.
- Redis where caching is justified.
- OpenAPI contracts for HTTP APIs.
- Docker Compose for local infrastructure.
- GitHub Actions for validation and governed releases.
- Application Insights and Log Analytics-compatible observability patterns.
- OpenTelemetry-compatible telemetry boundaries.

NoorPath does not add generic repositories, a generic unit of work, microservices, event buses, or architectural abstractions mechanically.

## Security and trust architecture

Security is part of every vertical slice rather than a final release activity.

Key controls include:

- Server-side customer, operator, and tenant isolation.
- Role and permission checks at protected boundaries.
- Safe not-found responses where disclosure would leak data existence.
- OAuth/OIDC-based identity integration.
- Managed secrets and environment-specific configuration.
- Private document storage and short-lived signed access.
- File validation and malware-scanning state.
- Webhook signature validation and idempotency.
- Optimistic concurrency for sensitive operational transitions.
- Append-oriented audit history.
- Restricted telemetry that excludes secrets, tokens, passport data, payment payloads, and private document URLs.
- Human approval for production deployment and material policy decisions.

## Repository structure

```text
apps/
  api/              ASP.NET Core application host
  web/              Next.js customer and operations PWA

src/
  Modules/          Domain modules and vertical slices

tests/
  Architecture/     Dependency and module-boundary tests
  Integration/      PostgreSQL-backed API and migration tests
  Web.E2E/          Playwright customer and operator journeys

packages/
  design-tokens/    Shared brand and semantic design tokens

docs/
  adr/              Durable architecture decisions
  design/           Component inventory and visual rules
  slices/           Approved vertical-slice specifications
  evidence/         Certification and review evidence where applicable

design-system/      NoorPath design-system authority
design-references/  Approved visual references
.agents/skills/     Project-local engineering and UI skills
```

## Delivery model

NoorPath is delivered one approved vertical slice at a time.

```text
Approved requirement IDs
→ approved TRD and ADRs
→ approved design authority
→ active vertical slice
→ bounded implementation
→ deterministic preflight
→ required tests and visual evidence
→ full certification
→ Product Owner review
→ merge
→ separately approved production deployment
```

Each slice should deliver a demonstrable end-to-end outcome rather than an isolated technical layer.

Tasks should remain approximately 30–90 minutes where practical, and implementation must not expand into future backlog scope.

## Local prerequisites

- .NET 10 SDK.
- Node.js 24 LTS.
- pnpm 10.
- Docker with Compose v2.
- Git.

Toolchain versions are pinned in `.node-version`, `package.json`, and `global.json`. CI remains the source of truth when a pinned runtime is unavailable locally.

## Local setup

Clone the repository and install dependencies:

```bash
git clone https://github.com/ksazid/NoorPath.git
cd NoorPath
pnpm install
```

Copy the environment template:

```bash
cp .env.example .env
```

Use local-only values. Never commit `.env` files, secrets, tokens, private connection strings, identity-provider credentials, payment credentials, or deployment hooks.

Start PostgreSQL:

```bash
docker compose up -d postgres
```

Run the web application and API using the repository commands documented in `package.json` and the application projects.

## Validation commands

Use fast validation during normal development:

```bash
pnpm preflight
```

Preflight covers the repository's fast formatting, linting, type, build, manifest, and targeted validation boundaries.

Run full certification only when the slice is complete and certification is explicitly requested:

```bash
pnpm certify
```

`pnpm precert` remains an alias where retained for compatibility.

Full certification may include:

- Slice and requirement traceability validation.
- Formatting, linting, and type checking.
- Next.js and .NET release builds.
- Unit, architecture, integration, and Playwright tests.
- PostgreSQL-backed migration validation.
- Migration registry checks.
- Secret scanning and dependency checks.
- Accessibility, responsive, interaction, and screenshot evidence.
- Exact commit SHA certification.

## Testing strategy

NoorPath uses multiple complementary test layers:

- Domain and policy tests for state transitions and business invariants.
- Unit tests for focused application behaviour.
- Architecture tests for dependency direction and module isolation.
- Integration tests against PostgreSQL-backed application boundaries.
- API tests for authentication, authorization, tenant isolation, validation, and failure states.
- Playwright tests for customer and operator journeys.
- Accessibility checks for keyboard use, semantics, focus, zoom/reflow, and reduced motion.
- Responsive and screenshot comparison against approved visual references.

A passing build alone does not constitute product or visual acceptance.

## Hosting and environments

### API and database

- Render uses `render.yaml` to build and host `apps/api`.
- Automatic production deployment is disabled.
- Liveness and readiness are exposed through `/health/live` and `/health/ready`.
- Neon provides hosted PostgreSQL.
- `ConnectionStrings__NoorPath` must be configured securely in the environment.
- `Database__MigrateOnStartup=true` applies module migrations sequentially before accepting traffic where approved.

### Frontend

Frontend deployment is separately controlled. When enabled, `NOORPATH_API_URL` must point to the approved public API origin.

### Production approval

Production deployment is manually initiated through GitHub Actions using the exact reviewed 40-character commit SHA from `main`.

The protected `production` environment requires Product Owner approval. The workflow verifies that `main` still matches the approved SHA before invoking the Render deployment hook. A mismatch stops deployment.

Merging a slice does not deploy production automatically.

## Observability

Operational diagnostics should make failures understandable without exposing private data.

Expected telemetry includes:

- Correlation and trace identifiers.
- Safe module and operation names.
- Health and readiness status.
- External-integration success and failure categories.
- Retry and idempotency outcomes.
- Stale-version and concurrency failures.
- Customer-safe and operator-safe error classifications.

Logs must never include secrets, access tokens, full payment payloads, passport information, or private document URLs.

## Requirement and design authority

Before making changes, read [AGENTS.md](AGENTS.md).

The governing order is:

1. Approved PRD requirement IDs.
2. Approved Pilot TRD and ADRs.
3. Approved UI/UX Design Baseline.
4. Pilot Execution Backlog and active slice specification.
5. Repository engineering instructions and installed skills.

If these sources conflict, implementation must stop and record the conflict rather than inventing a resolution.

## Definition of Done

A vertical slice is complete only when:

- Acceptance criteria pass.
- Requirement and slice traceability is current.
- Required unit, integration, architecture, and E2E tests pass.
- Formatting, linting, type checks, builds, migrations, and dependency checks pass.
- Authorization, tenant isolation, privacy, audit, and failure paths are tested.
- Loading, empty, error, offline, retry, validation, permission, stale, and success states are covered where relevant.
- Accessibility and responsive behaviour are verified.
- Visual QA passes against the approved design authority.
- Observability is sufficient to diagnose failures safely.
- Documentation and evidence are updated.
- Product Owner review is complete where required.

## Working agreement

NoorPath is a product with sensitive financial, identity, travel, document, and operational workflows. Contributors and coding agents must optimise for correctness, clarity, trust, accessibility, and auditability—not merely implementation speed.

Read `AGENTS.md`, the active slice, relevant ADRs, and the approved design authority before modifying code.