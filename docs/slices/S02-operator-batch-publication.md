# S02: Operator and Batch Publication

- Status: Approved for implementation
- Slice type: First product vertical slice
- Outcome: An authorised NoorPath admin creates and publishes a valid departure
  batch for an approved operator, and a customer can see its truthful summary in
  the public PWA.
- Primary PRD stories: `US-20`, `US-01`
- Supporting PRD stories: `US-02` and `US-04` apply only to the summary
  disclosures explicitly included below; their complete package-detail
  experience remains in S03.
- Exact technical trace: `TR-SEC-001`–`TR-SEC-006`, `TR-SEC-009`,
  `TR-SEC-010`, and `TR-INV-007`.
- Approved traceability matrix:
  [`S02-traceability-matrix.md`](../traceability/S02-traceability-matrix.md).
- Owning modules: Operators, Catalogue
- Customer API: `GET /api/v1/batches`
- Staff API: `POST /api/v1/admin/batches`,
  `POST /api/v1/admin/batches/{id}/publish`

## Product-owner scope approval

- Decision: S02 outcome and scope approved
- Product owner: Sazid Khan
- Date: 2026-07-28
- Publication boundary: Non-production test operators explicitly marked
  approved; live operator publication remains blocked by `OD-006`.
- Child-pricing boundary: Children and child pricing remain excluded pending
  `OD-010`.
- Design approval: Accepted by Sazid Khan on 2026-07-28.
- Implementation authorization: Granted on 2026-07-28 within the approved
  non-production and scope boundaries.

## Approved S02 design baseline

- Decision: Design approved
- Product owner: Sazid Khan
- Date: 2026-07-28
- Approval prototype:
  [`NoorPath-S02-Approval-Prototype.zip`](../../NoorPath-S02-Approval-Prototype.zip)
- Visual QA:
  [`design-qa.md`](../../prototypes/s02-approval/design-qa.md)
- Approved surfaces: desktop admin draft/create, review, explicit publication
  confirmation, publication success, customer discovery, and responsive 390 px
  customer discovery.
- Approved states: results, loading, empty, error, offline, retry, validation,
  permission denial, publication confirmation, and success.
- Accessibility evidence: keyboard focus, semantic headings and labels, dialog
  semantics, status treatment, practical mobile targets, and reduced-motion
  behaviour are included in the prototype and QA record.
- Scope boundary: The visual references inform S02's design system and trust
  density only. Package details, booking, payments, child pricing, documents,
  notifications, and other S03-or-later behaviour remain excluded.

## Approval and traceability gate

The gate is complete.

The approved PRD defines exact `US-*` stories and the Pilot TRD defines exact
`TR-*` controls. The TRD's section 25 `FR-*`, `SEC`, `NFR`, and `OPS` entries are
planning ranges or families without individually defined requirement text.
The approved
[`S02 traceability matrix`](../traceability/S02-traceability-matrix.md)
records this source mismatch and uses only authoritative `US-*`, `TR-*`, and
`S02-AC-*` identifiers. No missing identifier was guessed.

The implementation authorization retains these launch boundaries:

1. Live operator publication remains blocked by `OD-006`; S02 uses explicitly
   approved non-production test operators only.
2. Children and child pricing remain excluded unless `OD-010` is approved.
3. `US-02` and `US-04` remain deferred to S03 except that S02 truthfully shows
   the approved total starting price and omits unsupported hotel claims.

## User-visible outcome

### Admin

An authorised NoorPath admin can:

1. Select an approved operator.
2. create a draft package and departure batch;
3. enter operator-configurable summary content;
4. see validation errors without losing entered data;
5. publish the valid draft through an explicit confirmation action; and
6. receive a clear success result with the published batch identifier.

### Customer

An unauthenticated customer can:

1. open the public package-discovery page;
2. see only currently published batches;
3. see who operates each batch and whether NoorPath has verified that operator;
4. see departure city, destination/route, departure and return dates, duration,
   package tier, total starting price per person, a dynamic inclusions summary,
   and an honest availability label; and
5. distinguish loading, no-results, failed, offline, and retry states.

## In scope

### Operator publication eligibility

- Operators have a server-controlled lifecycle state.
- Only an operator in the approved state may own a publishable batch.
- Public responses expose only approved public operator fields: display name,
  verification status, and approved support identity where configured.
- The system must never infer or display verification from client input.

### Draft package and batch

The Catalogue module owns the minimum data required for this slice:

- Package: operator, public name, short summary, package tier, and dynamic
  inclusion summary items.
- Departure batch: departure city, destination/route, departure date, return
  date, capacity, availability display mode, lifecycle status, and publication
  timestamps.
- Price version: INR currency, total starting price per person, effective time,
  and version.

Content is dynamic:

- zero, one, or many inclusion summary items are supported;
- no room-sharing configuration is assumed;
- no single-hotel assumption is encoded;
- missing optional content is omitted rather than replaced with invented claims;
  and
- server validation uses bounded lengths and allowlisted enum/value sets.

### Publication

- New batches start as `Draft`.
- Publication is an explicit command, not a generic update.
- A publish request validates the expected version to prevent lost updates.
- Publication succeeds only when:
  - the actor has the required NoorPath admin permission;
  - the owning operator is approved;
  - required public summary fields are present;
  - return date is after departure date;
  - capacity is greater than zero;
  - an effective INR price version exists;
  - the availability display mode is valid; and
  - the record has not already been published or concurrently changed.
- A successful publication records actor, timestamp, correlation ID, previous
  status, new status, and safe batch metadata in an append-only audit event.
- Repeating the command returns a deterministic conflict or idempotent result,
  as defined by the approved API contract; it must not create duplicate batches
  or audit facts.

### Public discovery

- `GET /api/v1/batches` returns a projection, not Catalogue persistence models.
- Only `Published` batches whose publication and effective-price conditions are
  satisfied are returned.
- The default order is the approved product order; no urgency ranking or hidden
  paid promotion is introduced.
- Availability is one of the approved values represented by `TR-INV-007`:
  exact, limited, waitlist-only, or unavailable.
- The UI must not create artificial scarcity or countdown pressure.
- Public responses contain no internal notes, staff identity, tenant keys,
  unpublished content, or restricted/confidential data.
- Safe public catalogue responses may be cached; staff responses and mutations
  must not be cached.

## Explicitly out of scope

- Package-detail/“Know Your Booking” page and complete trust disclosure (S03)
- Full instalment schedule, due dates, due-now amount, and policy disclosure
  (S03)
- Hotel-detail gallery, room selection, meal plan, itinerary, and tier
  comparison (S03 or later approved slices)
- Customer filters and search beyond the approved minimal discovery view
- Customer OTP authentication
- Booking drafts, travellers, seat holds, reservations, or seat decrementing
- Razorpay, payment orders, webhooks, ledger, receipts, or refunds
- Passport/document upload or OCR
- Operator onboarding or operator-approval workflow
- Editing, pausing, closing, archiving, or deleting a published batch
- Notifications, analytics beyond approved allowlisted discovery events, and
  future-backlog features
- Children or child pricing unless `OD-010` is approved
- Publishing a live operator before `OD-006` is resolved

## Acceptance criteria

### AC-01 — Authorised draft creation

Given an approved operator and an authenticated user with the required NoorPath
admin permission, when the user submits valid batch summary data, then a `Draft`
package/batch is persisted in the Catalogue module and the API returns its
identifier, version, and draft status.

### AC-02 — Deny by default

Given an unauthenticated user, customer, operator member, or staff member without
the required permission, when they attempt to create or publish a batch, then
the server denies the request without revealing unpublished content or operator
tenant data.

### AC-03 — Tenant isolation

Given records belonging to different operators, when an authorised actor
creates, reads, or publishes within the staff flow, then server-side
authorization prevents cross-operator access and mutation. Automated tests cover
horizontal-access attempts.

### AC-04 — Validation and recovery

Given missing, malformed, overlong, invalid-enum, zero/negative-capacity,
invalid-date, or missing-price input, when draft creation or publication is
attempted, then the API returns safe RFC 9457 problem details, the UI associates
errors with the relevant fields, focus moves to an error summary, and the user
can correct and retry without re-entering valid values.

### AC-05 — Publication eligibility

Given a complete draft for an approved operator, when an authorised admin
confirms publication with the current expected version, then the batch becomes
`Published` atomically and one append-only publication audit event is recorded.

### AC-06 — Publication rejection

Given an unapproved operator, incomplete draft, invalid price version, stale
expected version, or already-published batch, when publication is attempted,
then no partial state change occurs, no public batch appears, and the response
communicates a safe, actionable failure.

### AC-07 — Public visibility

Given one valid published batch and one draft batch, when a customer opens the
discovery page, then only the published batch is returned and rendered with its
approved operator identity, departure summary, dates, duration, tier, total
starting price per person, dynamic inclusion highlights, and approved
availability label.

### AC-08 — Truthful dynamic content

Given package variations with long text, missing optional fields, and zero, one,
or many inclusion summary items, when the discovery card renders, then it does
not assume a hotel count, room-sharing type, fixed inclusion, or unapproved
claim; content remains readable without clipping at approved breakpoints.

### AC-09 — Public empty state

Given no currently published batches, when the discovery query succeeds, then a
calm no-packages state is shown with human-support guidance approved for this
screen; draft or internal data is not exposed.

### AC-10 — Loading, error, offline, and retry

When catalogue data is loading, unavailable, or the device is offline, then the
page presents distinct accessible states and a retry action where meaningful.
Cached data, if shown, is labelled with its freshness and contains only safe
public catalogue content.

### AC-11 — Accessibility and responsive behaviour

The admin flow and discovery view meet WCAG 2.2 AA for semantic structure,
keyboard operation, visible focus, labels, error association, status
announcements, contrast, 44px-equivalent targets, zoom/text expansion, and
reduced motion at the approved mobile and desktop breakpoints.

### AC-12 — Observability and privacy

Create, publish, rejection, and public-query outcomes emit structured,
allowlisted telemetry with correlation IDs. Logs, traces, audit events, and
analytics contain no secrets, tokens, internal notes, personal data, or
unpublished package content. Approved discovery analytics use only
`batch_list_viewed` and allowlisted non-identifying properties.

### AC-13 — Visual fidelity

Implementation screenshots at the approved smallest mobile viewport and one
representative desktop viewport match the approved S02 design baseline. Material
differences are corrected or explicitly accepted by the product owner.

### AC-14 — End-to-end demonstration

An automated E2E scenario creates a valid draft through the staff surface,
publishes it, opens the public discovery page in a clean customer context, and
finds the newly published summary. A negative E2E or integration scenario proves
that an invalid/unapproved batch never appears publicly.

## API and contract expectations

These are contract intentions, not permission to implement generic CRUD:

| Operation | Required behaviour |
| --- | --- |
| `POST /api/v1/admin/batches` | Authorised command; creates a draft only |
| `POST /api/v1/admin/batches/{id}/publish` | Authorised transition with expected version |
| `GET /api/v1/batches` | Anonymous safe projection of published batches only |

All endpoints use camelCase JSON, UTC ISO 8601 timestamps, correlation IDs, and
RFC 9457-style problem details. OpenAPI is the contract source.

## Architecture constraints

- Implement the use case as a Catalogue vertical slice inside the modular
  monolith.
- The Catalogue module owns package, batch, price-version, and publication
  state.
- Operator approval is obtained through an explicit Operators module contract;
  Catalogue must not query Operators tables directly.
- EF Core `DbContext` may be used directly in application handlers.
- Do not add MediatR, a generic repository, a generic unit of work, an outbox, or
  domain events unless this slice demonstrates a concrete need and the existing
  ADRs permit it.
- Domain code has no API, UI, EF Core, or infrastructure dependency.
- Use a forward-only migration and validate it against PostgreSQL.
- Do not preload S03 models, endpoints, screens, or policy behaviour.

## Security and threat-model checks

Before completion, record at minimum:

- privilege escalation against create/publish commands;
- cross-operator object access;
- mass assignment of lifecycle, verification, tenant, audit, and publication
  fields;
- unpublished-content leakage through list, error, cache, log, or OpenAPI
  responses;
- stored content injection/XSS in public package fields;
- CSRF on cookie-authenticated staff mutations;
- stale-version and duplicate-publication behaviour; and
- denial-of-service controls for public catalogue queries.

## Test requirements

### Unit

- Package/batch validation and date invariants
- Publication eligibility and lifecycle transition
- Dynamic inclusion-summary handling
- Public availability-label mapping

### Integration

- PostgreSQL migration applies from an empty database
- Authorised draft create and publish
- Unauthorized and forbidden create/publish
- Cross-operator isolation
- Unapproved-operator publication rejection
- Invalid/stale/duplicate publication rollback behaviour
- Public query returns published projection only
- No internal fields appear in the public contract

### Architecture

- Catalogue dependency direction
- Catalogue cannot access Operators persistence
- Domain has no infrastructure/UI/API references

### Web component and E2E

- Admin validation and retry states
- Customer loading, empty, error, offline, and retry states
- Long, missing, one-item, and multi-item dynamic content
- Keyboard, focus, screen-reader naming, and reduced-motion checks
- Mobile and desktop screenshot comparison
- Publish-to-public end-to-end scenario

## Observability and operational evidence

- Metrics: draft creation outcome, publication outcome/rejection reason category,
  public query success/failure, response latency, and result count.
- Logs: safe identifiers, actor category, operator/batch identifiers where
  approved, correlation ID, outcome, and reason category.
- Audit: actor, action, target, expected version, previous/new lifecycle state,
  reason where required, timestamp, and correlation ID.
- Runbook: diagnose a batch missing from discovery; revoke/pause remains a
  documented S03-or-later limitation and must be handled through the approved
  operational control until implemented.

## 30–90 minute implementation tasks

- `S02-T01` Complete: exact authoritative PRD/TRD identifiers are recorded in
  the approved traceability matrix and implementation is authorised.
- `S02-T02` Record the S02 threat-model delta and publication-abuse cases.
- `S02-T03` Define OpenAPI contracts and safe problem details for draft create,
  publish, and public list.
- `S02-T04` Add Catalogue module skeleton and enforce architecture boundaries.
- `S02-T05` Model package, departure batch, price version, inclusion summary,
  lifecycle, and publication invariants.
- `S02-T06` Add the forward-only Catalogue migration and PostgreSQL integration
  fixture.
- `S02-T07` Implement the approved-operator contract and server-side permission/
  tenant checks.
- `S02-T08` Implement draft creation with validation, concurrency version, and
  unit/integration tests.
- `S02-T09` Implement explicit publication with atomic audit evidence and
  negative-path tests.
- `S02-T10` Implement the anonymous published-batch projection and leakage tests.
- `S02-T11` Build the approved admin draft/publication surface and all UI states.
- `S02-T12` Build the approved customer discovery surface with dynamic content
  and all UI states.
- `S02-T13` Add safe telemetry and the missing-batch diagnostic runbook.
- `S02-T14` Add accessibility automation, publish-to-public E2E, and visual
  regression coverage.
- `S02-T15` Run complete repository validation, capture evidence, and correct
  only S02 defects.
- `S02-T16` Conduct screenshot comparison and record product-owner acceptance.

## Definition of Done and exit evidence

S02 is complete only when:

- every exact approved requirement ID is linked;
- the S02 design artifact is approved and linked;
- all acceptance criteria pass;
- formatting, linting, type checking, builds, migrations, dependency, secret,
  unit, integration, architecture, E2E, accessibility, and visual checks pass;
- tenant isolation, authorization, privacy, audit, concurrency, failure, empty,
  offline, and retry paths have evidence;
- the threat-model delta and operational runbook are committed;
- CI is green on the pull request;
- the implementation is demonstrated from admin draft to public discovery;
- no S03 or future-backlog behaviour is introduced; and
- the product owner records dated acceptance with the PR, CI run, design
  baseline, screenshots, and any accepted limitations.

## Product-owner acceptance record

Complete this section only after review:

```text
Decision: Accepted | Accepted with recorded limitations | Rejected
Product owner:
Date (UTC):
Pull request:
Merge commit:
CI run:
Design baseline:
Visual evidence:
Acceptance criteria reviewed:
Accepted limitations:
Follow-up items:
```

## Codex Cloud implementation prompt

Approved for use on 2026-07-28:

> Read `AGENTS.md`, the approved PRD/TRD/ADRs,
> `docs/traceability/S02-traceability-matrix.md`, the linked S02 design
> baseline, and `docs/slices/S02-operator-batch-publication.md`. Implement only S02:
> an authorised NoorPath admin creates and publishes a valid departure batch
> for an explicitly approved non-production test operator, and an anonymous
> customer sees its truthful summary in the PWA. Start by citing S02 and the
> exact authoritative identifiers in the approved traceability matrix. Do not
> invent individual `FR/SEC/NFR/OPS` identifiers: the approved matrix records
> that those source entries are undefined planning ranges/families. Preserve the
> modular monolith and module ownership. Deny by default, enforce server-side
> permission and operator isolation, expose only published safe projections,
> record publication audit evidence, and implement all success, failure, empty,
> offline, retry, accessibility, responsive, security, observability, and visual
> acceptance criteria. Do not implement S03 package details, booking, seat
> holds, authentication, payments, documents, notifications, refunds, children,
> or other future scope. Run every required validation, capture screenshots
> against the approved baseline, commit on a dedicated S02 branch, and open a
> draft pull request with traceability and evidence. Stop and report any
> conflict, attempt to publish a live operator, attempt to introduce child
> pricing, or missing design evidence instead of inventing policy.
