import { expect, test, type Page } from "@playwright/test";

const operatorAccess = {
  accountId: "operator-member-a",
  displayName: "Yusuf Ali",
  operator: { id: "operator-a", displayName: "Noor Travel" },
  permissions: ["operator.admin.access"],
};

async function mockOperatorShell(page: Page) {
  await page.route("**/api/v1/operator/access", (route) =>
    route.fulfill({ status: 200, json: operatorAccess }),
  );
  await page.route("**/api/v1/operator/catalogue", (route) =>
    route.fulfill({ status: 200, json: { items: [] } }),
  );
}

test("operator collections share one wordmark, header and route navigation", async ({
  page,
}) => {
  await mockOperatorShell(page);

  await page.goto("/operator/packages");
  const header = page.locator(".np-staff-header");
  const sidebar = page.locator(".np-staff-sidebar");

  await expect(header).toBeVisible();
  await expect(sidebar).toBeVisible();
  await expect(
    header.getByRole("img", { name: "NoorPath" }),
  ).toBeVisible();
  await expect(header.getByText("Noor Travel", { exact: true })).toBeVisible();
  await expect(
    page.locator('a[href="/operator/packages"][aria-current="page"]'),
  ).toBeVisible();
  await expect(
    page.locator('a[href="/operator/departures"]'),
  ).toHaveCount(2);

  const packagesHeader = await header.boundingBox();
  await page.goto("/operator/departures");
  const departuresHeader = await header.boundingBox();

  expect(packagesHeader?.x).toBe(departuresHeader?.x);
  expect(packagesHeader?.width).toBe(departuresHeader?.width);
  await expect(
    page.locator('a[href="/operator/departures"][aria-current="page"]'),
  ).toBeVisible();
});

test("package authoring keeps task UI but removes competing operator chrome", async ({
  page,
}) => {
  await mockOperatorShell(page);
  await page.goto("/operator/packages/new");

  await expect(page.locator(".np-staff-header")).toBeVisible();
  await expect(page.locator(".np-staff-sidebar")).toBeVisible();
  await expect(page.locator(".np-operator-legacy-embed")).toBeVisible();
  await expect(page.locator(".np-operator-legacy-embed .admin-sidebar")).toBeHidden();
  await expect(
    page.locator(".np-staff-header").getByRole("img", { name: "NoorPath" }),
  ).toBeVisible();
  await expect(page.getByRole("heading", { name: "Create a package draft" })).toBeVisible();
});

test("operator shell reflows to native mobile navigation without overflow", async ({
  page,
}) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await mockOperatorShell(page);
  await page.goto("/operator/packages");

  await expect(page.locator(".np-staff-sidebar")).toBeHidden();
  const menu = page.locator(".np-staff-menu > summary");
  await expect(menu).toBeVisible();
  await menu.click();
  await expect(
    page.locator(".np-staff-menu__panel").getByRole("link", { name: "Departures" }),
  ).toBeVisible();

  const overflow = await page.evaluate(
    () =>
      document.documentElement.scrollWidth >
      document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});
