import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const departureId = "40000000-0000-0000-0000-000000000001";

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

function handover(overrides: Record<string, unknown> = {}) {
  return {
    departure: {
      id: departureId,
      packageName: "12 Days Umrah from Delhi",
      origin: "Delhi (DEL)",
      departureDate: "2027-01-10",
      returnDate: "2027-01-21",
    },
    summary: {
      travellers: 2,
      ready: 2,
      blocked: 0,
      paymentBlocked: 0,
      documentBlocked: 0,
      visaBlocked: 0,
      accommodationBlocked: 0,
    },
    canComplete: true,
    handover: {
      isCompleted: false,
      finalNote: null,
      completedByAccountId: null,
      completedAtUtc: null,
      version: 0,
    },
    audits: [],
    ...overrides,
  };
}

test("operator completes final departure handover and sees immutable closeout", async ({
  page,
}) => {
  await mockAccess(page);
  let current = handover();
  let completionRequest: unknown;

  await page.route(
    `**/api/v1/operator/departures/${departureId}/handover`,
    async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(current),
      });
    },
  );
  await page.route(
    `**/api/v1/operator/departures/${departureId}/handover/complete`,
    async (route) => {
      completionRequest = route.request().postDataJSON();
      current = handover({
        handover: {
          isCompleted: true,
          finalNote: "Ground team briefed and departure handover complete.",
          completedByAccountId: "account-1",
          completedAtUtc: "2026-08-08T15:30:00Z",
          version: 1,
        },
        audits: [
          {
            action: "completed",
            note: "Ground team briefed and departure handover complete.",
            actorAccountId: "account-1",
            previousVersion: 0,
            resultingVersion: 1,
            travellerCount: 2,
            blockedCount: 0,
            occurredAtUtc: "2026-08-08T15:30:00Z",
          },
        ],
      });
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          isCompleted: true,
          finalNote: "Ground team briefed and departure handover complete.",
          completedByAccountId: "account-1",
          completedAtUtc: "2026-08-08T15:30:00Z",
          version: 1,
          idempotent: false,
        }),
      });
    },
  );

  await page.goto(`/operator/departures/${departureId}/handover`);

  await expect(
    page.getByRole("heading", { level: 1, name: "Final departure handover" }),
  ).toBeVisible();
  await expect(
    page.getByText("Payment: clear", { exact: true }),
  ).toBeVisible();
  await expect(page.getByText("Visa: clear", { exact: true })).toBeVisible();

  await page
    .getByLabel("Final operational note")
    .fill("Ground team briefed and departure handover complete.");
  await page.getByRole("button", { name: "Complete final handover" }).click();

  expect(completionRequest).toEqual({
    finalNote: "Ground team briefed and departure handover complete.",
    expectedVersion: 0,
  });
  await expect(
    page.getByText("Final departure handover completed.", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Handover completed" }),
  ).toBeVisible();
  await expect(page.getByLabel("Final operational note")).toBeDisabled();

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("blocked handover remains locked and foreign departure is safe not-found", async ({
  page,
}) => {
  await mockAccess(page);
  await page.setViewportSize({ width: 390, height: 844 });

  await page.route(
    `**/api/v1/operator/departures/${departureId}/handover`,
    async (route) => {
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(
          handover({
            summary: {
              travellers: 2,
              ready: 1,
              blocked: 1,
              paymentBlocked: 0,
              documentBlocked: 0,
              visaBlocked: 1,
              accommodationBlocked: 0,
            },
            canComplete: false,
          }),
        ),
      });
    },
  );

  await page.goto(`/operator/departures/${departureId}/handover`);
  await expect(
    page.getByText("Visa: 1 blocked", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText(
      /Final handover remains locked until every traveller readiness blocker is resolved/i,
    ),
  ).toBeVisible();
  await page.getByLabel("Final operational note").fill("Attempted closeout.");
  await expect(
    page.getByRole("button", { name: "Complete final handover" }),
  ).toBeDisabled();
  await expectNoHorizontalOverflow(page);
  await expectMinimumTargets(page);

  await page.unroute(`**/api/v1/operator/departures/${departureId}/handover`);
  await page.route(
    `**/api/v1/operator/departures/${departureId}/handover`,
    async (route) => {
      await route.fulfill({
        status: 404,
        contentType: "application/json",
        body: "{}",
      });
    },
  );
  await page.reload();
  await expect(
    page.getByText("Departure handover not found", { exact: true }),
  ).toBeVisible();
  await expect(page.getByText(/belongs to another operator/i)).toBeVisible();
});
