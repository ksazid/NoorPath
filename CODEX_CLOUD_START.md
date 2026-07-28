# NoorPath: GitHub and Codex Cloud Start

This repository bundle contains the NoorPath engineering foundation. It does
not contain the confidential PRD or TRD.

## 1. Upload the repository

1. Extract `NoorPath-GitHub-Upload-Bundle.zip`.
2. Open the empty private GitHub repository `ksazid/NoorPath`.
3. Select **Add file → Upload files**.
4. Upload the extracted contents, including `.github`.
5. Commit directly to `main` with:

   `chore: establish NoorPath engineering foundation`

6. Confirm the **CI** workflow starts under the repository's **Actions** tab.

Alternatively, from Windows PowerShell with Git and GitHub CLI installed:

```powershell
cd C:\Projects\NoorPath
git init -b main
git add .
git commit -m "chore: establish NoorPath engineering foundation"
gh repo set-default ksazid/NoorPath
git remote add origin https://github.com/ksazid/NoorPath.git
git push -u origin main
```

## 2. Create the Codex Cloud environment

1. Open `https://chatgpt.com/codex/settings/environments`.
2. Select **Create environment**.
3. Choose `ksazid/NoorPath`.
4. Select the `main` branch.
5. Keep network access off unless a task genuinely needs it.
6. Add no production secrets at this stage.
7. Save the environment.

## 3. Run the engineering-foundation slice

Start a Codex Cloud task with:

> Read `AGENTS.md`, all files in `docs/adr`, and
> `docs/slices/S01-engineering-foundation.md`. Treat S01 as the only approved
> slice. Inspect and validate the existing foundation before changing it.
> Complete only missing S01 acceptance criteria that can be derived from the
> repository without inventing product policy. Run formatting, linting, type
> checking, unit tests, production builds, .NET tests, and a secret scan.
> Preserve the modular-monolith and Clean Architecture boundaries. If an
> acceptance criterion requires unavailable approved requirements or an
> external repository setting, report it as a blocker. Commit changes on a new
> branch and open a draft pull request linked to S01.

Expected validation commands:

```bash
corepack enable
pnpm install --frozen-lockfile
pnpm check
pnpm test
pnpm build
dotnet restore NoorPath.slnx
dotnet format NoorPath.slnx --verify-no-changes --no-restore
dotnet build NoorPath.slnx --configuration Release --no-restore
dotnet test NoorPath.slnx --configuration Release --no-build
```

## 4. Review and merge S01

Before merging:

- Confirm every S01 acceptance criterion has evidence in the pull request.
- Confirm CI is green.
- Confirm no secrets or confidential planning documents were committed.
- Review Codex's diff and unresolved blockers.
- Merge only after product-owner acceptance.

After the first successful CI run, configure `main` branch protection to require
the web and API checks and at least one reviewed pull request.

## 5. Prepare the first product slice

Do not ask Codex to infer the package-publishing product rules from this bundle.
First add an approved slice specification that cites the relevant PRD
requirement IDs and defines acceptance criteria for:

`Authorised admin publishes a valid departure batch → customer views it in the PWA`

Then run that product slice in a separate branch and pull request.
