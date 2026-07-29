# ADR-003 — Transactional Outbox & Cross-Module Messaging

Status: Accepted for V2 foundation
Date: 2026-07-29

## Context
NoorPath is a modular monolith. Business capabilities must remain independently owned even though they share one PostgreSQL database and deployment boundary. Cross-module work must not rely on direct table mutation or fragile in-process fire-and-forget behavior.

Examples include Booking confirmation producing notification work, Payment settlement informing Booking, Catalogue publication producing public projections, and Documents/Visa changes updating derived readiness.

## Decision
Use a PostgreSQL-backed transactional Outbox as the MVP reliability mechanism for committed cross-module integration events.

No external message broker is required for MVP. A broker may be introduced later behind the same integration contracts when scale, isolation, external integrations, operational throughput or independent deployment justify it.

## Core semantics
- Delivery guarantee: at least once.
- Consumers are idempotent.
- No assumption of global ordering.
- Where ordering matters for one aggregate/resource, include aggregate version or sequence information.
- An integration event is created only after the owning domain transition is accepted.
- The domain state change and corresponding Outbox record are persisted atomically within the owner's local database transaction.
- Secondary processing failure never rolls back an already committed business fact.

## Outbox record
Minimum logical fields:
- EventId
- EventType
- EventVersion
- OccurredAtUtc
- ProducerModule
- AggregateType
- AggregateId
- AggregateVersion/Sequence where required
- CorrelationId
- CausationId where applicable
- OperatorId only where legitimately required by the consumer
- Payload
- CreatedAtUtc
- Processing status / attempt metadata
- NextAttemptAtUtc or equivalent retry scheduling information
- ProcessedAtUtc when complete

Payloads contain only the minimum facts required by consumers. Sensitive document bytes, passport data, payment credentials/tokens, authentication secrets and private URLs are prohibited.

## Storage ownership
Outbox persistence belongs to a shared technical platform mechanism, but event creation remains the responsibility of the producing module.

A producer must not write another module's business tables to emulate messaging.

## Dispatcher
A background worker polls/claims pending Outbox records, dispatches them through the in-process integration-event abstraction, and records successful processing.

Requirements:
- safe concurrent claiming when multiple worker instances exist
- bounded batch size
- explicit retry policy
- exponential/backoff behavior appropriate to failure type
- poison-message/dead-letter handling after a governed maximum or explicit terminal failure state
- structured telemetry for pending count, oldest age, attempts, failures and processing latency
- graceful shutdown without losing committed work

Exact SQL locking/claiming implementation is an implementation detail, but duplicate dispatch must remain safe.

## Consumer idempotency
Every consequential consumer must use EventId or a purpose-specific idempotency key to avoid duplicate business effects.

High-risk examples:
- PaymentSettled must not confirm a booking twice.
- BookingConfirmed must not reserve inventory twice.
- RefundSettled must not apply the same refund twice.
- Notification consumers may resend only according to an explicit delivery policy.

Consumers must not treat receiving an event as proof that the resulting local action succeeded; they record their own local business truth.

## Synchronous vs asynchronous communication
Use synchronous module contracts when the caller requires an immediate authoritative decision to complete the current user/business operation.

Examples:
- validate Operator publication eligibility before publication
- request authoritative Pricing quote
- acquire Inventory hold

Use asynchronous integration events when work can happen after the producer commits or where retry/isolation matters.

Examples:
- notifications
- reporting projections
- non-blocking readiness updates
- post-commit secondary processing

Do not use events merely because two modules communicate. Do not use synchronous calls for retryable secondary work.

## Failure handling
- Producer commit succeeds even if dispatcher is unavailable.
- Dispatcher failure leaves work pending for retry.
- Consumer failure is retried according to policy without modifying producer truth.
- Permanent failure is surfaced operationally with diagnostics and manual recovery path where needed.
- No silent message loss.

## External brokers later
Azure Service Bus or another broker may be added only when justified by measured need. The broker becomes a transport adapter; integration-event semantics, ownership, idempotency and producer contracts remain unchanged.

## Rejected alternatives
- Direct cross-module database writes.
- In-memory fire-and-forget events without durable persistence.
- Distributed transactions across modules.
- Introducing Service Bus during MVP without evidence.
- Exactly-once delivery assumptions.

## VS-00 verification
VS-00 must prove:
1. a domain/business change and Outbox record commit atomically;
2. worker dispatch survives restart;
3. duplicate delivery is safe for a reference consumer;
4. failed handling remains retryable/observable;
5. architecture rules prevent direct cross-module business-table mutation;
6. correlation/event metadata appears in logs/traces without sensitive payload leakage.
