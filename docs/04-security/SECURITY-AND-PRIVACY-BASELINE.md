# NoorPath V2 Security & Privacy Baseline

Status: Draft baseline
Version: 0.1
Step: 10

## Purpose

This document defines the mandatory security and privacy controls that all NoorPath capabilities, APIs, admin workflows, documents, payments, deployments and engineering processes must satisfy before implementation reaches Definition of Ready.

Security is treated as a system property, not as an authentication feature.

## Standards Baseline

NoorPath uses the following external references as the current baseline:

- OWASP ASVS 5.0.0 as the application security verification baseline, primarily Level 2 with stronger controls applied to privileged identity, payments and identity documents where risk justifies it.
- OWASP API Security Top 10 (2023 edition) for API-specific threat coverage.
- PCI DSS 4.0.1 for payment-account-data security obligations and scope decisions.
- GDPR privacy-by-design/data-minimisation principles for personal and identity-document processing where applicable.

Exact legal/compliance applicability must be confirmed before production launch; this document defines the engineering baseline, not legal advice.

## Core Security Principles

1. Deny by default.
2. Authenticate the principal, authorize the action.
3. Least privilege for humans and workloads.
4. Tenant/operator isolation at every owning capability boundary.
5. Sensitive-data minimisation in storage, APIs, logs, events, analytics and notifications.
6. No secrets in source, client bundles, ordinary configuration or logs.
7. Consequential privileged actions are auditable.
8. External events/providers are untrusted until verified and reconciled.
9. Security invariants are not operator-configurable.
10. Uncertain authorization, payment, document or integrity states fail safely.

## Identity & Authentication

- Standards-based identity provider preferred over custom credentials.
- Secure account recovery and session management.
- Rate limiting and abuse protection for sign-in/recovery.
- MFA mandatory for privileged production access.
- Reauthentication/step-up for high-risk operations where justified.
- Managed/workload identity preferred for service-to-service access.

## Authorization & Tenant Isolation

Authorization evaluates Identity + Permission + Operator Scope + Resource Ownership + Resource State.

- Server-side object-level and function-level authorization on every protected action.
- Route IDs and booking references are not credentials.
- Cross-operator access denied by default.
- Clients cannot choose another tenant/operator scope without explicit authority.
- Cross-tenant read/write tests are mandatory.
- Support/admin sensitive access is least privilege and auditable.

## API & Web Security

- HTTPS only.
- Strict request validation and output minimisation.
- Protection against mass assignment/property overposting.
- Parameterised database access.
- Secure CORS.
- CSRF protection where cookie-authenticated state changes exist.
- Secure cookies where cookies are used.
- CSP and appropriate browser security headers.
- Contextual output encoding/XSS prevention.
- SSRF protection where server-side URL access exists.
- Request/body/resource limits and rate limits on sensitive flows.
- Consistent Problem Details without sensitive leakage.
- API inventory/version/deprecation governance.

Sensitive flows receive explicit abuse analysis: account recovery, inventory holds, checkout, payments, document upload, visa updates and admin overrides.

## Secrets & Cryptography

- Production secrets in managed secret storage such as Azure Key Vault.
- Managed Identity where possible.
- Environment-separated secrets.
- Rotation and emergency revocation procedures.
- Current platform cryptographic libraries/defaults; no custom crypto.
- CI secret scanning mandatory.

## Data Classification

- Public: published package/operator/support content.
- Internal: operational configuration/workflow data.
- Confidential: accounts, bookings, travellers, commercial/financial references, support context.
- Highly Sensitive: passport/identity-document content and extracted data, authentication/payment secrets and key material.

Every persisted/API/event/log field must be classifiable.

## Document & Passport Security

- Private storage only; no permanent public URLs.
- Short-lived authorised access.
- Purpose-bound access and access auditing.
- Encryption in transit/at rest.
- Type, size, MIME/signature and malware validation before reviewer trust.
- Quarantine unsafe uploads.
- No document/passport content in generic logs, analytics or integration events.
- Retention/deletion policy required before production document storage.
- No production documents copied to lower environments.
- OCR remains V1.x; extracted sensitive data inherits strong protection.

## Payment Security

NoorPath minimises PCI scope through provider-hosted/tokenised flows.

- Never persist PAN/CVV.
- Browser success callback never establishes settlement.
- Webhooks authenticated/verified and replay-safe/idempotent.
- Provider credentials server-side only.
- Append-only financial ledger semantics.
- Permissioned/audited refunds and reconciliation.
- No sensitive card/authentication data in logs/analytics.

## File Upload Security

Uploads require authenticated intent, server-controlled size/type policy, MIME/signature validation, opaque storage identity, quarantine, malware scanning, safe filenames, non-executable serving, short-lived access, audit and safe resubmission UX.

## Logging & Audit

Ordinary logs never contain passwords, tokens, raw payment secrets/card data, passport contents, signed private-document URLs or unnecessary PII.

Security telemetry covers authentication anomalies, authorization denials, privileged role changes, operator approval/suspension, payment/reconciliation exceptions, document access, unsafe-upload patterns, manual overrides and identity/secret failures.

## External Integrations

Each integration defines authentication, credential scope, validation, timeout, retry, idempotency, rate limits, reconciliation, data classification, redaction and callback authenticity. Third-party responses are never trusted solely because the provider is known.

## Privacy Engineering

Before production NoorPath establishes data purpose/minimisation, retention/deletion, correction/export handling where applicable, processor/subprocessor inventory, region/residency decisions, backup implications, support access and incident/breach ownership.

## Environment & Infrastructure Security

LOCAL, CI, DEV, STAGING and PROD are isolated. Production secrets/PII are not standard lower-environment test inputs. Infrastructure is governed through IaC, least-privilege cloud RBAC/workload identity, restricted data-store access, protected backups and deployment/startup configuration validation.

## Secure SDLC & CI Gates

Progressively mandatory controls include threat modelling, peer review, secret scanning, SAST, dependency scanning, SBOM, IaC/container scanning where applicable, authorization/API security tests, real PostgreSQL integration tests, upload-security tests, webhook/idempotency tests, staging DAST where practical and vulnerability remediation policy.

Mandatory security-control failures block release.

## MVP Threat Priorities

1. Customer reads another customer's booking/traveller/document data.
2. Operator staff accesses another operator's resources.
3. Privileged role has excessive access.
4. Account/session/recovery abuse.
5. Checkout identifier/price/occupancy manipulation.
6. Inventory-hold abuse/exhaustion.
7. Duplicate/replayed payment events.
8. Fake browser payment confirmation.
9. Malicious file reaches staff/execution context.
10. Passport object becomes publicly accessible.
11. Sensitive document URL/data leaks through telemetry.
12. Forged visa/document/admin state changes.
13. SSRF through integrations.
14. Secret exposure through source/build/runtime telemetry.
15. Third-party outage/compromise creates unsafe state.

## Security Definition of Ready

Each slice defines actor/permissions, tenant/resource ownership, data classification, sensitive fields, threat/abuse cases, authorization, validation/rate limits, redaction, audit, trust boundaries, privacy/retention requirements and security acceptance tests.

## Security Definition of Done

For each delivered slice: server-side authorization and tenant tests pass; secrets and sensitive logs are absent; security/dependency scans satisfy policy; idempotency/trust-boundary tests pass; audit/telemetry is verified; and threat-model controls have evidence.

## Open Security Decisions

Still to be frozen through architecture/security work: identity provider, exact role/permission matrix, session/reauthentication policy, physical tenant isolation, payment provider/PCI scope, document storage/encryption/access mechanism, malware scanner, retention periods, data residency, WAF/rate limits/CSP specifics, incident/remediation SLAs, penetration-test cadence and backup-security implementation.

## Freeze Rule

No product, design, implementation, admin tool or AI agent may weaken these baseline invariants without an explicit security decision, risk analysis and approval.
