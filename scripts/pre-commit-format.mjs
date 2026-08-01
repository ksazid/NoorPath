import { relative, resolve } from "node:path";
import { spawnSync } from "node:child_process";

const pnpmCommand = process.platform === "win32" ? "pnpm.cmd" : "pnpm";

function splitNullSeparated(value) {
  return value.split("\0").filter(Boolean);
}

function run(command, args, options = {}) {
  const result = spawnSync(command, args, {
    encoding: "utf8",
    ...options,
  });

  if (result.error) {
    throw result.error;
  }

  return result;
}

function runGit(args, repositoryRoot) {
  return run("git", args, {
    cwd: repositoryRoot,
    stdio: ["ignore", "pipe", "pipe"],
  });
}

function chunk(values, size) {
  const chunks = [];
  for (let index = 0; index < values.length; index += size) {
    chunks.push(values.slice(index, index + size));
  }
  return chunks;
}

const repositoryResult = runGit(["rev-parse", "--show-toplevel"]);
if (repositoryResult.status !== 0) {
  throw new Error(repositoryResult.stderr.trim() || "Git repository not found.");
}

const repositoryRoot = repositoryResult.stdout.trim();
const stagedResult = runGit(
  ["diff", "--cached", "--name-only", "--diff-filter=ACMR", "-z"],
  repositoryRoot,
);

if (stagedResult.status !== 0) {
  throw new Error(stagedResult.stderr.trim() || "Could not read staged files.");
}

const stagedFiles = splitNullSeparated(stagedResult.stdout);
if (stagedFiles.length === 0) {
  process.exit(0);
}

const unstagedResult = runGit(
  ["diff", "--name-only", "--diff-filter=ACMR", "-z"],
  repositoryRoot,
);

if (unstagedResult.status !== 0) {
  throw new Error(
    unstagedResult.stderr.trim() || "Could not read unstaged files.",
  );
}

const unstagedFiles = new Set(splitNullSeparated(unstagedResult.stdout));
const partiallyStagedFiles = stagedFiles.filter((file) =>
  unstagedFiles.has(file),
);

if (partiallyStagedFiles.length > 0) {
  console.error(
    "Prettier was not run because these files contain both staged and unstaged changes:",
  );
  for (const file of partiallyStagedFiles) {
    console.error(`  - ${file}`);
  }
  console.error("Stage or stash those changes, then commit again.");
  process.exit(1);
}

const webDirectory = resolve(repositoryRoot, "apps/web");
const prettierPaths = stagedFiles.map((file) =>
  relative(webDirectory, resolve(repositoryRoot, file)).replaceAll("\\", "/"),
);

for (const files of chunk(prettierPaths, 100)) {
  const prettierResult = run(
    pnpmCommand,
    [
      "--dir",
      "apps/web",
      "exec",
      "prettier",
      "--write",
      "--ignore-unknown",
      "--",
      ...files,
    ],
    { cwd: repositoryRoot, stdio: "inherit" },
  );

  if (prettierResult.status !== 0) {
    console.error(
      "Prettier failed. Run `corepack enable && pnpm install`, then commit again.",
    );
    process.exit(prettierResult.status ?? 1);
  }
}

for (const files of chunk(stagedFiles, 100)) {
  const addResult = runGit(["add", "--", ...files], repositoryRoot);
  if (addResult.status !== 0) {
    throw new Error(addResult.stderr.trim() || "Could not restage formatted files.");
  }
}

console.log(`Prettier formatted ${stagedFiles.length} staged file(s).`);
