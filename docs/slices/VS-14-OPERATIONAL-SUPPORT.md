# VS-14 — Operational Support

Status: Registered for implementation

## Outcome

Authorized Operator Operations Staff can find, understand and safely act on MVP booking exceptions across Booking, Payments, Documents and Visa without bypassing the owning module's rules.

## Primary workflow

`Exception queue → scoped search/filter → case summary → approved module-owned action → audited outcome`

The workspace is exception-first. It is not a generic administration database or unrestricted record editor.

## Ownership

- Operational Support owns the queue projection, prioritization, assignment and support activity timeline.
- Booking owns booking state and recovery commands.
- Payments owns payment state, reconciliation evidence and payment recovery commands.
- Documents owns document review state, correction requests and private-file access.
- Visa owns visa state and governed transitions.
- Operators owns active membership, operator scope and support permissions.

Operational Support composes projections and dispatches approved commands. It never edits another module's persistence directly.

## Initial exception categories

- Confirmation exceptions and bookings that cannot safely complete.
- Payment states requiring reconciliation or an approved retry path.
- Document cases awaiting correction or blocked review.
- Visa cases requiring operator action or stale/conflicting updates.

A category is included only when the owning module already exposes sufficient state and a governed action contract.

## Authorization

- Staff require an active operator membership and explicit operational-support permission.
- Queue results and case details are restricted to the current operator scope.
- Foreign operator resources return safe not-found responses.
- Platform Administrator status does not implicitly grant operator case access.
- Sensitive document or payment details remain protected by their owning module's permissions.

## Queue projection

Expose only the minimum operational facts required to triage:

- opaque exception identifier;
- booking reference;
- owning module and exception category;
- safe summary;
- severity and age;
- current owner or unassigned state;
- available approved actions;
- optimistic version and last-updated time.

Never expose card data, provider secrets, passport data, private object keys, visa references, unrestricted customer identity data or internal payloads.

## Approved actions

Actions must be explicit and module-owned. Candidate actions include:

- retry or resume a safe confirmation step;
- request payment reconciliation through Payments;
- reissue an existing document correction request through Documents;
- apply an allowed Visa transition through Visa;
- assign or release the support exception;
- record an operational reason or resolution note that contains no sensitive content.

The final action set must be confirmed against existing module contracts before implementation. No generic `set status` or arbitrary patch endpoint is permitted.

## Concurrency and audit

- Every command carries the current optimistic version.
- Stale actions are rejected and the operator is prompted to refresh.
- Every attempted support action records actor, operator scope, target module, action, reason, timestamp, correlation identifier and outcome.
- Existing module audit histories remain authoritative for domain-state changes.
- Operational Support records orchestration and support activity, not duplicate domain truth.

## UX states

The operator workspace must include:

- loading;
- actionable queue;
- empty queue;
- no search results;
- permission denied;
- safe not found;
- partial projection unavailable;
- stale conflict;
- action failed with retry guidance;
- resolved exception;
- responsive and keyboard-complete behavior.

The design must extend the approved NoorPath operator visual language rather than introduce a separate admin aesthetic.

## Security and telemetry

Logs and telemetry use opaque exception, booking and correlation identifiers. They exclude traveller identity, passport/document content, payment credentials, provider payloads and visa references.

Measure queue load latency, exception counts by safe category, exception age, command outcomes, stale conflicts and projection failures.

## Exclusions

No full CRM or ticketing platform, customer messaging, unrestricted database search, generic record editing, bulk mutation, new provider integrations, automated consequential decisions or silent exception resolution.

Family and Mahram booking rules are not part of this slice unless separately registered and approved; an earlier VS-13 exclusion incorrectly implied they belonged to VS-14.

## Product Owner gate

The action catalogue and operator UX must be reviewed before broad implementation. Exact-head certification and Product Owner approval are required before merge. Merge does not deploy production.
