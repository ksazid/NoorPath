# NoorPath V2 Source Audit Register

- Status: In progress
- Purpose: Inventory every existing source, identify conflicts/gaps, and prevent silent assumptions during the V2 reset
- Rule: This register records evidence; it does not resolve product or architecture decisions by itself

## 1. Audit classifications

- **Canonical candidate** — likely to remain authoritative after review.
- **Supporting evidence** — useful input but not governing by itself.
- **Implementation evidence** — current code/PR/test behaviour to reconcile against V2.
- **Stale/conflicted** — contains known contradictions, outdated repository state, or unresolved decisions.
- **Missing authoritative source** — referenced by governance but not available in the connected repository audit surface.

Severity:

- **P0** — blocks trustworthy V2 baseline creation.
- **P1** — must resolve before affected capability implementation.
- **P2** — must resolve before production/release or before the affected UX is finalized.

## 2. Confirmed repository sources

| Source | Classification | Current finding | Action |
| --- | --- | --- | --- |
| `AGENTS.md` | Canonical candidate | Strong current rules for modular monolith, security, design authority, Loop Engineering and DoD. It lacks the new pre-slice discovery/Definition-of-Ready model and does not yet define authority for Figma, Impeccable or Emil design-engineering guidance. | Preserve current strengths; revise only after V2 governance is approved. |
| `README.md` | Supporting evidence | States product and technical baselines are maintained separately and implementation must trace to approved IDs. This means repository-only audit is insufficient. | Acquire authoritative PRD and TRD source files and record provenance/version. |
| `design-system/MASTER.md` | Stale/conflicted | Contains valuable visual rules, but its repository-state section says approved Landing/Package references and S02 prototype evidence are absent. The same `main` branch currently contains both approved reference PNGs and the prototype ZIP. | Reconcile/rewrite after design source inventory. Do not use stale repository-state claims as V2 truth. |
| `design-references/noorpath-landing-reference.png` | Canonical design evidence candidate | Present on `main`. | Preserve; validate provenance/licensing and map into Figma/brand baseline. |
| `design-references/noorpath-package-reference.png` | Canonical design evidence candidate | Present on `main`. | Preserve; validate provenance/licensing and map into Figma/brand baseline. |
| `NoorPath-S02-Approval-Prototype.zip` | Supporting/approval evidence | Present on `main`; S02-specific, not universal product authority. | Inspect as S02 behaviour/visual evidence, not as future product policy. |
| PR #11 `S02: Add catalogue module...` | Implementation evidence | Open S02 implementation PR. It includes domain, persistence, migrations, API, security, UI, browser tests and CI changes. | Frozen as draft during V2 reset. Reconcile rather than merge blindly. |

## 3. Confirmed external/library sources

| Source | Classification | Current finding | Action |
| --- | --- | --- | --- |
| Product Requirements Document V1.0 | Missing authoritative source (P0) | Referenced by S02 traceability and governing order, but full authoritative copy has not yet been located in the connected repository/library audit. | Obtain exact approved file/version before rebuilding PRD V2. |
| Pilot TRD V1.0 | Missing authoritative source (P0) | Referenced by S02 traceability and governing order, but full authoritative copy has not yet been located in the connected repository/library audit. | Obtain exact approved file/version before rebuilding TRD V2. |
| `S02-traceability-matrix.md` | Canonical slice evidence candidate | Explicitly documents a source mismatch: PRD has `US-*`; TRD uses individually numbered `TR-*` but also references `FR/SEC/NFR/OPS` families without individually defined source requirements. | Carry mismatch into Gap & Conflict Register; PRD/TRD V2 must use one explicit requirement taxonomy and complete traceability. |
| Later UI/UX MASTER analysis copy | Supporting evidence | More complete than the repository MASTER: records inspected Landing/Package references, prototype, responsive states, components, open decisions and design risks. | Compare against repository MASTER; merge only approved durable conclusions into V2 design baseline. |

## 4. Initial P0/P1 conflicts and gaps

### GAP-001 — Authoritative PRD is not in the current repository audit set
- Severity: P0
- Impact: Cannot reconstruct product capabilities, business rules, MVP scope or requirement identifiers with full confidence.
- Required resolution: Obtain the exact approved PRD V1.0 file, version/date and approval record.

### GAP-002 — Authoritative Pilot TRD is not in the current repository audit set
- Severity: P0
- Impact: Cannot reliably distinguish durable architecture/security requirements from planning ranges and slice interpretations.
- Required resolution: Obtain exact approved Pilot TRD V1.0 file, version/date and approval record.

### CONFLICT-003 — Requirement taxonomy mismatch
- Severity: P0
- Evidence: S02 traceability states PRD uses `US-*`, TRD has `TR-*`, while `FR/SEC/NFR/OPS` appear as ranges/families without individually defined source requirements.
- Impact: Creates ambiguous traceability and encourages implementation-time interpretation.
- V2 direction: Define one normalized requirement catalogue with unique IDs, text, owner, status, source, acceptance evidence and supersession rules.

### CONFLICT-004 — Repository MASTER has stale evidence-gap claims
- Severity: P1
- Evidence: `design-system/MASTER.md` says Landing/Package references and S02 prototype ZIP are absent; all three are currently present on `main`.
- Impact: Agents may unnecessarily block or infer wrong design authority; repository-state analysis cannot be trusted without refresh.
- V2 direction: Separate durable design rules from transient repository-state/audit observations.

### GAP-005 — Product operating model is not explicit enough
- Severity: P0 before S03
- Impact: Package template vs departure batch, bulk/clone workflows, pricing, inventory, booking, documents, visa and operations risk being designed slice-by-slice without a coherent lifecycle.
- V2 direction: Capability Map + Admin Operating Model + Domain Map + State Machines before new features.

### GAP-006 — Pricing and inventory models are not baseline dependencies of Catalogue/Booking
- Severity: P0 before S03/booking
- Impact: A single starting price/capacity can leak into architecture as product policy and create later schema/API/UI rework.
- V2 direction: First-class Pricing and Inventory capability specifications, including versioning, occupancy/traveller basis, holds, reservations, waitlists and concurrency.

### GAP-007 — Definition of Ready is missing
- Severity: P0 delivery governance
- Impact: Loop Engineering can begin with an approved slice while domain/security/design/environment prerequisites remain unresolved.
- V2 direction: Add mandatory pre-slice readiness evidence; keep Loop Engineering as execution technique after readiness.

### GAP-008 — Design tool/plugin authority is incomplete
- Severity: P1 before new design work
- Impact: Multiple design agents can produce conflicting recommendations or accidental brand drift.
- V2 direction: Product/brand/MASTER/Figma govern; UI UX Pro Max audits UX; Impeccable refines craft; Emil guidance governs purposeful motion; Ponytail governs implementation simplicity.

### GAP-009 — Figma is not yet the canonical screen/component workspace
- Severity: P1
- Impact: Approved visuals, MASTER, tokens and production code can diverge without a shared component/screen artifact.
- V2 direction: Establish Figma foundations/components/patterns/journeys/approved screens and connect them to MASTER + machine-readable tokens.

### GAP-010 — Database migration policy is not strong enough
- Severity: P0 engineering
- Impact: Model/snapshot drift and invalid baseline migrations can survive static checks and emerge during CI/runtime migration tests.
- V2 direction: EF-generated snapshots, pending-model check, empty-DB test, upgrade-path test, immutable merged migrations, destructive-change review, expand/migrate/contract guidance.

### GAP-011 — Environment readiness is not a slice-entry requirement
- Severity: P0 engineering
- Impact: Tests can be implemented but remain unexecutable in the work environment, delaying discovery until CI.
- V2 direction: Every required verification layer must have an executable environment before slice implementation begins.

### GAP-012 — Security/privacy baseline must precede document/OCR/payment work
- Severity: P0 before those capabilities
- Impact: Passport/identity/payment processing creates high-impact privacy, authorization, retention, logging, storage and incident-response requirements.
- V2 direction: Security baseline, threat model, identity/RBAC, data classification, privacy/DPIA assessment, document-security and payment-security specifications before implementation.

## 5. Current design evidence rule

During reset, preserve the existing NoorPath visual DNA:

- Makkah / Haram visual gravity;
- Madinah calm;
- authentic Saudi/Haramain character;
- established ivory, Kaaba black, restrained gold, Madinah green and sandstone direction;
- calm trust rather than marketplace urgency;
- customer reassurance and journey clarity;
- admin accuracy, state visibility and safe actions.

No generated exploration or plugin recommendation may supersede the approved references without explicit product-owner approval and a recorded design decision.

## 6. Next audit targets

Priority order:

1. exact PRD V1.0;
2. exact Pilot TRD V1.0;
3. all ADRs;
4. all existing slice specifications and traceability matrices;
5. all open decisions (`OD-*`) and unresolved conflicts;
6. `packages/design-tokens` values and coverage;
7. approved reference assets, prototype and design QA;
8. current `main` application architecture and CI;
9. open PR #11 implementation and test evidence;
10. deployment/environment/secrets/storage/observability/runbook material;
11. security/privacy/payment/document assumptions;
12. capability and roadmap/backlog documents.

## 7. Exit condition for audit phase

The audit phase is complete only when every governing/supporting source has:

- exact name/path/location;
- version/date;
- approval status/owner where applicable;
- classification;
- extracted requirements/decisions;
- known conflicts/gaps;
- supersession status;
- mapped V2 destination.

No V2 PRD/TRD/domain baseline is considered authoritative before the P0 source gaps are resolved.
