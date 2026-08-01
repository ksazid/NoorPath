# NoorPath

Trusted, operator-backed Umrah booking and journey platform for Indian families.

## Repository status

This repository contains the engineering foundation for the NoorPath pilot. Product and technical baselines are maintained separately; implementation work must trace to approved requirement and slice IDs.

## Architecture

- Next.js customer and operations PWA
- ASP.NET Core .NET 10 modular monolith
- Clean Architecture dependency rules with vertical slices
- Selective CQRS; no generic repository or generic unit of work
- PostgreSQL system of record
- OpenTelemetry-compatible observability

## Structure

```text
apps/
  api/              ASP.NET Core host (created in the API foundation task)
  web/              Next.js PWA
src/
  Modules/          Domain modules and vertical slices
tests/
  Architecture/     Dependency and module-boundary tests
  Integration/      PostgreSQL-backed API tests
  Web.E2E/          Playwright critical-flow tests
packages/
  design-tokens/    Shared brand and semantic tokens
docs/
  adr/              Architecture decisions
  design/           Component inventory and visual rules
  slices/           Approved vertical-slice specifications
```

## Local prerequisites

- .NET 10 SDK
- Node.js 24 LTS
- pnpm 10
- Docker with Compose

Toolchain versions are pinned in `.node-version`, `package.json`, and
`global.json`. CI is the source of truth when a pinned runtime is unavailable
locally.

## Start local PostgreSQL

Copy `.env.example` to `.env`, set a local-only database password, then run:

```bash
docker compose up -d postgres
```

The example password is intentionally non-secret and is used only for local
development. `.env` files remain ignored by Git. CI validates the Compose file,
starts PostgreSQL, waits for its health check, and removes its test volume.

## Validation boundaries

- `apps/web` owns the Next.js shell and its tests.
- `apps/api` is the ASP.NET Core host; reusable inward dependencies live under
  `src`, and .NET tests live under `tests`.
- `packages/design-tokens` exposes machine-readable JSON and consumable CSS.
- `docs/adr`, `docs/design`, and `docs/slices` contain durable decisions,
  design governance, and approved slice specifications.
- `.github/workflows/ci.yml` gates secret scanning, dependency restore,
  formatting, linting, type checking, builds, tests, the empty foundation
  migration state, and a healthy PostgreSQL startup.

## Delivery commands

Run `pnpm preflight` during normal feature work. It performs the fast repository,
format, type/lint, and release-build checks without the full test and migration
model suite. Run `pnpm certify` only when an implementation is complete and full
certification has been explicitly requested (`pnpm precert` remains an alias).

Documentation-only changes under `docs/`, Markdown-only changes, and design
references do not start the full CI workflow. Workflow, delivery manifest, and
source changes continue to be validated, and outdated runs retain concurrency
cancellation.

## Free hosting and production approval

- Render uses [`render.yaml`](render.yaml) to build `apps/api` on the free web
  service plan. Automatic deploys are disabled and readiness is checked at
  `/health/ready`.
- Set `ConnectionStrings__NoorPath` in Render to the Neon pooled PostgreSQL
  connection string and set `Authentication__Authority` and
  `Authentication__Audience` for the production identity provider; never commit
  their values. `Database__MigrateOnStartup=true` applies every module's EF Core
  migrations sequentially before the API accepts traffic. Add the existing
  payment and authorisation settings from `.env.example` when those production
  capabilities are enabled.
- Set `NOORPATH_API_URL` in Vercel to the public Render API origin. Vercel Git
  deployments remain disabled in `apps/web/vercel.json`.
- Configure the GitHub `production` environment with the Product Owner as a
  required reviewer and add environment secrets `RENDER_DEPLOY_HOOK_URL` and
  `VERCEL_DEPLOY_HOOK_URL`. Each hook must deploy `main`.

Production is deployed only by manually running **Production deployment** from
GitHub Actions with the exact 40-character `main` commit SHA reviewed by the
Product Owner. The job pauses for the Product Owner's GitHub environment
approval, then verifies that `main` still matches that SHA before invoking the
native Render and Vercel deploy hooks. A mismatch stops the job without calling
either hook. Slice pull requests and merges to `main` cannot deploy. Do not
approve or run this workflow until the release is certified.

## Working agreement

Read [AGENTS.md](AGENTS.md) before making changes. A slice is complete only when its acceptance criteria, tests, security checks, accessibility checks, and visual approval requirements pass.
