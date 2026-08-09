import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

async function arrangePlatformAccess(page: import("@playwright/test").Page) {
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
}

test("platform approver sees submitted packages with immutable review context", async ({
  page,
}) => {
  await arrangePlatformAccess(page);
  await page.route("**/api/v1/platform/publications", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        items: [
          {
            departureId: "50000000-0000-0000-0000-000000000002",
            operatorId: "operator-b",
            packageName: "Premium Umrah from Mumbai",
            origin: "Mumbai (BOM)",
            departureDate: "2027-02-15",
            returnDate: "2027-02-27",
            departureVersion: 4,
            submittedAtUtc: "2026-08-07T10:30:00Z",
          },
          {
            departureId: "50000000-0000-0000-0000-000000000001",
            operatorId: "operator-a",
            packageName: "Classic Umrah from Delhi",
            origin: "Delhi (DEL)",
            departureDate: "2027-01-10",
            returnDate: "2027-01-21",
            departureVersion: 7,
            submittedAtUtc: "2026-08-07T09:30:00Z",
          },
        ],
      }),
    });
  });

  await page.goto("/platform/publications");

  await expect(
    page.getByRole("heading", { level: 1, name: "Publication reviews" }),
  ).toBeVisible();
  await expect(
    page
      .getByRole("navigation", { name: "Platform administration navigation" })
      .getByRole("link", { name: "Publication reviews" }),
  ).toHaveAttribute("aria-current", "page");
  await expect(
    page.getByText("2 awaiting approval", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText("Independent approval", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText("Exact saved versions", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText("Customer-visible action", { exact: true }),
  ).toBeVisible();

  const cards = page.locator(".platform-approval-item");
  await expect(cards).toHaveCount(2);
  await expect(cards.nth(0)).toContainText("Classic Umrah from Delhi");
  await expect(cards.nth(0)).toContainText("Catalogue version");
  await expect(cards.nth(0)).toContainText("v7");
  await expect(cards.nth(1)).toContainText("Premium Umrah from Mumbai");

  await expect(
    page.getByRole("link", { name: "Review for publication" }).first(),
  ).toHaveAttribute(
    "href",
    "/platform/publications/50000000-0000-0000-0000-000000000001",
  );
  await expect(
    page.getByRole("contentinfo").getByText("NoorPath Platform Administration"),
  ).toBeVisible();

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("platform approval queue has an explicit empty state", async ({
  page,
}) => {
  await arrangePlatformAccess(page);
  await page.route("**/api/v1/platform/publications", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ items: [] }),
    });
  });

  await page.goto("/platform/publications");

  await expect(
    page.getByText("0 awaiting approval", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("status").getByText("No packages are waiting"),
  ).toBeVisible();
  await expect(
    page.getByText(
      "New operator submissions will appear here for independent review.",
      { exact: true },
    ),
  ).toBeVisible();
});

test("platform administration navigation remains usable at mobile width", async ({
  page,
}) => {
  await arrangePlatformAccess(page);
  await page.route("**/api/v1/platform/publications", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ items: [] }),
    }),
  );
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/platform/publications");

  const menu = page.locator(".np-platform-admin-shell .np-staff-menu");
  await expect(menu.getByText("Platform Admin menu", { exact: true })).toBeVisible();
  await menu.getByText("Platform Admin menu", { exact: true }).click();
  await expect(
    menu.getByRole("link", { name: "Publication reviews" }),
  ).toHaveAttribute("aria-current", "page");

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});
