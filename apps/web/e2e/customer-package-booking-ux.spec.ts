import { expect, test, type Page } from "@playwright/test";

const departure = {
  departureId: "11111111-1111-1111-1111-111111111111",
  operator: { id: "operator-a", displayName: "Noor Travel" },
  packageName: "September Umrah",
  summary: "A calm, guided Umrah journey.",
  origin: "Delhi (DEL)",
  departureDate: "2026-09-18",
  returnDate: "2026-09-30",
  durationNights: 12,
  makkah: {
    hotelName: "Makkah Hotel",
    classification: "5 star",
    distanceDisclosure: "Approximately 300 m from Masjid al-Haram",
    nights: 7,
    confirmationState: "confirmed",
  },
  madinah: {
    hotelName: "Madinah Hotel",
    classification: "5 star",
    distanceDisclosure: "Approximately 250 m from Al-Masjid an-Nabawi",
    nights: 5,
    confirmationState: "confirmed",
  },
  travel: {
    routeSummary: "Delhi → Jeddah → Makkah → Madinah → Delhi",
    details: "Operator travel details",
    confirmationState: "pending",
  },
  inclusions: ["Return air travel", "Visa support"],
  exclusions: ["Personal expenses"],
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
        availableQuantity: 4,
        status: "available",
      },
      {
        occupancy: "quad",
        amount: 90000,
        availableQuantity: 0,
        status: "unavailable",
      },
    ],
  },
};

async function mockDeparture(page: Page) {
  await page.route("**/api/v1/departures/**", async (route) => {
    if (route.request().url().includes("/quotes")) {
      await route.fallback();
      return;
    }
    await route.fulfill({ status: 200, json: departure });
  });
  await page.route("**/api/v1/travellers", (route) =>
    route.fulfill({ status: 401, json: { title: "Sign in required" } }),
  );
}

test("package occupancy selection carries into the booking planner", async ({
  page,
}) => {
  await mockDeparture(page);
  await page.goto(`/packages/${departure.departureId}`);

  const triple = page.locator('input[name="package-occupancy"][value="triple"]');
  await expect(triple).toBeEnabled();
  await triple.check();

  const continueLink = page.getByRole("link", {
    name: /continue with triple sharing/i,
  });
  await expect(continueLink).toHaveAttribute(
    "href",
    `/packages/${departure.departureId}/plan?occupancy=triple`,
  );

  const quad = page.locator('input[name="package-occupancy"][value="quad"]');
  await expect(quad).toBeDisabled();

  await continueLink.click();
  await expect(page).toHaveURL(/\/plan\?occupancy=triple$/);
  await expect(
    page.locator('input[name="occupancy"][value="triple"]'),
  ).toBeChecked();
  await expect(page.getByRole("link", { name: "Package" })).toHaveAttribute(
    "href",
    `/packages/${departure.departureId}`,
  );
});

test("package flow uses one customer shell and reflows on mobile", async ({
  page,
}) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await mockDeparture(page);
  await page.goto(`/packages/${departure.departureId}`);

  await expect(page.locator(".np-customer-header")).toHaveCount(1);
  await expect(page.locator(".np-customer-footer")).toHaveCount(1);
  await expect(page.locator(".public-topbar")).toBeHidden();

  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);

  const selectable = page.locator(
    'label.occupancy-choice:has(input[value="double"])',
  );
  const box = await selectable.boundingBox();
  expect(box?.height ?? 0).toBeGreaterThanOrEqual(44);
});
