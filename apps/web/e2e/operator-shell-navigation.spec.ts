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

async function operatorNavigation(page: Page) {
  if ((page.viewportSize()?.width ?? 1280) <= 900) {
    const menu = page.locator(".np-staff-menu");
    const summary = menu.locator(":scope > summary");
    await expect(summary).toBeVisible();
    if (
      !(await menu.evaluate(
        (element) => (element as HTMLDetailsElement).open,
      ))
    ) {
      await summary.click();
    }
    return menu.locator(".np-staff-menu__panel");
  }

  return page.locator(".np-staff-sidebar");
}

test("operator collections share one wordmark, header and route navigation", async ({
  page,
}) => {
  await mockOperatorShell(page);

  await page.goto("/operator/packages");
  const header = page.locator(".np-staff-header");
  const packagesNavigation = await operatorNavigation(page);
  const packageLink = packagesNavigation.locator(
    'a[href="/operator/packages"][aria-current="page"]',
  );

  await expect(header).toBeVisible();
  await expect(packagesNavigation).toBeVisible();
  await expect(header.getByRole("img", { name: "NoorPath" })).toBeVisible();
  await expect(header.getByText("Noor Travel", { exact: true })).toHaveText(
    "Noor Travel",
  );
  await expect(packageLink).toBeVisible();
  await expect(
    packagesNavigation.getByRole("link", { name: "Departures" }),
  ).toHaveAttribute("href", "/operator/departures");

  const packagesHeader = await header.boundingBox();
  await page.goto("/operator/departures");
  const departuresHeader = await header.boundingBox();
  const departuresNavigation = await operatorNavigation(page);
  const departureLink = departuresNavigation.locator(
    'a[href="/operator/departures"][aria-current="page"]',
  );

  expect(packagesHeader?.x).toBe(departuresHeader?.x);
  expect(packagesHeader?.width).toBe(departuresHeader?.width);
  await expect(departuresNavigation).toBeVisible();
  await expect(departureLink).toBeVisible();
});

test("package authoring keeps task UI but removes competing operator chrome", async ({
  page,
}) => {
  await mockOperatorShell(page);
  await page.goto("/operator/packages/new");

  const navigation = await operatorNavigation(page);
  const embeddedSidebar = page.locator(
    ".np-operator-legacy-embed .admin-sidebar",
  );
  const wordmark = page
    .locator(".np-staff-header")
    .getByRole("img", { name: "NoorPath" });
  const packageHeading = page.getByRole("heading", {
    name: "Create a package draft",
  });

  await expect(page.locator(".np-staff-header")).toBeVisible();
  await expect(navigation).toBeVisible();
  await expect(page.locator(".np-operator-legacy-embed")).toBeVisible();
  await expect(embeddedSidebar).toBeHidden();
  await expect(wordmark).toBeVisible();
  await expect(packageHeading).toBeVisible();
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
  const departuresLink = page
    .locator(".np-staff-menu__panel")
    .getByRole("link", { name: "Departures" });
  await expect(departuresLink).toBeVisible();

  const overflow = await page.evaluate(
    () =>
      document.documentElement.scrollWidth >
      document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});
