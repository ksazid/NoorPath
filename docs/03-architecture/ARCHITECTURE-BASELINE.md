# NoorPath V2 Architecture Baseline

Status: Draft baseline
Version: 0.1
Step: 11

## Purpose

This document defines the target MVP architecture before detailed infrastructure, schema, or implementation work resumes. The objective is to preserve strong boundaries and reliability while keeping deployment and operational complexity low.

## 1. Architectural Style

NoorPath uses a **modular monolith** for MVP.

Reasons:
- domain boundaries are already clear enough to modularise logically
- a single deployable backend keeps operational complexity low
- transaction handling and debugging remain simpler than a distributed system
- modules can later be extracted if independent scale, ownership, security, or lifecycle requirements justify it

Microservices are explicitly deferred.

## 2. Backend Structure

Target backend structure:

- ASP.NET Core API host
- modular domain/application/infrastructure assemblies by capability
- background worker host for outbox and asynchronous processing
- shared technical primitives only where genuinely cross-cutting

Each module should own:

- Domain
- Application
- Infrastructure
- API contracts/endpoints or endpoint registration
- persistence ownership
- tests

Modules include:

- Identity
- Operators
- Catalogue
- Pricing
- Inventory
- Traveller
- Booking
- Payments
- Documents
- Visa
- Notifications
- Support
- Audit
- Reporting

## 3. Frontend Structure

Next.js remains the customer/admin web application technology.

The frontend is organised around user journeys and capability-backed features rather than mirroring backend database modules one-to-one.

Primary experience areas:

- Public discovery
- Package details
- Checkout
- Customer journey dashboard
- Operator operations
- NoorPath administration

Shared design-system primitives and tokens are reused across customer and admin experiences.

## 4. Layering Rules

Within a backend module:

### Domain
Owns business concepts, invariants, state transitions and domain rules. No infrastructure dependencies.

### Application
Owns use cases, commands, queries, orchestration within module boundaries, authorization requirements and integration contract usage.

### Infrastructure
Owns EF Core persistence, external providers, storage adapters, messaging/outbox plumbing and other technical adapters.

### API
Owns HTTP transport concerns, authentication context mapping, request validation and response contracts. Controllers/endpoints never bypass Application/Domain rules.

## 5. Module Boundary Rules

1. A module never writes another module's tables.
2. A module never imports another module's ORM persistence entities as its domain model.
3. Cross-module calls use explicit contracts.
4. Business events describe committed facts.
5. Commands request behaviour from an owner.
6. Experience composition may read multiple projections, but does not become a new source of truth.
7. Security and tenant scope are enforced at the owning module boundary.
8. Architecture tests should enforce dependency rules.

## 6. Persistence Strategy

PostgreSQL remains the system of record.

Recommended MVP direction:

- one PostgreSQL database
- logical ownership by module
- separate PostgreSQL schemas per major module where practical
- each module has its own persistence configuration/DbContext boundary where this improves ownership clarity
- cross-schema relational constraints may be limited deliberately to avoid tight persistence coupling

Candidate schemas:

- identity
- operators
- catalogue
- pricing
- inventory
- traveller
- booking
- payments
- documents
- visa
- notifications
- support
- audit
- reporting

The exact DbContext arrangement is an implementation ADR, but no single generic repository/UoW abstraction is required. Direct EF Core usage inside module infrastructure/application boundaries is acceptable.

## 7. Data Consistency Model

Within a module transaction, use normal PostgreSQL transactional consistency.

Across modules, do not assume a distributed transaction.

Coordination model:

- command to owner
- owner commits state
- owner records outbox event atomically with state when asynchronous propagation is required
- worker publishes/processes events
- consumers are idempotent
- reconciliation/compensation handles partial cross-module failure

Examples:

- PaymentSettled does not directly update Booking tables
- Booking consumes trusted payment outcome and confirms only after its own prerequisites pass
- Booking cancellation and inventory release are separate outcomes
- Booking cancellation and refund are separate outcomes

## 8. Outbox and Worker

Transactional Outbox is part of the MVP architecture baseline.

Each module that emits reliable integration events records the event in the same transaction as the state change.

A background worker:

- reads pending outbox records
- dispatches internal integration events
- retries failures safely
- records processing state
- supports idempotent consumers
- exposes backlog/failure telemetry

MVP does not require Azure Service Bus. The worker may dispatch within the application boundary initially. Service Bus can be introduced later without changing domain semantics if external integration/scale warrants it.

## 9. Synchronous vs Asynchronous Communication

Use synchronous calls where the caller needs an immediate authoritative decision and latency/failure coupling is acceptable.

Examples:

- validate operator publication eligibility
- obtain a current price quote
- acquire inventory hold
- fetch traveller summary

Use asynchronous events where:

- the initiating transaction should remain valid even if the consumer is unavailable
- the consumer is secondary or independently recoverable
- retries are expected

Examples:

- notifications
- audit projection
- reporting analytics
- post-commit operational updates

Critical cross-module workflows may combine synchronous commands with post-commit events.

## 10. API Architecture

RESTful HTTP APIs remain the MVP external transport.

Standards to define in API-STANDARDS later:

- /api/v1 versioning
- Problem Details
- request validation
- pagination/filter/sort conventions
- idempotency keys where needed
- optimistic concurrency where needed
- consistent authorization
- OpenAPI contracts
- correlation IDs
- safe error responses

No generic API gateway abstraction is required inside the monolith.

## 11. Identity and Authorization Architecture

Identity provider details remain an ADR, but the architecture assumes:

- external/standards-based authentication provider preferred
- API receives authenticated principal claims
- NoorPath maps principal to internal Account identity
- operator membership and business permissions remain owned by Operators/authorization policy
- privileged access requires MFA
- tenant/resource authorization is enforced server-side

Authorization is not centralised only in middleware; owning modules validate business/resource scope.

## 12. Document Architecture

Document bytes are not stored in PostgreSQL.

Architecture assumption:

- PostgreSQL stores document metadata/state
- private object storage stores document content
- upload passes through controlled intake/quarantine
- validation/malware scanning before reviewer trust
- short-lived authorised download/access tokens/URLs
- document access audited

Exact Azure storage/scanning service remains an ADR.

## 13. Payment Architecture

Payments use provider-hosted/tokenised flows.

Architecture assumptions:

- server creates provider payment intent/session
- client is redirected/uses provider-hosted secure payment UI
- authoritative settlement arrives through verified provider webhook/reconciliation
- webhook processing is idempotent
- financial ledger is append-only
- booking reacts to trusted payment outcome through explicit contract/event

No raw PAN/CVV storage.

## 14. Search and Caching

MVP starts simple:

- PostgreSQL-backed catalogue queries/search
- CDN/browser caching for public static/media assets as appropriate
- application/query optimisation before introducing distributed cache

Redis is not baseline infrastructure for MVP.

Elasticsearch/OpenSearch is not baseline infrastructure for MVP.

Both require measured need.

## 15. Deployment Topology

Target production direction:

Azure Front Door + WAF
  -> Next.js Web
  -> ASP.NET Core API
  -> Background Worker

Shared managed services:

- Azure Database for PostgreSQL
- private Blob/Object Storage for documents
- Azure Key Vault
- Application Insights / Log Analytics

Azure Container Apps is the preferred target to evaluate for hosting web/API/worker because it supports independently scalable containerised workloads without Kubernetes operational overhead. Final platform choice is an ADR.

## 16. Scalability Model

Scale by workload before splitting domains:

- stateless web/API instances
- independent worker scaling
- database connection pooling
- query/index optimisation
- pagination
- asynchronous secondary processing
- object storage/CDN for file/media delivery
- rate limiting and abuse controls

Later options only when justified:

- Redis
- Service Bus
- read replicas
- partitioning
- dedicated search
- extracted services

## 17. Reliability Model

Architectural reliability principles:

- idempotent external event handling
- retry-safe outbox consumers
- explicit timeout/retry policies
- reconciliation states for ambiguous payment outcomes
- exception states for cross-module failures after consequential facts
- graceful degradation for notifications/OCR/analytics
- no external secondary service failure may corrupt committed booking/payment truth

## 18. Observability Architecture

All runtime components use structured telemetry with correlation.

Baseline:

- logs
- metrics
- distributed traces where useful
- business-operation telemetry
- outbox backlog/failure metrics
- payment reconciliation alerts
- document-processing failure alerts
- authorization/security events

Sensitive values remain redacted/minimised.

## 19. Deployment Environments

Required environments:

- LOCAL
- CI
- DEV
- STAGING
- PROD

Rules:

- isolated secrets/data
- no routine production PII in lower environments
- staging is production-like enough for migrations, security, smoke and E2E validation
- IaC defines production infrastructure

## 20. Migration Architecture

EF Core migrations remain the schema-evolution mechanism.

Rules:

- generated migrations and snapshots only
- pending-model-change CI gate
- clean database migration test
- previous release -> current release upgrade test
- destructive change review
- expand/migrate/contract for risky changes
- merged migration history is not casually rewritten
- production migration procedure and backup/recovery evidence required

## 21. Architecture Testing

Architecture tests should verify at minimum:

- domain does not reference infrastructure/API
- modules do not depend on another module's infrastructure/persistence
- forbidden cross-module references fail CI
- experience/API composition does not bypass ownership rules

## 22. Explicit MVP Non-Goals

- microservices
- Kubernetes
- event sourcing
- CQRS everywhere
- shared generic repository/UoW abstraction
- Redis by default
- Service Bus by default
- distributed search by default
- synchronous coupling of all modules
- distributed transactions across modules

## 23. Vertical Slice Implementation Shape

A slice may touch multiple modules, but only for one user/business outcome.

Example `VS-CAT-01 Publish a basic saleable departure`:

- Operators: publication eligibility contract
- Catalogue: package/departure lifecycle
- Pricing: one valid published price version
- Inventory: one valid pool
- API: admin command and public read endpoint
- Web: admin publish workflow + customer package projection
- Security: operator scope/permission tests
- Data: migrations for only required fields
- Outbox/Audit: publication event and audit evidence
- Tests: domain, integration, API, E2E, accessibility, visual
- Observability: publication success/failure telemetry

No unrelated future features are implemented merely because the module is touched.

## 24. Architecture Freeze Rule

Any proposal to add a new runtime service, database, cache, message broker, search engine, or module must justify at least one of:

- independent scale requirement
- independent security/privacy boundary
- independent deployment/lifecycle requirement
- independent ownership/team requirement
- measurable performance/reliability need

Technical preference alone is insufficient.

## 25. Open Architecture Decisions

1. Exact identity provider.
2. Exact hosting platform (Container Apps vs alternative managed compute).
3. Exact PostgreSQL schema/DbContext arrangement.
4. Internal event dispatch implementation and outbox table topology.
5. Exact document storage/malware-scanning services.
6. Payment provider.
7. Front Door/WAF/rate-limit details.
8. Data residency/region.
9. IaC tool (Bicep or Terraform).
10. Deployment strategy/traffic switching.
11. Backup/RTO/RPO targets.
12. Observability retention/alert thresholds.
