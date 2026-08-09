import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

test("sign in offers Google without collecting credentials", async ({
  page,
}) => {
  await page.goto("/auth/sign-in?returnUrl=/account");

  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "Sign in to NoorPath",
  );
  await expect(
    page.getByRole("link", { name: "Continue with Google" }),
  ).toHaveAttribute("href", /method=google/);
  await expect(page.getByText(/Phone OTP will be enabled/)).toBeVisible();
  await expect(page.locator("input")).toHaveCount(0);
  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
  await expect(page).toHaveScreenshot("sign-in.png", { fullPage: true });
});

for (const account of [
  { path: "/account", endpoint: "account", heading: "My NoorPath" },
  { path: "/admin", endpoint: "platform", heading: "Platform operations" },
]) {
  test(`${account.heading} is protected and responsive`, async ({ page }) => {
    await page.route(`**/api/v1/${account.endpoint}/access`, (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          accountId: `${account.endpoint}-demo`,
          displayName: "Demo member",
        }),
      }),
    );

    if (account.endpoint === "platform") {
      await page.route("**/api/v1/platform/operators/summary", (route) =>
        route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            total: 0,
            pendingApproval: 0,
            approved: 0,
            suspended: 0,
            rejected: 0,
            deactivated: 0,
          }),
        }),
      );
      await page.route("**/api/v1/platform/operators", (route) =>
        route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({ items: [] }),
        }),
      );
    }

    await page.goto(account.path);
    await expect(page.getByRole("heading", { level: 1 })).toHaveText(
      account.heading,
    );
    await expectNoA11yViolations(page);
    await expectMinimumTargets(page);
    await expectNoHorizontalOverflow(page);

    if (account.endpoint === "account") {
      await expect(page).toHaveScreenshot(`${account.endpoint}-shell.png`, {
        fullPage: true,
      });
    }
  });
}
