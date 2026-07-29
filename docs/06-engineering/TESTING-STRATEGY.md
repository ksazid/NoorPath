# NoorPath V2 Testing & Quality Strategy

Status: Draft baseline
Version: 0.1
Step: 18

## Purpose

Define the minimum quality gates each NoorPath MVP vertical slice must satisfy without turning testing into heavyweight enterprise process.

## 1. Quality Principle

Testing follows risk and business consequence, not arbitrary coverage percentages.

Every slice proves its user/business outcome across the layers it changes. High-risk flows such as inventory, booking, payments, documents, authorization and migrations receive deeper evidence than simple content reads.

## 2. Test Pyramid for MVP

### Domain unit tests
Use for invariants, state transitions, pricing rules, booking rules, eligibility decisions, and other deterministic domain behaviour.

### Application tests
Use for commands/queries, orchestration, authorization requirements, error mapping, idempotency behaviour and module contracts.

### Integration tests
Run against real PostgreSQL for persistence mappings, constraints, transactions, concurrency, outbox behaviour and migration-sensitive workflows.

### API tests
Verify HTTP contracts, authentication/authorization, validation, Problem Details, serialization, tenant/resource isolation, idempotency, concurrency and safe error behaviour.

### End-to-end tests
Cover only critical customer/operator journeys. Avoid duplicating every lower-level test through browser automation.

### Visual regression
Use approved Figma/reference screens for important customer/admin states and responsive breakpoints.

### Accessibility
Automated checks plus keyboard/focus/manual verification for critical flows. WCAG 2.2 AA remains the target.

### Security tests
Include cross-tenant/object authorization, privilege boundaries, sensitive-data leakage, upload validation, webhook verification, replay/idempotency and other slice-specific threat controls.

### Performance/reliability tests
Introduce when a slice has meaningful latency, concurrency or failure risk. Do not create artificial load suites for trivial screens.

## 3. Mandatory Slice Evidence

Every implementation slice must provide, where applicable:

- acceptance criteria mapped to automated/manual evidence
- domain/application tests for business rules
- PostgreSQL integration tests for changed persistence
- API contract tests for changed endpoints
- authorization and tenant/resource isolation tests
- migration validation when schema changes
- E2E proof for the slice's critical user outcome
- accessibility evidence for changed interactive UI
- visual comparison for approved screens/states
- telemetry verification for consequential operations
- failure/retry/idempotency evidence for external or asynchronous behaviour

Not-applicable gates must be explicitly marked rather than silently omitted.

## 4. Database and Migration Quality Gates

For every migration-bearing change:

1. EF model has no unexplained pending changes.
2. Empty database can migrate to current head.
3. Previous supported release can migrate forward to current head.
4. Migration and model snapshot are generated and consistent.
5. Destructive operations are explicitly reviewed.
6. Risky changes use expand -> migrate/backfill -> contract where appropriate.
7. Roll-forward/recovery procedure is understood before production deployment.

## 5. Critical Journey E2E Set

Keep browser E2E intentionally small and high value. MVP candidates:

- operator publishes a valid saleable departure
- customer discovers and opens package
- customer creates/continues booking with travellers
- quote and inventory hold are acquired safely
- payment success/pending/failure states are handled correctly
- confirmed booking appears in My Journey
- customer uploads required document securely
- operator/admin processes document/visa readiness state
- customer can reach human support context from journey

The exact suite grows with implemented slices, not with hypothetical future capabilities.

## 6. Concurrency and Race Tests

Mandatory for consequential shared state:

- competing inventory holds cannot oversell
- hold expiry vs checkout/payment race is deterministic
- duplicate payment webhook does not duplicate settlement
- stale admin edits do not silently overwrite protected state
- repeated commands with the same idempotency key do not duplicate consequences
- outbox retry does not duplicate business effects

## 7. Security Quality Gates

For affected slices verify:

- unauthenticated access denied where required
- unauthorized function denied
- cross-customer resource access denied
- cross-operator access denied
- privileged actions audited
- sensitive fields absent from logs/errors/events/analytics
- request overposting cannot mutate protected properties
- file uploads enforce validation/quarantine/scanning flow where applicable
- external webhook/request authenticity is verified

## 8. Frontend Quality

For changed customer/admin UX:

- happy path
- loading
- empty
- validation error
- system/provider error
- permission denied where applicable
- stale/conflict state where applicable
- mobile/tablet/desktop behaviour required by the design spec
- keyboard/focus behaviour
- reduced-motion behaviour when motion exists
- long-content/text expansion stress test

## 9. CI Quality Pipeline

MVP pipeline grows incrementally but converges on:

Restore -> Format/Lint -> Compile -> Unit -> Architecture -> Integration -> Migration -> API/Contract -> Frontend -> E2E -> Accessibility -> Visual -> Security/Dependency scans -> Build artifact/container -> Staging smoke

Only gates relevant to implemented capabilities are enabled initially; once introduced they should remain reproducible and reliable.

## 10. Flaky Test Policy

A flaky test is a defect.

- do not normalize rerunning until green
- investigate deterministic data/time/concurrency/environment causes
- quarantining requires an explicit issue and owner
- release-blocking critical-path tests cannot remain quarantined indefinitely

## 11. Test Data

- deterministic builders/fixtures
- no production PII in automated tests
- synthetic documents/payment references
- explicit tenant/operator/customer separation in fixtures
- time-dependent behaviour uses controllable clock abstractions where useful

## 12. Manual Testing

Manual testing is retained for areas automation cannot adequately prove, including:

- UX comprehension and trust
- accessibility nuance
- responsive ergonomics
- visual quality
- exceptional admin workflows
- payment/provider sandbox behaviour when automation is constrained

Manual evidence complements automation; it does not replace repeatable tests for critical invariants.

## 13. Definition of Done Quality Gate

A slice is Done only when required test evidence is green, migrations are validated, accessibility/visual/security obligations are satisfied, telemetry is verified, and any accepted residual risk is explicitly documented.

## 14. MVP Non-Goals

Do not require for every slice:

- 100% code coverage
- full browser regression of the entire application
- load testing for trivial CRUD/content flows
- mutation testing across the whole solution
- large device/browser matrices before product evidence warrants them
- exhaustive penetration testing for every PR

## 15. Implementation Timing

This strategy is implemented progressively:

- test foundations and CI harness before the first new V2 slice
- slice-specific tests implemented with the slice, not afterward
- staging smoke/security/accessibility/visual gates mature before production launch
- performance, resilience and recovery suites deepen when the relevant capabilities and real workload exist
