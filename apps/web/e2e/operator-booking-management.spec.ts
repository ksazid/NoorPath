import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

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

test("operator can manage bookings with payment document and visa progress", async ({
  page,
}) => {
  await mockAccess(page);
  await page.route("**/api/v1/operator/bookings", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        summary: { total: 2, confirmed: 1, actionRequired: 1, travellers: 4 },
        items: [
          {
            bookingId: "50000000-0000-0000-0000-000000000001",
            reference: "NP-2027-0001",
            accountId: "customer-1",
            departureId: "40000000-0000-0000-0000-000000000001",
            packageName: "12 Days Umrah from Delhi",
            origin: "Delhi (DEL)",
            departureDate: "2027-01-10",
            returnDate: "2027-01-21",
            state: "confirmed",
            occupancy: "double",
            travellerCount: 2,
            travellers: [
              {
                travellerId: "60000000-0000-0000-0000-000000000001",
                fullName: "Amina Rahman",
                dateOfBirth: "1987-04-05",
              },
              {
                travellerId: "60000000-0000-0000-0000-000000000002",
                fullName: "Omar Rahman",
                dateOfBirth: "1985-02-11",
              },
            ],
            payment: {
              currency: "INR",
              total: 189000,
              paid: 94500,
              outstanding: 94500,
              status: "partiallyPaid",
              nextInstalment: {
                sequence: 2,
                dueDate: "2026-10-15",
                amount: 47250,
              },
            },
            documents: { status: "inProgress", required: 4, approved: 2 },
            visa: { status: "actionRequired", total: 2, approved: 1 },
            createdAtUtc: "2026-08-01T08:00:00Z",
            updatedAtUtc: "2026-08-07T08:00:00Z",
          },
          {
            bookingId: "50000000-0000-0000-0000-000000000002",
            reference: "NP-2027-0002",
            accountId: "customer-2",
            departureId: "40000000-0000-0000-0000-000000000002",
            packageName: "Premium Umrah from Mumbai",
            origin: "Mumbai (BOM)",
            departureDate: "2027-02-05",
            returnDate: "2027-02-16",
            state: "pendingPayment",
            occupancy: "double",
            travellerCount: 2,
            travellers: [
              {
                travellerId: "60000000-0000-0000-0000-000000000003",
                fullName: "Sara Khan",
                dateOfBirth: "1990-01-01",
              },
              {
                travellerId: "60000000-0000-0000-0000-000000000004",
                fullName: "Yusuf Khan",
                dateOfBirth: "1988-01-01",
              },
            ],
            payment: {
              currency: "INR",
              total: 210000,
              paid: 0,
              outstanding: 210000,
              status: "awaitingPayment",
              nextInstalment: null,
            },
            documents: { status: "notStarted", required: 0, approved: 0 },
            visa: { status: "notStarted", total: 2, approved: 0 },
            createdAtUtc: "2026-08-02T08:00:00Z",
            updatedAtUtc: "2026-08-06T08:00:00Z",
          },
        ],
      }),
    });
  });

  await page.goto("/operator/bookings");

  await expect(
    page.getByRole("heading", { level: 1, name: "Bookings" }),
  ).toBeVisible();
  await expect(page.getByText("2", { exact: true }).first()).toBeVisible();
  await expect(page.getByText("NP-2027-0001", { exact: false })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "12 Days Umrah from Delhi" }),
  ).toBeVisible();
  await expect(page.getByText("Amina Rahman", { exact: true })).toBeVisible();
  await expect(page.getByText("Omar Rahman", { exact: true })).toBeVisible();
  await expect(
    page.getByText("INR 94,500 paid", { exact: false }),
  ).toBeVisible();
  await expect(
    page.getByText("2 of 4 requirements approved", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText("1 of 2 travellers approved", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("link", { name: "Open document review" }),
  ).toHaveAttribute("href", "/operator/documents");
  await expect(
    page.getByRole("link", { name: "Open visa processing" }),
  ).toHaveAttribute("href", "/operator/visa");

  await page.getByLabel("Search bookings").fill("Amina");
  await expect(
    page.getByRole("heading", { name: "12 Days Umrah from Delhi" }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Premium Umrah from Mumbai" }),
  ).toHaveCount(0);

  await page.getByLabel("Search bookings").fill("");
  await page.getByRole("button", { name: "Confirmed" }).click();
  await expect(
    page.getByRole("heading", { name: "12 Days Umrah from Delhi" }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Premium Umrah from Mumbai" }),
  ).toHaveCount(0);

  await expect(page.getByRole("link", { name: "Bookings" })).toHaveAttribute(
    "aria-current",
    "page",
  );
  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("operator booking management has a useful empty state", async ({
  page,
}) => {
  await mockAccess(page);
  await page.route("**/api/v1/operator/bookings", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        summary: { total: 0, confirmed: 0, actionRequired: 0, travellers: 0 },
        items: [],
      }),
    });
  });

  await page.goto("/operator/bookings");
  await expect(
    page.getByRole("heading", { name: "No bookings yet" }),
  ).toBeVisible();
  await expect(
    page.getByRole("link", { name: "View packages" }),
  ).toHaveAttribute("href", "/operator/packages");
  await expectNoHorizontalOverflow(page);
});
