import { expect, test } from "@playwright/test";
import {
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

test("operator reviews an exception-first support case", async ({ page }) => {
  await page.route("**/api/v1/operator/support?*", (route) =>
    route.fulfill({
      json: {
        items: [
          {
            bookingId: "11111111-1111-1111-1111-111111111111",
            bookingReference: "NP-240801",
            category: "confirmation",
            title: "Confirmation needs recovery",
            code: "inventory_commit_failed",
            updatedAtUtc: "2026-08-02T05:00:00Z",
            actionLabel: "Retry confirmation",
            actionTarget:
              "/api/v1/operator/bookings/11111111-1111-1111-1111-111111111111/confirmation/retry",
          },
        ],
      },
    }),
  );
  await page.route("**/api/v1/operator/support/bookings/*", (route) =>
    route.fulfill({
      json: {
        booking: {
          id: "11111111-1111-1111-1111-111111111111",
          reference: "NP-240801",
          state: "ConfirmationException",
          confirmationExceptionCode: "inventory_commit_failed",
          updatedAtUtc: "2026-08-02T05:00:00Z",
        },
        payment: {
          state: "Succeeded",
          updatedAtUtc: "2026-08-02T04:59:00Z",
        },
        documents: [
          {
            kind: "Passport",
            state: "Approved",
            malwareStatus: "Safe",
            version: 2,
          },
        ],
        visa: [
          {
            travellerId: "t1",
            status: "ActionRequired",
            version: 3,
            updatedAtUtc: "2026-08-02T05:00:00Z",
          },
        ],
        allowedActions: [
          {
            code: "retry_confirmation",
            label: "Retry confirmation",
            target:
              "/api/v1/operator/bookings/11111111-1111-1111-1111-111111111111/confirmation/retry",
          },
        ],
      },
    }),
  );

  await page.goto("/operator/support");
  await expect(
    page.getByRole("heading", { name: "Operational support" }),
  ).toBeVisible();
  await expect(page.getByText("Confirmation needs recovery")).toBeVisible();
  await page.getByRole("button", { name: "Review case" }).click();
  await expect(
    page.getByText("Case state: ConfirmationException"),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Retry confirmation" }),
  ).toBeVisible();
  await expectNoA11yViolations(page);
  await page.setViewportSize({ width: 390, height: 844 });
  await expectNoHorizontalOverflow(page);
});

test("operator support renders permission denial", async ({ page }) => {
  await page.route("**/api/v1/operator/support?*", (route) =>
    route.fulfill({ status: 403 }),
  );
  await page.goto("/operator/support");
  await expect(
    page.getByText("You do not have operational support permission."),
  ).toBeVisible();
  await expectNoA11yViolations(page);
});
