import { expect, test, type Page } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const departureId = "3c9d522a-9481-4b79-9486-64cf997bfe31";

const packageDetails = {
  departureId,
  operator: {
    id: "operator-noor",
    displayName: "Noor International Tours & Travels",
  },
  packageName: "October Umrah Journey — Delhi",
  origin: "Delhi (DEL)",
  departureDate: "2026-10-10",
  returnDate: "2026-10-22",
  durationNights: 12,
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

const travellers = [
  {
    travellerId: "11111111-1111-4111-8111-111111111111",
    fullName: "Amina Khan",
    dateOfBirth: "1994-05-17",
  },
  {
    travellerId: "22222222-2222-4222-8222-222222222222",
    fullName: "Yusuf Khan",
    dateOfBirth: "1990-09-08",
  },
  {
    travellerId: "33333333-3333-4333-8333-333333333333",
    fullName: "Maryam Khan",
    dateOfBirth: "1998-02-14",
  },
  {
    travellerId: "44444444-4444-4444-8444-444444444444",
    fullName: "Ibrahim Khan",
    dateOfBirth: "1988-12-02",
  },
];

const quote = {
  quoteId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
  departureId,
  priceVersionId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
  occupancy: "double",
  travellerCount: 2,
  currency: "INR",
  unitPrice: 110000,
  total: 220000,
  dueNow: 44000,
  remaining: 176000,
  instalments: [
    { sequence: 1, dueDate: "2026-08-05", amount: 88000 },
    { sequence: 2, dueDate: "2026-09-05", amount: 88000 },
  ],
  createdAtUtc: "2026-07-31T06:00:00Z",
  expiresAtUtc: "2099-07-31T06:30:00Z",
  expired: false,
  availabilityReserved: false,
};

async function mockPlanApis(page: Page) {
  await page.route(`**/api/v1/departures/${departureId}`, (route) =>
    route.fulfill({ json: packageDetails }),
  );
  await page.route("**/api/v1/travellers", (route) => {
    if (route.request().method() === "GET") {
      return route.fulfill({ json: { items: travellers } });
    }
    return route.fulfill({ status: 405, json: { title: "Not allowed" } });
  });
  await page.route(`**/api/v1/departures/${departureId}/quotes`, (route) =>
    route.fulfill({ json: quote }),
  );
}

async function completeQuote(page: Page) {
  await page.goto(`/packages/${departureId}/plan`);
  await expect(
    page.getByRole("heading", { name: "Build your Umrah plan" }),
  ).toBeVisible();
  await expect(page.getByText("October Umrah Journey — Delhi")).toBeVisible();

  await page.locator('input[name="occupancy"][value="double"]').check();
  await page.getByRole("checkbox", { name: /Amina Khan/ }).check();
  await page.getByRole("checkbox", { name: /Yusuf Khan/ }).check();
  const selectionCount = page.locator(".plan-selection-count");
  await expect(selectionCount).toContainText("2");
  await expect(selectionCount).toContainText("of 2 travellers selected");

  await page.getByRole("button", { name: "See my complete quote" }).click();
  await expect(
    page.getByText("Authoritative quote", { exact: true }),
  ).toBeVisible();
  await expect(page.getByText("₹2,20,000")).toBeVisible();
  await expect(page.getByText("₹44,000")).toBeVisible();
  await expect(page.getByText("₹1,76,000")).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Payment schedule" }),
  ).toBeVisible();
  await expect(page.getByText("Instalment 1")).toBeVisible();
  await expect(page.getByText("No place is reserved yet.")).toBeVisible();
}

test("VS-07 renders the authoritative traveller quote flow accessibly", async ({
  page,
}, testInfo) => {
  await mockPlanApis(page);
  await completeQuote(page);

  await expectNoHorizontalOverflow(page);
  await expectMinimumTargets(page);
  await expectNoA11yViolations(page);

  await page.getByRole("link", { name: "Package", exact: true }).focus();
  const focused = page.locator(":focus");
  await expect(focused).toBeVisible();
  expect(
    await focused.evaluate((element) => getComputedStyle(element).outlineStyle),
  ).not.toBe("none");

  await page.screenshot({
    path: `test-results/vs07-plan-${testInfo.project.name}.png`,
    fullPage: true,
  });
});

test("VS-07 remains usable at 360px, 200% text and reduced motion", async ({
  page,
}, testInfo) => {
  test.skip(testInfo.project.name !== "mobile-390", "360px gate runs once");
  await mockPlanApis(page);
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.setViewportSize({ width: 360, height: 800 });
  await completeQuote(page);

  await expectNoHorizontalOverflow(page);
  await expectMinimumTargets(page);
  await expectNoA11yViolations(page);

  await page.evaluate(() => {
    document.documentElement.style.fontSize = "200%";
  });
  await expectNoHorizontalOverflow(page);
  await expect(page.getByText("₹2,20,000")).toBeVisible();

  await page.screenshot({
    path: "test-results/vs07-plan-mobile-360-text-200.png",
    fullPage: true,
  });
});
