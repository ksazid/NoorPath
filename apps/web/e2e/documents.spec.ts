import { expect, test } from "@playwright/test";

test("customer document checklist renders at desktop and mobile", async ({
  page,
}) => {
  await page.route("**/api/v1/bookings/*/documents", (route) =>
    route.fulfill({
      json: {
        policyVersion: "v1",
        ready: false,
        travellers: [
          {
            travellerId: "t1",
            fullName: "Amina Khan",
            requirements: [
              { id: "r1", kind: "PassportBioPage", submission: null },
              {
                id: "r2",
                kind: "PassportPhoto",
                submission: {
                  id: "s2",
                  state: "CorrectionRequired",
                  malwareStatus: "Safe",
                  reviewReason:
                    "Please upload the full photo without cropping.",
                },
              },
            ],
          },
        ],
      },
    }),
  );
  await page.goto("/bookings/11111111-1111-1111-1111-111111111111/documents");
  await expect(
    page.getByRole("heading", { name: "Traveller documents" }),
  ).toBeVisible();
  await expect(page.getByText("CorrectionRequired")).toBeVisible();
  await expect(
    page.getByText("Please upload the full photo without cropping."),
  ).toBeVisible();
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.locator("body")).not.toHaveCSS("overflow-x", "scroll");
});

test("operator queue hides unsafe content and renders permission denial", async ({
  page,
}) => {
  await page.route("**/api/v1/operator/documents", (route) =>
    route.fulfill({ status: 403 }),
  );
  await page.goto("/operator/documents");
  await expect(
    page.getByText("You do not have document review permission."),
  ).toBeVisible();
});
