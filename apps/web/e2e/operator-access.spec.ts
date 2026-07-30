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
    heading: "Noor Tours",
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
    heading: "This account cannot open operator administration",
  },
] as const;

for (const state of states) {
  test(`${state.name} operator access is accessible and responsive`, async ({
    page,
  }) => {
    await page.route("**/api/v1/operator/access", (route) =>
      route.fulfill({
        status: state.status,
        contentType: "application/json",
        body: JSON.stringify(state.body),
      }),
    );
    await page.goto("/operator");
    await expect(page.getByRole("heading", { level: 1 })).toHaveText(
      state.heading,
    );
    if (state.name === "authorized") {
      await expect(
        page.getByRole("link", { name: "Create new draft" }),
      ).toHaveAttribute("href", "/operator/departures/new");
    }
    await expectNoA11yViolations(page);
    await expectMinimumTargets(page);
    await expectNoHorizontalOverflow(page);
    await expect(page).toHaveScreenshot(`operator-${state.name}.png`, {
      fullPage: true,
    });
  });
}

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
  await expect(page.getByText("Reference: safe-reference")).toBeVisible();
  await page.getByRole("button", { name: "Try again" }).click();
  expect(attempts).toBeGreaterThan(1);
});
