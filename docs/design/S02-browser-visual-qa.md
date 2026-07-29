# S02 browser and visual QA

The reproducible S02 browser suite uses Playwright Chromium at the approved
1363 × 936 desktop review viewport and the approved 390 × 844 mobile viewport.
It covers publication, non-publication, customer data states, keyboard/dialog
behavior, axe WCAG scanning, reduced motion, target sizing, text expansion, and
horizontal overflow.

Run the complete suite with PostgreSQL and the ASP.NET API available:

```bash
pnpm --filter @noorpath/web e2e
```

The first reviewed baseline must be captured explicitly with:

```bash
pnpm --filter @noorpath/web e2e --update-snapshots
```

Before committing any generated snapshot, compare the desktop and mobile
results against, in authority order:

1. `design-references/noorpath-landing-reference.png`
2. `design-references/noorpath-package-reference.png`
3. `NoorPath-S02-Approval-Prototype.zip`
4. `design-system/MASTER.md`

Snapshot generation is not visual acceptance. Record the reviewer, date,
commit, viewport, evidence artifact, material differences, and product-owner
decision. CI runs without `--update-snapshots`; missing or changed baselines
fail rather than silently approving a visual change.
