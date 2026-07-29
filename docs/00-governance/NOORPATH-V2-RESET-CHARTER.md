# NoorPath V2 Product and Architecture Reset Charter

- Status: Active planning baseline
- Branch: `planning/noorpath-v2-baseline`
- Scope: Product, domain, architecture, security, UX/design, engineering platform, deployment, operations, and delivery governance
- Implementation status: Feature development is frozen while this baseline is produced

## 1. Purpose

NoorPath is pausing feature expansion to establish a complete product and engineering baseline before further implementation. The objective is not to rewrite the application or discard approved work. The objective is to ensure that future implementation proceeds from explicit product rules, domain ownership, state transitions, security controls, UX specifications, operational requirements, and executable quality gates.

The reset exists to prevent implementation-time discovery of foundational decisions such as business lifecycle semantics, data ownership, pricing/inventory behaviour, authorization boundaries, migration strategy, environment requirements, and design authority.

## 2. Freeze rule

Until the V2 baseline is approved:

- no S03 or later feature implementation starts;
- no speculative product behaviour is added;
- no new framework, library, infrastructure service, or architectural abstraction is introduced merely for convenience;
- existing S02 work may be inspected, stabilized, and reconciled, but it is not automatically authoritative for the V2 domain model;
- critical security/build/repository defects may be corrected when necessary to preserve the project;
- planning, research, architecture, design, testing, and operational specifications may proceed.

Open feature work must remain non-mergeable by governance until its requirements and implementation have been reconciled against the V2 baseline.

## 3. Product delivery model

NoorPath adopts dual-track delivery:

```text
DISCOVERY
  -> PRODUCT MODEL
  -> DOMAIN MODEL
  -> ARCHITECTURE + SECURITY
  -> UX / DESIGN
  -> CAPABILITY SPECIFICATION
  -> DEFINITION OF READY
  -> LOOP / VERTICAL-SLICE ENGINEERING
  -> VERIFICATION
  -> RELEASE
  -> MEASURE
```

Loop engineering remains the preferred implementation method. It is not the product-definition method. A slice may enter implementation only after its Definition of Ready is satisfied.

## 4. Governing model to establish

The V2 baseline will define, in order:

1. product charter, personas, journeys, capability map, scope, business rules, and success measures;
2. domain map, ownership, data dictionary, state machines, domain events, configuration boundaries, and cross-capability dependencies;
3. architecture, API conventions, persistence, integration, scalability, reliability, and ADRs;
4. identity, RBAC/ABAC/resource ownership, tenant isolation, privacy, payment security, document security, threat modelling, and security verification;
5. information architecture, customer/admin journeys, brand, design system, accessibility, responsive behaviour, content rules, and interaction/motion rules;
6. environments, dependency policy, database migration policy, testing strategy, CI/CD, release controls, and AI-agent/plugin governance;
7. observability, SLOs, backup/restore, disaster recovery, incident response, runbooks, and production readiness;
8. capability sequencing, slice decomposition, traceability, Definition of Ready, Definition of Done, and release acceptance.

## 5. Intended capability architecture

The initial capability map to validate is:

- Identity & Access
- Operators
- Catalogue
- Pricing
- Inventory
- Traveller & Family
- Booking
- Payments & Refunds
- Documents & OCR
- Visa
- Notifications
- Journey
- Support
- Audit & Compliance
- Reporting & Analytics

This list is a hypothesis until the Product Capability Map is approved. Capability boundaries must be validated against the PRD, TRD, real operator workflows, customer journeys, regulatory obligations, and failure scenarios before implementation decisions are frozen.

## 6. Architecture direction to validate

The current modular-monolith direction remains the default hypothesis:

- ASP.NET Core .NET modular monolith;
- Next.js customer/staff web surfaces;
- PostgreSQL transactional system of record;
- strict module ownership and inward dependencies;
- no module directly queries or mutates another module's tables;
- explicit application contracts and transactional/outbox events for cross-module effects;
- infrastructure extracted into separate services only when evidence justifies operational separation.

Microservices are not the default target. Any future extraction requires an ADR with a demonstrated scaling, organizational, isolation, deployment, or reliability need.

## 7. Design authority

NoorPath's approved Landing and Package references preserve the visual identity and remain protected design evidence. The design direction must retain the essence of Makkah, Madinah, the Haramain, and Saudi visual character without becoming decorative, generic, or culturally superficial.

Design authority is layered:

1. approved product requirements and business rules;
2. approved brand/visual-cultural baseline;
3. `design-system/MASTER.md` and approved design decisions;
4. approved Figma components, patterns, journeys, and screens;
5. machine-readable design tokens;
6. production components and feature implementation.

Specialist tools have bounded roles:

- Figma: canonical component/screen workspace and approved visual specification;
- UI UX Pro Max: usability, information architecture, accessibility, responsiveness, interaction and heuristic review;
- Impeccable: refinement, hierarchy, spacing, typography, content stress-testing, consistency, and design debt review;
- Emil design-engineering guidance: purposeful interaction/motion, easing, gestures, feedback, performance, and reduced-motion behaviour;
- Ponytail: implementation simplicity after product/architecture/design decisions are approved.

No plugin or agent may redefine NoorPath's brand, product policy, security boundary, domain model, or architecture without an explicit approved decision.

## 8. Design principles to preserve

- sacred calm plus operational confidence;
- factual trust, never manufactured urgency;
- authentic and licensed Haramain/Saudi imagery;
- ivory, Kaaba black, restrained Kiswah gold, Madinah green, and Saudi sandstone as the established direction unless formally changed;
- customer surfaces prioritize reassurance, journey clarity, price/status transparency, and human support;
- admin surfaces prioritize state, accuracy, exceptions, safe actions, auditability, and operational efficiency;
- mobile is designed intentionally, not produced by shrinking desktop;
- WCAG 2.2 AA is the minimum accessibility target;
- every data-driven experience specifies loading, empty, error, offline, retry, permission, conflict/stale, success, long-content, and localization behaviour as applicable;
- motion communicates state, causality, hierarchy, or feedback and is never decorative by default.

## 9. Research requirement

Durable decisions must be researched against primary or authoritative sources where relevant. The baseline must cover at minimum:

- current supported .NET, Next.js, PostgreSQL, browser and package-tooling baselines;
- OWASP ASVS and API Security guidance;
- NIST secure-software-development guidance;
- GDPR/privacy-by-design, data minimization, retention and DPIA requirements applicable to the operating model;
- payment-provider architecture and PCI DSS scope reduction;
- WCAG 2.2 and applicable accessibility obligations;
- Azure architecture, networking, identity, secrets, managed PostgreSQL, storage, observability, WAF, backup/restore and disaster recovery;
- supply-chain security, SBOM, dependency scanning and infrastructure-as-code practices;
- performance budgets, Core Web Vitals, capacity/load assumptions and resilience testing.

Research findings must distinguish normative requirements, recommendations, product decisions, architectural hypotheses, and implementation choices.

## 10. Source-of-truth rule during reset

No existing document is silently discarded or treated as automatically correct. Existing PRD/TRD/ADRs/design artifacts/slice specifications/code are evidence to audit.

When sources conflict:

1. record the conflict;
2. identify its impact;
3. determine which decision-maker owns resolution;
4. resolve it explicitly;
5. update all affected baselines and traceability;
6. only then authorize implementation.

## 11. Required V2 document set

Target structure:

```text
docs/
  00-governance/
  01-product/
  02-domain/
  03-architecture/
  04-security/
  05-design/
  06-engineering/
  07-operations/
  08-capabilities/
  09-slices/
```

Key artifacts include Product Capability Map, PRD V2, Business Rules, Domain Map, Data Dictionary, State Machines, Domain Events, Configuration Model, TRD V2, Module Ownership, API Standards, Data/Integration/Scalability/Reliability architectures, Security Baseline, Threat Model, Identity/RBAC, Privacy and Data Classification, Brand/IA/Journey/Accessibility specifications, MASTER/tokens/components, Environments, Database Migration Policy, Testing Strategy, CI/CD, Dependency Policy, AI Agent Governance, Observability, Backup/Restore, Disaster Recovery, Release Checklist, capability specifications and slice contracts.

## 12. Definition of Ready direction

Every implementation slice will ultimately require explicit evidence for:

- capability and requirement IDs;
- user/persona objective;
- business rules;
- affected state transitions;
- domain/data ownership;
- module and integration dependencies;
- authorization/tenant/privacy classification;
- API and error contracts;
- UX/design artifacts and complete UI states;
- accessibility and responsive behaviour;
- persistence/migration impact;
- concurrency/idempotency/failure handling where applicable;
- observability;
- executable test strategy and available test environment;
- external dependency readiness;
- acceptance evidence.

## 13. Definition of Done direction

Completion requires all applicable product, architecture, security, data, migration, unit, integration, contract, E2E, accessibility, visual, performance, resilience, observability, documentation and release gates to pass against the committed revision. 'Implemented but unable to execute the required verification' is not Done.

## 14. Immediate execution order

1. preserve/freeze current implementation state;
2. inventory every authoritative and supporting source;
3. produce Gap & Conflict Register;
4. build Product Capability Map;
5. rebuild PRD;
6. build Domain Model and Data Dictionary;
7. define state machines and domain events;
8. build Security/Privacy/Identity baseline;
9. define NFRs;
10. finalize system/data/integration architecture;
11. finalize IA, journeys, brand/design governance and design system;
12. finalize engineering platform, environments, migrations, testing and CI/CD;
13. finalize deployment, observability, recovery and operations;
14. rebuild capability roadmap and slice decomposition;
15. reconcile existing implementation against V2;
16. resume feature implementation only after approval.

## 15. Approval boundary

This charter authorizes planning and audit work only. It does not approve a new product capability, production architecture, cloud topology, third-party provider, pricing rule, security exception, UI redesign, or feature implementation by itself.
