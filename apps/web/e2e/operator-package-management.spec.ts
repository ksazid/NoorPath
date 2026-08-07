import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const items = [
  {
    departureId: "40000000-0000-0000-0000-000000000001",
    packageTemplateId: "50000000-0000-0000-0000-000000000001",
    packageVersionId: "60000000-0000-0000-0000-000000000001",
    packageName: "Delhi Winter Umrah",
    summary: "12 day guided package",
    origin: "Delhi (DEL)",
    departureDate: "2027-01-10",
    returnDate: "2027-01-21",
    status: "draft",
    version: 3,
    updatedAtUtc: "2026-08-07T10:00:00Z",
  },
  {
    departureId: "40000000-0000-0000-0000-000000000002",
    packageTemplateId: "50000000-0000-0000-0000-000000000002",
    packageVersionId: "60000000-0000-0000-0000-000000000002",
    packageName: "Mumbai Ramadan Umrah",
    summary: "Ramadan departure from Mumbai",
    origin: "Mumbai (BOM)",
    departureDate: "2027-02-15",
    returnDate: "2027-02-26",
    status: "readyForReview",
    version: 4,
    updatedAtUtc: "2026-08-07T10:00:00Z",
  },
  {
    departureId: "40000000-0000-0000-0000-000000000003",
    packageTemplateId: "50000000-0000-0000-0000-000000000003",
    packageVersionId: "60000000-0000-0000-0000-000000000003",
    packageName: "Kolkata Spring Umrah",
    summary: "Published spring package",
    origin: "Kolkata (CCU)",
    departureDate: "2027-03-20",
    returnDate: "2027-03-31",
    status: "published",
    version: 5,
    updatedAtUtc: "2026-08-07T10:00:00Z",
  },
];

test.beforeEach(async ({ page }) => {
  await page.route("**/api/v1/operator/catalogue", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ items }),
    });
  });
});

test("operator package management shows lifecycle summary and contextual actions", async ({
  page,
}) => {
  await page.goto("/operator/packages");

  await expect(
    page.getByRole("heading", { name: "Package management" }),
  ).toBeVisible();
  await expect(page.getByText("All packages")).toBeVisible();
  await expect(
    page.getByText("Awaiting approval", { exact: true }).first(),
  ).toBeVisible();
  await expect(
    page.getByText("Published", { exact: true }).first(),
  ).toBeVisible();

  const draft = page
    .getByRole("article")
    .filter({ hasText: "Delhi Winter Umrah" });
  await expect(
    draft.getByRole("link", { name: "Continue setup" }),
  ).toHaveAttribute(
    "href",
    "/operator/departures/40000000-0000-0000-0000-000000000001",
  );

  const review = page
    .getByRole("article")
    .filter({ hasText: "Mumbai Ramadan Umrah" });
  await expect(
    review.getByRole("link", { name: "View approval status" }),
  ).toHaveAttribute(
    "href",
    "/operator/departures/40000000-0000-0000-0000-000000000002/review",
  );

  const published = page
    .getByRole("article")
    .filter({ hasText: "Kolkata Spring Umrah" });
  await expect(
    published.getByRole("link", { name: "View customer page" }),
  ).toHaveAttribute("href", "/packages/40000000-0000-0000-0000-000000000003");

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("operator can filter and search packages without leaving the workspace", async ({
  page,
}) => {
  await page.goto("/operator/packages");

  await page.getByLabel("Status").selectOption("published");
  await expect(
    page.getByRole("heading", { name: "Kolkata Spring Umrah" }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Delhi Winter Umrah" }),
  ).toHaveCount(0);

  await page.getByLabel("Status").selectOption("all");
  await page.getByLabel("Search packages").fill("Mumbai");
  await expect(
    page.getByRole("heading", { name: "Mumbai Ramadan Umrah" }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Kolkata Spring Umrah" }),
  ).toHaveCount(0);
});
