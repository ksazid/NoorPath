import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const departureId = "40000000-0000-0000-0000-000000000001";
const travellerId = "60000000-0000-0000-0000-000000000001";

async function mockAccess(page) {
  await page.route("**/api/v1/operator/access", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        accountId: "account-1",
        operator: { id: "operator-1", displayName: "NoorPath Travel" },
        permissions: ["operator.admin.access"],
      }),
    });
  });
}

function manifest(overrides: Record<string, unknown> = {}) {
  return {
    departure: {
      id: departureId,
      packageName: "12 Days Umrah from Delhi",
      origin: "Delhi (DEL)",
      departureDate: "2027-01-10",
      returnDate: "2027-01-21",
    },
    summary: {
      travellers: 1,
      ready: 0,
      blocked: 1,
      paymentBlocked: 0,
      documentBlocked: 0,
      visaBlocked: 0,
      accommodationBlocked: 1,
    },
    items: [
      {
        bookingId: "50000000-0000-0000-0000-000000000001",
        bookingReference: "NP-2027-0001",
        travellerId,
        position: 1,
        fullName: "Amina Rahman",
        dateOfBirth: "1987-04-05",
        readiness: "blocked",
        blockers: ["accommodation"],
        payment: { ready: true, paid: 189000, total: 189000, currency: "INR" },
        documents: { ready: true, required: 1 },
        visa: { ready: true, status: "Approved" },
        accommodation: {
          ready: false,
          makkahAssigned: true,
          madinahAssigned: false,
        },
        operation: null,
      },
    ],
    ...overrides,
  };
}

test("operator opens departure manifest, sees blockers and records follow-up", async ({
  page,
}) => {
  await mockAccess(page);
  let current = manifest();
  let operationRequest: unknown;

  await page.route(
    `**/api/v1/operator/departures/${departureId}/manifest`,
    async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(current),
      });
    },
  );
  await page.route(
    `**/api/v1/operator/departures/${departureId}/manifest/travellers/${travellerId}/operations`,
    async (route) => {
      operationRequest = route.request().postDataJSON();
      const updated = manifest();
      updated.items[0].operation = {
        note: "Confirm Madinah room before manifest handoff.",
        isAcknowledged: true,
        version: 1,
        actorAccountId: "account-1",
        updatedAtUtc: "2026-08-08T06:30:00Z",
      };
      current = updated;
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(updated.items[0].operation),
      });
    },
  );

  await page.goto(`/operator/departures/${departureId}/manifest`);

  await expect(
    page.getByRole("heading", { level: 1, name: "Pilgrim manifest" }),
  ).toBeVisible();
  await expect(page.getByText("Amina Rahman", { exact: true })).toBeVisible();
  await expect(
    page.getByText("Room assignment incomplete", { exact: true }),
  ).toBeVisible();

  await page
    .getByLabel("Operational note")
    .fill("Confirm Madinah room before manifest handoff.");
  await page.getByLabel("Follow-up acknowledged").check();
  await page.getByRole("button", { name: "Save operational update" }).click();

  expect(operationRequest).toEqual({
    note: "Confirm Madinah room before manifest handoff.",
    isAcknowledged: true,
    expectedVersion: 0,
  });
  await expect(
    page.getByText("Operational update saved for Amina Rahman.", { exact: true }),
  ).toBeVisible();

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("manifest supports readiness filtering and safe foreign-departure not-found", async ({
  page,
}) => {
  await mockAccess(page);
  await page.route(
    `**/api/v1/operator/departures/${departureId}/manifest`,
    async (route) => {
      if (route.request().headers()["x-test-not-found"] === "1") {
        await route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(manifest()),
      });
    },
  );

  await page.goto(`/operator/departures/${departureId}/manifest`);
  await page.getByLabel("Readiness filter").selectOption("visa");
  await expect(
    page.getByText("No travellers match this view", { exact: true }),
  ).toBeVisible();
  await expectNoHorizontalOverflow(page);

  await page.unroute(`**/api/v1/operator/departures/${departureId}/manifest`);
  await page.route(
    `**/api/v1/operator/departures/${departureId}/manifest`,
    async (route) => {
      await route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
    },
  );
  await page.reload();
  await expect(
    page.getByText("Departure manifest not found", { exact: true }),
  ).toBeVisible();
  await expect(page.getByText(/belongs to another operator/i)).toBeVisible();
});
