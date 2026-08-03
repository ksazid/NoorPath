import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

test("release candidate exposes truthful live and ready contracts", async ({
  request,
}) => {
  const live = await request.get("http://127.0.0.1:5080/health/live");
  expect(live.status()).toBe(200);
  expect(await live.json()).toMatchObject({ status: "Healthy" });

  const ready = await request.get("http://127.0.0.1:5080/health/ready");
  expect(ready.status()).toBe(200);
  expect(await ready.json()).toMatchObject({ status: "Ready" });

  expect(live.headers()["x-content-type-options"]).toBe("nosniff");
  expect(ready.headers()["x-correlation-id"]).toBeTruthy();
});

test("critical customer and operator entry points remain accessible and responsive", async ({
  page,
}, testInfo) => {
  await page.route("**/api/v1/account/access", (route) =>
    route.fulfill({ status: 200, json: { accountId: "release-customer" } }),
  );
  await page.route("**/api/v1/platform/access", (route) =>
    route.fulfill({ status: 403, json: { code: "forbidden" } }),
  );
  await page.route("**/api/v1/operator/access", (route) =>
    route.fulfill({
      status: 200,
      json: {
        accountId: "release-operator",
        operator: { id: "noor", displayName: "Noor Tours" },
        permissions: ["operator.admin.access"],
      },
    }),
  );

  await page.goto("/auth/sign-in?returnUrl=/account");
  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "Sign in to NoorPath",
  );
  await expect(
    page.getByRole("link", { name: "Continue with Google" }),
  ).toBeVisible();
  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);

  await page.goto("/account");
  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "My NoorPath",
  );
  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);

  await page.goto("/operator");
  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "Operator administration",
  );
  await expect(page.getByText("Your secure workspace is ready")).toBeVisible();
  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);

  await page.screenshot({
    path: `test-results/production-readiness-entry-points-${testInfo.project.name}.png`,
    fullPage: true,
  });
});
