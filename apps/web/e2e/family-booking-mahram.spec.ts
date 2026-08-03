import { expect, test } from "@playwright/test";
import { expectNoA11yViolations, expectNoHorizontalOverflow } from "./helpers";

const party = {
  party: {
    id: "11111111-1111-1111-1111-111111111111",
    name: "Khan family",
    status: "Validated",
    policyVersion: "vs15-structural-v1",
    version: 4,
    updatedAtUtc: "2026-08-03T12:00:00Z",
  },
  members: [
    {
      travellerId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      version: 0,
      addedAtUtc: "2026-08-03T10:00:00Z",
    },
    {
      travellerId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      version: 0,
      addedAtUtc: "2026-08-03T10:01:00Z",
    },
  ],
  mahramLinks: [
    {
      id: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      protectedTravellerId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      mahramTravellerId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      relationshipType: "Brother",
      declaration: "I confirm this family relationship is accurate.",
      version: 0,
      updatedAtUtc: "2026-08-03T11:00:00Z",
    },
  ],
};

async function mockFamily(page: import("@playwright/test").Page) {
  await page.route("**/api/v1/travellers", (route) =>
    route.fulfill({
      json: {
        items: [
          {
            id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            travellerId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
            fullName: "Amina Khan",
            dateOfBirth: "1995-04-12",
          },
          {
            id: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            travellerId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
            fullName: "Omar Khan",
            dateOfBirth: "1992-08-20",
          },
        ],
      },
    }),
  );
  await page.route("**/api/v1/family-parties", (route) =>
    route.fulfill({ json: { parties: [party.party] } }),
  );
  await page.route("**/api/v1/family-parties/*", (route) => {
    if (route.request().method() === "GET")
      return route.fulfill({ json: party });
    return route.fallback();
  });
}

async function waitForFamilyReady(page: import("@playwright/test").Page) {
  await page.waitForLoadState("networkidle");
  await expect(
    page.getByRole("heading", { name: "Travellers in this party", level: 2 }),
  ).toBeVisible({ timeout: 15_000 });
  await expect(
    page.getByRole("heading", { name: "Loading family travellers", level: 2 }),
  ).toBeHidden();
}

test("customer reviews a validated family party and Mahram link", async ({
  page,
}) => {
  await mockFamily(page);
  await page.goto("/account/family");
  await waitForFamilyReady(page);

  await expect(
    page.getByRole("heading", { name: "Family travellers", level: 1 }),
  ).toBeVisible();

  const travellersSection = page
    .getByRole("heading", { name: "Travellers in this party", level: 2 })
    .locator("..");
  await expect(
    travellersSection.getByText("Amina Khan", { exact: true }),
  ).toBeVisible();
  await expect(
    travellersSection.getByText("Omar Khan", { exact: true }),
  ).toBeVisible();
  await expect(
    page.locator("strong").filter({ hasText: /^Amina Khan → Omar Khan$/ }),
  ).toBeVisible();
  await expect(page.getByText("Validated", { exact: true })).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Validate party" }),
  ).toBeVisible();

  await expectNoA11yViolations(page);
  await page.setViewportSize({ width: 390, height: 844 });
  await expectNoHorizontalOverflow(page);
});

test("customer receives recoverable stale-version guidance", async ({
  page,
}) => {
  await mockFamily(page);
  await page.route("**/api/v1/family-parties/*/validate", (route) =>
    route.fulfill({
      status: 409,
      json: {
        code: "stale_family_party",
        message:
          "This family party was updated elsewhere. Refresh before validating again.",
      },
    }),
  );
  await page.goto("/account/family");
  await waitForFamilyReady(page);

  const validateButton = page.getByRole("button", { name: "Validate party" });
  await expect(validateButton).toBeVisible();
  await validateButton.click();
  await expect(page.getByRole("alert")).toContainText(
    "This family party was updated elsewhere",
  );
});
