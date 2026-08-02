import { expect, test } from "@playwright/test";
import { expectNoA11yViolations, expectNoHorizontalOverflow } from "./helpers";

test("customer sees truthful traveller status and safe action", async ({
  page,
}) => {
  await page.route("**/api/v1/bookings/*/visa", (route) =>
    route.fulfill({
      json: {
        travellers: [
          {
            travellerId: "t1",
            fullName: "Amina Khan",
            status: "Action required",
            code: "ActionRequired",
            updatedAtUtc: "2026-08-02T12:00:00Z",
            requiredAction: "Upload a clear passport bio page.",
          },
          {
            travellerId: "t2",
            fullName: "Omar Khan",
            status: "Approved",
            code: "Approved",
            updatedAtUtc: "2026-08-02T12:00:00Z",
          },
        ],
      },
    }),
  );
  await page.goto("/bookings/11111111-1111-1111-1111-111111111111/visa");
  await expect(
    page.getByRole("heading", { name: "Visa progress" }),
  ).toBeVisible();
  await expect(
    page.getByText("Upload a clear passport bio page."),
  ).toBeVisible();
  await expect(page.getByText("Approved", { exact: true })).toBeVisible();
  await expectNoA11yViolations(page);
  await page.setViewportSize({ width: 390, height: 844 });
  await expectNoHorizontalOverflow(page);
});

test("operator queue renders denial and recoverable queue states", async ({
  page,
}) => {
  await page.route("**/api/v1/operator/visa", (route) =>
    route.fulfill({ status: 403 }),
  );
  await page.goto("/operator/visa");
  await expect(
    page.getByText("You do not have visa processing permission."),
  ).toBeVisible();
  await expectNoA11yViolations(page);
});
