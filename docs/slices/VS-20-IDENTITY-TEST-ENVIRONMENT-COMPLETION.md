# VS-20 — Identity Test Environment Completion

Status: Implementation

## Objective

Complete the Auth0-enabled NoorPath release environment so every protected customer, operator and platform route can be validated with a real identity. The immediate defect is that the Documents persistence migration exists in source but is not discoverable or protected by the module migration registry.

## Why this slice is needed

The real Auth0 smoke-test account was successfully provisioned as:

- an authenticated customer owning demo booking `DEMO-LKO-001`;
- an active member of approved operator `demo-noorpath-operator`;
- a member with `operator.admin.access`, `operator.documents.review`, `operator.support.manage` and `operator.visa.process`;
- an explicitly allow-listed Platform Administrator and publication approver.

During live validation, the shared Neon database was found to have no `documents` schema. `DocumentsDbContext.Database.MigrateAsync()` runs at API startup, but the existing migration lacks standard generated EF metadata and the Documents module is absent from `delivery/modules.json`. Consequently EF reports the database as current while Customer Documents, Operator Documents and Operational Support depend on tables that do not exist.

## Scope

- register `DocumentsDbContext` in `delivery/modules.json`;
- replace the undiscoverable hand-written migration with standard generated EF migration metadata;
- add the generated migration designer and one frozen model snapshot;
- validate model parity and migration application through the existing permanent registry workflow;
- apply the certified migration to the NoorPath release database;
- resume the previously blocked real-identity route tests.

## Deliverables

- registered Documents migration module;
- generated EF migration, designer and model snapshot;
- fresh-database migration evidence;
- exact-head CI and governance evidence;
- Neon rollback branch retained for recovery;
- deployed release API evidence;
- customer/operator/platform identity verification matrix.

## User flows

### Customer

1. Open an owned My Journey deep link.
2. Authenticate through Auth0 Google sign-in.
3. Return to the requested journey.
4. Open journey, confirmation, payment, documents, visa and cancellation status.
5. Attempt a foreign booking and receive safe not-found behavior.

### Operator

1. Open an operator deep link.
2. Authenticate through Auth0.
3. Resolve active membership and approved operator state.
4. Open package, departure, document, visa, cancellation and support workspaces according to explicit permissions.

### Platform administrator

1. Authenticate through Auth0.
2. Resolve the normalized NoorPath account ID.
3. Pass the explicit platform administrator allow-list.
4. Open administrator and publication-review surfaces.

## Backend and data changes

This slice creates only the already-approved Documents persistence objects represented by `DocumentsDbContext`:

- document requirements;
- document submissions;
- append-oriented document audit records;
- existing indexes defined by the EF model.

It does not add a new domain model or alter document-policy decisions.

Test-environment provisioning already performed outside source control:

- Neon rollback branch `backup-before-identity-access-test-2026-08-04`;
- active all-permission operator membership for the normalized Auth0 account;
- ownership of `DEMO-LKO-001` assigned to that account;
- Delhi and Mumbai demo journeys preserved as foreign-account fixtures;
- platform administrator and publication-approver allow-lists updated in managed Render configuration.

## UI/UX changes

None.

No page component, route, copy, layout, token, styling rule, responsive behavior or interaction pattern is changed by VS-20.

## Security and privacy

- Auth0 secrets, access tokens and database credentials remain in managed providers and are never committed.
- Test authentication headers remain prohibited in Production.
- Operator access still requires an active membership, Approved operator state and explicit permission.
- Platform access remains a separate explicit allow-list.
- Foreign-account booking access must continue returning safe not-found behavior.
- No real passport or customer document is uploaded.
- Document production storage remains disabled.
- The all-access identity is a smoke-test account, not a least-privilege validation substitute.

## Explicit exclusions

- customer or staff UI redesign;
- Auth0 tenant redesign, phone OTP, password or account recovery;
- operator-membership administration UI;
- production document storage or malware-infrastructure enablement;
- payment, refund or cancellation execution;
- document, visa, booking or commercial policy changes;
- real customer production launch.

## Acceptance criteria

1. Documents is present in the migration registry with a dedicated test database and environment key.
2. The Documents migrations directory contains one generated designer for each migration and exactly one frozen model snapshot.
3. `dotnet ef migrations has-pending-model-changes` succeeds for `DocumentsDbContext`.
4. A fresh registered Documents database applies every migration successfully.
5. API startup applies the Documents migration to Neon and the required tables exist.
6. Customer Documents, Operator Documents and Operational Support no longer fail due to missing tables.
7. The real Auth0 account can reach its owned journey and all granted operator/platform surfaces.
8. Foreign demo journeys remain inaccessible to the real customer account.
9. Unauthenticated and unauthorized outcomes remain fail-closed.
10. No UI file or unrelated domain behavior changes.

## Testing

- delivery manifest and module-registry validation;
- formatting and static analysis;
- .NET build and tests;
- EF migration metadata validation;
- pending-model-change validation;
- fresh PostgreSQL migration application;
- API startup and readiness checks;
- schema existence verification in Neon;
- real Auth0 customer deep-link and return-to smoke test;
- operator membership and permission smoke tests;
- platform administrator allow-list smoke test;
- foreign-account safe-not-found test;
- Render and Vercel runtime-log review.

## Dependencies

- VS-12 Documents domain implementation;
- VS-17 production-readiness controls;
- VS-19 customer shell adoption;
- Auth0-enabled `noorpath-release` Vercel project;
- Render `noorpath-api` service;
- NoorPath Neon project and rollback branch.

## Risks and decisions

- The migration repair must be generated from the current EF model rather than manually imitating generated metadata.
- The current Neon rollback branch must be retained until live identity tests pass.
- A single all-access account validates positive access only. Negative and cross-account checks remain mandatory.
- Any commit after certification invalidates exact-head evidence.

## Delivery boundary

- Branch: `hotfix/vs20-documents-migration-discovery`
- PR title: `VS-20: Restore document migration discovery for identity testing`
- Merge requires Product Owner approval of the exact certified SHA.
- Deployment is restricted to NoorPath's release/test Vercel, Render and Neon environment.
- No real customer production launch is authorized.
