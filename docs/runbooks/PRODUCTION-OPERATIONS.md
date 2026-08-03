# NoorPath Production Operations Runbook

## Purpose

This runbook defines the minimum safe response for NoorPath pilot incidents. It does not authorize production access, deployment, refund execution, direct database correction or disclosure of customer data.

Every incident record must capture:

- UTC start time;
- exact deployed commit SHA;
- environment;
- customer/operator impact;
- correlation identifiers using opaque resource IDs only;
- incident owner and escalation target;
- actions taken and their results;
- recovery verification;
- follow-up owner.

## Global safety rules

1. Do not edit module tables directly to force a business outcome.
2. Do not retry payment, confirmation, inventory release or refund operations outside their idempotent module-owned commands.
3. Do not paste credentials, tokens, provider payloads, passport details, traveller details or payment data into tickets, chat or logs.
4. Do not enable production refunds, document storage or external notifications without the corresponding approved configuration and credentials.
5. Stop and escalate when the exact deployed SHA, affected account scope or authoritative state cannot be established.
6. A rollback changes application code only. It must not erase committed migrations, bookings, payments, documents, visa cases, cancellations, refunds or audit facts.

## Severity guide

- **SEV-1:** broad outage, security/privacy event, financial duplication risk, inventory oversell risk or unrecoverable data-integrity concern.
- **SEV-2:** critical journey unavailable for a material subset of users, repeated confirmation/refund failures or operator processing blocked.
- **SEV-3:** isolated recoverable case with a safe workaround and no evidence of data or financial corruption.

The Product Owner or delegated incident authority decides customer communication and go/no-go status. Suspected security or privacy events are escalated immediately.

## API readiness or database outage

### Trigger

- `/health/ready` returns `503`;
- database connection failures increase;
- Render reports the service unhealthy;
- critical APIs return dependency errors.

### Diagnose

1. Confirm `/health/live` separately from `/health/ready`.
2. Record the deployed SHA and Render deployment identifier.
3. Check Neon/PostgreSQL availability and connection limits without exposing the connection string.
4. Check whether a migration or configuration change preceded the outage.
5. Inspect privacy-safe logs by correlation ID and error category.

### Safe actions

- restore the database dependency or connection configuration;
- scale down non-essential diagnostic traffic;
- rollback application code only when the previous build is compatible with the current schema;
- use the approved backup/restore procedure only under the restore authority.

### Prohibited actions

- deleting migration-history rows;
- dropping schemas or databases;
- changing booking/payment state manually;
- restoring over production without explicit restore authorization.

### Recovery verification

- `/health/live` returns `200`;
- `/health/ready` returns `200` consistently;
- representative public, customer and operator reads succeed;
- error rate returns below the approved threshold;
- no pending migration or model drift is reported.

## Migration failure

### Trigger

- application startup fails during migration;
- migration validation fails;
- production readiness reports schema drift.

### Safe actions

1. Stop further deployment attempts.
2. Preserve logs and the exact candidate SHA.
3. Determine whether any migration committed before failure.
4. Prefer a forward corrective migration when data changes have committed.
5. Roll back application code only when it remains compatible with the resulting schema.

Never edit an applied migration or delete migration history to make validation pass.

## Authentication or authorization outage

### Trigger

- sign-in fails broadly;
- valid customers or operators receive unexpected denial;
- unauthorized access is suspected.

### Safe actions

- verify Auth0 authority, audience, callback URLs and environment configuration;
- confirm server time and identity-provider availability;
- inspect authorization failures using opaque account/operator identifiers;
- disable the affected privileged surface if isolation cannot be trusted.

Do not switch production to test authentication or bypass authorization middleware.

## Payment or confirmation exception

### Trigger

- payment provider callback verification fails;
- settled payment does not produce confirmation;
- confirmation exception queue grows;
- duplicate financial effects are suspected.

### Safe actions

1. Establish authoritative provider and NoorPath payment facts.
2. Use the existing idempotent reconciliation or confirmation-recovery command.
3. Preserve the immutable commercial snapshot and original payment facts.
4. Escalate before any customer-facing financial promise when provider state is unclear.

Do not create a second booking/payment attempt to repair an existing settled transaction.

## Inventory inconsistency

### Trigger

- availability becomes negative;
- a confirmed booking has no committed inventory;
- a cancelled booking appears to release inventory more than once.

### Safe actions

- stop publication or new holds for the affected departure when oversell risk exists;
- compare Inventory-owned hold, commitment and release facts with booking identifiers;
- invoke only the Inventory-owned recovery command;
- record the exact affected departure and booking IDs.

Do not edit capacity, holds or release rows directly to hide the inconsistency.

## Document storage or malware-scanning failure

### Trigger

- uploads fail;
- scanner is unavailable;
- quarantine/review transitions are delayed;
- signed access cannot be generated safely.

### Safe actions

- keep document storage fail-closed when scanner or private storage is unavailable;
- prevent operators from treating unscanned content as approved;
- use safe customer language and retry guidance;
- verify retention/legal-hold workers after recovery.

Never expose bucket objects publicly or download untrusted content outside the approved viewer path.

## Visa processing failure

### Trigger

- governed transition is rejected unexpectedly;
- customer status is stale;
- operator queue cannot load or update.

### Safe actions

- verify operator scope, permission, expected version and allowed transition;
- reload current state before retry;
- use the Visa-owned correction path with a required reason;
- confirm customer projection after recovery.

Do not skip terminal-state protection or modify visa history.

## Cancellation or refund failure

### Trigger

- cancellation remains recovery-required;
- inventory release fails after booking cancellation;
- refund provider submission/callback is delayed or fails;
- reconciliation indicates a mismatch.

### Safe actions

1. Treat booking cancellation, inventory release and refund settlement as separate facts.
2. Verify the immutable policy/entitlement snapshot and authoritative settled amount.
3. Use the owning module's idempotent recovery action.
4. Keep production refund execution disabled when provider approval or credentials are incomplete.
5. Communicate only confirmed customer-safe refund state.

Never type an arbitrary refund amount or erase the original payment.

## Deployment failure and rollback

### Trigger

- production readiness does not recover after deployment;
- smoke tests fail;
- elevated errors begin with the new SHA.

### Safe actions

1. Stop the release and record the failed SHA.
2. Confirm whether migrations committed.
3. Obtain rollback-authority approval.
4. Restore the previous known-good application build through Render.
5. Do not reverse committed migrations unless a separately reviewed forward recovery plan exists.
6. Repeat readiness and smoke verification.

### Recovery verification

- deployed SHA matches the intended rollback build;
- readiness is stable;
- critical public/customer/operator entry points work;
- no increase in payment, confirmation, inventory or refund exceptions;
- incident evidence is attached to the release record.

## Backup and isolated restore

1. Confirm restore authority and target environment.
2. Create or select the approved backup without logging credentials.
3. Restore only into an isolated database first.
4. Verify schemas, migration histories and critical invariant queries.
5. Start the application against the restored database and verify readiness.
6. Record backup timestamp, restore start/end, achieved recovery point and achieved recovery time.
7. Production replacement requires a separate explicit decision and maintenance plan.

## Customer communication

Customer communication must be approved by the Product Owner or delegated incident authority and must:

- state the confirmed impact without speculation;
- avoid internal identifiers, provider payloads and sensitive data;
- provide a safe next action or support reference;
- avoid promising payment/refund settlement before authoritative confirmation;
- record the message and audience in the incident evidence.

## Closure

An incident closes only when:

- the service and affected journey are verified;
- authoritative domain state is reconciled;
- monitoring is stable for the approved observation period;
- customer/operator follow-up is assigned;
- evidence and corrective actions are recorded;
- any accepted residual risk has an owner and review date.
