import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

const states = [
  {
    name: "authorized",
    status: 200,
    body: {
      accountId: "account",
      operator: { id: "noor", displayName: "Noor Tours" },
      permissions: ["operator.admin.access"],
    },
    heading: "Operator administration",
  },
  {
    name: "unauthenticated",
    status: 401,
    body: { code: "not_authenticated" },
    heading: "Sign in to continue",
  },
  {
    name: "forbidden",
    status: 403,
    body: { code: "forbidden" },
    heading: "Access unavailable",
  },
] as const;

for (const state of states) {
  test(`${state.name} operator access is accessible and responsive`, async ({
    page,
  }) => {
    await page.route("**/api/v1/platform/access", (route) =>
      route.fulfill({
        status: 403,
        contentType: "application/json",
        body: JSON.stringify({ code: "forbidden" }),
      }),
    );
    await page.route("**/api/v1/operator/access", (route) =>
      route.fulfill({
        status: state.status,
        contentType: "application/json",
        body: JSON.stringify(state.body),
      }),
    );
    await page.route("**/api/v1/operator/catalogue", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [
            {
              departureId: "departure-1",
              packageTemplateId: "package-1",
              packageName: "Noor Umrah",
              origin: "Delhi, IN",
              status: "published",
            },
          ],
        }),
      }),
    );
    await page.route("**/api/v1/operator/visa", (route) =>
      route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [
            {
              caseId: "case-1",
              bookingId: "booking-1",
              travellerId: "traveller-1",
              status: "NotStarted",
            },
          ],
        }),
      }),
    );

    await page.goto("/operator");
    await expect(page.getByRole("heading", { level: 1 })).toHaveText(
      state.heading,
    );
    if (state.name === "authorized") {
      await expect(
        page.getByText("Your secure workspace is ready"),
      ).toBeVisible();
      await expect(page.getByRole("link", { name: "Packages", exact: true })).toHaveAttribute(
        "href",
        "/operator/packages",
      );
      await expect(page.getByRole("link", { name: "Departures", exact: true })).toHaveAttribute(
        "href",
        "/operator/departures",
      );
      await expect(page.getByRole("link", { name: "Account", exact: true })).toHaveAttribute(
        "href",
        "/operator/account",
      );
      await expect(page.getByText("1", { exact: true }).first()).toBeVisible();
    }
    await expectNoA11yViolations(page);
    await expectMinimumTargets(page);
    await expectNoHorizontalOverflow(page);
  });
}

test("Platform Administrator opening operator is guided to admin", async ({
  page,
}) => {
  await page.route("**/api/v1/operator/access", (route) =>
    route.fulfill({
      status: 403,
      contentType: "application/json",
      body: JSON.stringify({ code: "forbidden" }),
    }),
  );
  await page.route("**/api/v1/platform/access", (route) =>
    route.fulfill({
      status: 200,
      contentType: "application/json",
      body: JSON.stringify({ accountId: "platform-administrator" }),
    }),
  );

  await page.goto("/operator");

  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "Use NoorPath administration",
  );
  await expect(
    page.getByText(/signed in as a Platform Administrator/i),
  ).toBeVisible();
  await expect(
    page.getByRole("link", { name: "Open admin workspace" }),
  ).toHaveAttribute("href", "/admin");
  await expect(
    page.getByRole("link", { name: "Sign in securely" }),
  ).toHaveCount(0);
  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("operator access failure offers retry without leaking internals", async ({
  page,
}) => {
  let attempts = 0;
  await page.route("**/api/v1/operator/access", (route) => {
    attempts++;
    return route.fulfill({
      status: 503,
      contentType: "application/json",
      body: JSON.stringify({ correlationId: "safe-reference" }),
    });
  });
  await page.goto("/operator");
  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "We could not verify access",
  );
  await page.getByRole("button", { name: "Try again" }).click();
  expect(attempts).toBeGreaterThan(1);
});
