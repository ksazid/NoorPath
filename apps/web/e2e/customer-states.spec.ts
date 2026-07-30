import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const published = {
  items: [
    {
      departureId: "3c9d522a-9481-4b79-9486-64cf997bfe31",
      operator: {
        id: "operator-noor",
        displayName: "Noor International Tours & Travels",
      },
      packageName: "Browser Verified Journey",
      summary: "Supported journey",
      origin: "Delhi (DEL)",
      departureDate: "2026-10-10",
      returnDate: "2026-10-22",
      durationNights: 12,
      makkah: {
        hotelName: "Makkah Hotel",
        classification: "4 star",
        distanceDisclosure: "850 m from Masjid al-Haram",
        nights: 6,
        confirmationState: "confirmed",
      },
      madinah: {
        hotelName: "Madinah Hotel",
        classification: "4 star",
        distanceDisclosure: "450 m from Al-Masjid an-Nabawi",
        nights: 5,
        confirmationState: "confirmed",
      },
      travelConfirmationState: "pending",
      inclusionHighlights: ["Return flights", "Breakfast"],
      headlinePrice: {
        amount: 90000,
        currency: "INR",
        occupancy: "quad",
      },
      availability: {
        status: "available",
        occupancies: [
          { occupancy: "double", availableQuantity: 10 },
          { occupancy: "triple", availableQuantity: 8 },
          { occupancy: "quad", availableQuantity: 6 },
        ],
      },
    },
  ],
};

test("loading resolves to truthful published results", async ({ page }) => {
  await page.route("**/api/v1/departures", async (route) => {
    await new Promise((resolve) => setTimeout(resolve, 500));
    await route.fulfill({ json: published });
  });

  await page.goto("/");
  await expect(page.locator('[aria-busy="true"]')).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Browser Verified Journey" }),
  ).toBeVisible();
  await expect(page.getByText("From ₹90,000")).toBeVisible();
  await expect(page.getByText("Available")).toBeVisible();
  await expect(
    page.getByText("Noor International Tours & Travels"),
  ).toBeVisible();
  await expectNoA11yViolations(page);
});

test("empty discovery has an accessible calm state", async ({ page }) => {
  await page.route("**/api/v1/departures", (route) =>
    route.fulfill({ json: { items: [] } }),
  );

  await page.goto("/");
  await expect(page.getByRole("status")).toContainText(
    "No Umrah packages are currently available.",
  );
  await expectNoA11yViolations(page);
  await expectNoHorizontalOverflow(page);
});

test("error retry recovers to published results", async ({ page }) => {
  let attempts = 0;
  await page.route("**/api/v1/departures", (route) =>
    ++attempts === 1
      ? route.fulfill({
          status: 503,
          headers: { "X-Correlation-ID": "discovery-test-503" },
          json: {},
        })
      : route.fulfill({ json: published }),
  );

  await page.goto("/");
  await expect(page.getByRole("alert")).toContainText(
    "We could not load Umrah packages right now.",
  );
  await expect(page.getByRole("alert")).toContainText("discovery-test-503");
  await page.getByRole("button", { name: "Try again" }).click();
  await expect(
    page.getByRole("heading", { name: "Browser Verified Journey" }),
  ).toBeVisible();
});

test("mobile, keyboard, targets and reduced motion remain usable", async ({
  page,
}) => {
  await page.route("**/api/v1/departures", (route) =>
    route.fulfill({ json: published }),
  );
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/");

  await expect(
    page.getByRole("heading", { name: "Browser Verified Journey" }),
  ).toBeVisible();
  await expect(
    page.getByRole("link", { name: /View package/ }),
  ).toBeVisible();
  await expectNoHorizontalOverflow(page);
  await expectMinimumTargets(page);

  await page.getByRole("link", { name: /View package/ }).focus();
  const focused = page.locator(":focus");
  await expect(focused).toBeVisible();
  expect(
    await focused.evaluate((element) => getComputedStyle(element).outlineStyle),
  ).not.toBe("none");

  await page.evaluate(() => {
    document.documentElement.style.fontSize = "200%";
  });
  await expectNoHorizontalOverflow(page);
  await expectNoA11yViolations(page);
});
