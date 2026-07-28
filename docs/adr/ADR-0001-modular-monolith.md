# ADR-0001: Modular Monolith with Vertical Slices

- Status: Accepted
- Date: 2026-07-28

## Context

The pilot needs fast, safe delivery by a small team while preserving clear business boundaries around packages, bookings, payments, documents, fulfilment, and operations.

## Decision

Use one deployable ASP.NET Core modular monolith. Organise each module by vertical slice and enforce Clean Architecture dependency rules. Use selective CQRS. PostgreSQL is the transactional system of record.

Generic repositories and generic unit-of-work abstractions are prohibited. A focused repository is allowed only when it expresses aggregate intent or supports complex transactional loading.

## Consequences

- Contract, schema, API, UI, and test changes can remain atomic.
- Operational and hosting cost stays low.
- Module boundaries must be architecture-tested to prevent accidental coupling.
- A module can be extracted later only when measured scaling or ownership pressure justifies it.

