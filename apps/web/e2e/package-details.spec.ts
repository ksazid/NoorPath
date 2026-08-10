import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const departureId = "3c9d522a-9481-4b79-9486-64cf997bfe31";
const siblingDepartureId = "9c84aee0-e094-4cb8-bad0-1173fc34f2f6";

const publishedDetail = {
  departureId,
  operator: {
    id: "operator-noor",
    displayName: "Noor International Tours & Travels",
  },
  packageName: "Browser Verified Journey",
  summary: "A published journey backed by authoritative NoorPath facts.",
  origin: "Mumbai (BOM)",
  departureDate: "2026-08-31",
  returnDate: "2026-09-14",
  durationNights: 14,
  makkah: {
    hotelName: "Pullman ZamZam Makkah",
    classification: "5 star",
    distanceDisclosure: "450 m from Masjid al-Haram",
    nights: 7,
    confirmationState: "confirmed",
  },
  madinah: {
    hotelName: "Anwar Al Madinah Mövenpick",
    classification: "5 star",
    distanceDisclosure: "200 m from Al-Masjid an-Nabawi",
    nights: 7,
    confirmationState: "pending",
  },
  travel: {
    routeSummary: "Mumbai → Jeddah → Makkah → Madinah",
    details: "Intercity transfer by bus. Final flight timing remains pending.",
    confirmationState: "pending",
  },
  inclusions: [
    "Return flights",
    "Visa included",
    "Makkah accommodation",
    "Madinah accommodation",
    "Breakfast, lunch and dinner",
    "Intercity travel by bus",
    "Ziyarat transport",
    "Umrah guidance",
    "Luggage tag",
    "Neck pouch / document wallet",
    "ID card",
    "Pocket Dua guide",
    "Zamzam handling guidance",
  ],
  exclusions: ["Personal expenses", "Extra baggage"],
  travelDates: [
    {
      departureId: "64dd7c04-f72e-4413-b1ca-c2f0faf87d61",
      departureDate: "2026-08-24",
      returnDate: "2026-09-07",
      status: "sold-out",
    },
    {
      departureId,
      departureDate: "2026-08-31",
      returnDate: "2026-09-14",
      status: "available",
    },
    {
      departureId: siblingDepartureId,
      departureDate: "2026-09-07",
      returnDate: "2026-09-21",
      status: "available",
    },
  ],
  pricing: {
    currency: "INR",
    occupancies: [
      {
        occupancy: "double",
        amount: 110000,
        availableQuantity: 10,
        status: "available",
        financials: {
          adultGuests: 2,
          total: 220000,
          dueNow: 44000,
          remaining: 176000,
          instalments: [
            { sequence: 1, dueDate: "2026-08-20", amount: 88000 },
            { sequence: 2, dueDate: "2026-08-25", amount: 88000 },
          ],
          finalDueDate: "2026-08-25",
        },
      },
      {
        occupancy: "triple",
        amount: 100000,
        availableQuantity: 0,
        status: "unavailable",
        financials: {
          adultGuests: 3,
          total: 300000,
          dueNow: 60000,
          remaining: 240000,
          instalments: [{ sequence: 1, dueDate: "2026-08-25", amount: 240000 }],
          finalDueDate: "2026-08-25",
        },
      },
      {
        occupancy: "quad",
        amount: 90000,
        availableQuantity: 6,
        status: "available",
        financials: {
          adultGuests: 4,
          total: 360000,
          dueNow: 72000,
          remaining: 288000,
          instalments: [
            { sequence: 1, dueDate: "2026-08-20", amount: 144000 },
            { sequence: 2, dueDate: "2026-08-25", amount: 144000 },
          ],
          finalDueDate: "2026-08-25",
        },
      },
    ],
  },
};

const siblingDetail = {
  ...publishedDetail,
  departureId: siblingDepartureId,
  departureDate: "2026-09-07",
  returnDate: "2026-09-21",
};

test.beforeEach(async ({ page }) => {
  await page.route(`**/api/v1/departures/${departureId}`, (route) =>
    route.fulfill({ json: publishedDetail }),
  );
  await page.route(`**/api/v1/departures/${siblingDepartureId}`, (route) =>
    route.fulfill({ json: siblingDetail }),
  );
});

test.afterEach(async ({ page }, testInfo) => {
  await testInfo.attach(`rendered-${testInfo.title}`, {
    body: await page.screenshot({ fullPage: true }),
    contentType: "image/png",
  });
});

test("package details show travel dates, journey and operator-authored content", async ({
  page,
}) => {
  await page.goto(`/packages/${departureId}`);

  await expect(
    page.getByRole("heading", { name: "Available Travel Dates" }),
  ).toBeVisible();
  await expect(page.getByText("24 Aug 2026")).toBeVisible();
  await expect(
    page.locator(".package-date-card.current").getByText("31 Aug 2026"),
  ).toBeVisible();
  await expect(
    page.getByRole("link", { name: /07 Sep(?:t)? 2026/ }),
  ).toBeVisible();
  await expect(page.getByText("Sold out")).toHaveCount(1);
  await expect(
    page.getByRole("heading", { name: "Noor International Tours & Travels" }),
  ).toBeVisible();
  await expect(
    page.getByText("Pullman ZamZam Makkah", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText("Anwar Al Madinah Mövenpick", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Your itinerary" }),
  ).toBeVisible();
  await expect(page.getByText("Makkah stay", { exact: true })).toBeVisible();
  await expect(
    page.getByText("Intercity travel", { exact: true }),
  ).toBeVisible();
  await expect(page.getByText("Madinah stay", { exact: true })).toBeVisible();

  const packageItems = page.locator(
    ".package-content-grid li > span:last-child",
  );
  await expect(
    packageItems.filter({ hasText: /^Umrah visa included$/ }),
  ).toBeVisible();
  await expect(
    packageItems.filter({ hasText: /^Document wallet$/ }),
  ).toBeVisible();
  await expect(
    packageItems.filter({ hasText: /^Pocket Dua guide$/ }),
  ).toBeVisible();
  await expect(
    packageItems.filter({ hasText: /^Personal expenses$/ }),
  ).toBeVisible();
  await expectNoA11yViolations(page);
});

test("available same-origin dates navigate and browser back returns safely", async ({
  page,
}) => {
  await page.goto(`/packages/${departureId}`);

  await page.getByRole("link", { name: /07 Sep(?:t)? 2026/ }).click();
  await expect(page).toHaveURL(`/packages/${siblingDepartureId}`);
  await expect(
    page.locator(".package-date-card.current").getByText(/07 Sep(?:t)? 2026/),
  ).toBeVisible();

  await page.goBack();
  await expect(page).toHaveURL(`/packages/${departureId}`);
  await expect(
    page.locator(".package-date-card.current").getByText("31 Aug 2026"),
  ).toBeVisible();
});

test("guest categories, room sharing and payment breakdown are visible before booking", async ({
  page,
}) => {
  await page.goto(`/packages/${departureId}`);

  await expect(page.locator('output[aria-label="Adult guests"]')).toHaveText(
    "2",
  );
  await expect(page.getByText("Children (2–11 years)")).toBeVisible();
  await expect(page.getByText("Children (2–4 years)")).toBeVisible();
  await expect(page.getByText("Infants (0–2 years)")).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Increase children with bed" }),
  ).toBeDisabled();
  await expect(
    page.getByRole("button", { name: "Increase infants" }),
  ).toBeDisabled();

  await expect(
    page.getByRole("radio", { name: /Double sharing/ }),
  ).toBeChecked();
  await expect(page.getByText("₹2,20,000").first()).toBeVisible();
  await expect(page.getByText("₹44,000").first()).toBeVisible();
  await expect(page.getByText("₹1,76,000").first()).toBeVisible();
  await expect(page.getByRole("radio", { name: /Pay Full/ })).toBeVisible();
  await expect(page.getByRole("radio", { name: /Pay Full/ })).toBeChecked();
  await expect(page.getByRole("radio", { name: /Pay Later/ })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Pay full breakdown" }),
  ).toBeVisible();
  await expect(
    page.locator(".package-payment-breakdown").getByText("₹2,20,000"),
  ).toBeVisible();
  await expect(page.getByText("Total Price Before Discount")).toBeVisible();
  await expect(page.getByText("Total Price After Discount")).toBeVisible();
  await expect(page.getByText("Service Provider")).toBeVisible();
  await expect(page.getByText("Powered & Supported by")).toBeVisible();
  await expect(page.getByText("0% · ₹0")).toBeVisible();

  await page.getByRole("radio", { name: /Milestone/ }).check();
  await expect(
    page.getByRole("heading", { name: "Milestone payment breakdown" }),
  ).toBeVisible();
  await expect(page.getByText("₹88,000").first()).toBeVisible();

  await page.getByRole("radio", { name: /Pay Full/ }).check();
  await expect(
    page.getByRole("heading", { name: "Pay full breakdown" }),
  ).toBeVisible();
  await expect(
    page.locator(".package-payment-breakdown").getByText("₹2,20,000"),
  ).toBeVisible();

  await page.getByRole("radio", { name: /Pay Later/ }).check();
  await expect(
    page.getByRole("heading", { name: "Pay later breakdown" }),
  ).toBeVisible();
  await expect(
    page.locator(".package-payment-breakdown").getByText("₹1,76,000"),
  ).toBeVisible();

  await page.getByRole("radio", { name: /Quad sharing/ }).check();
  await expect(page.locator('output[aria-label="Adult guests"]')).toHaveText(
    "4",
  );
  await expect(page.getByText("₹3,60,000").first()).toBeVisible();
  await expect(page.getByText("₹72,000").first()).toBeVisible();

  await expect(
    page.getByRole("button", { name: /Book now/ }).first(),
  ).toBeVisible();
  await expectNoA11yViolations(page);
});

test("date controls stay hash-free and Book now previews OTP then traveller names", async ({
  page,
}) => {
  await page.goto(`/packages/${departureId}`);
  await expect(page).not.toHaveURL(/#/);

  await page.getByRole("button", { name: "Next travel dates" }).click();
  await page.getByRole("button", { name: "Previous travel dates" }).click();
  await expect(page).not.toHaveURL(/#/);

  await page
    .getByRole("button", { name: /Book now/ })
    .first()
    .click();
  await expect(page).not.toHaveURL(/#/);
  await expect(
    page.getByRole("heading", { name: "Login with mobile OTP" }),
  ).toBeVisible();
  await expect(page.getByText(/no SMS is sent yet/i)).toBeVisible();

  await page.getByLabel("Mobile number").fill("9876543210");
  await page.getByRole("button", { name: "Send code" }).click();
  await page.getByLabel("6-digit verification code").fill("123456");
  await page.getByRole("button", { name: "Verify & continue" }).click();
  await expect(
    page.getByRole("heading", { name: "Add travellers" }),
  ).toBeVisible();
  await expect(page.getByLabel("Traveller 1")).toBeVisible();
  await page.getByRole("button", { name: "+ Add traveller" }).click();
  await expect(page.getByLabel("Traveller 2")).toBeVisible();
  await expect(
    page.getByRole("button", { name: "+ Add traveller" }),
  ).toHaveCount(0);
  await expect(page).not.toHaveURL(/#/);
  await expectNoA11yViolations(page);
});

test("package detail exposes safe unavailable and retry states", async ({
  page,
}) => {
  await page.unroute(`**/api/v1/departures/${departureId}`);
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
  await expectNoHorizontalOverflow(page);
  await expectNoA11yViolations(page);

  await page.unroute(`**/api/v1/departures/${departureId}`);
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
  await page.reload();
  const errorCard = page.locator(".package-state-card.error");
  await expect(errorCard).toContainText(
    "We could not load this package right now.",
  );
  await expect(errorCard).toContainText("detail-test-503");
  await page.getByRole("button", { name: "Try again" }).click();
  await expect(
    page.getByText("Browser Verified Journey").first(),
  ).toBeVisible();
});

test("package details remain usable on mobile, at 200 percent text and reduced motion", async ({
  page,
}) => {
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`/packages/${departureId}`);

  await expect(
    page.getByRole("heading", { name: "Available Travel Dates" }),
  ).toBeVisible();
  await expect(page.getByText("Infants (0–2 years)")).toBeVisible();
  await expect(page.getByText("₹44,000").first()).toBeVisible();
  await expect(
    page.getByRole("button", { name: /Book now/ }).first(),
  ).toBeVisible();
  await expectNoHorizontalOverflow(page);
  await expectMinimumTargets(page);
  await expectNoA11yViolations(page);

  await page.evaluate(() => {
    document.documentElement.style.fontSize = "200%";
  });
  await expectNoHorizontalOverflow(page);
  await page.evaluate(() => {
    document.documentElement.style.fontSize = "100%";
  });
});
