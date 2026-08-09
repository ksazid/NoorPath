import { expect, test, type Page } from "@playwright/test";

const operatorAccess = {
  accountId: "operator-member-a",
  displayName: "Yusuf Ali",
  operator: { id: "operator-a", displayName: "Noor Travel" },
  permissions: ["operator.admin.access"],
};

async function mockOperator(page: Page) {
  await page.route("**/api/v1/operator/access", (route) =>
    route.fulfill({ status: 200, json: operatorAccess }),
  );
}

test("operator can move package items between Included and Not included", async ({
  page,
}) => {
  await mockOperator(page);
  await page.goto("/operator/packages/new");

  const included = page.getByTestId("included-board");
  const excluded = page.getByTestId("excluded-board");

  await expect(
    included.getByText("Return flights", { exact: true }),
  ).toBeVisible();
  await expect(
    excluded.getByText("Personal expenses", { exact: true }),
  ).toBeVisible();

  await included
    .locator('[data-item="Return flights"]')
    .getByRole("button", { name: "Move to Not included" })
    .click();

  await expect(
    included.getByText("Return flights", { exact: true }),
  ).toHaveCount(0);
  await expect(
    excluded.getByText("Return flights", { exact: true }),
  ).toBeVisible();
  await expect(page.getByRole("status")).toContainText(
    "Return flights moved to Not included",
  );

  await excluded
    .locator('[data-item="Return flights"]')
    .getByRole("button", { name: "Move to Included" })
    .click();
  await expect(
    included.getByText("Return flights", { exact: true }),
  ).toBeVisible();
  await expect(
    excluded.getByText("Return flights", { exact: true }),
  ).toHaveCount(0);
});

test("pointer drag uses the same mutually exclusive move behavior", async ({
  page,
}) => {
  await mockOperator(page);
  await page.goto("/operator/packages/new");

  const included = page.getByTestId("included-board");
  const excludedZone = page.locator(
    '.package-drop-zone[data-destination="excluded"]',
  );
  await included.locator('[data-item="Visa included"]').dragTo(excludedZone);

  await expect(
    included.getByText("Visa included", { exact: true }),
  ).toHaveCount(0);
  await expect(
    page
      .getByTestId("excluded-board")
      .getByText("Visa included", { exact: true }),
  ).toBeVisible();
});

test("custom item offers the existing icon family and explicit destination", async ({
  page,
}) => {
  await mockOperator(page);
  await page.goto("/operator/packages/new");

  await page.getByRole("button", { name: "Add custom item" }).click();
  await page.getByLabel("Custom item").fill("Wheelchair assistance");
  await page.getByLabel("Guidance").check();
  await page.getByLabel("Not included").check();
  await page.getByRole("button", { name: "Add item" }).click();

  const custom = page
    .getByTestId("excluded-board")
    .locator('[data-item="Wheelchair assistance"]');
  await expect(custom).toBeVisible();
  await expect(custom.locator("svg")).toBeVisible();
  await expect(
    page
      .getByTestId("included-board")
      .getByText("Wheelchair assistance", { exact: true }),
  ).toHaveCount(0);
});

test("composer reflows at 390px with usable move targets and shared navigation", async ({
  page,
}) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await mockOperator(page);
  await page.goto("/operator/packages/new");

  const move = page
    .getByTestId("included-board")
    .locator('[data-item="Return flights"]')
    .getByRole("button", { name: "Move to Not included" });
  const box = await move.boundingBox();
  expect(box?.height ?? 0).toBeGreaterThanOrEqual(44);

  const menu = page.locator(".np-staff-menu > summary");
  await expect(menu).toBeVisible();
  await menu.click();
  await expect(
    page
      .locator(".np-staff-menu__panel")
      .getByRole("link", { name: "Packages" }),
  ).toBeVisible();

  const overflow = await page.evaluate(
    () =>
      document.documentElement.scrollWidth >
      document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});
