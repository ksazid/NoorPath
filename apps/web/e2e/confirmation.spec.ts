import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const bookingId = "dddddddd-dddd-4ddd-8ddd-dddddddddddd";
const base = {
  bookingId,
  bookingReference: "NP-20260801-CONFIRM",
  departureId: "3c9d522a-9481-4b79-9486-64cf997bfe31",
  currency: "INR",
  total: 220000,
  dueNow: 44000,
  remaining: 176000,
};

test("VS-10 shows a confirmed booking and links to My Journey", async ({
  page,
}, testInfo) => {
  await page.route(`**/api/v1/bookings/${bookingId}`, (route) =>
    route.fulfill({
      json: {
        ...base,
        state: "Confirmed",
        confirmedAtUtc: "2026-08-01T07:00:00Z",
      },
    }),
  );
  await page.goto(`/bookings/${bookingId}/confirmation`);
  await expect(
    page.getByRole("heading", { name: "Your booking is confirmed" }),
  ).toBeVisible();
  await expect(page.getByText("₹44,000")).toBeVisible();
  await expect(
    page.getByRole("link", { name: "Continue to My Journey" }),
  ).toHaveAttribute("href", `/bookings/${bookingId}/journey`);
  await expectNoHorizontalOverflow(page);
  await expectMinimumTargets(page);
  await expectNoA11yViolations(page);
  await page.screenshot({
    path: `test-results/vs10-confirmed-${testInfo.project.name}.png`,
    fullPage: true,
  });
});

test("VS-10 makes confirmation exceptions explicit without asking for another payment", async ({
  page,
}, testInfo) => {
  await page.route(`**/api/v1/bookings/${bookingId}`, (route) =>
    route.fulfill({
      json: {
        ...base,
        state: "ConfirmationException",
        confirmationException: {
          code: "inventory_commitment_unavailable",
          message:
            "Payment was received, but confirmation needs NoorPath support. Do not pay again.",
        },
      },
    }),
  );
  await page.goto(`/bookings/${bookingId}/confirmation`);
  await expect(
    page.getByRole("heading", {
      name: "Payment received — action required",
    }),
  ).toBeVisible();
  await expect(page.getByText("Do not pay again.")).toBeVisible();
  await expect(page.getByText(/Recovery is restricted/)).toBeVisible();
  await expectNoHorizontalOverflow(page);
  await expectNoA11yViolations(page);
  await page.screenshot({
    path: `test-results/vs10-exception-${testInfo.project.name}.png`,
    fullPage: true,
  });
});
