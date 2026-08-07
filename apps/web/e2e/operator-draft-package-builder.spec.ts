import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

test("operator can start a package draft with standard NoorPath terminology", async ({
  page,
}) => {
  await page.goto("/operator/packages/new");

  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "Create a package draft",
  );
  await expect(
    page.getByText("Visa included", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText("Breakfast, lunch and dinner", { exact: true }),
  ).toBeVisible();
  await expect(page.getByRole("radio", { name: "Bus" })).toBeChecked();
  await expect(page.getByRole("radio", { name: "Train" })).not.toBeChecked();

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("journey dates calculate the package title and duration", async ({
  page,
}) => {
  await page.goto("/operator/packages/new");

  await page.getByLabel("Departure origin").fill("Delhi (DEL)");
  await page.getByLabel("Departure date").fill("2027-01-10");
  await page.getByLabel("Return date").fill("2027-01-21");

  await expect(
    page.getByText("12 Days / 11 Nights", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText("12 Days / 11 Nights Umrah from Delhi (DEL)", {
      exact: true,
    }),
  ).toBeVisible();
});

test("saved package draft renders a private customer-style preview", async ({
  page,
}) => {
  const departureId = "40000000-0000-0000-0000-000000000099";
  await page.route(
    `**/api/v1/operator/departures/${departureId}`,
    async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          departureId,
          packageName: "12 Days / 11 Nights Umrah from Delhi",
          summary:
            "A guided Umrah journey with confirmed accommodation disclosures.",
          origin: "Delhi (DEL)",
          departureDate: "2027-01-10",
          returnDate: "2027-01-21",
          makkah: {
            hotelName: "Makkah Stay",
            classification: "4 star",
            distanceDisclosure: "850 m from Masjid al-Haram",
            nights: 6,
          },
          madinah: {
            hotelName: "Madinah Stay",
            classification: "4 star",
            distanceDisclosure: "450 m from Al-Masjid an-Nabawi",
            nights: 5,
          },
          travel: {
            routeSummary: "Delhi → Jeddah → Makkah → Madinah",
            details: "Intercity travel by bus",
          },
          inclusions: [
            "Visa included",
            "Breakfast, lunch and dinner",
            "Intercity travel",
          ],
          exclusions: ["Personal expenses"],
        }),
      });
    },
  );

  await page.goto(`/operator/departures/${departureId}/preview`);

  await expect(
    page.getByText("Customer preview", { exact: true }),
  ).toBeVisible();
  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "12 Days / 11 Nights Umrah from Delhi",
  );
  await expect(
    page.getByText("Visa included", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText("Private draft · not visible to customers", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("link", { name: "Review publication" }),
  ).toBeVisible();

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});
