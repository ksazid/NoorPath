import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

test("operator can start a package draft with standard NoorPath terminology", async ({ page }) => {
  await page.goto("/operator/packages/new");

  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "Create a package draft",
  );
  await expect(page.getByText("Visa included", { exact: true })).toBeVisible();
  await expect(
    page.getByText("Breakfast, lunch and dinner", { exact: true }),
  ).toBeVisible();
  await expect(page.getByRole("radio", { name: "Bus" })).toBeChecked();
  await expect(page.getByRole("radio", { name: "Train" })).not.toBeChecked();

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("journey dates calculate the package title and duration", async ({ page }) => {
  await page.goto("/operator/packages/new");

  await page.getByLabel("Departure origin").fill("Delhi (DEL)");
  await page.getByLabel("Departure date").fill("2027-01-10");
  await page.getByLabel("Return date").fill("2027-01-21");

  await expect(page.getByText("12 Days / 11 Nights", { exact: true })).toBeVisible();
  await expect(
    page.getByText("12 Days / 11 Nights Umrah from Delhi (DEL)", {
      exact: true,
    }),
  ).toBeVisible();
});
