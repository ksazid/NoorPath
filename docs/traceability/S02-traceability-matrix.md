# S02 Traceability Matrix: Operator and Batch Publication

- Status: Approved for implementation
- Slice: `S02`
- Product owner: Sazid Khan
- Approved on: 2026-07-28
- Governing slice specification:
  [`S02-operator-batch-publication.md`](../slices/S02-operator-batch-publication.md)
- Approved design evidence:
  [`design-qa.md`](../../prototypes/s02-approval/design-qa.md)

## Traceability normalization

The approved Product Requirements Document defines product requirements as
`US-*` user stories. The approved Pilot TRD defines individually numbered
technical controls as `TR-*`. Its `FR-*`, `SEC`, `NFR`, and `OPS` references in
section 25 are planning ranges or requirement families; the source baselines do
not define individual requirement text for those identifiers.

No individual `FR-*`, `SEC-*`, `NFR-*`, or `OPS-*` identifier is therefore
invented for S02. For this slice:

- exact normative product traceability uses `US-*`;
- exact normative technical traceability uses `TR-*`;
- numbered `S02-AC-*` acceptance criteria provide slice-level verification;
- relevant numbered TRD sections provide additional governing constraints; and
- the broad `FR-010–026` and `FR-120–128` ranges remain non-normative planning
  cross-references until the PRD baseline formally defines their individual
  requirements.

This normalization records and resolves the source mismatch for S02. It does
not change product behaviour or authorise future scope.

## Exact requirement set

| ID | Requirement applied to S02 | Scope |
| --- | --- | --- |
| `US-20` | An admin creates a group batch with departure city, dates, hotel/package tier, and seat capacity, and the system displays the eligible published batch to users. | Primary |
| `US-01` | A first-time pilgrim browses available Umrah group packages by departure city and date. S02 implements the minimal unfiltered discovery list; additional filtering is deferred. | Primary, partial |
| `US-02` | Clear instalment breakdown before booking. S02 exposes only total starting price per person and does not claim to satisfy the instalment breakdown. | Deferred to S03 |
| `US-04` | Hotel name, rating, and Haram distance before package selection. S02 does not assume a single hotel and does not claim to satisfy the complete hotel disclosure. | Deferred to S03 |
| `TR-INV-007` | Published availability identifies exact, limited, waitlist-only, or unavailable status; artificial scarcity is prohibited. | Required |
| `TR-SEC-001` | Threat-model the create, publish, and public-discovery flow before implementation completion. | Required |
| `TR-SEC-002` | Deny protected create/publish operations by default and enforce authorization server-side. | Required |
| `TR-SEC-003` | Prevent and test horizontal access across customer, operator, and staff boundaries. | Required |
| `TR-SEC-004` | Validate all staff and public inputs with allowlists and domain constraints. | Required |
| `TR-SEC-005` | Apply CSRF protection to cookie-authenticated create and publish mutations. | Required when cookie auth is used |
| `TR-SEC-006` | Enforce secure headers plus dependency, secret, and static security scanning through CI/release controls. | Required |
| `TR-SEC-009` | Use structured allowlisted logs with redaction; exclude unpublished content and personal or secret data. | Required |
| `TR-SEC-010` | Apply proportionate abuse/rate controls to the public catalogue query. | Required |

`TR-SEC-007` and `TR-SEC-008` remain baseline environment controls but create no
new secret or production-data feature in S02.

## Acceptance-criterion matrix

| S02 criterion | Product trace | Technical trace | Verification evidence |
| --- | --- | --- | --- |
| `S02-AC-01` Authorised draft creation | `US-20` | `TR-SEC-002`, `TR-SEC-004`; TRD §§5.2, 9.1, 14.3 | Domain/unit tests; authorised API integration test; PostgreSQL persistence test |
| `S02-AC-02` Deny by default | `US-20` | `TR-SEC-002` | Unauthenticated and forbidden API tests |
| `S02-AC-03` Operator isolation | `US-20` | `TR-SEC-003`; TRD §§6.1, 8.2 | Cross-operator integration and architecture tests |
| `S02-AC-04` Validation and recovery | `US-20` | `TR-SEC-004`; TRD §§14.1, 16.5 | Validator tests; RFC 9457 contract test; accessible form-error UI test |
| `S02-AC-05` Publication eligibility | `US-20` | `TR-SEC-002`, `TR-SEC-004`; TRD §§10, 14.3, 17.3 | Atomic publish integration test; audit assertion |
| `S02-AC-06` Publication rejection | `US-20` | `TR-SEC-002`, `TR-SEC-003`, `TR-SEC-004` | Unapproved, invalid, stale, and duplicate publication tests |
| `S02-AC-07` Public visibility | `US-20`, `US-01` | `TR-INV-007`; TRD §§9.3, 14.2 | Public-projection integration test; discovery component/E2E test |
| `S02-AC-08` Truthful dynamic content | `US-01`; `US-02`/`US-04` deferred | `TR-INV-007`; TRD §§16.1–16.3 | Zero/one/many inclusion tests; long/missing-content visual tests |
| `S02-AC-09` Public empty state | `US-01` | TRD §§16.1, 16.5 | Empty projection and UI-state tests |
| `S02-AC-10` Loading/error/offline/retry | `US-01` | TRD §§16.1, 16.4, 16.5 | Component and E2E state tests; safe-cache assertion |
| `S02-AC-11` Accessibility/responsive | `US-01`, `US-20` | TRD §§16.1–16.5 | Automated accessibility checks; keyboard review; mobile/desktop screenshots |
| `S02-AC-12` Observability/privacy | `US-20`, `US-01` | `TR-SEC-009`, `TR-SEC-010`; TRD §§16.6, 20.1–20.3 | Telemetry allowlist tests; redaction tests; missing-batch runbook |
| `S02-AC-13` Visual fidelity | `US-01`, `US-20` | TRD §§16.1, 16.3 | Screenshot comparison against the approved S02 prototype |
| `S02-AC-14` End-to-end demonstration | `US-20`, `US-01` | `TR-SEC-002`, `TR-SEC-003`, `TR-SEC-004`, `TR-INV-007`; TRD §§22.1–22.2 | Publish-to-public E2E plus negative non-publication scenario |

## API, ownership, and data trace

| Implementation element | Owner | Requirement/criterion trace |
| --- | --- | --- |
| `POST /api/v1/admin/batches` | Catalogue | `US-20`, `S02-AC-01`–`04` |
| `POST /api/v1/admin/batches/{id}/publish` | Catalogue using an Operators contract | `US-20`, `S02-AC-02`–`06`, `TR-SEC-002`–`005` |
| `GET /api/v1/batches` | Catalogue public projection | `US-01`, `S02-AC-07`–`12`, `TR-INV-007`, `TR-SEC-009`–`010` |
| Operator approval lookup | Operators contract | `S02-AC-03`, `S02-AC-05`, `S02-AC-06`, `TR-SEC-003` |
| Package/batch/price/publication persistence | Catalogue | `S02-AC-01`, `S02-AC-05`, `S02-AC-06`; TRD §§5.3, 9 |
| Publication audit record | Catalogue/audit contract | `S02-AC-05`, `S02-AC-12`; TRD §17.3 |
| Admin create/review/publish UI | Web staff surface | `US-20`, `S02-AC-01`–`06`, `S02-AC-11`, `S02-AC-13` |
| Customer discovery UI | Public PWA | `US-01`, `S02-AC-07`–`13`, `TR-INV-007` |

## Scope and decision controls

- `OD-006`: live operator publication remains blocked. S02 implementation and
  evidence use explicitly approved non-production test operators only.
- `OD-010`: children and child pricing remain excluded.
- `US-02` and `US-04`: complete instalment and hotel disclosure remain S03.
- Booking, authentication, seat holds, payments, documents, notifications,
  refunds, live-operator onboarding, and future-backlog features remain out of
  scope.

## Approval

- Decision: Approved for S02 implementation
- Product owner: Sazid Khan
- Date: 2026-07-28
- Approved sources: Product Requirements Document V1.0, Pilot TRD V1.0,
  `AGENTS.md`, S02 slice specification, and approved S02 design/QA evidence
- Accepted limitation: the baseline has no individually defined
  `FR/SEC/NFR/OPS` requirements; authoritative `US/TR/S02-AC` identifiers are
  used without inventing IDs
- Implementation authorization: Granted for S02 within the boundaries above

## Automated evidence implementation

The following committed tests implement the S02 automated evidence and execute in
CI with `NOORPATH_TEST_DB`:

- `CatalogueApiTests`: authorised/denied draft creation (`S02-AC-01`–`04`),
  operator isolation and publication rejection (`S02-AC-02`–`06`), published-only
  safe projection and caching (`S02-AC-07`, `09`, `10`, `12`), and public-query
  rate limiting (`TR-SEC-010`).
- `CataloguePersistenceTests`: empty-database migration and durable atomic
  publication/audit persistence (`S02-AC-01`, `05`, `06`, `12`).
- `BatchTests`: validation, lifecycle, inclusion normalization, and availability
  allowlisting (`S02-AC-04`–`08`).
- `FoundationTests`: domain dependency direction and Operators-persistence
  separation (`S02-AC-03`).

Browser E2E, automated accessibility, keyboard review, and desktop/390 px visual
comparison remain required evidence under `S02-AC-11`, `13`, and `14`; this
matrix does not mark those checks complete until they execute and are accepted.

### Browser evidence implementation

- `apps/web/e2e/publication.spec.ts` implements the positive publish-to-public
  and negative draft-never-public browser paths (`S02-AC-01`, `04`–`07`, `11`,
  `14`), including explicit confirmation, dialog focus, Escape, focus return,
  retained values, correction, and status announcements.
- `apps/web/e2e/customer-states.spec.ts` implements loading, results, empty,
  error/retry, offline/recovery, axe WCAG 2.2 AA scanning, keyboard focus,
  44 CSS px targets, reduced motion, text expansion, and overflow checks
  (`S02-AC-08`–`11`).
- `apps/web/playwright.config.ts` runs Chromium at 1363 × 936 and 390 × 844 and
  enforces reviewed visual snapshots (`S02-AC-11`, `13`). Reproduction and
  approval rules are recorded in `docs/design/S02-browser-visual-qa.md`.

The automated browser implementation does not replace the required human
visual comparison, manual accessibility review, or dated product-owner
acceptance. Those remain incomplete until recorded against the final commit.
