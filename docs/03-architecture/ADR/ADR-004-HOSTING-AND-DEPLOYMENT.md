# ADR-004 — Hosting, Azure Infrastructure and Deployment

Status: Accepted for V2 foundation  
Date: 2026-07-29

## Context

NoorPath needs a production-capable but MVP-sized Azure hosting model that supports independent Web/API/Worker deployment, managed PostgreSQL, private document storage, managed identities, centralized telemetry, safe releases and future scale without introducing Kubernetes or microservices.

## Decision

Use Azure as the target production platform with the following baseline:

- **Web:** Next.js container workload on Azure Container Apps.
- **API:** ASP.NET Core modular-monolith API on Azure Container Apps.
- **Worker:** background/outbox workload on Azure Container Apps.
- **Database:** Azure Database for PostgreSQL Flexible Server.
- **Object storage:** Azure Blob Storage with private containers for sensitive documents.
- **Secrets/config secrets:** Azure Key Vault.
- **Telemetry:** Application Insights / Azure Monitor / Log Analytics using OpenTelemetry-compatible instrumentation.
- **Images:** Azure Container Registry.
- **Infrastructure as Code:** Bicep for the MVP baseline.
- **CI/CD:** GitHub Actions builds immutable artifacts/images, validates quality gates, deploys through DEV -> STAGING -> PROD approval flow.

Azure Container Apps is workload separation, not microservices. Web, API and Worker remain parts of one NoorPath system and the backend remains a modular monolith.

## Edge and ingress

Do **not** require Azure Front Door on day one merely to satisfy architecture aesthetics.

Baseline:
- DEV/STAGING may use Container Apps ingress directly with HTTPS.
- PROD introduces Azure Front Door Standard/Premium + WAF when the production threat/performance/reliability review justifies the edge layer.
- If Front Door is used, origins must be restricted as far as practical so it cannot be bypassed unintentionally.

This preserves the target architecture while avoiding unnecessary pre-production cost/complexity.

## Networking

MVP starts with the simplest secure managed networking that satisfies production controls.

Requirements:
- HTTPS only for public entry points.
- API/database/storage/Key Vault access constrained according to environment and risk.
- PostgreSQL is never intentionally exposed as a public application endpoint.
- Sensitive storage containers remain private.
- Private endpoints/VNet integration are introduced for production where the final security review and service topology require them; they are not mandatory for every local/DEV workload.

## Managed identity

Azure workloads use managed identity for supported Azure service access instead of long-lived credentials.

Examples:
- API/Worker -> Key Vault.
- API/Worker -> Blob Storage.
- workloads -> other Azure resources where Entra authentication is supported.

Use the narrowest identity/RBAC scope practical. Shared human credentials are prohibited.

## Container Apps scaling

Scale configuration is workload-specific.

- Web/API may scale based on HTTP demand.
- Worker may use bounded replicas appropriate to Outbox processing and database concurrency.
- Do not optimize for arbitrary high scale before measurement.
- Production workloads that must avoid cold-start impact may set a non-zero minimum replica count.
- Scale-to-zero may be used for lower environments or suitable non-critical workloads.

Autoscaling does not replace application-level concurrency, idempotency or capacity controls.

## Environments

Required:
- LOCAL
- CI
- DEV
- STAGING
- PROD

Each environment has isolated configuration, secrets and business data.

Production PII/document data must not be copied to lower environments.

Optional PR preview environments are deferred until they produce enough development value to justify cost and complexity.

## Build and release

Baseline flow:

PR
-> validation/security/tests
-> build immutable container images/artifacts
-> push to registry
-> deploy DEV
-> smoke/integration validation
-> promote same version to STAGING
-> migrations + critical E2E/accessibility/security/smoke validation
-> approval
-> PROD deployment
-> production smoke verification
-> telemetry observation

Prefer build-once/promote-same-artifact semantics.

## Database deployment

Schema migrations are explicit release artifacts, not automatic application-startup side effects in production.

Before production:
- clean database migration validation passes;
- supported previous release -> target upgrade passes;
- no unexplained pending EF model changes;
- destructive migrations receive explicit review;
- backup/recovery path is known for risky migrations.

Application rollback is supported only where database compatibility allows it. Database recovery defaults to roll-forward.

## Infrastructure as Code

Use **Bicep** for MVP Azure infrastructure.

Reasons:
- NoorPath target cloud is Azure;
- native support keeps the initial IaC surface small;
- no current multi-cloud requirement justifies Terraform abstraction.

A future ADR may migrate to Terraform if organizational/platform needs justify it.

IaC should eventually own:
- resource groups/environment topology;
- Container Apps environment/workloads;
- PostgreSQL;
- Blob Storage;
- Key Vault;
- identities/RBAC;
- registry;
- monitoring/alerts;
- ingress/Front Door/WAF where enabled;
- DNS/certificate configuration where applicable.

Manual production infrastructure changes are exceptions and must be reconciled into IaC.

## Secrets and configuration

- non-secret configuration is environment-specific and externally supplied;
- secrets never live in Git or browser bundles;
- Key Vault/managed identity is preferred for production secrets;
- missing critical configuration fails fast;
- configuration cannot disable core security invariants.

## Availability and disaster recovery

Do not build active-active multi-region MVP infrastructure.

Start with:
- managed service availability features;
- automated database backups;
- documented restore procedure;
- monitoring/alerts;
- tested recovery before production release.

Multi-region architecture requires measured business/recovery requirements that cannot be met by the baseline.

## Explicit non-goals

Not required for MVP:
- AKS/Kubernetes;
- service mesh;
- multi-region active-active;
- Redis by default;
- Service Bus by default;
- dedicated search cluster;
- complex deployment orchestrators;
- per-PR environments by default.

## Consequences

### Positive
- low operational complexity relative to Kubernetes;
- independent scaling/deployment for Web/API/Worker;
- Azure-native managed identities and managed data services;
- natural future path to add Front Door, Service Bus, caching or independent services when evidence requires them;
- infrastructure reproducibility through Bicep.

### Trade-offs
- Azure platform dependency is deliberate;
- Container Apps operational characteristics must be understood and monitored;
- Bicep is less portable than Terraform;
- adding Front Door later requires planned ingress/origin hardening rather than assuming it from day one.

## VS-00 verification

VS-00 must prove only the minimum deployable skeleton:

1. Web/API/Worker images can be built reproducibly.
2. DEV infrastructure is reproducible through Bicep or the first approved IaC increment.
3. workloads receive environment configuration correctly.
4. managed identity/secrets pattern is demonstrable without storing production credentials.
5. PostgreSQL connectivity/migrations follow the approved release pattern.
6. application health/smoke endpoints and telemetry are observable.
7. CI produces immutable versioned artifacts.

Full production hardening remains part of later slices and VS-15 Production Readiness.