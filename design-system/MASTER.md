# NoorPath UI/UX Implementation Baseline

- **Status:** Canonical durable UI/UX baseline; historical repository inventory is superseded as recorded below
- **Version:** 1.1
- **Established:** 2026-07-28
- **Reconciled:** 2026-07-30
- **Applies to:** Every existing and future NoorPath customer, operator, and admin web surface
- **Change scope:** Design governance and durable implementation rules

## 1. Authority and use

This file is the implementation-level visual specification requested before
S02. It is subordinate, in order, to approved PRD requirement IDs, the approved
Pilot TRD and ADRs, the approved UI/UX baseline, and an active approved slice.
`AGENTS.md` remains the engineering operating agreement. A later approved visual
artifact overrides any conflicting inference in this document.

Use the following evidence hierarchy when implementing a screen:

1. the approved NoorPath Landing page;
2. the approved NoorPath Package page;
3. the approved S02 prototype for S02 surfaces only;
4. the committed design tokens and ADR-0002;
5. existing approved components and patterns; then
6. accessibility or usability guidance that does not alter NoorPath's identity.

Do not substitute a generated or generic design system for NoorPath's approved
language. In particular, the UI UX Pro Max suggestion of a vibrant purple,
block-based marketplace is rejected because it conflicts with the approved
ivory, black, gold, green, and sandstone modern-minimalist direction.

## 1.1 Current evidence reconciliation

The transient repository inventory and missing-source statements originally
recorded in sections 2, 3, 7, 8, and 9 describe the 2026-07-28 baseline checkout
and are retained only as historical analysis. They must not be interpreted as
current repository truth.

As of 2026-07-30:

- `design-references/noorpath-landing-reference.png` is present and is the
  primary visual authority;
- `design-references/noorpath-package-reference.png` is present and is the
  second visual authority;
- `NoorPath-S02-Approval-Prototype.zip` is present as slice-specific supporting
  evidence;
- VS-01 Operator Access and VS-02 Package & Departure Authoring UI are committed
  implementation evidence; and
- the current source inventory is governed by
  `docs/00-governance/SOURCE-AUDIT-REGISTER.md`.

The references being committed does not itself complete visual acceptance.
Each changed screen still requires an accessible rendered implementation,
same-viewport screenshot comparison, relevant accessibility/interaction
evidence, and Product Owner acceptance.

The full workflow in
`docs/05-design/DESIGN-SYSTEM-AND-FIGMA-BASELINE.md` applies to every existing
screen refinement and every new screen. Skills refine the approved NoorPath
identity; they never supersede it.

## 2. Evidence gap and safe interpretation

The repository does **not** currently contain the approved Landing page,
approved Package page, their reference images, the linked
`NoorPath-S02-Approval-Prototype.zip`, or the prototype implementation described
by `prototypes/s02-approval/design-qa.md`. The only implemented web route is the
S01 foundation placeholder at `/`; it is not a Landing or Package page. There
are no committed customer header, navigation, footer, cards, forms, icons, or
admin surfaces to inspect or reuse.

Consequently:

- exact measurements and visual details absent from committed evidence are
  marked **unresolved**, not invented;
- the QA record may establish reviewed characteristics, but cannot supply exact
  CSS values or reusable code;
- the S01 placeholder must not be treated as a completed visual source of truth;
- before visual S02 implementation, restore the approved reference images and
  prototype (or provide an accessible authoritative location), verify their
  provenance/licensing, and compare implementation screenshots against them;
  and
- the product owner must accept any material difference. This documentation
  does not itself satisfy S02 visual acceptance.

## 3. Current repository and slice state

### 3.1 Architecture and implementation inventory

| Area              | Current state                                                                                                                                                                                                                           |
| ----------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Web               | Next.js 16 / React 19 App Router workspace. One `/` server-component placeholder, one root layout, one global stylesheet, and one shallow render test. No PWA manifest/service worker is committed despite the planned PWA description. |
| API               | Minimal ASP.NET Core .NET 10 host exposing only live/readiness health endpoints.                                                                                                                                                        |
| Domain/modules    | Building-block health response only. No Operators or Catalogue module exists yet.                                                                                                                                                       |
| Data              | PostgreSQL Compose foundation exists; no application persistence or migrations exist.                                                                                                                                                   |
| Shared design     | Workspace package exports `tokens.json` and `tokens.css`. JSON is broader than CSS; neither is a complete component theme.                                                                                                              |
| Assets/icons/type | No application visual assets, icon dependency, local font files, or external font loading. Current CSS uses Inter when locally available, then system sans; QA says the absent prototype used Georgia/system serif for display type.    |
| Customer flows    | Foundation message only. No discovery, package-detail, support, navigation, header, or footer flow.                                                                                                                                     |
| Admin flows       | None. No authentication, authorization, operator selection, draft, review, confirmation, success, or failure UI.                                                                                                                        |
| Tests             | One web render assertion and one .NET assembly availability test; foundation CI gates exist. No component, integration, E2E, accessibility, responsive, or visual tests.                                                                |

### 3.2 S01 completion assessment

S01 is recorded as product-owner accepted and complete. Its repository-level
foundation is present: workspace boundaries, governance, version pins, minimal
web/API shells, shared tokens, ADRs, PostgreSQL Compose, CI, ownership, and a PR
template. It deliberately introduced no product behaviour.

The assessment is **complete as an accepted engineering foundation**, with
external evidence limitations: acceptance placeholders still contain
`<PR link>` and `<successful Actions run>`, repository settings cannot be proven
from the checkout, and the planned directory descriptions exceed the currently
created skeleton. These limitations do not authorize reopening or redesigning
S01.

### 3.3 Exact S02 scope and identifiers

S02 is approved but unimplemented. Its outcome is one vertical path:

> An authorised NoorPath admin creates and explicitly publishes a valid
> departure batch for an approved non-production test operator; an anonymous
> customer sees its truthful public summary.

**Authoritative product trace:** `US-20` and partial `US-01`. `US-02` and
`US-04` govern only the explicit boundary: show the total starting price per
person and do not make unsupported hotel/instalment claims; their full
experiences remain S03.

**Authoritative technical trace:** `TR-INV-007`, `TR-SEC-001` through
`TR-SEC-006`, `TR-SEC-009`, and `TR-SEC-010`. Cookie-based staff mutation makes
`TR-SEC-005` applicable. Do not invent individual `FR`, `SEC`, `NFR`, or `OPS`
IDs because the approved sources contain only ranges/families.

**Slice criteria:** `S02-AC-01` through `S02-AC-14`, covering authorised draft
creation, default denial, tenant isolation, validation/recovery, atomic eligible
publication, rejection/rollback, public visibility, dynamic truthful content,
empty/loading/error/offline/retry states, WCAG/responsiveness, safe
observability, visual fidelity, and publish-to-public E2E proof.

The only approved APIs are:

- `POST /api/v1/admin/batches` — create a draft;
- `POST /api/v1/admin/batches/{id}/publish` — explicit version-checked publish;
  and
- `GET /api/v1/batches` — anonymous safe published projection.

Package detail, full instalments, hotel detail, filters/search, customer auth,
bookings, travellers, holds, payments, documents, onboarding, published-batch
management, notifications, children/child pricing, and live-operator
publication remain out of scope.

### 3.4 S02 dependencies

1. **S01 foundations:** build/test tooling, API and web hosts, PostgreSQL,
   shared token package, CI, ADRs, and architecture rules.
2. **Operators contract:** server-owned approval and safe public operator fields;
   Catalogue must not access Operators persistence directly.
3. **Catalogue vertical slice:** package, departure batch, price version,
   inclusions, lifecycle, concurrency, publication, public projection, and
   append-only audit persistence.
4. **Security:** deny-by-default staff authorization, server-side operator
   isolation, allowlisted validation, CSRF if cookie-authenticated, secure
   headers/scanning, safe telemetry, rate controls, and the S02 threat-model
   delta.
5. **Contracts and operations:** OpenAPI, RFC 9457 problem details, correlation
   IDs, a forward-only PostgreSQL migration, and a missing-batch runbook.
6. **Visual evidence:** restoration of the approved Landing/Package sources and
   S02 prototype is a prerequisite for faithful UI implementation and AC-13,
   though non-visual contract/domain tasks can be prepared without inventing UI.
7. **Launch decisions:** only explicitly approved non-production test operators
   (`OD-006` boundary); no children or child pricing (`OD-010` boundary).

## 4. Canonical visual language

### 4.1 Character

NoorPath is calm, trustworthy, modern-minimalist, editorial, and explicit—not
playful, urgency-led, glossy, or generically “marketplace.” Trust comes from
clear operator identity and factual disclosures, not decorative badges,
religious imagery, countdowns, scarcity pressure, or unverified claims.

### 4.2 Colour

The committed palette is authoritative:

| Role            | Token                                                | Value     | Use                                                                       |
| --------------- | ---------------------------------------------------- | --------- | ------------------------------------------------------------------------- |
| Haram ivory     | `color.brand.haramIvory` / `--color-brand-haram`     | `#F7F2E8` | Warm brand field or restrained section surface.                           |
| Kaaba black     | `color.brand.kaabaBlack` / `--color-brand-kaaba`     | `#171715` | Inverse surface and strongest brand contrast.                             |
| Kiswah gold     | `color.brand.kiswahGold` / `--color-brand-kiswah`    | `#B89548` | Restrained accent, never large decorative saturation or status by itself. |
| Madinah green   | `color.brand.madinahGreen` / `--color-brand-madinah` | `#176B50` | Verification and positive/primary action where approved.                  |
| Saudi sandstone | `color.brand.saudiSand` / `--color-brand-sand`       | `#CBA77B` | Warm supporting accent.                                                   |
| Canvas          | `color.surface.canvas` / `--color-surface-canvas`    | `#FBF9F5` | Default page background.                                                  |
| Raised          | `color.surface.raised` / `--color-surface-raised`    | `#FFFFFF` | Cards, forms, dialogs, and raised content.                                |
| Primary text    | `color.text.primary` / `--color-text-primary`        | `#1D211F` | Headings and high-emphasis copy.                                          |
| Secondary text  | `color.text.secondary` / `--color-text-secondary`    | `#5D645F` | Supporting copy only after contrast verification.                         |
| Success         | `color.status.success`                               | `#267A58` | Positive status with text/icon.                                           |
| Warning         | `color.status.warning`                               | `#A76617` | Caution/pending with text/icon.                                           |
| Danger          | `color.status.danger`                                | `#B33A3A` | Validation/destructive state with text/icon.                              |
| Info            | `color.status.info`                                  | `#356A8A` | Neutral information with text/icon.                                       |

`surface.inverse` and `text.inverse` are `#171715` and `#FFFFFF`. The status,
inverse, border, focus, disabled, hover, pressed, and skeleton mappings are not
all exported in CSS yet. Implementers should extend the shared token package
only when the active slice requires them, not hard-code per screen. Test every
foreground/background pair to WCAG 2.2 AA; colour never carries meaning alone.
Dark mode is not established by the approved committed baseline and must not be
invented in S02.

### 4.3 Typography

- **Display:** editorial serif character. The QA record establishes
  Georgia/system serif in the approved S02 prototype, but the exact Landing and
  Package typeface is unresolved until references are restored.
- **Interface/body:** compact system sans. The committed token is
  `Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif`;
  Inter is not bundled, so system fallback is the current reliable behaviour.
- **Current foundation example, not a complete scale:** display `clamp(2.5rem,
7vw, 5rem)`, weight 600, tracking `-0.04em`, line-height `.98`; body
  `1.125rem/1.7`; eyebrow `.8125rem`, weight 700, tracking `.12em`, uppercase.
- Body and form controls should remain at least 1rem on small screens, wrap
  under text expansion, and keep long-form measure near 60–75 characters.
- Use tabular figures for prices, capacities, versions, and dates where visual
  alignment benefits. Always label currency and price basis in text.

Do not promote the foundation example into additional type roles without visual
comparison. Exact role sizes for card titles, labels, captions, buttons, admin
headings, and navigation remain unresolved.

### 4.4 Spacing, containers, and grid

- Committed spacing primitives are `--space-3: .75rem`, `--space-6: 1.5rem`,
  and `--space-16: 4rem`.
- The foundation placeholder uses a readable `48rem` max-width, centered, with
  `4rem 1.5rem` page padding. This is evidence for editorial rhythm, not proof
  of the Landing/Package container or grid.
- QA establishes generous desktop editorial spacing, responsive stacking, and
  no horizontal overflow at 390 × 844.
- Maintain a 4/8px-compatible rhythm when a needed value is absent; first add a
  shared semantic token justified by the component, rather than scattering raw
  values.
- Preserve content order on mobile. Primary trust and price information must
  precede secondary detail. Avoid fixed content heights and clipping.

Exact container maximums, desktop columns, gaps, section padding, and additional
breakpoints are **unresolved**. S02 must at minimum verify 390 × 844 and a
representative desktop viewport; also check 200% zoom and text expansion.

### 4.5 Shape, borders, elevation, and motion

- Radii: small `.5rem`, medium `.875rem`, large `1.25rem`, pill `999px`.
- QA establishes light borders, restrained radii, and minimal elevation. Exact
  border/elevation values are unresolved; do not invent decorative shadows.
- Motion: fast `120ms`, standard `200ms`, slow `320ms`, easing
  `cubic-bezier(.2, 0, 0, 1)`.
- Motion communicates interaction state only. Prefer opacity/colour/transform,
  avoid layout shift, and never delay task completion for decoration.
- The committed global reduced-motion rule effectively disables transitions and
  animations and prevents forced smooth scrolling. Preserve it.

### 4.6 Imagery and icons

- Approved art direction is bright, respectful photographic Haramain imagery
  with an ivory/green/gold context and purposeful cropping.
- Religious imagery must be authentic, licensed, respectful, have meaningful
  alt text when informative, and never act as unsupported evidence of trust.
- Reserve image dimensions/aspect ratio to avoid layout shift; prioritize only
  above-fold/LCP imagery and lazy-load below-fold media.
- QA says the absent S02 prototype used Phosphor compact line icons. No icon
  package is currently installed in the app, so there is no reusable committed
  icon implementation. Restore/reuse the approved prototype choice if supplied;
  otherwise an icon dependency requires active-slice justification and visual
  approval. Do not use emoji as structural icons or handcrafted substitutes for
  brand assets.
- Keep stroke, optical size, alignment, and outline/filled treatment consistent;
  icon-only controls need an accessible name and at least a 44 × 44 CSS px hit
  target.

## 5. Component language

The component inventory describes intended baseline components, not committed
implementations. S02 should build only the minimum subset it actually uses and
promote a component to shared status only after the inventory's approval rule
is met.

### 5.1 Buttons and links

- One clear primary action per step; green may express an approved positive
  action, while secondary/tertiary actions remain subordinate.
- Use semantic `button` and link elements, visible text, stable hover/pressed/
  focus states, genuine `disabled` state, and a 44px-equivalent target.
- Async buttons expose pending state, prevent duplicate submission, and retain
  a readable label. Destructive or irreversible actions require distinct copy
  and confirmation, not colour alone.
- Exact fill, border, padding, and font specifications are unresolved pending
  source restoration.

### 5.2 Cards and trust disclosure

- Raised white surfaces on the calm canvas, light separation, restrained radius,
  minimal or no elevation, and editorial whitespace.
- Discovery cards render dynamic data and tolerate missing, long, zero/one/many,
  and conditional values. Never assume quad sharing, one hotel, or fixed
  inclusions.
- Place operator identity and text verification treatment prominently. Show
  departure/route, dates and duration, tier, total starting price per person,
  dynamic inclusion highlights, and an honest availability label.
- Do not add “Book now,” instalment claims, hotel claims, timers, popularity,
  scarcity copy, or other S03/future behaviour.

### 5.3 Forms and admin workflow

- Use semantic native controls wherever they satisfy the approved interaction.
  Every field has a persistent visible label; supporting constraints/help and
  required state are explicit.
- Group the admin flow into numbered, readable sections as established by QA.
  Preserve valid input through failure.
- Associate inline errors with fields, provide an error summary that links to
  invalid fields, and move focus to the summary after failed submission.
- Review repeats consequential public values. Publication uses an explicitly
  labelled modal/dialog that repeats public package name, total price, and
  capacity, with focus containment, labelled semantics, Escape/cancel path, and
  safe focus return.
- Success announces the published state and identifier. Permission denial,
  concurrency conflict, validation, network failure, and retry are distinct.

### 5.4 Status and feedback

- Always pair status colour with plain language and, where useful, a consistent
  icon. Do not rely on colour, position, or icon alone.
- Keep lifecycle (`Draft`, `Published`) separate from operator verification and
  availability (`exact`, `limited`, `waitlist-only`, `unavailable`). Do not
  visually imply one from another.
- Loading uses a labelled status/skeleton without false data. Empty states are
  calm and provide only approved human-support guidance. Error and offline
  states explain the difference and provide retry where meaningful. Cached
  public data must show freshness.
- Use appropriate live regions for status updates without stealing focus.

### 5.5 Header, navigation, and footer

The approved Landing/Package header, navigation, and customer footer are not
present in this checkout, so their exact structure, copy, links, spacing, and
responsive behaviour cannot be extracted safely.

- Applicable customer pages must reuse one customer-facing header/navigation and
  the existing approved customer footer once their source implementation is
  restored; do not make a discovery-specific variant.
- Preserve semantic landmarks (`header`, `nav`, `main`, `footer`), a skip link,
  logical heading order, visible current-page state, keyboard access, and
  responsive navigation semantics.
- Staff navigation is a separate task-oriented surface and must not masquerade
  as customer navigation, but should reuse tokens and interaction primitives.
- Do not invent footer policy, support claims, contact details, legal links, or
  social links. The absence of the approved footer is a visual/content blocker
  to completing customer visual QA, not permission to replace it.

## 6. Accessibility and responsive contract

All active-slice UI must meet WCAG 2.2 AA and the stricter explicit S02
criteria:

- semantic landmarks, headings, lists, forms, tables, buttons, links, and dialog;
- keyboard operation and logical focus order with a clearly visible focus ring;
- error association, focusable error summary, and announced async status;
- 44px-equivalent interactive targets with adequate separation;
- no horizontal overflow at 390px, 200% zoom, or supported text expansion;
- normal text contrast at least 4.5:1 and large text/UI graphics at least 3:1;
- meaningful image alternatives and decorative images hidden from assistive
  technology;
- no information communicated by colour alone;
- reduced-motion behaviour using the committed global override; and
- responsive screenshots and keyboard/screen-reader review, not inference from
  automated checks alone.

Every data-driven S02 surface includes results, loading, empty, error, offline,
and meaningful retry states. Admin state additionally includes validation,
permission denial, review, publication confirmation, conflict/failure, and
success.

## 7. Reuse map for S02

### Reuse now

- root Next.js layout and global stylesheet entry point;
- shared CSS/JSON colour, radius, and motion tokens;
- reduced-motion override;
- foundation sans stack and the documented editorial-serif direction;
- API host, PostgreSQL Compose, CI gates, ADRs, governance, and PR template; and
- established trust copy principles and dynamic-content rules from the approved
  slice/QA documentation.

### Restore, then reuse

- approved customer header, navigation, and footer;
- Landing/Package container, grid, section, card, button, icon, image, and type
  treatments;
- S02 admin numbered sections, review, confirmation dialog, and success pattern;
- S02 discovery cards and state surfaces; and
- approved images, icon setup, screenshots, and visual-comparison harness.

### Do not claim as existing

No reusable React button, card, form, badge, dialog, header, footer, navigation,
package, operator, or state component is currently committed. The component
inventory is a roadmap/approval catalogue, not implementation evidence.

## 8. Inconsistencies and risks relevant to S02

1. **Missing primary visual sources (blocking AC-13):** both approved primary
   screens and their images are absent.
2. **Broken prototype reference:** the approved S02 slice links a ZIP that is
   absent; QA describes files and an implementation that are also absent.
3. **No existing customer chrome:** the requirement to keep the footer
   consistent cannot be executed from current code without restoring it.
4. **Placeholder versus approved identity:** `/` is an S01 message, not the
   approved Landing page. Replacing or extrapolating it during this baseline
   task would redesign completed work and is prohibited.
5. **Token parity:** JSON has inverse/status/radius/motion values missing from
   CSS; CSS has spacing/font variables missing from JSON. There are no border,
   focus, elevation, breakpoint, icon-size, disabled, or skeleton tokens.
6. **Typography provenance:** the QA-approved prototype used Georgia/system
   serif while the app exports only a sans family; the exact source display face
   is unknown.
7. **Prototype dependency mismatch:** QA names Phosphor, but the web app has no
   icon dependency or assets.
8. **Responsive evidence mismatch:** QA records 390px success but no committed
   runnable surface/screenshots exist to reproduce it.
9. **Accessibility gap:** reduced motion exists, but no skip link, focus token,
   shared focus treatment, semantic data-flow states, accessibility automation,
   or E2E harness exists yet.
10. **Flow gap:** no customer/admin routes or components exist, so S02 should
    reuse foundations rather than pretend product components already exist.

These findings should be resolved in the relevant S02 tasks, except visual
source restoration, which should occur before visual implementation. Do not
expand this baseline task into S02 implementation.

## 9. Recommended S02 implementation plan

Follow the approved `S02-T01`–`S02-T16` sequence, using the smallest complete
vertical changes:

1. **Recover visual evidence:** restore the approved Landing/Package references,
   S02 prototype, approved assets, and comparison view; confirm licensing and
   product-owner provenance. Reconcile this file only where exact evidence
   differs.
2. **Record controls:** commit the S02 threat-model delta (`TR-SEC-001`) and make
   the authentication/CSRF decision explicit before mutation UI/API work.
3. **Define contracts:** OpenAPI for the three approved operations, safe public
   DTOs, camelCase/UTC/correlation conventions, RFC 9457 errors, and allowlisted
   values. Do not add generic CRUD.
4. **Create module boundaries:** minimum Operators approval contract and
   Catalogue vertical slice, with architecture tests preventing persistence
   coupling.
5. **Model and persist:** only the approved package, batch, price version,
   inclusions, lifecycle, concurrency, publication audit, and forward-only
   PostgreSQL migration; cover invariants with focused tests.
6. **Secure commands:** deny by default, isolate operators server-side, prevent
   mass assignment, create drafts, and publish atomically with version/audit and
   rollback tests.
7. **Expose discovery:** return only eligible published safe projections in the
   approved order with truthful availability, bounded rate controls, safe
   caching, and leakage tests.
8. **Build the admin surface:** reuse restored approved patterns; implement
   create → review → explicit confirmation → success plus validation,
   permission, stale/conflict, failure, and retry states.
9. **Build customer discovery:** reuse the restored shared customer header and
   footer and approved discovery patterns; render dynamic summaries and all
   data states without S03 links/claims/behaviour.
10. **Add observability and runbook:** safe allowlisted metrics/logs/traces and
    `batch_list_viewed` properties only; document missing-batch diagnosis.
11. **Verify end to end:** unit, PostgreSQL integration, architecture, component,
    accessibility, E2E, security, privacy, responsive, reduced-motion, and
    screenshot comparison at 390 × 844 and desktop.
12. **Close only with evidence:** full repository validation, green CI,
    screenshot comparison, and dated product-owner acceptance. Do not begin S03.

## 10. Baseline change control

- A durable change to the approved visual direction requires review and
  versioning under ADR-0002; record a new ADR when the architectural decision
  changes.
- Extend shared tokens/components only for an active approved slice and only
  after checking existing implementation, platform capability, and installed
  dependencies.
- Review any new/changed shared component for responsive, keyboard,
  screen-reader, reduced-motion, loading, error, and screenshot states before
  calling it baseline.
- Record unresolved conflicts rather than silently selecting a visual value or
  product policy.
