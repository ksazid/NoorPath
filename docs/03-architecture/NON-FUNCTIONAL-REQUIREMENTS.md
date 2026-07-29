# NoorPath V2 MVP Non-Functional Requirements & Reliability Baseline

Status: Draft baseline
Version: 0.1
Step: 13

## Purpose

This document defines the minimum non-functional guardrails NoorPath MVP must satisfy without over-engineering for speculative scale. These requirements are implemented incrementally through vertical slices and hardened before production launch.

The principle is:

> Build measurable reliability and performance into every slice; add infrastructure only when evidence shows a need.

## 1. Scope and Anti-Overengineering Rule

For MVP, NoorPath defines measurable customer performance goals, API latency/error budgets, essential availability targets, backup/recovery expectations, timeout/retry/idempotency rules, minimum load and concurrency validation, observability requirements, and graceful degradation expectations.

MVP does not require active-active multi-region architecture, microservices, Kubernetes, Redis, Service Bus, read replicas, database sharding, distributed search, or complex autoscaling policies by default.

## 2. Customer Web Performance

Customer-facing pages should target good Core Web Vitals under realistic mobile conditions:

- LCP <= 2.5 seconds at p75
- INP <= 200 ms at p75
- CLS <= 0.1 at p75

Important flows include landing, package listing/detail, checkout, and booking/journey dashboard.

Guardrails: responsive images, minimal unnecessary JavaScript, lazy-load non-critical media, avoid unjustified blocking third-party scripts, and provide explicit loading states.

## 3. API Performance

For ordinary synchronous MVP APIs not waiting on slow external providers, starting engineering targets are:

- p50 <= 250 ms
- p95 <= 750 ms
- p99 <= 1.5 s

These are engineering targets, not contractual SLAs. Endpoint-specific budgets may differ with justification.

Rules: no unbounded list endpoints, pagination for growth-prone collections, efficient projections, avoid N+1 access, no synchronous waiting for notification/reporting work, and explicit tests for expensive paths.

## 4. Availability

Initial production objective for NoorPath-controlled core customer/API runtime is 99.9% monthly availability once MVP is live.

Critical capabilities include discovery, authentication, booking, payment verification, and booking/payment/readiness status.

Secondary capabilities such as analytics or notifications may degrade without taking down core commercial truth.

## 5. Reliability and Failure Isolation

Committed truth must survive partial failure.

Examples:
- notification outage does not undo booking confirmation
- analytics outage does not fail payment processing
- document-processing outage remains recoverable
- duplicate webhook does not duplicate payment
- worker restart does not lose outbox work

Uncertain consequential outcomes use reconciliation rather than false success.

## 6. Timeout Policy

Every network/external dependency has an explicit bounded timeout. Timeout does not automatically mean business failure when provider outcome may still be unknown. Ambiguous consequential outcomes enter reconciliation/exception handling.

## 7. Retry Policy

Retries are used only for retry-safe/idempotent operations. Use finite retries, backoff/jitter where appropriate, no blind retry of auth/validation/business failures, and visible failure after exhaustion.

## 8. Idempotency

Mandatory where duplicates/retries can create consequential effects, including payment/refund webhooks, inventory conversion, outbox consumers, and selected state-changing APIs.

## 9. Concurrency

MVP tests races that threaten integrity, including final-inventory contention, hold expiry vs confirmation, duplicate/out-of-order payment events, cancellation vs settlement, document resubmission vs review, and relevant admin/operator transition races.

Correctness takes priority over throughput.

## 10. Capacity and Scalability

Before production, define a realistic pilot baseline for concurrent browsing, catalogue reads, checkout sessions, inventory holds, payment-webhook bursts, admin activity, document uploads, and outbox throughput.

Scale path:
1. optimize queries/indexes
2. scale stateless web/API/worker
3. tune DB connections/resources
4. move secondary work async
5. add cache/messaging/read replicas only when metrics justify
6. extract services only for proven independent needs

## 11. Database Reliability

PostgreSQL is system of record. Production requires managed hosting preference, automated backups, recoverability, tested restore, migration testing, monitored connection pools, observable slow queries, and no manual schema drift.

## 12. Recovery Objectives

Provisional engineering objectives for design validation:

- RPO <= 15 minutes for authoritative transactional data
- RTO <= 4 hours for a major production data-service incident

These are not final commitments and must be revisited before production based on business impact, provider capability, and cost.

## 13. Backup and Restore

A backup is not considered reliable until restore is tested. Production readiness requires automated backup, documented restore procedure, isolated restore test, governed retention/access, privacy validation on restored data, and document/object-storage recovery strategy.

## 14. Outbox and Background Work

Requirements: atomic business-state + outbox commit, at-least-once assumption, idempotent consumers, bounded retries, visible failures/backlog, and restart safety.

No broker is required for MVP unless these guarantees cannot be met operationally.

## 15. External Provider Resilience

Every provider defines timeout, retry safety, idempotency, rate-limit behaviour, degraded mode, reconciliation, and telemetry.

Payment outage must not create false settlement. Notification outage must not invalidate business state. Document scanning outage must leave uploads safely pending/quarantined.

## 16. Graceful Degradation

Core commercial truth should continue where secondary systems fail. Security controls never fail open.

## 17. Observability

Every production slice must answer whether it works, whether it is slow, what failed, which dependency/correlation is affected, and whether business truth remains safe.

Minimum telemetry includes request rate/latency/errors, DB latency/errors, worker/outbox health, payment reconciliation, inventory conflict/hold failures, document-processing failures, provider failures/timeouts, and authorization/security anomalies.

Sensitive data is excluded from telemetry.

## 18. Vertical Slice NFR Contract

Each vertical slice declares only the NFRs it exercises.

Example: a package-publication slice needs a latency budget, concurrency-safe transition, reliable post-commit event, tenant authorization tests, actionable telemetry, and relevant frontend performance/accessibility evidence. It does not need to implement platform-wide disaster recovery itself.

## 19. Testing Strategy

During each slice: latency sanity checks, concurrency/idempotency tests where relevant, dependency-failure tests, telemetry validation.

Before staging: representative integration/E2E performance and migration/failure checks.

Before production: realistic load test, backup restore test, recovery/runbook exercise, alert verification, and production-like performance baseline.

After launch: real telemetry replaces assumptions.

## 20. Release Blocking Conditions

Block release where known race/retry scenarios can corrupt business integrity, critical paths exceed agreed budgets without an accepted decision, migrations are unsafe, production backup/restore readiness is missing, critical background work can be silently lost, consequential provider ambiguity has no recovery path, or required monitoring is absent.

## 21. Deferred Scale Features

Deferred until evidence justifies them: Redis, Azure Service Bus, Elasticsearch/OpenSearch, read replicas, partitioning/sharding, multi-region active-active, microservices, Kubernetes, and complex autoscaling.

Each requires metrics/evidence, an ADR, and an expected measurable benefit.

## 22. Open Decisions

Before production resolve: realistic pilot traffic model, final SLA/SLO commitments, final RTO/RPO, PostgreSQL backup tier/capability, alert thresholds/on-call ownership, provider latency/retry budgets, object-storage recovery requirements, and production load-test profile.

## Freeze Rule

NFRs exist to protect customer experience and business integrity, not to justify infrastructure complexity. New reliability/scale infrastructure must solve a measured or clearly evidenced risk the simpler baseline cannot adequately address.
