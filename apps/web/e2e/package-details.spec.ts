import { expect, test, type Page, type TestInfo } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const departureId = "3c9d522a-9481-4b79-9486-64cf997bfe31";

const publishedDetail = {
  departureId,
  operator: {
    id: "operator-noor",
    displayName: "Noor International Tours & Travels",
  },
  packageName: "Browser Verified Journey",
  summary: "A published journey backed by authoritative NoorPath facts.",
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
    confirmationState: "pending",
  },
  travel: {
    routeSummary: "Delhi → Jeddah → Makkah → Madinah",
    details: "Final carrier and flight timing remain pending.",
    confirmationState: "pending",
  },
  inclusions: ["Return flights", "Breakfast", "Visa support"],
  exclusions: ["Personal expenses"],
  pricing: {
    currency: "INR",
    occupancies: [
      {
        occupancy: "double",
        amount: 110000,
        availableQuantity: 10,
        status: "available",
      },
      {
        occupancy: "triple",
        amount: 100000,
        availableQuantity: 0,
        status: "unavailable",
      },
      {
        occupancy: "quad",
        amount: 90000,
        availableQuantity: 6,
        status: "available",
      },
    ],
  },
};

async function captureEvidence(page: Page, testInfo: TestInfo, name: string) {
  await page.screenshot({
    path: testInfo.outputPath(name),
    fullPage: true,
    animations: "disabled",
  });
}

test("published package detail renders authoritative facts", async ({ page }, testInfo) => {
  await page.route(`**/api/v1/departures/${departureId}`, (route) =>
    route.fulfill({ json: publishedDetail }),
  );

  await page.goto(`/packages/${departureId}`);

  await expect(
    page.getByRole("heading", { name: "Noor International Tours & Travels" }),
  ).toBeVisible();
  await expect(
    page.getByText("Browser Verified Journey").first(),
  ).toBeVisible();
  await expect(page.getByText("Makkah Hotel")).toBeVisible();
  await expect(page.getByText("Madinah Hotel")).toBeVisible();
  await expect(
    page.getByText("Delhi → Jeddah → Makkah → Madinah"),
  ).toBeVisible();
  await expect(page.getByText("Return flights")).toBeVisible();
  await expect(page.getByText("Personal expenses")).toBeVisible();
  await expect(page.getByText("₹1,10,000")).toBeVisible();
  await expect(page.getByText("₹1,00,000")).toBeVisible();
  await expect(page.getByText("Currently unavailable")).toBeVisible();
  await expect(page.getByText("₹90,000").first()).toBeVisible();
  await expect(page.getByText("Pending confirmation").first()).toBeVisible();

  await expect(page.getByText("IATA Accredited")).toHaveCount(0);
  await expect(page.getByText("ISO 9001:2015")).toHaveCount(0);
  await expect(page.getByText("15+ Years Experience")).toHaveCount(0);
  await expect(page.getByText("Pay today")).toHaveCount(0);
  await expectNoA11yViolations(page);
  await captureEvidence(page, testInfo, "populated.png");
});

test("package detail exposes a calm unavailable state", async ({ page }, testInfo) => {
  await page.route(`**/api/v1/departures/${departureId}`, (route) =>
    route.fulfill({
      status: 404,
      json: { title: "Published package not found" },
    }),
  );

  await page.goto(`/packages/${departureId}`);
  await expect(page.getByRole("status")).toContainText(
    "This package is not currently available.",
  );
  await expect(
    page.getByRole("link", { name: "Browse available packages" }),
  ).toBeVisible();
  await expectNoHorizontalOverflow(page);
  await expectNoA11yViolations(page);
  await captureEvidence(page, testInfo, "unavailable.png");
});

test("package detail retry recovers after a public API error", async ({ page }, testInfo) => {
  let attempts = 0;
  await page.route(`**/api/v1/departures/${departureId}`, (route) =>
    ++attempts === 1
      ? route.fulfill({
          status: 503,
          headers: { "X-Correlation-ID": "detail-test-503" },
          json: {},
        })
      : route.fulfill({ json: publishedDetail }),
  );

  await page.goto(`/packages/${departureId}`);
  await expect(page.getByRole("alert")).toContainText(
    "We could not load this package right now.",
  );
  await expect(page.getByRole("alert")).toContainText("detail-test-503");
  await captureEvidence(page, testInfo, "error-before-retry.png");

  await page.getByRole("button", { name: "Try again" }).click();
  await expect(
    page.getByText("Browser Verified Journey").first(),
  ).toBeVisible();
  await captureEvidence(page, testInfo, "retry-recovered.png");
});

test("package detail remains usable at mobile widths, zoom and reduced motion", async ({
  page,
}, testInfo) => {
  await page.route(`**/api/v1/departures/${departureId}`, (route) =>
    route.fulfill({ json: publishedDetail }),
  );
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`/packages/${departureId}`);

  await expect(
    page.getByText("Browser Verified Journey").first(),
  ).toBeVisible();
  await expect(page.getByText("₹90,000").first()).toBeVisible();
  await expect(page.getByText("Currently unavailable")).toBeVisible();
  await expectNoHorizontalOverflow(page);
  await expectMinimumTargets(page);
  await expectNoA11yViolations(page);
  await captureEvidence(page, testInfo, "mobile-390.png");

  await page.getByRole("link", { name: /Review options/ }).focus();
  const focused = page.locator(":focus");
  await expect(focused).toBeVisible();
  expect(
    await focused.evaluate((element) => getComputedStyle(element).outlineStyle),
  ).not.toBe("none");

  await page.evaluate(() => {
    document.documentElement.style.fontSize = "200%";
  });
  await expectNoHorizontalOverflow(page);
  await captureEvidence(page, testInfo, "mobile-390-text-200.png");

  await page.evaluate(() => {
    document.documentElement.style.fontSize = "100%";
  });
  await page.setViewportSize({ width: 360, height: 800 });
  await expect(
    page.getByText("Browser Verified Journey").first(),
  ).toBeVisible();
  await expectNoHorizontalOverflow(page);
  await expectMinimumTargets(page);
  await captureEvidence(page, testInfo, "mobile-360.png");
});
