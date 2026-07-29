import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const published = [
  {
    id: "batch-browser",
    operatorName: "Noor Tours",
    operatorVerified: true,
    packageName: "Browser Verified Journey",
    summary: "Supported journey",
    tier: "Comfort",
    departureCity: "Delhi",
    route: "Jeddah to Makkah",
    departureDate: "2026-10-10",
    returnDate: "2026-10-22",
    durationDays: 12,
    capacity: 24,
    totalStartingPriceInr: 94500,
    availability: 0,
    inclusions: ["Flights", "Breakfast"],
  },
];

test("loading resolves to published results", async ({ page }) => {
  await page.route("**/api/v1/batches", async (route) => {
    await new Promise((resolve) => setTimeout(resolve, 500));
    await route.fulfill({ json: published });
  });
  await page.goto("/");
  await expect(page.locator('[aria-busy="true"]')).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Browser Verified Journey" }),
  ).toBeVisible();
  await expectNoA11yViolations(page);
});

test("empty catalogue has an accessible calm support state", async ({
  page,
}) => {
  await page.route("**/api/v1/batches", (route) => route.fulfill({ json: [] }));
  await page.goto("/");
  await expect(page.getByRole("status")).toContainText(
    "New journeys are being prepared",
  );
  await expectNoA11yViolations(page);
  await expectNoHorizontalOverflow(page);
  await expect(page).toHaveScreenshot("customer-empty.png", { fullPage: true });
});

test("error retry recovers to results", async ({ page }) => {
  let attempts = 0;
  await page.route("**/api/v1/batches", (route) =>
    ++attempts === 1
      ? route.fulfill({ status: 503, json: {} })
      : route.fulfill({ json: published }),
  );
  await page.goto("/");
  await expect(page.getByRole("status")).toContainText(
    "We couldn’t load packages",
  );
  await page.getByRole("button", { name: "Try again" }).click();
  await expect(
    page.getByRole("heading", { name: "Browser Verified Journey" }),
  ).toBeVisible();
});

test("offline state recovers after connectivity returns", async ({
  page,
  context,
}) => {
  await page.route("**/api/v1/batches", (route) =>
    route.fulfill({ json: published }),
  );
  await page.goto("/");
  await expect(
    page.getByRole("heading", { name: "Browser Verified Journey" }),
  ).toBeVisible();
  await context.setOffline(true);
  await page.getByRole("button", { name: "offline" }).click();
  await expect(page.getByRole("status")).toContainText(
    "You appear to be offline",
  );
  await context.setOffline(false);
  await page.getByRole("button", { name: "Check connection" }).click();
  await expect(
    page.getByRole("heading", { name: "Browser Verified Journey" }),
  ).toBeVisible();
});

test("keyboard focus, target size, reduced motion, and 200% text remain usable", async ({
  page,
}) => {
  await page.route("**/api/v1/batches", (route) =>
    route.fulfill({ json: published }),
  );
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.goto("/");
  await page.keyboard.press("Tab");
  const focused = page.locator(":focus");
  await expect(focused).toBeVisible();
  expect(
    await focused.evaluate((element) => getComputedStyle(element).outlineStyle),
  ).not.toBe("none");
  await expectMinimumTargets(page);
  expect(
    await page
      .locator(".package-card")
      .evaluate((element) => getComputedStyle(element).transitionDuration),
  ).toMatch(/^(0s|0\.00001s)$/);
  await page.evaluate(() => {
    document.documentElement.style.fontSize = "200%";
  });
  await expectNoHorizontalOverflow(page);
});
