import { defineConfig, devices } from "@playwright/test";

const ci = Boolean(process.env.CI);

export default defineConfig({
  testDir: "./e2e",
  outputDir: "./test-results",
  fullyParallel: false,
  workers: 1,
  retries: ci ? 1 : 0,
  reporter: ci ? [["html", { open: "never" }], ["github"]] : "list",
  use: {
    baseURL: "http://127.0.0.1:3000",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  expect: {
    toHaveScreenshot: { animations: "disabled", maxDiffPixelRatio: 0.01 },
  },
  projects: [
    {
      name: "desktop-chromium",
      use: {
        ...devices["Desktop Chrome"],
        viewport: { width: 1363, height: 936 },
      },
    },
    {
      name: "mobile-390",
      use: { ...devices["iPhone 13"], viewport: { width: 390, height: 844 } },
    },
  ],
  webServer: [
    {
      command:
        "dotnet run --project ../api/NoorPath.Api.csproj --urls http://127.0.0.1:5080",
      url: "http://127.0.0.1:5080/health/ready",
      reuseExistingServer: !ci,
      timeout: 120_000,
    },
    {
      command: "pnpm build && pnpm start --hostname 127.0.0.1 --port 3000",
      url: "http://127.0.0.1:3000",
      reuseExistingServer: !ci,
      timeout: 180_000,
      env: { NOORPATH_API_URL: "http://127.0.0.1:5080" },
    },
  ],
});
