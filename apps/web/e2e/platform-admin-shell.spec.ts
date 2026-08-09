import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

async function arrangeAdminShell(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/platform/access", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        accountId: "platform-administrator",
        displayName: "Platform Administrator",
      }),
    }),
  );
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

test("platform admin shell keeps section navigation and footer consistent", async ({
  page,
}) => {
  await arrangeAdminShell(page);
  await page.goto("/admin");

  await expect(
    page.getByRole("heading", { level: 1, name: "Platform operations" }),
  ).toBeVisible();
  await expect(
    page.getByRole("img", { name: "NoorPath" }).first(),
  ).toBeVisible();

  const navigation = page
    .locator(".np-platform-admin-shell .np-staff-sidebar")
    .getByRole("navigation", { name: "Platform administration navigation" });
  const overview = navigation.getByRole("link", { name: "Overview" });
  const operators = navigation.getByRole("link", { name: "Operators" });

  await expect(overview).toHaveAttribute("aria-current", "page");
  await operators.click();
  await expect(page).toHaveURL(/\/admin#operators$/);
  await expect(operators).toHaveAttribute("aria-current", "page");
  await expect(
    page.getByRole("heading", { level: 2, name: "Operator lifecycle" }),
  ).toBeVisible();

  await expect(
    page.getByRole("contentinfo").getByText("NoorPath Platform Administration"),
  ).toBeVisible();

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("platform admin shell denies unauthorized identities before rendering admin navigation", async ({
  page,
}) => {
  await page.route("**/api/v1/platform/access", (route) =>
    route.fulfill({ status: 403 }),
  );

  await page.goto("/admin");

  await expect(
    page.getByRole("heading", {
      level: 1,
      name: "Platform administrator access required",
    }),
  ).toBeVisible();
  await expect(
    page.getByRole("navigation", { name: "Platform administration navigation" }),
  ).toHaveCount(0);
  await expect(page.getByRole("link", { name: "Return to account" })).toBeVisible();
});
