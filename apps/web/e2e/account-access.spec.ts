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
        body: "{}",
      }),
    );
    await page.goto(account.path);
    await expect(page.getByRole("heading", { level: 1 })).toHaveText(
      account.heading,
    );
    await expectNoA11yViolations(page);
    await expectMinimumTargets(page);
    await expectNoHorizontalOverflow(page);
    await expect(page).toHaveScreenshot(`${account.endpoint}-shell.png`, {
      fullPage: true,
    });
  });
}
