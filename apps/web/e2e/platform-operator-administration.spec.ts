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

const publicationDepartureId = "50000000-0000-0000-0000-000000000001";

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

async function arrangePublicationApi(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/platform/publications", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({
        items: [
          {
            departureId: publicationDepartureId,
            operatorId: "operator-a",
            packageName: "Classic Umrah from Delhi",
            origin: "Delhi (DEL)",
            departureDate: "2027-01-10",
            returnDate: "2027-01-21",
            departureVersion: 7,
            submittedAtUtc: "2026-08-07T09:30:00Z",
          },
        ],
      }),
    }),
  );

  await page.route(
    `**/api/v1/platform/publications/${publicationDepartureId}`,
    (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          departureId: publicationDepartureId,
          operatorId: "operator-a",
          status: "readyForReview",
          departureVersion: 7,
          pricingVersion: 3,
          inventoryVersion: 2,
          ready: true,
          checks: [
            {
              key: "operator-approved",
              label: "Operator approved",
              passed: true,
              detail: "Operator approval is current.",
            },
          ],
          package: {
            name: "Classic Umrah from Delhi",
            summary: "A clear, operator-backed Umrah journey from Delhi.",
            origin: "Delhi (DEL)",
            departureDate: "2027-01-10",
            returnDate: "2027-01-21",
            makkah: {
              hotelName: "Makkah Hotel",
              classification: "4 star",
              distanceDisclosure: "Distance disclosed by operator",
              nights: 6,
              confirmationState: "confirmed",
            },
            madinah: {
              hotelName: "Madinah Hotel",
              classification: "4 star",
              distanceDisclosure: "Distance disclosed by operator",
              nights: 5,
              confirmationState: "confirmed",
            },
            travel: {
              routeSummary: "Delhi → Jeddah → Delhi",
              details: "Flight details confirmed by operator.",
              confirmationState: "confirmed",
            },
            inclusions: ["Umrah visa included", "Hotel stay"],
            exclusions: ["Personal expenses"],
          },
          pricing: {
            currency: "INR",
            version: 3,
            occupancies: [{ occupancy: "quad", amount: 125000 }],
          },
          inventory: {
            version: 2,
            pools: [
              { occupancy: "quad", capacity: 20, availableQuantity: 14 },
            ],
          },
        }),
      }),
  );
}

async function openPlatformAdminMenu(page: import("@playwright/test").Page) {
  const sidebar = page.locator(".np-platform-admin-shell .np-staff-sidebar");
  if (await sidebar.isVisible()) {
    return sidebar.getByRole("navigation", {
      name: "Platform administration navigation",
    });
  }

  const menu = page.locator(".np-platform-admin-shell .np-staff-menu");
  if (!(await menu.getAttribute("open"))) {
    await menu.getByText("Platform Admin menu", { exact: true }).click();
  }
  return menu.getByRole("navigation", {
    name: "Platform administration navigation",
  });
}

test("platform administrator can approve an operator from the command centre", async ({
  page,
}) => {
  await arrangeAdminApi(page);
  await page.goto("/admin");

  await expect(
    page.getByRole("heading", { level: 1, name: "Platform operations" }),
  ).toBeVisible();
  await expect(page.getByRole("img", { name: "NoorPath" }).first()).toBeVisible();

  const navigation = await openPlatformAdminMenu(page);
  await expect(navigation.getByRole("link", { name: "Overview" })).toHaveAttribute(
    "aria-current",
    "page",
  );
  await navigation.getByRole("link", { name: "Operators" }).click();
  await expect(page).toHaveURL(/\/admin#operators$/);
  await expect(navigation.getByRole("link", { name: "Operators" })).toHaveAttribute(
    "aria-current",
    "page",
  );

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
  await expect(
    page.getByRole("contentinfo").getByText("NoorPath Platform Administration"),
  ).toBeVisible();

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

test("platform administrator keeps the shared shell through publication review", async ({
  page,
}) => {
  await arrangeAdminApi(page);
  await arrangePublicationApi(page);
  await page.goto("/admin");

  let navigation = await openPlatformAdminMenu(page);
  await navigation.getByRole("link", { name: "Publication reviews" }).click();
  await expect(page).toHaveURL(/\/platform\/publications$/);
  await expect(
    page.getByRole("heading", { level: 1, name: "Publication reviews" }),
  ).toBeVisible();

  navigation = await openPlatformAdminMenu(page);
  await expect(
    navigation.getByRole("link", { name: "Publication reviews" }),
  ).toHaveAttribute("aria-current", "page");
  await expect(
    page.getByRole("contentinfo").getByText("NoorPath Platform Administration"),
  ).toBeVisible();

  await page.getByRole("link", { name: "Review for publication" }).click();
  await expect(page).toHaveURL(
    new RegExp(`/platform/publications/${publicationDepartureId}$`),
  );
  await expect(
    page.getByRole("heading", { level: 1, name: "Review publication" }),
  ).toBeVisible();

  navigation = await openPlatformAdminMenu(page);
  await expect(
    navigation.getByRole("link", { name: "Publication reviews" }),
  ).toHaveAttribute("aria-current", "page");
  await expect(page.getByRole("link", { name: "Back to queue" })).toBeVisible();

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
  const menu = page.locator(".np-platform-admin-shell .np-staff-menu");
  await expect(menu.getByText("Platform Admin menu", { exact: true })).toBeVisible();
  const navigation = await openPlatformAdminMenu(page);
  await expect(navigation.getByRole("link", { name: "Overview" })).toHaveAttribute(
    "aria-current",
    "page",
  );
  await expect(
    page.getByRole("contentinfo").getByText("NoorPath Platform Administration"),
  ).toBeVisible();

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});