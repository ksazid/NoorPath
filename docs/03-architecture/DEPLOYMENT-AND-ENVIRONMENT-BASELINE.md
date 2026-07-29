# NoorPath V2 Deployment & Environment Baseline

Status: Draft baseline
Version: 0.1
Step: 17

## Purpose

Define the minimum deployment, environment, configuration, release, rollback, and infrastructure rules required for a safe MVP without adding enterprise infrastructure before it is justified.

## 1. Environments

Required environments:

- LOCAL
- CI
- DEV
- STAGING
- PROD

Optional later:

- short-lived preview environments for selected pull requests

Rules:

- each environment has isolated configuration, secrets and data
- production PII is not copied into lower environments
- staging must be production-like enough for migrations, E2E, security and smoke testing
- environment-specific behaviour must be explicit, not hidden in code branches

## 2. Runtime Topology

MVP target direction:

- Next.js web
- ASP.NET Core API
- background worker
- PostgreSQL
- private object storage for documents
- managed secret store
- centralized telemetry

Preferred Azure target to evaluate:

- Azure Front Door + WAF at the edge where justified for production
- Azure Container Apps for Web/API/Worker
- Azure Database for PostgreSQL
- Azure Blob Storage
- Azure Key Vault
- Application Insights / Log Analytics

Final hosting choice remains an ADR and should prioritize operational simplicity over theoretical scale.

## 3. Deployment Units

Web, API and Worker are separate deployable workloads even though the backend remains a modular monolith.

Benefits:

- web scales independently from API
- worker can retry/background process without tying up request workloads
- each workload can be rolled independently where compatibility permits

This is workload separation, not microservices.

## 4. Infrastructure as Code

Production infrastructure must be reproducible using IaC.

Tool choice remains between Bicep and Terraform until ADR.

IaC scope should include at minimum:

- compute
- PostgreSQL
- storage
- Key Vault
- identities/RBAC
- networking/ingress
- monitoring/alerts
- DNS/certificates where applicable

Manual production changes are exceptions and must be reconciled back into IaC.

## 5. Configuration and Secrets

Configuration is externalized by environment.

Rules:

- no production secrets in Git
- managed identity/workload identity preferred
- Key Vault or equivalent for secrets
- configuration validation at startup/deployment
- missing critical configuration fails fast
- feature/config toggles cannot bypass security invariants

## 6. Database Deployment

Database migrations are treated as release artifacts.

Before production deployment:

- migration generation reviewed
- pending-model-change check passes
- clean database migration test passes
- previous release -> target release migration test passes
- destructive changes explicitly reviewed
- backup/recovery path confirmed for risky changes

Risky changes use expand -> migrate/backfill -> contract rather than incompatible one-step changes.

## 7. Release Flow

Baseline flow:

PR/branch
-> CI quality/security gates
-> build immutable artifacts/images
-> deploy DEV
-> deploy STAGING
-> migration + smoke + E2E/accessibility/security validation
-> approval
-> PROD deployment
-> production smoke checks
-> observe telemetry

The exact GitHub Actions workflow will be defined during engineering platform implementation.

## 8. Artifact Integrity

- build once where practical and promote the same immutable artifact
- version container/images/releases by commit SHA or release identifier
- SBOM generated as the pipeline matures
- dependencies and container images scanned according to security baseline

## 9. Rollback and Roll-Forward

Application rollback must be possible for compatible releases.

Database changes default to roll-forward recovery because schema/data migrations may make binary rollback unsafe.

Every risky slice must document:

- application rollback compatibility
- database forward recovery path
- external integration/reconciliation impact

## 10. Deployment Safety

Production deployment should avoid unnecessary downtime.

MVP principles:

- health/readiness checks
- graceful shutdown
- schema/app compatibility during deployment
- staged traffic switching or revision-based rollback when supported by the selected platform
- no requirement for multi-region active-active deployment in MVP

## 11. Availability and Scaling

Start small and scale managed workloads based on evidence.

Baseline:

- stateless Web/API
- API horizontal scaling supported by platform
- Worker independently scalable
- PostgreSQL sized for measured workload
- connection pooling
- media/static asset caching/CDN where appropriate

Deferred until measured need:

- Redis
- Service Bus
- read replicas
- multi-region deployment
- Kubernetes
- dedicated search cluster

## 12. Document Storage

Production document storage is private.

- no public containers
- environment-isolated storage
- quarantine/validation workflow
- controlled short-lived access
- backup/retention aligned with privacy policy
- lower environments use synthetic/non-production documents

## 13. Observability at Deployment

Every production workload must expose enough telemetry to answer:

- did the release deploy successfully?
- are health/error rates abnormal?
- are database migrations healthy?
- is the outbox/worker processing?
- are payment/document integrations failing?

A release is not considered complete merely because the deployment command succeeded.

## 14. Production Access

- least privilege
- MFA for privileged human access
- no shared credentials
- production database/storage access restricted
- privileged access auditable
- emergency access procedure documented before launch

## 15. Backup and Recovery

Detailed RTO/RPO values remain a later launch decision, but MVP requires:

- managed PostgreSQL backups enabled
- document storage protection/retention appropriate to policy
- restore procedure documented
- restore tested before production launch
- backup access protected from ordinary application identities

## 16. Cost Control

MVP infrastructure should be intentionally small.

- managed services over operationally heavy platforms
- avoid always-on infrastructure with no demonstrated use
- basic cost monitoring/budgets before production
- scaling driven by telemetry, not forecast fantasies

## 17. Vertical Slice Deployment Contract

A slice introducing runtime or persistence changes must identify:

- workloads affected
- configuration/secrets added
- migration impact
- external integration impact
- health/telemetry evidence
- rollout/rollback implications
- environment test requirements

A slice should not introduce new infrastructure merely because it may be useful later.

## 18. Explicit MVP Non-Goals

- Kubernetes
- multi-region active-active
- elaborate blue/green orchestration if the hosting platform's revisions are sufficient
- dedicated message broker without demonstrated need
- distributed cache without evidence
- production-like infrastructure for every PR
- separate database server per module
- manual cloud configuration as the normal deployment method

## 19. Open Decisions

1. Container Apps vs other managed hosting target.
2. Bicep vs Terraform.
3. Azure region/data residency.
4. Exact Front Door/WAF production scope.
5. Production release traffic-switching strategy.
6. Exact RTO/RPO targets.
7. Environment sizing and budgets.
8. Preview-environment policy.
9. Production access/emergency-access implementation.

## Freeze Rule

Deployment architecture must remain proportional to the MVP. New infrastructure needs evidence from security, reliability, performance, operational, or business requirements; future possibility alone is insufficient.
