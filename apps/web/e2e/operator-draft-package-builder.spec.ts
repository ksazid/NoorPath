import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

// prettier-ignore
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

test("operator can configure default, optional, custom and excluded package items", async ({
  page,
}) => {
  await page.goto("/operator/packages/new");

  await expect(
    page.getByRole("heading", { name: "Package includes", exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Travel kit included", exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Umrah kit included", exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Not included", exact: true }),
  ).toBeVisible();

  const visa = page.getByRole("checkbox", { name: "Visa included" });
  await expect(visa).toBeChecked();
  await visa.uncheck();
  await expect(visa).not.toBeChecked();

  const luggageTag = page.getByRole("checkbox", { name: "Luggage tag" });
  await expect(luggageTag).not.toBeChecked();
  await luggageTag.check();
  await expect(luggageTag).toBeChecked();

  const personalExpenses = page.getByRole("checkbox", {
    name: "Personal expenses",
  });
  await expect(personalExpenses).toBeChecked();
  await personalExpenses.uncheck();
  await expect(personalExpenses).not.toBeChecked();

  await page
    .getByRole("button", { name: "Add another item to Travel kit included" })
    .click();
  await page.getByLabel("Custom item").fill("Wheelchair assistance");
  await page.getByRole("button", { name: "Add item" }).click();

  await expect(
    page.getByRole("checkbox", { name: "Wheelchair assistance" }),
  ).toBeChecked();

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

// prettier-ignore
test("journey dates calculate title, duration and stay nights", async ({
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
  await expect(page.getByText("6 nights", { exact: true })).toBeVisible();
  await expect(page.getByText("5 nights", { exact: true })).toBeVisible();
});

test("operator can complete journey, stays, intercity and create the draft", async ({
  page,
}) => {
  const departureId = "40000000-0000-0000-0000-000000000123";
  let submitted: Record<string, unknown> | null = null;

  await page.route("**/api/v1/operator/departures", async (route) => {
    if (route.request().method() !== "POST") {
      await route.continue();
      return;
    }

    submitted = route.request().postDataJSON() as Record<string, unknown>;
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ departureId }),
    });
  });

  await page.route(`**/api/v1/operator/departures/${departureId}`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        departureId,
        packageName: "12 Days / 11 Nights Umrah from Delhi (DEL)",
        summary: "Draft package",
        origin: "Delhi (DEL)",
        departureDate: "2027-01-10",
        returnDate: "2027-01-21",
        makkah: {
          hotelName: "Makkah Stay",
          classification: "5 star",
          distanceDisclosure: "",
          nights: 6,
        },
        madinah: {
          hotelName: "Madinah Stay",
          classification: "4 star",
          distanceDisclosure: "",
          nights: 5,
        },
        travel: {
          routeSummary: "Delhi (DEL) → Jeddah → Makkah → Madinah",
          details: "Intercity transfer by train. Flight details to be confirmed.",
        },
        inclusions: ["Intercity travel by train"],
        exclusions: ["Personal expenses"],
      }),
    });
  });

  await page.goto("/operator/packages/new");
  await page.getByLabel("Departure origin").fill("Delhi (DEL)");
  await page.getByLabel("Departure date").fill("2027-01-10");
  await page.getByLabel("Return date").fill("2027-01-21");
  await page.getByLabel("Hotel name").nth(0).fill("Makkah Stay");
  await page.getByLabel("Hotel classification").nth(0).selectOption("5 star");
  await page.getByLabel("Hotel name").nth(1).fill("Madinah Stay");
  await page.getByLabel("Hotel classification").nth(1).selectOption("4 star");
  await page.getByRole("radio", { name: "Train" }).check();
  await page.getByRole("checkbox", { name: "Luggage tag" }).check();

  await page.getByRole("button", { name: "Create draft & continue" }).click();
  await expect(page).toHaveURL(new RegExp(`/operator/departures/${departureId}$`));

  expect(submitted).not.toBeNull();
  expect(submitted).toMatchObject({
    origin: "Delhi (DEL)",
    departureDate: "2027-01-10",
    returnDate: "2027-01-21",
    makkah: {
      hotelName: "Makkah Stay",
      classification: "5 star",
      nights: 6,
    },
    madinah: {
      hotelName: "Madinah Stay",
      classification: "4 star",
      nights: 5,
    },
    travel: {
      details: "Intercity transfer by train. Flight details to be confirmed.",
    },
  });

  const inclusions = (submitted as { inclusions: string[] }).inclusions;
  expect(inclusions).toContain("Intercity travel by train");
  expect(inclusions).toContain("Luggage tag");
});

// prettier-ignore
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
