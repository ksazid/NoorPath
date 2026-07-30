# VS-01 — Operator Access

Status: Implementation review; environment-limited verification incomplete

## Outcome

An approved operator staff principal can authenticate and enter only the NoorPath administration capabilities allowed by an active membership, explicit permission, operator scope and operator state.

## Traceability

- `INV-ID-003`–`INV-ID-006`
- `INV-OP-001`–`INV-OP-003`
- ADR-0001 — Modular Monolith with Vertical Slices
- ADR-001 — Module, PostgreSQL Schema, DbContext and Migration Ownership
- ADR-002 — Identity, Authentication and Authorization Architecture

No stable approved PRD requirement ID was found; none is invented here.

## Included

- provider-neutral bearer authentication configuration and normalized internal `AccountId`;
- deterministic Development/Test authentication that fails startup elsewhere;
- Operators-owned operator state, active membership and explicit `operator.admin.access` permission;
- server-derived operator scope and deny-by-default authorization;
- protected operator-access API with safe Problem Details and correlation context;
- authorized, unauthenticated, forbidden, loading and retryable-error web states;
- Operators-owned PostgreSQL schema, DbContext and migration lineage.

## Excluded

PackageTemplate, PackageVersion, departures, pricing, inventory, publication, operator lifecycle mutations and every Catalogue persistence change are excluded.

## Access rule

Access requires an authenticated internal AccountId, active membership, `operator.admin.access`, and an `Approved` operator. Draft, PendingApproval, Rejected, Suspended and Deactivated operators are denied. Client-supplied scope never grants access.

## API

`GET /api/v1/operator/access` returns the current safe operator context. It returns `401` when unauthenticated and `403` when membership, permission or operator state does not allow access.

## Migration

The additive Operators migration creates `operators.operators`, `operators.operator_memberships`, and `operators.operator_membership_permissions`. It does not seed production identities or change Catalogue.

## Completion evidence

Unit, PostgreSQL integration, API authorization, architecture, web, E2E, accessibility, responsive, migration-integrity, formatting and build checks are required. Visual completion additionally requires screenshot comparison and Product Owner acceptance.
