import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const bookingId = "50000000-0000-0000-0000-000000000001";

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
    instalments: [],
  },
  documents: { required: 2, approved: 2 },
  visa: { total: 2, approved: 2 },
  travellers: [
    {
      travellerId: "60000000-0000-0000-0000-000000000001",
      fullName: "Amina Rahman",
      dateOfBirth: "1987-04-05",
      documents: [],
      visa: { status: "Approved", customerAction: null },
    },
    {
      travellerId: "60000000-0000-0000-0000-000000000002",
      fullName: "Omar Rahman",
      dateOfBirth: "1985-02-11",
      documents: [],
      visa: { status: "Approved", customerAction: null },
    },
  ],
};

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

async function mockDetail(page, status = 200) {
  await page.route(`**/api/v1/operator/bookings/${bookingId}`, async (route) => {
    await route.fulfill({
      status,
      contentType: "application/json",
      body: status === 200 ? JSON.stringify(detail) : "{}",
    });
  });
}

const preview = {
  bookingId,
  reference: detail.reference,
  bookingVersion: 3,
  current: {
    occupancy: "double",
    travellers: detail.travellers,
    financials: {
      currency: "INR",
      unitPrice: 94500,
      total: 189000,
      dueNow: 37800,
      remaining: 151200,
      instalments: [],
    },
  },
  proposed: {
    occupancy: "triple",
    travellers: [
      detail.travellers[0],
      detail.travellers[1],
      {
        travellerId: "60000000-0000-0000-0000-000000000003",
        fullName: "Sara Rahman",
        dateOfBirth: "1990-03-10",
      },
    ],
    financials: {
      currency: "INR",
      unitPrice: 87500,
      total: 262500,
      dueNow: 52500,
      remaining: 210000,
      instalments: [],
    },
    priceVersionId: "80000000-0000-0000-0000-000000000001",
  },
  priceDelta: 73500,
  changesMoney: true,
  previewToken: "protected-preview-token",
  expiresAtUtc: "2026-08-07T17:00:00Z",
};

test("confirmed booking opens amendment flow and requires authoritative preview before confirmation", async ({
  page,
}) => {
  await mockAccess(page);
  await mockDetail(page);

  let previewRequest: unknown;
  let confirmRequest: unknown;
  await page.route(
    `**/api/v1/operator/bookings/${bookingId}/amendments/preview`,
    async (route) => {
      previewRequest = route.request().postDataJSON();
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(preview),
      });
    },
  );
  await page.route(
    `**/api/v1/operator/bookings/${bookingId}/amendments/confirm`,
    async (route) => {
      confirmRequest = route.request().postDataJSON();
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          amendmentId: "90000000-0000-0000-0000-000000000001",
          bookingId,
          bookingVersion: 4,
          priceDelta: 73500,
          currency: "INR",
        }),
      });
    },
  );

  await page.goto(`/operator/bookings/${bookingId}`);
  const amendLink = page.getByRole("link", { name: "Amend booking" });
  await expect(amendLink).toHaveAttribute(
    "href",
    `/operator/bookings/${bookingId}/amend`,
  );
  await amendLink.click();

  await expect(page).toHaveURL(
    new RegExp(`/operator/bookings/${bookingId}/amend$`),
  );
  await expect(
    page.getByRole("heading", { level: 1, name: "Amend booking" }),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Confirm amendment" }),
  ).toHaveCount(0);

  await page.getByLabel("Occupancy").selectOption("triple");
  await page
    .getByRole("group", { name: "Traveller 3" })
    .getByLabel("Full name")
    .fill("Sara Rahman");
  await page
    .getByRole("group", { name: "Traveller 3" })
    .getByLabel("Date of birth")
    .fill("1990-03-10");
  await page
    .getByLabel("Reason for amendment")
    .fill("Add the confirmed third traveller and move to triple sharing.");
  await page.getByRole("button", { name: "Preview amendment impact" }).click();

  expect(previewRequest).toMatchObject({
    occupancy: "triple",
    reason: "Add the confirmed third traveller and move to triple sharing.",
  });
  await expect(page.getByText("INR 2,62,500", { exact: true })).toBeVisible();
  await expect(page.getByText("+INR 73,500", { exact: true })).toBeVisible();
  await expect(
    page.getByText("Additional collection stays in Payments."),
  ).toBeVisible();

  const confirmation = page.getByRole("checkbox");
  const confirmButton = page.getByRole("button", { name: "Confirm amendment" });
  await expect(confirmButton).toBeDisabled();
  await confirmation.check();
  await expect(confirmButton).toBeEnabled();
  await confirmButton.click();

  expect(confirmRequest).toEqual({
    previewToken: "protected-preview-token",
    confirmed: true,
  });
  await expect(page).toHaveURL(
    new RegExp(`/operator/bookings/${bookingId}\\?amended=1$`),
  );

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("stale confirmation invalidates the preview and requires re-preview", async ({
  page,
}) => {
  await mockAccess(page);
  await mockDetail(page);
  await page.route(
    `**/api/v1/operator/bookings/${bookingId}/amendments/preview`,
    async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          ...preview,
          proposed: {
            ...preview.proposed,
            occupancy: "double",
            travellers: detail.travellers,
            financials: preview.current.financials,
          },
          priceDelta: 0,
          changesMoney: false,
        }),
      });
    },
  );
  await page.route(
    `**/api/v1/operator/bookings/${bookingId}/amendments/confirm`,
    async (route) => {
      await route.fulfill({
        status: 409,
        contentType: "application/json",
        body: JSON.stringify({
          code: "booking_stale",
          message:
            "The booking changed after this preview was created. Refresh and preview again.",
        }),
      });
    },
  );

  await page.goto(`/operator/bookings/${bookingId}/amend`);
  await page
    .getByLabel("Reason for amendment")
    .fill("Correct the booked traveller snapshot.");
  await page.getByRole("button", { name: "Preview amendment impact" }).click();
  await page.getByRole("checkbox").check();
  await page.getByRole("button", { name: "Confirm amendment" }).click();

  await expect(
    page.getByText(/booking changed after this preview was created/i),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Confirm amendment" }),
  ).toHaveCount(0);
});

test("foreign operator booking stays a safe not-found in amendment route", async ({
  page,
}) => {
  await mockAccess(page);
  await mockDetail(page, 404);

  await page.goto(`/operator/bookings/${bookingId}/amend`);
  await expect(page.getByText("Booking not found.", { exact: true })).toBeVisible();
  await expect(
    page.getByText(/does not belong to your operator account/i),
  ).toBeVisible();
  await expectNoHorizontalOverflow(page);
});
