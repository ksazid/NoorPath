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

## Working agreement

Read [AGENTS.md](AGENTS.md) before making changes. A slice is complete only when its acceptance criteria, tests, security checks, accessibility checks, and visual approval requirements pass.
