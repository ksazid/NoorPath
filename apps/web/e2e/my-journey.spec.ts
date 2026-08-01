import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const bookingId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const journey = {
  bookingId,
  bookingReference: "NP-20260801-JOURNEY",
  state: "Confirmed",
  occupancy: "Double",
  confirmedAtUtc: "2026-08-01T07:00:00Z",
  journey: {
    packageName: "The NoorPath Signature Umrah",
    origin: "London Heathrow",
    departureDate: "2026-11-08",
    returnDate: "2026-11-20",
    makkahHotelName: "Jabal Omar Marriott",
    makkahNights: 7,
    madinahHotelName: "Pullman Zamzam Madina",
    madinahNights: 5,
    travelRouteSummary: "London to Jeddah · Madinah to London",
  },
  travellers: [{ fullName: "Amina Rahman" }, { fullName: "Yusuf Rahman" }],
  commercial: { currency: "GBP", total: 6400, paid: 1600, remaining: 4800 },
  payment: {
    state: "Succeeded",
    instalments: [
      { sequence: 1, dueDate: "2026-09-15", amount: 2400, status: "Scheduled" },
      { sequence: 2, dueDate: "2026-10-15", amount: 2400, status: "Scheduled" },
    ],
  },
  readiness: { documents: "ComingNext", visa: "ComingNext" },
  support: {
    bookingReference: "NP-20260801-JOURNEY",
    correlationId: "safe-correlation-id",
  },
};

test("VS-11 presents authoritative journey, payments and truthful placeholders", async ({
  page,
}, testInfo) => {
  await page.route(`**/api/v1/journeys/${bookingId}`, (route) =>
    route.fulfill({ json: journey }),
  );
  await page.goto(`/bookings/${bookingId}/journey`);
  await expect(
    page.getByRole("heading", { name: journey.journey.packageName }),
  ).toBeVisible();
  await expect(page.getByText("£1,600")).toBeVisible();
  await expect(
    page.getByText("Coming next. Upload and review are not available yet."),
  ).toBeVisible();
  await expect(
    page.getByRole("link", { name: "Contact support" }),
  ).toHaveAttribute("href", /NP-20260801-JOURNEY/);
  await expectNoHorizontalOverflow(page);
  await expectMinimumTargets(page);
  await expectNoA11yViolations(page);
  await page.screenshot({
    path: `test-results/vs11-journey-${testInfo.project.name}.png`,
    fullPage: true,
  });
});

test("VS-11 explains a delayed projection and offers retry", async ({
  page,
}) => {
  await page.route(`**/api/v1/journeys/${bookingId}`, (route) =>
    route.fulfill({
      status: 503,
      contentType: "application/problem+json",
      body: JSON.stringify({
        code: "projection_delayed",
        correlationId: "safe-correlation-id",
      }),
    }),
  );
  await page.goto(`/bookings/${bookingId}/journey`);
  await expect(
    page.getByRole("heading", { name: "Your journey is still being prepared" }),
  ).toBeVisible();
  await expect(page.getByRole("button", { name: "Retry" })).toBeVisible();
  await expectNoA11yViolations(page);
});

test("VS-11 list has an actionable empty state", async ({ page }) => {
  await page.route("**/api/v1/journeys", (route) =>
    route.fulfill({ json: { items: [] } }),
  );
  await page.goto("/journeys");
  await expect(
    page.getByRole("heading", { name: "No confirmed journeys yet" }),
  ).toBeVisible();
  await expect(
    page.getByRole("link", { name: "Explore packages" }),
  ).toBeVisible();
  await expectNoHorizontalOverflow(page);
  await expectNoA11yViolations(page);
});
