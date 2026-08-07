import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const bookingId = "50000000-0000-0000-0000-000000000001";
const travellerA = "60000000-0000-0000-0000-000000000001";
const travellerB = "60000000-0000-0000-0000-000000000002";
const roomId = "70000000-0000-0000-0000-000000000001";

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
      travellerId: travellerA,
      fullName: "Amina Rahman",
      dateOfBirth: "1987-04-05",
      documents: [],
      visa: { status: "Approved", customerAction: null },
    },
    {
      travellerId: travellerB,
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

async function mockBookingDetail(page) {
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
}

function workspace(occupants: string[] = [], version = 1, isLocked = false) {
  return {
    bookingId,
    reference: detail.reference,
    bookingState: "confirmed",
    bookingOccupancy: "double",
    travellers: [
      { travellerId: travellerA, position: 1, fullName: "Amina Rahman" },
      { travellerId: travellerB, position: 2, fullName: "Omar Rahman" },
    ],
    rooms: [
      {
        roomId,
        stay: "makkah",
        roomType: "double",
        label: "Makkah 201",
        capacity: 2,
        version,
        isLocked,
        occupants,
      },
    ],
    unassigned: [
      {
        stay: "makkah",
        travellerIds: [travellerA, travellerB].filter(
          (travellerId) => !occupants.includes(travellerId),
        ),
      },
      { stay: "madinah", travellerIds: [travellerA, travellerB] },
    ],
    history:
      occupants.length === 0
        ? []
        : [
            {
              auditId: "80000000-0000-0000-0000-000000000001",
              travellerId: travellerA,
              previousRoomId: null,
              roomId,
              stay: "makkah",
              action: "assigned",
              reason: "Keep family allocation together.",
              occurredAtUtc: "2026-08-07T18:00:00Z",
            },
          ],
  };
}

test("operator opens accommodation from booking detail and assigns a traveller", async ({
  page,
}) => {
  await mockAccess(page);
  await mockBookingDetail(page);

  let current = workspace();
  let assignmentRequest: unknown;
  await page.route(
    `**/api/v1/operator/bookings/${bookingId}/accommodation`,
    async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(current),
      });
    },
  );
  await page.route(
    `**/api/v1/operator/bookings/${bookingId}/accommodation/rooms/${roomId}/assign`,
    async (route) => {
      assignmentRequest = route.request().postDataJSON();
      current = workspace([travellerA], 2);
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          roomId,
          travellerId: travellerA,
          roomVersion: 2,
          action: "assigned",
        }),
      });
    },
  );

  await page.goto(`/operator/bookings/${bookingId}`);
  const accommodationLink = page.getByRole("link", { name: "Accommodation" });
  await expect(accommodationLink).toHaveAttribute(
    "href",
    `/operator/bookings/${bookingId}/accommodation`,
  );
  await accommodationLink.click();

  await expect(page).toHaveURL(
    new RegExp(`/operator/bookings/${bookingId}/accommodation$`),
  );
  await expect(
    page.getByRole("heading", { level: 1, name: "Accommodation" }),
  ).toBeVisible();
  await expect(page.getByRole("heading", { name: "Makkah 201" })).toBeVisible();
  await expect(
    page
      .getByLabel("Makkah rooms")
      .getByText("2 unassigned", { exact: true }),
  ).toBeVisible();

  const room = page.getByRole("article").filter({ hasText: "Makkah 201" });
  await room.getByLabel("Traveller to assign or move").selectOption(travellerA);
  await room
    .getByLabel("Operational reason")
    .fill("Keep family allocation together.");
  await room.getByRole("button", { name: "Assign / move" }).click();

  expect(assignmentRequest).toEqual({
    travellerId: travellerA,
    reason: "Keep family allocation together.",
    expectedRoomVersion: 1,
    expectedPreviousRoomVersion: null,
  });
  await expect(room.getByText("Amina Rahman", { exact: true })).toBeVisible();
  await expect(page.getByText("1 unassigned", { exact: true })).toBeVisible();
  await expect(
    page.getByText("Traveller assigned.", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText("Keep family allocation together.", { exact: true }),
  ).toBeVisible();

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("locked accommodation disables assignment controls", async ({ page }) => {
  await mockAccess(page);
  await page.route(
    `**/api/v1/operator/bookings/${bookingId}/accommodation`,
    async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(workspace([], 4, true)),
      });
    },
  );

  await page.goto(`/operator/bookings/${bookingId}/accommodation`);
  const room = page.getByRole("article").filter({ hasText: "Makkah 201" });
  await expect(room.getByText("Locked", { exact: true })).toBeVisible();
  await expect(room.getByLabel("Traveller to assign or move")).toBeDisabled();
  await expect(
    room.getByRole("button", { name: "Assign / move" }),
  ).toBeDisabled();
  await expect(room.getByRole("button", { name: "Unlock room" })).toBeEnabled();
  await expectNoHorizontalOverflow(page);
});

test("foreign accommodation route stays a safe not-found", async ({ page }) => {
  await mockAccess(page);
  await page.route(
    `**/api/v1/operator/bookings/${bookingId}/accommodation`,
    async (route) => {
      await route.fulfill({
        status: 404,
        contentType: "application/json",
        body: "{}",
      });
    },
  );

  await page.goto(`/operator/bookings/${bookingId}/accommodation`);
  await expect(
    page.getByText("Booking accommodation not found.", { exact: true }),
  ).toBeVisible();
  await expect(page.getByText(/belongs to another operator/i)).toBeVisible();
  await expectNoHorizontalOverflow(page);
});
