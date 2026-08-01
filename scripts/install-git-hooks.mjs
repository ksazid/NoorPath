import { chmodSync, mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { spawnSync } from "node:child_process";

function runGit(args, cwd) {
  return spawnSync("git", args, {
    cwd,
    encoding: "utf8",
    stdio: ["ignore", "pipe", "pipe"],
  });
}

if (process.env.CI) {
  console.log("Skipping local Git hook installation in CI.");
  process.exit(0);
}

const repositoryResult = runGit(["rev-parse", "--show-toplevel"]);
if (repositoryResult.status !== 0) {
  console.log("Skipping Git hook installation outside a Git working tree.");
  process.exit(0);
}

const repositoryRoot = repositoryResult.stdout.trim();
const gitDirectoryResult = runGit(
  ["rev-parse", "--path-format=absolute", "--git-dir"],
  repositoryRoot,
);

if (gitDirectoryResult.status !== 0) {
  throw new Error(gitDirectoryResult.stderr.trim() || "Git directory not found.");
}

const hooksDirectory = join(gitDirectoryResult.stdout.trim(), "noorpath-hooks");
const preCommitPath = join(hooksDirectory, "pre-commit");
const preCommitWrapper = `#!/usr/bin/env sh
set -eu
repository_root="$(git rev-parse --show-toplevel)"
exec node "$repository_root/scripts/pre-commit-format.mjs"
`;

mkdirSync(hooksDirectory, { recursive: true });
writeFileSync(preCommitPath, preCommitWrapper, "utf8");
chmodSync(preCommitPath, 0o755);

const configurationResult = runGit(
  ["config", "core.hooksPath", hooksDirectory],
  repositoryRoot,
);

if (configurationResult.status !== 0) {
  throw new Error(
    configurationResult.stderr.trim() || "Could not configure Git hooks.",
  );
}

console.log(`NoorPath Git hooks installed at ${hooksDirectory}.`);
