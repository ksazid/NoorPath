# NoorPath Slice Delivery Factory

## Objective
Increase delivery speed without weakening feature completeness. Heavy CI is not an implementation feedback loop; it is the exact-head certification gate used after a slice is functionally complete.

## Operating model

### 1. Specify from a manifest
Every active product slice has one manifest in `delivery/slices/VS-XX.json` containing:

- outcome and actor;
- dependencies and domain ownership;
- route/journey transitions;
- acceptance criteria and exclusions;
- required quality gates;
- rendered test target;
- specification and checklist paths.

Run:

```bash
pnpm slice:validate
pnpm slice:new VS-09
pnpm slice:status VS-09
```

The generator is idempotent and never overwrites an existing specification or checklist.

### 2. Develop in Draft mode
A product slice PR stays Draft while implementation is changing.

- Full CI and rendered review do not run merely because another development commit was pushed.
- Local or targeted checks are used during implementation.
- The lightweight Slice Governance workflow validates manifests, required documents and PR discipline.
- Do not apply `certify` to an incomplete feature.

### 3. Enter certification mode explicitly
Apply the `certify` label only after:

- the complete product outcome works end to end;
- all required success, loading, empty, error, conflict, expiry and recovery states exist;
- local/targeted tests pass;
- the implementation checklist is reconciled;
- the PR body accurately describes scope and exclusions.

With `certify` present, every new commit invalidates prior evidence and automatically reruns:

- formatting, static analysis, Node tests and production build;
- .NET format/build/tests and architecture checks;
- clean PostgreSQL and module migration validation;
- secret scanning;
- slice-specific Chromium/WebKit rendered acceptance;
- accessibility, responsive and journey-linking evidence.

Remove `certify` before returning to active development.

### 4. Exact-head merge gate
The final merge gate is successful only when all required automated checks are green for the current PR head and `po-approved` is present.

The Product Owner applies `po-approved` only after reviewing:

- the exact feature outcome;
- rendered desktop and mobile evidence or a working preview;
- error, conflict and recovery behaviour;
- explicit exclusions and known limitations;
- the final SHA shown by certification.

Any later commit invalidates the prior approval and certification evidence.

## Preview policy
A hosted preview is useful but must not create a second implementation branch or duplicate application behaviour.

- Deterministic preview mode belongs in the same feature commit behind explicit non-production configuration.
- Preview publication failure caused by hosting limits is infrastructure evidence, not a product test failure.
- Rendered Playwright reports remain the reproducible fallback for Product Owner review.
- Production is never targeted by a slice preview workflow.

## Required labels

- `certify`: run full exact-head certification. Remove during further implementation.
- `po-approved`: Product Owner accepted the exact certified head.

## Merge discipline

- No slice merges merely because code compiles.
- No stale workflow run, artifact or approval can certify a newer SHA.
- No unresolved review thread or known regression remains at merge.
- Dependent slices may be specified early but implementation is stacked only after predecessor contracts are stable.
- VS-09, VS-10 and VS-11 therefore proceed in dependency order, while their manifests and acceptance contracts are prepared together.
