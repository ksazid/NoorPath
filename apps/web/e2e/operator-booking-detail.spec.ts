import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const bookingId = "50000000-0000-0000-0000-000000000001";

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

const detail = {
  bookingId,
  reference: "NP-2027-0001",
  departureId: "40000000-0000-0000-0000-000000000001",
  packageName: "12 Days Umrah from Delhi",
  origin: "Delhi (DEL)",
  departureDate: "2027-01-10",
  returnDate: "2027-01-21",
  state: "confirmed",
  occupancy: "double",
  travellerCount: 2,
  createdAtUtc: "2026-08-01T08:00:00Z",
  updatedAtUtc: "2026-08-07T08:00:00Z",
  payment: {
    currency: "INR",
    total: 189000,
    paid: 94500,
    outstanding: 94500,
    status: "partiallyPaid",
    instalments: [
      {
        sequence: 1,
        dueDate: "2026-08-01",
        amount: 47250,
        status: "paid",
      },
      {
        sequence: 2,
        dueDate: "2026-10-15",
        amount: 47250,
        status: "paid",
      },
      {
        sequence: 3,
        dueDate: "2026-12-01",
        amount: 94500,
        status: "due",
      },
    ],
  },
  documents: { required: 4, approved: 2 },
  visa: { total: 2, approved: 1 },
  travellers: [
    {
      travellerId: "60000000-0000-0000-0000-000000000001",
      fullName: "Amina Rahman",
      dateOfBirth: "1987-04-05",
      documents: [
        {
          requirementId: "70000000-0000-0000-0000-000000000001",
          kind: "PassportBioPage",
          status: "Approved",
        },
      ],
      visa: { status: "Approved", customerAction: null },
    },
    {
      travellerId: "60000000-0000-0000-0000-000000000002",
      fullName: "Omar Rahman",
      dateOfBirth: "1985-02-11",
      documents: [
        {
          requirementId: "70000000-0000-0000-0000-000000000002",
          kind: "PassportBioPage",
          status: "UnderReview",
        },
      ],
      visa: {
        status: "ActionRequired",
        customerAction: "Upload the corrected passport scan.",
      },
    },
  ],
};

test("operator opens a booking and sees traveller financial document and visa context", async ({
  page,
}) => {
  await mockAccess(page);
  await page.route(
    `**/api/v1/operator/bookings/${bookingId}`,
    async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(detail),
      });
    },
  );

  await page.goto(`/operator/bookings/${bookingId}`);

  await expect(
    page.getByRole("heading", { level: 1, name: "Booking detail" }),
  ).toBeVisible();
  await expect(page.getByText("NP-2027-0001", { exact: false })).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "12 Days Umrah from Delhi" }),
  ).toBeVisible();
  await expect(page.getByText("INR 189,000", { exact: true })).toBeVisible();
  await expect(
    page.getByText("INR 94,500", { exact: true }).first(),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Amina Rahman" }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Omar Rahman" }),
  ).toBeVisible();
  await expect(
    page.getByText("Visa: Action Required", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText("Customer action: Upload the corrected passport scan."),
  ).toBeVisible();

  await expect(
    page.getByRole("link", { name: "Back to bookings", exact: false }),
  ).toHaveAttribute("href", "/operator/bookings");
  await expect(
    page.getByRole("link", { name: "Open departure" }),
  ).toHaveAttribute("href", `/operator/departures/${detail.departureId}`);
  await expect(
    page.getByRole("link", { name: "Document review" }),
  ).toHaveAttribute("href", "/operator/documents");
  await expect(
    page.getByRole("link", { name: "Visa processing" }),
  ).toHaveAttribute("href", "/operator/visa");
  await expect(
    page.getByRole("link", { name: "Support actions" }),
  ).toHaveAttribute("href", "/operator/support");
  await expect(
    page.getByRole("link", { name: "Cancellation requests" }),
  ).toHaveAttribute("href", "/operator/cancellations");
  await expect(
    page.getByRole("link", { name: "Bookings", exact: true }),
  ).toHaveAttribute("aria-current", "page");

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("booking management click-through reaches the exact booking detail", async ({
  page,
}) => {
  await mockAccess(page);
  await page.route("**/api/v1/operator/bookings", async (route) => {
    await route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        summary: {
          total: 1,
          confirmed: 1,
          actionRequired: 1,
          travellers: 2,
        },
        items: [
          {
            bookingId,
            reference: detail.reference,
            accountId: "customer-1",
            departureId: detail.departureId,
            packageName: detail.packageName,
            origin: detail.origin,
            departureDate: detail.departureDate,
            returnDate: detail.returnDate,
            state: detail.state,
            occupancy: detail.occupancy,
            travellerCount: detail.travellerCount,
            travellers: detail.travellers.map(
              ({ travellerId, fullName, dateOfBirth }) => ({
                travellerId,
                fullName,
                dateOfBirth,
              }),
            ),
            payment: {
              currency: detail.payment.currency,
              total: detail.payment.total,
              paid: detail.payment.paid,
              outstanding: detail.payment.outstanding,
              status: detail.payment.status,
              nextInstalment: {
                sequence: 3,
                dueDate: "2026-12-01",
                amount: 94500,
              },
            },
            documents: { status: "inProgress", required: 4, approved: 2 },
            visa: { status: "actionRequired", total: 2, approved: 1 },
            createdAtUtc: detail.createdAtUtc,
            updatedAtUtc: detail.updatedAtUtc,
          },
        ],
      }),
    });
  });
  await page.route(
    `**/api/v1/operator/bookings/${bookingId}`,
    async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(detail),
      });
    },
  );

  await page.goto("/operator/bookings");
  await page.getByRole("link", { name: "Open booking" }).click();
  await expect(page).toHaveURL(new RegExp(`/operator/bookings/${bookingId}$`));
  await expect(
    page.getByText("Booking NP-2027-0001", { exact: true }),
  ).toBeVisible();
  await expectNoHorizontalOverflow(page);
});

test("foreign or unavailable operator booking is a safe not-found", async ({
  page,
}) => {
  await mockAccess(page);
  await page.route(
    `**/api/v1/operator/bookings/${bookingId}`,
    async (route) => {
      await route.fulfill({
        status: 404,
        contentType: "application/json",
        body: "{}",
      });
    },
  );

  await page.goto(`/operator/bookings/${bookingId}`);
  await expect(
    page.getByText("Booking not found.", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText(/does not belong to your operator account/i),
  ).toBeVisible();
  await expect(
    page.getByRole("link", { name: "Back to bookings" }),
  ).toHaveAttribute("href", "/operator/bookings");
});

test("forbidden booking detail gives explicit operator access feedback", async ({
  page,
}) => {
  await mockAccess(page);
  await page.route(
    `**/api/v1/operator/bookings/${bookingId}`,
    async (route) => {
      await route.fulfill({
        status: 403,
        contentType: "application/json",
        body: "{}",
      });
    },
  );

  await page.goto(`/operator/bookings/${bookingId}`);
  await expect(
    page.getByText("You do not have access to this operator booking."),
  ).toBeVisible();
});

test("booking detail retry recovers after a transient API failure", async ({
  page,
}) => {
  await mockAccess(page);
  let requests = 0;
  await page.route(
    `**/api/v1/operator/bookings/${bookingId}`,
    async (route) => {
      requests += 1;
      await route.fulfill(
        requests === 1
          ? { status: 503, contentType: "application/json", body: "{}" }
          : {
              status: 200,
              contentType: "application/json",
              body: JSON.stringify(detail),
            },
      );
    },
  );

  await page.goto(`/operator/bookings/${bookingId}`);
  await expect(
    page.getByText("Booking details are temporarily unavailable."),
  ).toBeVisible();
  await page.getByRole("button", { name: "Retry" }).click();
  await expect(
    page.getByRole("heading", { name: "12 Days Umrah from Delhi" }),
  ).toBeVisible();
});
