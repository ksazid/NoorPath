# NoorPath Pilot Go/No-Go Record

## Current status

**NOT READY — release candidate and Product Owner production decisions are not yet complete.**

This record must be completed for one exact unchanged commit SHA. Certification or merge does not authorize production deployment.

## Release candidate

- Release ID: `noorpath-pilot-v1`
- Exact SHA: _unassigned_
- Certification environment: _unassigned_
- Certification started UTC: _unassigned_
- Certification completed UTC: _unassigned_
- Release operator: _unapproved_
- Rollback authority: _unapproved_

## Release scope

Release scope is VS-00 through VS-17 as registered in `delivery/releases/pilot-v1.json`.

Deferred capabilities must remain truthful and visible in the release configuration. At minimum, production refund execution and document storage remain disabled until separately approved and configured.

## Required evidence

- [ ] Slice Governance passed on the exact candidate SHA.
- [ ] CI, registered migrations, PostgreSQL and secret scanning passed.
- [ ] Rendered production-readiness review passed on desktop Chromium and mobile WebKit.
- [ ] Release configuration certification passed with no unresolved decision.
- [ ] Liveness, readiness, degraded-readiness and recovery checks passed.
- [ ] Database backup and isolated restore verification passed.
- [ ] Restored database started the application successfully.
- [ ] Critical customer journeys passed.
- [ ] Critical operator journeys passed.
- [ ] Account and operator isolation were reverified.
- [ ] Payment, confirmation, inventory, cancellation and refund idempotency evidence passed.
- [ ] Approved performance thresholds passed.
- [ ] Accessibility and visual-regression evidence passed.
- [ ] Monitoring signals, owners and escalation were verified.
- [ ] Production configuration and secret names were reviewed without exposing values.
- [ ] Rollback procedure and previous known-good build were verified.
- [ ] Operational runbook review completed.

## Required Product Owner decisions

- [ ] Pilot P95 API latency threshold.
- [ ] Maximum acceptable error rate.
- [ ] Maximum pilot checkout concurrency.
- [ ] RPO and RTO.
- [ ] Backup retention and restore owner.
- [ ] Monitoring owner and escalation target.
- [ ] Release operator and rollback authority.
- [ ] Change window and observation period.
- [ ] Accepted known risks and explicit release blockers.
- [ ] Launch feature flags and integrations that remain disabled.
- [ ] Post-deployment smoke-test set.

## Risk register

| Risk | Severity | Evidence | Mitigation | Owner | Decision |
| --- | --- | --- | --- | --- | --- |
| _Add risk_ | _TBD_ | _Link_ | _Action_ | _Owner_ | _Open/Accepted/Blocked_ |

## Go/no-go decision

- Decision: **NO-GO**
- Exact SHA approved for merge: _unassigned_
- Exact SHA separately approved for production deployment: _unassigned_
- Product Owner: _unassigned_
- Decision UTC: _unassigned_
- Conditions or exclusions: _unassigned_

Any commit after approval invalidates the affected evidence and requires a new exact-SHA decision.
