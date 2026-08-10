import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const departureId = "40000000-0000-0000-0000-000000000138";

test("operator authors confirmed airline and airport facts", async ({ page }) => {
  let submitted: Record<string, unknown> | null = null;

  await page.route(
    `**/api/v1/operator/departures/${departureId}/travel-facts`,
    async (route) => {
      if (route.request().method() === "PUT") {
        submitted = route.request().postDataJSON() as Record<string, unknown>;
        const request = submitted as { legs: unknown[] };
        await route.fulfill({
          status: 200,
          contentType: "application/json",
          body: JSON.stringify({
            departureId,
            version: 2,
            editable: true,
            legs: request.legs,
          }),
        });
        return;
      }

      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          departureId,
          version: 1,
          editable: true,
          legs: [],
        }),
      });
    },
  );

  await page.goto(`/operator/departures/${departureId}/travel-facts`);

  await expect(
    page.getByRole("heading", { name: "Airline & airport facts" }),
  ).toBeVisible();
  await expect(page.getByText("No flight facts recorded yet")).toBeVisible();

  await page.getByRole("button", { name: "Add flight leg" }).click();
  await page.getByLabel("Airline name", { exact: true }).fill("Saudia");
  await page.getByLabel("Airline code", { exact: true }).fill("SV");
  await page.getByLabel("Flight number", { exact: true }).fill("SV759");
  await page
    .getByLabel("Departure airport", { exact: true })
    .fill("Chhatrapati Shivaji Maharaj International Airport");
  await page
    .getByLabel("Departure airport code", { exact: true })
    .fill("BOM");
  await page
    .getByLabel("Arrival airport", { exact: true })
    .fill("King Abdulaziz International Airport");
  await page.getByLabel("Arrival airport code", { exact: true }).fill("JED");
  await page.getByLabel("Flight fact status").selectOption("confirmed");
  await page.getByRole("button", { name: "Save travel facts" }).click();

  await expect(page.getByText("Travel facts saved")).toBeVisible();
  expect(submitted).toMatchObject({
    expectedVersion: 1,
    legs: [
      {
        airlineName: "Saudia",
        airlineCode: "SV",
        flightNumber: "SV759",
        departureAirportCode: "BOM",
        arrivalAirportCode: "JED",
        confirmationState: "confirmed",
      },
    ],
  });

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("operator can truthfully retain a partial pending flight leg", async ({
  page,
}) => {
  await page.route(
    `**/api/v1/operator/departures/${departureId}/travel-facts`,
    async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          departureId,
          version: 4,
          editable: true,
          legs: [
            {
              airlineName: "Saudia",
              airlineCode: "SV",
              flightNumber: "",
              departureAirportName: "",
              departureAirportCode: "BOM",
              arrivalAirportName: "King Abdulaziz International Airport",
              arrivalAirportCode: "JED",
              confirmationState: "pending",
            },
          ],
        }),
      });
    },
  );

  await page.goto(`/operator/departures/${departureId}/travel-facts`);

  await expect(page.getByLabel("Flight fact status")).toHaveValue("pending");
  await expect(page.getByLabel("Flight number")).toHaveValue("");
  await expect(
    page.getByText(
      "External airline or airport lookup is not configured in this slice",
      { exact: false },
    ),
  ).toBeVisible();
});
