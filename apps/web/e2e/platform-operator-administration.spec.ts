import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const pendingOperator = {
  id: "barakah",
  displayName: "Barakah Umrah",
  state: "pendingApproval",
  version: 1,
  createdAtUtc: "2026-08-09T10:00:00Z",
  updatedAtUtc: "2026-08-09T10:00:00Z",
  allowedTransitions: ["approved", "rejected"],
};

const approvedOperator = {
  id: "noor",
  displayName: "Noor Tours",
  state: "approved",
  version: 4,
  createdAtUtc: "2026-07-30T10:00:00Z",
  updatedAtUtc: "2026-08-08T10:00:00Z",
  allowedTransitions: ["suspended", "deactivated"],
};

async function arrangeAdminApi(page: import("@playwright/test").Page) {
  let approved = false;

  await page.route("**/api/v1/platform/access", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        accountId: "platform-administrator",
        displayName: "Platform Administrator",
      }),
    }),
  );

  await page.route("**/api/v1/platform/operators/summary", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        total: 2,
        pendingApproval: approved ? 0 : 1,
        approved: approved ? 2 : 1,
        suspended: 0,
        rejected: 0,
        deactivated: 0,
      }),
    }),
  );

  await page.route("**/api/v1/platform/operators/barakah", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        operator: approved
          ? {
              ...pendingOperator,
              state: "approved",
              version: 2,
              allowedTransitions: ["suspended", "deactivated"],
            }
          : pendingOperator,
        memberships: [],
        history: approved
          ? [
              {
                id: "audit-1",
                fromState: "pendingApproval",
                toState: "approved",
                actorAccountId: "platform-administrator",
                reason: "Business verification completed.",
                operatorVersion: 2,
                timestamp: "2026-08-09T12:00:00Z",
              },
            ]
          : [],
      }),
    }),
  );

  await page.route(
    "**/api/v1/platform/operators/barakah/state",
    async (route) => {
      const request = route.request();
      const body = request.postDataJSON() as {
        targetState: string;
        expectedVersion: number;
        reason?: string | null;
      };
      expect(body.targetState).toBe("approved");
      expect(body.expectedVersion).toBe(1);
      approved = true;
      await route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          operator: {
            ...pendingOperator,
            state: "approved",
            version: 2,
            allowedTransitions: ["suspended", "deactivated"],
          },
          changedAtUtc: "2026-08-09T12:00:00Z",
        }),
      });
    },
  );

  await page.route("**/api/v1/platform/operators", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        items: [
          approved
            ? {
                ...pendingOperator,
                state: "approved",
                version: 2,
                allowedTransitions: ["suspended", "deactivated"],
              }
            : pendingOperator,
          approvedOperator,
        ],
      }),
    }),
  );
}

test("platform administrator can approve an operator from the command centre", async ({
  page,
}) => {
  await arrangeAdminApi(page);
  await page.goto("/admin");

  await expect(
    page.getByRole("heading", { level: 1, name: "Platform operations" }),
  ).toBeVisible();
  await expect(
    page.getByText("Pending approval", { exact: true }).first(),
  ).toBeVisible();
  await expect(page.getByText("Barakah Umrah", { exact: true })).toBeVisible();

  const card = page.locator(".platform-operator-card").filter({
    hasText: "Barakah Umrah",
  });
  await expect(card.getByLabel("Decision")).toHaveValue("approved");
  await card.getByLabel(/Reason/).fill("Business verification completed.");
  await card.getByRole("button", { name: "Apply decision" }).click();

  await expect(page.getByText("Barakah Umrah is now Approved.")).toBeVisible();
  await expect(
    card.getByText("Approved", { exact: true }).first(),
  ).toBeVisible();
  await expect(
    page.getByText("Pending approval", { exact: true }).first(),
  ).toHaveText("Pending approval");

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("platform administrator can inspect append-only operator decision history", async ({
  page,
}) => {
  await arrangeAdminApi(page);
  await page.goto("/admin");

  const card = page.locator(".platform-operator-card").filter({
    hasText: "Barakah Umrah",
  });
  await card.getByRole("button", { name: "Apply decision" }).click();
  await expect(page.getByText("Barakah Umrah is now Approved.")).toBeVisible();
  await card.getByRole("button", { name: "View history" }).click();

  await expect(
    card.getByRole("heading", { name: "Decision history" }),
  ).toBeVisible();
  await expect(card.getByText("Pending approval → Approved")).toBeVisible();
  await expect(
    card.getByText("Business verification completed."),
  ).toBeVisible();

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("platform administration reflows at mobile width", async ({ page }) => {
  await arrangeAdminApi(page);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/admin");

  await expect(
    page.getByRole("heading", { name: "Platform operations" }),
  ).toBeVisible();
  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});
