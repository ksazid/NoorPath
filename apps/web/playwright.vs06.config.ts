import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  outputDir: "./test-results/vs06",
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [
    ["html", { open: "never", outputFolder: "playwright-report/vs06" }],
    ["github"],
  ],
  use: {
    baseURL: "http://127.0.0.1:3000",
    trace: "retain-on-failure",
    screenshot: "only-on-failure",
  },
  projects: [
    {
      name: "desktop-chromium",
      use: {
        ...devices["Desktop Chrome"],
        viewport: { width: 1363, height: 936 },
      },
    },
  ],
  webServer: {
    command: "pnpm build && pnpm start --hostname 127.0.0.1 --port 3000",
    url: "http://127.0.0.1:3000",
    reuseExistingServer: false,
    timeout: 180_000,
  },
});
