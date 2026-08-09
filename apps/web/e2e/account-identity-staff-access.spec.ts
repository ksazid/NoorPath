import { expect, test, type Page } from "@playwright/test";

function accountMenu(page: Page, displayName: string) {
  return page.locator(
    `summary[aria-label="Account menu for ${displayName}"]:visible`,
  );
}

test("customer account menu exposes identity, safe options and staff login", async ({
  page,
}) => {
  await page.route("**/api/v1/account/access", (route) =>
    route.fulfill({
      status: 200,
      json: { accountId: "customer-a", displayName: "Amina Khan" },
    }),
  );

  await page.goto("/");

  const menu = accountMenu(page, "Amina Khan");
  await expect(menu).toBeVisible();
  await menu.click();

  await expect(page.getByRole("link", { name: "My account" })).toHaveAttribute(
    "href",
    "/account",
  );
  await expect(page.getByRole("link", { name: "Settings" })).toHaveAttribute(
    "href",
    "/account",
  );
  await expect(page.getByRole("link", { name: "Help" })).toHaveAttribute(
    "href",
    "/support",
  );
  await expect(page.getByRole("link", { name: "Log out" })).toHaveAttribute(
    "href",
    "/api/auth/sign-out",
  );
  await expect(
    page.getByRole("link", { name: "Operator / Admin login" }),
  ).toHaveAttribute("href", "/auth/sign-in?returnUrl=%2Foperator");
});

test("operator shell exposes member identity without losing operator context", async ({
  page,
}) => {
  await page.route("**/api/v1/operator/access", (route) =>
    route.fulfill({
      status: 200,
      json: {
        accountId: "operator-member-a",
        displayName: "Yusuf Ali",
        operator: { id: "operator-a", displayName: "Noor Travel" },
        permissions: ["operator.admin.access"],
      },
    }),
  );

  await page.goto("/operator/account");

  await expect(
    page.getByText("Noor Travel", { exact: true }).first(),
  ).toBeVisible();
  const menu = accountMenu(page, "Yusuf Ali");
  await expect(menu).toBeVisible();
  await menu.click();
  await expect(page.getByRole("link", { name: "My account" })).toHaveAttribute(
    "href",
    "/operator/account",
  );
  await expect(page.getByRole("link", { name: "Settings" })).toHaveAttribute(
    "href",
    "/operator/account",
  );
  await expect(page.getByRole("link", { name: "Help" })).toHaveAttribute(
    "href",
    "/operator/support",
  );
});

test("account actions reflow at customer mobile width", async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.route("**/api/v1/account/access", (route) =>
    route.fulfill({
      status: 200,
      json: { accountId: "customer-a", displayName: "Amina Khan" },
    }),
  );

  await page.goto("/");
  await expect(accountMenu(page, "Amina Khan")).toBeVisible();
  const overflow = await page.evaluate(
    () =>
      document.documentElement.scrollWidth >
      document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});
