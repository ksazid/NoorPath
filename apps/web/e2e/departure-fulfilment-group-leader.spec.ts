import { expect, test, type Page } from "@playwright/test";

const departureId = "11111111-1111-1111-1111-111111111111";
const operatorAccess = {
  accountId: "operator-member-a",
  displayName: "Yusuf Ali",
  operator: { id: "operator-a", displayName: "Noor Travel" },
  permissions: ["operator.admin.access"],
};

async function mockManifest(page: Page) {
  let leader: string | null = null;
  let version = 0;

  await page.route("**/api/v1/operator/access", (route) =>
    route.fulfill({ status: 200, json: operatorAccess }),
  );

  await page.route(
    `**/api/v1/operator/departures/${departureId}/manifest/group-leader`,
    async (route) => {
      const body = route.request().postDataJSON() as {
        name?: string | null;
        expectedVersion: number;
      };
      if (body.expectedVersion !== version) {
        await route.fulfill({
          status: 409,
          json: {
            code: "departure_operations_stale",
            currentVersion: version,
          },
        });
        return;
      }
      leader = body.name?.trim() || null;
      version += 1;
      await route.fulfill({
        status: 200,
        json: { groupLeaderName: leader, version },
      });
    },
  );

  await page.route(
    `**/api/v1/operator/departures/${departureId}/manifest`,
    (route) =>
      route.fulfill({
        status: 200,
        json: {
          departure: {
            id: departureId,
            packageName: "NoorPath Essential Umrah",
            origin: "Mumbai",
            departureDate: "2026-10-10",
            returnDate: "2026-10-20",
          },
          summary: {
            travellers: 2,
            ready: 1,
            blocked: 1,
            paymentBlocked: 0,
            documentBlocked: 0,
            visaBlocked: 1,
            accommodationBlocked: 0,
          },
          fulfilment: {
            groupLeaderName: leader,
            version,
            isCompleted: false,
          },
          items: [],
        },
      }),
  );
}

test("departure fulfilment links the package and saves an accompanying group leader", async ({
  page,
}) => {
  await mockManifest(page);
  await page.goto(`/operator/departures/${departureId}/manifest`);

  const packageLink = page.getByRole("link", {
    name: "View package being fulfilled",
  });
  await expect(packageLink).toHaveAttribute(
    "href",
    `/operator/departures/${departureId}/preview`,
  );

  await page.getByLabel("Accompanying group leader").fill("Amina Rahman");
  await page.getByRole("button", { name: "Save group leader" }).click();
  await expect(page.getByRole("status")).toContainText(
    "Accompanying group leader saved",
  );
  await expect(page.getByLabel("Accompanying group leader")).toHaveValue(
    "Amina Rahman",
  );
  await expect(
    page.getByText(
      "Operational contact only. This does not add a booked traveller.",
    ),
  ).toBeVisible();

  await page.getByRole("button", { name: "Clear" }).click();
  await expect(page.getByRole("status")).toContainText(
    "Accompanying group leader cleared",
  );
  await expect(page.getByLabel("Accompanying group leader")).toHaveValue("");
});

test("departure fulfilment reflows on mobile with usable controls", async ({
  page,
}) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await mockManifest(page);
  await page.goto(`/operator/departures/${departureId}/manifest`);

  await page.getByLabel("Accompanying group leader").fill("Amina Rahman");
  const save = page.getByRole("button", { name: "Save group leader" });
  const box = await save.boundingBox();
  expect(box?.height ?? 0).toBeGreaterThanOrEqual(44);

  const overflow = await page.evaluate(
    () =>
      document.documentElement.scrollWidth >
      document.documentElement.clientWidth,
  );
  expect(overflow).toBe(false);
});
