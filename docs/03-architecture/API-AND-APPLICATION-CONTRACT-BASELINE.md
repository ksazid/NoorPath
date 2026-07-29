# NoorPath V2 API & Application Contract Baseline

Status: Draft baseline
Version: 0.1
Step: 16

## Purpose

Define the contract rules every NoorPath vertical slice must follow so APIs, application use cases, module integration and external provider interactions remain consistent as the MVP grows.

The API contract represents product capability behaviour, not database structure. EF Core entities and persistence schemas must never leak into public contracts by default.

## 1. Contract Principles

1. Public HTTP contracts are stable product interfaces.
2. Domain and persistence models remain internal implementation details.
3. Commands request behaviour; queries retrieve information.
4. Owning modules enforce authorization, invariants and resource scope.
5. APIs are explicit about validation, concurrency, idempotency and failure semantics.
6. Cross-module contracts expose only the facts required by the consumer.
7. Sensitive data is minimized in requests, responses, events, logs and OpenAPI examples.
8. Backwards-compatible evolution is preferred; breaking changes require explicit versioning/migration strategy.
9. External provider callbacks/webhooks are untrusted inputs until verified.
10. Every slice must provide contract tests and operational evidence proportional to its risk.

## 2. HTTP API Shape

MVP external API style: RESTful HTTP over HTTPS.

Base convention: `/api/v1/...`

Use nouns for resources and explicit action endpoints only where a domain transition cannot be represented clearly as ordinary resource mutation.

Examples:
- `GET /api/v1/batches`
- `GET /api/v1/batches/{batchId}`
- `POST /api/v1/bookings`
- `GET /api/v1/bookings/{bookingId}`
- `POST /api/v1/bookings/{bookingId}/cancel`
- `POST /api/v1/admin/departures/{departureId}/publish`

Avoid transport-driven names such as `/CreateBooking`, `/GetAllPackages`, `/UpdateStatus`.

## 3. Command vs Query Contract

Commands change authoritative business state or request a business transition. Queries return projections and do not mutate authoritative state.

Selective CQRS is acceptable; separate infrastructure is not required merely because commands and queries are conceptually distinct.

## 4. DTO Boundary

Public request/response DTOs are explicitly defined contracts. Never expose EF Core entities, aggregate roots, database navigation properties, internal state-machine implementation objects or provider SDK models by default.

## 5. Identifiers

API identifiers are opaque. Preferred domain identifiers are UUID-style values unless a capability ADR requires another strategy. Human-facing references such as booking numbers are separate from internal IDs.

## 6. Validation

Transport validation covers shape, format, length and range. Application/domain validation covers invariants, transition guards, ownership, resource validity, quote/price/inventory validity. Frontend validation is never authoritative.

## 7. Error Contract

Use Problem Details as the standard API error envelope with safe fields such as type, title, status, stable application error code, safe detail, correlation reference and field errors.

Stable categories include `validation_failed`, `not_authenticated`, `forbidden`, `resource_not_found`, `conflict`, `stale_version`, `quote_expired`, `inventory_unavailable`, `hold_expired`, `payment_pending`, and `external_provider_unavailable`.

Never expose stack traces, SQL, secrets, provider internals or sensitive document metadata.

## 8. HTTP Status Semantics

Use status codes consistently: 200 read/success, 201 created, 202 accepted async, 204 successful no-body mutation, 400 invalid request, 401 unauthenticated, 403 unauthorized, 404 not found/hidden, 409 conflict, 422 semantic validation if adopted consistently, 429 rate limited, 5xx server/provider failure.

## 9. Authentication and Authorization

Authorization evaluates `Identity + Permission + Operator Scope + Resource Ownership + Resource State -> Decision`.

Trusted scope is derived server-side. Client-supplied OperatorId never grants access. Cross-tenant negative tests are mandatory.

## 10. Money Contract

Money uses exact decimal semantics with explicit ISO currency code. Clients do not calculate authoritative totals. Quote/booking money is tied to a PriceVersion or committed commercial snapshot.

## 11. Date and Time Contract

Machine timestamps use ISO 8601 and UTC for instants. Date-only facts use date semantics. Timezone-sensitive rules identify the business timezone explicitly. Hold/quote/cutoff eligibility is server-evaluated.

## 12. Enum and State Serialization

Externally visible state values use stable string tokens rather than numeric ordinals. Internal names may differ from public/customer labels. Public labels may simplify, but never overstate certainty.

## 13. Pagination, Filtering and Sorting

Growing lists use bounded results, deterministic ordering, allow-listed filters and allow-listed sorts. Arbitrary database expressions are never exposed. Cursor pagination is introduced only when justified.

## 14. Idempotency

Mandatory where retry can duplicate money, inventory or workflow outcomes: payment webhook handling, payment initiation where supported, refund submission, hold/reservation conversion, selected booking operations and notification dispatch.

The same idempotency key with materially different payload fails safely.

## 15. Optimistic Concurrency

Use explicit concurrency protection where stale writes are dangerous, especially Inventory, Booking, Payments, Documents and mutable admin resources. Stale writes return an explicit conflict rather than overwriting newer state.

## 16. Long-Running Operations

Use 202 plus operation/status patterns only when work is genuinely asynchronous. Do not introduce generic workflow infrastructure for hypothetical future tasks.

## 17. Cross-Module Application Contracts

Internal contracts are capability-owned and independent of public HTTP DTOs. Examples include `OperatorPublicationEligibility`, `PriceQuote`, `InventoryHoldResult`, `TravellerSummary`, `BookingFinancialObligation`, `PaymentOutcome`, `DocumentReadiness`, and `VisaCustomerStatus`.

Consumers use contracts, never another module's persistence entity.

## 18. Domain and Integration Events

Events carry stable metadata such as EventId, EventType, EventVersion, OccurredAtUtc, CorrelationId and owning resource identifiers. Delivery is at-least-once; consumers are idempotent. Sensitive file/payment/authentication data is not placed in generic events.

## 19. External Provider Contracts

Provider SDK models remain behind adapters. Each integration defines authentication, timeout, retry, idempotency, rate-limit handling, validation, redaction and reconciliation/failure behaviour.

## 20. Webhooks

Webhook endpoints are dedicated trust boundaries with authenticity/signature verification, replay/duplicate protection, schema validation, idempotent processing, safe acknowledgement and reconciliation evidence.

A webhook never directly mutates another module's tables.

## 21. Correlation and Observability

Meaningful workflows carry correlation/trace context through safe logs, internal contracts/events and external calls where useful. Sensitive data is never encoded into correlation identifiers.

## 22. OpenAPI

Implemented HTTP contracts produce OpenAPI. CI should detect unintended contract changes where practical. Examples never contain real customer, passport, payment or secret data.

## 23. Backward Compatibility and Versioning

`/api/v1` is the MVP major line. Within v1, prefer additive optional fields and stable semantics. Removing/renaming fields, changing types or silently changing state meaning is breaking and requires an explicit compatibility/version decision.

## 24. Security and Privacy Contract Rules

Every endpoint defines authentication, authorization/resource scope, data classification, actor-specific fields, audit need, abuse protection and logging redaction. Document APIs never return permanent public object URLs. Payment APIs never expose PAN/CVV or provider secrets.

## 25. Contract Testing

Each slice provides proportionate API, contract, integration and external-provider tests, including validation, authorization, cross-tenant denial, conflicts, serialization, concurrency/idempotency, duplicate webhooks and invalid signatures where relevant.

## 26. Slice Contract Checklist

Before Definition of Ready:
- requirement/use case identified
- owning module identified
- command/query classification defined
- route/verb defined
- request/response DTOs defined
- authorization/resource scope defined
- validation and business errors defined
- HTTP mapping defined
- concurrency/idempotency considered
- privacy classification reviewed
- events/internal contracts identified
- observability/correlation defined
- API/contract/integration tests defined
- OpenAPI impact reviewed

Missing consequential decisions block implementation rather than being invented by an engineer or agent.

## 27. Explicit MVP Non-Goals

No GraphQL, gRPC internal module transport, internal API gateway orchestration, enterprise service bus abstraction, separate command/query databases, HATEOAS framework or public developer-platform program by default.

## 28. Freeze Rule

The public API is derived from product capability behaviour, not table/entity shape. EF/schema/provider changes do not authorize contract changes. Breaking contract changes require impact analysis across UX, clients, tests, security and rollout compatibility.

## 29. Open Decisions

1. Exact validation library/pipeline convention.
2. Final 400 vs 422 policy.
3. ETag vs explicit version token for concurrency.
4. Pagination style per high-volume capability.
5. Idempotency-key persistence and retention.
6. API compatibility tooling in CI.
7. Identity-provider claim mapping.
8. Payment/document provider-specific webhook details.

Resolve these when the first slice needing them reaches Definition of Ready rather than over-engineering them globally now.
