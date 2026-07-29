# NoorPath V2 Research Baseline

- Status: In progress
- Purpose: Record current authoritative external standards/platform facts before architecture, security, UX and deployment decisions are frozen
- Rule: A source being listed here does not automatically make a product/architecture choice. Each choice still requires a NoorPath decision/ADR where appropriate.

## 1. Software platform baselines

### .NET

Authoritative source: Microsoft .NET support policy, last updated 2026-07-14.

Verified current fact:

- .NET 10 is LTS, active support, with end of support 2028-11-14.
- Latest .NET 10 patch is 10.0.10, released 2026-07-14.
- Microsoft requires applications to remain current on released patch updates during support.
- Current .NET 10 SDK download line includes SDK 10.0.302.

V2 implication:

- Retain .NET 10 LTS as the baseline hypothesis.
- Pin and patch the SDK/runtime deliberately; do not freeze application dependencies to vulnerable initial patch versions.
- Define a recurring dependency/patch policy rather than treating framework version selection as a one-time decision.

Sources:

- https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core
- https://dotnet.microsoft.com/en-us/download/dotnet/10.0

### PostgreSQL

Authoritative source: PostgreSQL Global Development Group documentation.

Verified current fact:

- PostgreSQL 18 is the current supported major line.
- Current documentation is PostgreSQL 18.4.
- PostgreSQL 19 is still a development/beta line as of July 2026 and is not the production baseline.

V2 implication:

- PostgreSQL 18 remains the default production database hypothesis.
- Exact managed-service engine/patch policy, HA, backup, PITR, RTO/RPO and extension policy still require an architecture decision.

Sources:

- https://www.postgresql.org/docs/current/index.html
- https://www.postgresql.org/docs/current/release-18.html

### Next.js

Authoritative source: Next.js App Router production guidance, updated 2026-02-27.

Verified current guidance includes:

- shared layouts and `Link` navigation;
- deliberate rendering/data-fetching/caching choices;
- graceful error/404 handling;
- Content Security Policy consideration;
- Metadata/SEO support;
- TypeScript/type safety;
- production build verification;
- Core Web Vitals measurement;
- bundle analysis and dependency-size awareness.

V2 implication:

- Retain Next.js App Router + TypeScript as the web baseline hypothesis.
- Treat caching, rendering, security headers/CSP, SEO, bundle budgets and Web Vitals as explicit architecture/UX decisions, not framework defaults to accept blindly.

Source:

- https://nextjs.org/docs/app/guides/production-checklist

## 2. Application/API security baseline

### OWASP ASVS

Authoritative source: OWASP Application Security Verification Standard project.

Verified current fact:

- Latest stable ASVS is 5.0.0.
- OWASP recommends version-qualified requirement identifiers (for example `v5.0.0-x.y.z`) because identifiers may change between versions.

V2 implication:

- Use OWASP ASVS 5.0.0 as the primary technical application-security verification catalogue.
- Define a NoorPath target profile during the Security Baseline phase rather than asserting blanket conformance prematurely.
- Version-qualify ASVS references in requirements/tests.

Source:

- https://owasp.org/www-project-application-security-verification-standard/

### OWASP API Security

Authoritative source: OWASP API Security Top 10 2023.

Verified current fact:

- Authorization remains a dominant API risk.
- API1 covers Broken Object Level Authorization.
- API3 covers Broken Object Property Level Authorization/mass-assignment-style exposure.
- API5 covers Broken Function Level Authorization.
- API6 covers unrestricted access to sensitive business flows.
- API4 covers unrestricted resource consumption.
- API10 covers unsafe consumption of third-party APIs.

V2 implication:

NoorPath's API security model must explicitly address:

- resource/object ownership and tenant isolation;
- function/role permission checks;
- property allowlisting and response projection;
- anti-automation/abuse controls on sensitive workflows;
- resource/cost controls;
- third-party integration trust boundaries.

Sources:

- https://owasp.org/API-Security/editions/2023/en/0x11-t10/
- https://owasp.org/API-Security/editions/2023/en/0xa1-broken-object-level-authorization/

### NIST SSDF

Authoritative source: NIST CSRC.

Verified current state:

- NIST SP 800-218 SSDF Version 1.1 is final.
- NIST SP 800-218 Rev. 1 / SSDF Version 1.2 is an Initial Public Draft published 2025-12-17; it is not yet the final replacement baseline.

V2 implication:

- Baseline secure-SDLC governance on finalized SSDF 1.1.
- Track SSDF 1.2 changes during the reset and adopt only finalized changes or explicitly selected draft practices with clear status.

Sources:

- https://csrc.nist.gov/pubs/sp/800/218/final
- https://csrc.nist.gov/pubs/sp/800/218/r1/ipd

## 3. Privacy/data protection baseline

### GDPR principles and privacy by design/default

Authoritative source: European Commission GDPR guidance.

Verified guidance includes:

- lawfulness, fairness and transparency;
- purpose limitation;
- data minimisation;
- accuracy;
- storage limitation;
- integrity/confidentiality;
- privacy/data protection by design from the earliest design stage;
- privacy-protective defaults, including limited collection, shorter retention and need-to-know accessibility;
- pseudonymisation and encryption as example measures.

V2 implication:

Before passport/OCR/traveller/payment-support features are authorized, NoorPath must define:

- purpose and lawful basis per data category;
- data classification;
- minimisation;
- access model;
- encryption and secret/key ownership;
- retention/deletion;
- lower-environment data policy;
- auditability;
- processor/subprocessor boundaries;
- DPIA assessment/trigger.

Sources:

- https://commission.europa.eu/law/law-topic/data-protection/information-business-and-organisations/principles-gdpr_en
- https://commission.europa.eu/law/law-topic/data-protection/rules-business-and-organisations/obligations/what-does-data-protection-design-and-default-mean_en

## 4. Accessibility baseline

### WCAG

Authoritative source: W3C Web Accessibility Initiative.

Verified current state:

- W3C recommends using the latest WCAG 2 version.
- WCAG 2.2 is a W3C Recommendation and is also ISO/IEC 40500:2025.
- WCAG 2.2 adds requirements including Focus Not Obscured (Minimum), Dragging Movements, Target Size (Minimum), Consistent Help, Redundant Entry, and Accessible Authentication (Minimum).

V2 implication:

- Continue WCAG 2.2 AA as the baseline target.
- Treat accessibility as product/design/engineering acceptance, not only an automated axe scan.
- Design authentication, help/support placement, touch targets, focus visibility, repeated-entry flows and mobile interactions against WCAG 2.2 from the start.

Sources:

- https://www.w3.org/WAI/standards-guidelines/wcag/
- https://www.w3.org/WAI/standards-guidelines/wcag/new-in-22/

### European Accessibility Act

Authoritative source: European Commission / EUR-Lex.

Verified current fact:

- The European Accessibility Act includes e-commerce among covered services.
- Directive (EU) 2019/882 applies from 2025-06-28, subject to scope/exemptions and national implementation details.

V2 implication:

- NoorPath must perform a legal/compliance applicability assessment for the intended markets and operating entity rather than assuming WCAG alone resolves regulatory obligations.

Sources:

- https://commission.europa.eu/strategy-and-policy/policies/justice-and-fundamental-rights/disability/european-accessibility-act-eaa_en
- https://eur-lex.europa.eu/legal-content/EN/TXT/?uri=legissum:4403933

## 5. Payment security baseline

### PCI DSS

Authoritative source: PCI Security Standards Council.

Verified current fact:

- PCI DSS v4.0.1 is the current limited revision of the v4 standard; it clarified/corrected requirements without adding or deleting requirements.

V2 implication:

- Payment architecture must deliberately minimize NoorPath's cardholder-data environment and avoid storing card PAN/CVV where a provider-hosted/tokenized design can remove that need.
- Exact SAQ/scope/provider integration cannot be decided until the payment-provider and checkout architecture are selected.

Source:

- https://blog.pcisecuritystandards.org/just-published-pci-dss-v4-0-1

## 6. Web performance/user-experience baseline

Authoritative source: web.dev Core Web Vitals guidance.

Verified current 'good' thresholds measured at the 75th percentile, segmented for mobile/desktop:

- LCP <= 2.5 s;
- INP <= 200 ms;
- CLS <= 0.1.

V2 implication:

- Use these as baseline public-web UX performance objectives.
- Define NoorPath-specific API latency, page-weight, JavaScript, image and server-performance budgets separately after workload and device/network assumptions are researched.
- Use both lab and field/RUM evidence; lab testing does not replace field Web Vitals.

Source:

- https://web.dev/articles/vitals

## 7. Azure deployment hypotheses to research further

### Azure Container Apps

Authoritative source: Microsoft Learn.

Verified capabilities relevant to NoorPath:

- containerized API workloads;
- background/event-driven jobs;
- HTTP/event/CPU/memory/KEDA scaling;
- immutable revisions;
- multiple revisions/traffic splitting;
- internal ingress/service discovery;
- virtual-network integration;
- monitoring/logging integration.

Status: **architecture hypothesis only**. Container Apps is not yet approved as NoorPath's production host.

Sources:

- https://learn.microsoft.com/en-us/azure/container-apps/overview
- https://learn.microsoft.com/en-us/azure/container-apps/scale-app
- https://learn.microsoft.com/en-ca/azure/container-apps/revisions

### Azure Front Door WAF

Authoritative source: Microsoft Learn.

Verified capabilities relevant to NoorPath:

- managed WAF rules;
- custom rules;
- rate-limit rules;
- bot protection;
- geo/IP controls;
- monitoring/logging integration.

Status: **edge-security hypothesis only**. Tier, topology and rules require a production architecture/threat-model decision.

Source:

- https://learn.microsoft.com/en-us/azure/frontdoor/web-application-firewall

## 8. Research still required before architecture freeze

- Azure Database for PostgreSQL production HA/PITR/networking/maintenance constraints and cost model;
- Azure Blob private storage, malware scanning workflow, encryption, lifecycle and short-lived access patterns;
- managed identity, Key Vault, workload identity and secret rotation;
- OpenTelemetry/Application Insights/Log Analytics architecture;
- Azure Front Door private-origin options and network topology;
- IaC choice: Bicep vs Terraform;
- CI/CD deployment identity and environment promotion model;
- identity-provider options for customers, operators and privileged NoorPath staff;
- India payment-provider/current regulatory and webhook constraints for the actual launch market;
- WhatsApp Business provider/template/consent/retry requirements;
- OCR/provider/data-residency constraints;
- GDPR controller/processor model and DPIA applicability;
- EAA/local accessibility-law applicability by launch market/entity;
- threat modelling methodology and abuse-case catalogue;
- SBOM/signing/provenance/supply-chain controls;
- backup restore tests, RTO/RPO, DR and incident-response assumptions;
- expected traffic, booking concurrency, notification volume, document volume and cost envelopes.

## 9. Decision rule

A technology or standard enters the V2 architecture only after it is classified as one of:

- mandatory legal/regulatory requirement;
- normative security/accessibility requirement adopted by NoorPath;
- product requirement;
- architecture decision (ADR);
- implementation choice constrained by an approved capability/slice.

No current-version fact in this file authorizes implementation on its own.
