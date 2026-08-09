# MVP hardening and release readiness

Baseline SHA: `30a73a57cf07248682895f17ed7aa02d92f48b40`

## Scope

The functional MVP sequence VS-00 through VS-28 is complete. This phase does not add product features. It verifies that the full MVP can be treated as one release candidate without weakening existing security, isolation, audit, accessibility, recovery, or deployment controls.

## Hardening sequence

1. Align the pilot release manifest and release-readiness validator with VS-00 through VS-28.
2. Run normal CI and release-configuration validation on the hardening branch.
3. Run the production-readiness contract only after the hardening PR is explicitly certified.
4. Verify health degradation/recovery and isolated PostgreSQL backup/restore evidence.
5. Review production configuration, secrets, external-provider feature flags, monitoring ownership, and rollback/runbook evidence.
6. Present one exact release-candidate SHA for Product Owner release approval.
7. Keep production deployment as a separate explicit approval. Release-readiness certification does not authorize deployment.

## Current deliberate deferrals

- Production refund-provider execution remains disabled.
- Production document storage remains disabled until storage and malware-scanning controls are approved.
- Supplier, airline, hotel, insurance, and automated WhatsApp integrations remain deferred.
- No speculative multi-region or scaling infrastructure is introduced for the pilot.

## Exit criteria

- Full release scope is VS-00 through VS-28 with no gaps or duplicates.
- CI and release-readiness validation pass on the same exact head SHA.
- Recovery/health evidence passes on the same exact head SHA.
- No production-only feature is enabled without explicit approval.
- Release candidate remains undeployed until separate Product Owner deployment authorization.
