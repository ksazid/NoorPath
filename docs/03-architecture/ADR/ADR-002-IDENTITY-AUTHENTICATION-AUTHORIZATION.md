# ADR-002 — Identity, Authentication and Authorization Architecture

Status: Accepted for V2 foundation
Date: 2026-07-29

## Context
NoorPath serves multiple actor types: customers/booking owners, operator staff and NoorPath privileged staff. Authentication must remain vendor-replaceable, while authorization must enforce NoorPath-owned business scope such as operator membership, permission and resource ownership.

The MVP must support secure browser sign-in, API authorization, privileged-user MFA, account recovery, local/CI testing and future provider evolution without making provider claims the source of business authorization truth.

## Decision

### 1. External identity provider for human authentication
Use standards-based OpenID Connect / OAuth 2.0 authentication through an external identity provider. NoorPath does not build or store its own password credential system.

Preferred Azure-aligned MVP provider:
- Microsoft Entra External ID external tenant for customer-facing identities.
- Operator/NoorPath privileged identity integration may use the appropriate Entra workforce or External ID configuration during implementation, but API authorization remains provider-neutral.

Provider selection is an adapter/configuration concern. Application/domain code depends on NoorPath's internal principal model, not provider SDK types.

### 2. Internal Account identity
After successful authentication, NoorPath maps the trusted `(issuer, subject)` identity to an internal `AccountId`.

Provider claims answer primarily **who authenticated**. NoorPath-owned capabilities decide **what that person may do**.

Do not use email address as the durable identity key. Email/phone/profile attributes are mutable profile/contact facts.

### 3. Authorization model
Every consequential authorization decision follows:

`Authenticated Principal + Permission + Operator Scope + Resource Ownership + Resource State -> Decision`

Role names may group permissions for administration, but role membership alone is not sufficient authorization for operator-scoped or resource-scoped actions.

Authoritative ownership:
- Identity owns Account/security linkage.
- Operators owns operator membership, operator scope and operator-business permissions.
- Each resource-owning module owns resource ownership/state facts required for authorization.
- NoorPath platform privileges are explicitly controlled and auditable.

### 4. API authentication boundary
ASP.NET Core authentication validates incoming trusted tokens/sessions and creates a principal. API/application authorization translates that principal into a NoorPath authorization context.

Endpoints declare required permissions/policies, but owning application/domain handlers re-check resource scope/state where the decision depends on authoritative business data.

Never trust client-supplied `OperatorId`, `AccountId`, role or ownership data as authorization evidence.

### 5. Browser/session model
Use secure standards-based browser authentication. Exact Next.js integration pattern is implementation-level, but it must:
- avoid exposing long-lived credentials to browser JavaScript where avoidable;
- use secure, HttpOnly, SameSite cookies for server-managed sessions where that pattern is selected;
- protect state-changing cookie-authenticated requests against CSRF;
- rotate/revoke sessions appropriately after security-sensitive events;
- clearly distinguish unauthenticated (`401`) from authenticated-but-forbidden (`403`) API outcomes.

### 6. Privileged-user security
MFA is mandatory for privileged production operator/NoorPath users whose permissions can publish, access sensitive documents, act on payments/finance, change operator access or perform similarly consequential operations.

Step-up/re-authentication may be required for especially sensitive actions when risk analysis justifies it.

Customer MFA is not mandatory for MVP by default; account recovery and abuse protection remain required.

### 7. Local and CI authentication
Do not require a live cloud identity tenant for ordinary automated tests.

Provide a test-only authentication scheme/principal factory enabled only in test/local development configuration. It issues deterministic identities/claims sufficient to exercise:
- unauthenticated vs authenticated behaviour;
- customer identity;
- operator membership/scope;
- permissions;
- cross-operator denial;
- privileged scenarios.

The test scheme must be impossible to enable accidentally in production; startup/configuration validation must fail closed if insecure test authentication is configured in a production environment.

Integration/E2E tests verify NoorPath authorization logic with deterministic test identities. A smaller staging smoke suite verifies real provider integration.

### 8. Service-to-service/workload identity
Cloud workloads use managed/workload identity where available. Human credentials are never reused for services. Provider/client-credential tokens authenticate workloads, but application authorization still grants only the minimum service permissions required.

### 9. Claims discipline
Claims are normalized at the authentication boundary. Provider-specific claim names do not leak into domain modules.

Claims such as provider role/group membership may be inputs to identity mapping but are not automatically trusted as NoorPath operator-resource authorization unless explicitly mapped through a governed rule.

### 10. Deny by default and audit
Protected capabilities deny access unless an explicit policy permits it. Consequential authorization failures and privileged actions produce safe operational/audit evidence without logging tokens or sensitive identity data.

## Initial internal authorization primitives
VS-00 should establish only the minimum reusable concepts:
- `CurrentPrincipal` / `AccountId`
- permission identifier
- optional `OperatorId` scope
- resource-authorization requirement/handler pattern
- test principal factory

Do not create a generic policy engine, ABAC platform or permissions microservice for MVP.

## Initial MVP actor concepts
Keep these distinct even where one human may hold multiple roles:
- Customer / Booking Owner
- Traveller (not necessarily an authenticated Account)
- Operator Staff
- NoorPath Operations/Admin
- specialized finance/document/visa permissions only where the slice requires separation

## Rejected alternatives

### Custom username/password authentication
Rejected because credential security, recovery, MFA, abuse protection and lifecycle are better delegated to a mature identity platform.

### Provider roles as complete authorization model
Rejected because provider roles cannot safely encode NoorPath operator membership, resource ownership and dynamic domain state.

### `Customer` / `Admin` boolean authorization
Rejected because it cannot enforce cross-operator isolation or later permission separation safely.

### Live cloud IdP for every automated test
Rejected because it makes tests slow, brittle and dependent on external infrastructure.

### Build a full generic authorization platform now
Rejected as unnecessary MVP complexity.

## Consequences
Positive:
- authentication security is delegated to a mature standards-based provider;
- business authorization remains under NoorPath control;
- provider replacement is feasible because domain modules do not depend on provider SDK/claim shape;
- deterministic auth testing supports CI;
- operator/tenant isolation is a first-class boundary.

Trade-offs:
- identity mapping and authorization context require deliberate implementation;
- real-provider integration still needs staging verification;
- privileged MFA/user lifecycle configuration must be managed in the selected IdP.

## VS-00 verification
Before VS-00 exits:
1. API supports provider-neutral authentication configuration.
2. Internal principal mapping contract exists.
3. Test authentication works only in approved local/test environments.
4. Architecture/integration tests prove unauthenticated, forbidden, permitted and cross-operator-denied scenarios.
5. Production configuration cannot enable test auth.
6. No module reads raw provider claims to make resource authorization decisions.
7. Privileged MFA requirement is represented in deployment/identity configuration guidance.

## References
- Microsoft Entra External ID external tenants are Microsoft's CIAM platform for consumer/business-customer applications and provide standards-based app integration and sign-in flows.
- ASP.NET Core supports multiple authentication schemes and policy-based authorization, allowing production and controlled local/test schemes without changing domain logic.
- OWASP guidance requires strong separation of authentication, session management and access control, with sensitive-account protections and secure session handling.
