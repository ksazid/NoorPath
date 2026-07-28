# S01: Engineering Foundation

- Status: Ready
- Outcome: The monorepo builds and validates repeatably, with architectural, security, design, and CI guardrails ready for product slices.
- Product code: No booking behavior is introduced in this slice.

## Acceptance criteria

1. The repository contains documented web, API, module, test, package, and documentation boundaries.
2. Durable engineering rules exist in `AGENTS.md`.
3. CI defines secret scanning, dependency restore, formatting, linting, type checking, builds, tests, and migration validation gates.
4. Approved NoorPath design tokens are machine-readable and consumable by the web app.
5. Architecture and design decisions are recorded as ADRs.
6. Local PostgreSQL can be started without committing credentials.
7. The next product slice can cite explicit requirements and reuse this foundation.

## 30–90 minute tasks

- S01-T01 Confirm repository owner, private visibility, and `main` default branch.
- S01-T02 Create monorepo directories and root governance files.
- S01-T03 Add Node, pnpm, and .NET version pins.
- S01-T04 Add the minimal Next.js PWA shell.
- S01-T05 Add machine-readable design tokens and reduced-motion defaults.
- S01-T06 Record the initial component inventory.
- S01-T07 Add API and module boundary placeholders.
- S01-T08 Add PostgreSQL local infrastructure with non-secret defaults.
- S01-T09 Add CI validation workflow.
- S01-T10 Add pull-request template and ownership metadata.
- S01-T11 Validate the Node workspace.
- S01-T12 Validate the .NET solution when a .NET 10 runner is available.
- S01-T13 Create the private GitHub repository and push the baseline.
- S01-T14 Configure branch protection after the first CI run.

## Exit evidence

- Clean CI run
- No committed secrets
- Web production build
- API build and test
- Design-token validation
- Approved foundation pull request

