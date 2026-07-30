# VS-01 Traceability Matrix — Operator Access

| Acceptance outcome | Governing source | Evidence |
|---|---|---|
| Provider-neutral principal normalization | ADR-002 §§1–4, 9 | API authentication and architecture tests |
| `401` unauthenticated behavior | ADR-002 §5; API baseline §§7–9 | API and web/E2E tests |
| Active membership and explicit permission | `INV-ID-004`; ADR-002 §3 | Domain/API tests |
| Approved operator-state requirement | `INV-OP-001`; operator state machine | Domain/API tests |
| Server-derived scope and cross-operator denial | `INV-ID-003`–`005`; ADR-002 §4 | API negative tests |
| Authorized and forbidden admin UX | VS-01 slice map; WCAG 2.2 AA baseline | Component, E2E, axe and screenshots |
| Deterministic test authentication fails closed | ADR-002 §7 | Startup/integration tests |
| Safe audit/correlation evidence | `INV-ID-006`; ADR-002 §10 | API tests and structured-log review |
| Operators persistence isolation | ADR-001 | Migration and architecture tests |
| No Catalogue persistence change | Approved VS-01 exclusions | Git diff and Catalogue migration validation |

No stable approved PRD requirement ID was found; none is invented.
