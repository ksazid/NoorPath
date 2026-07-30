import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

async function openAdmin(page) {
  await page.goto("/");
  await page.getByRole("button", { name: "Admin preview" }).click();
  await expect(
    page.getByRole("heading", { name: "Create a publishable journey" }),
  ).toBeVisible();
}

test("approved operator draft is explicitly published and appears in discovery", async ({
  page,
}, testInfo) => {
  const packageName = `Noor E2E ${testInfo.project.name}`;
  await openAdmin(page);
  await page.getByLabel("Public package name").fill(packageName);
  await page.getByRole("button", { name: "Review and publish" }).click();
  const dialog = page.getByRole("dialog", {
    name: "Make this journey public?",
  });
  await expect(dialog).toBeVisible();
  await expect(dialog.getByText(packageName)).toBeVisible();
  await expect(dialog.getByText("₹94,500 per person")).toBeVisible();
  await expect(
    dialog.getByRole("button", { name: "Keep as draft" }),
  ).toBeFocused();
  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await dialog.getByRole("button", { name: "Confirm publication" }).click();
  await expect(page.getByRole("status")).toContainText(
    "Published successfully",
  );
  await page.getByRole("button", { name: "View customer page" }).click();
  await expect(page.getByRole("heading", { name: packageName })).toBeVisible();
  await expect(page.getByText("₹94,500")).toBeVisible();
  await expectNoA11yViolations(page);
  await expectNoHorizontalOverflow(page);
  await expect(page).toHaveScreenshot("published-customer.png", {
    fullPage: true,
  });
});

test("a saved but non-published batch never appears in customer discovery", async ({
  page,
}, testInfo) => {
  const packageName = `Never public ${testInfo.project.name}`;
  await openAdmin(page);
  await page.getByLabel("Public package name").fill(packageName);
  await page.getByRole("button", { name: "Save draft" }).click();
  await expect(
    page.getByRole("button", { name: "Draft saved" }),
  ).toBeDisabled();
  await page.goto("/");
  await expect(page.getByRole("heading", { name: packageName })).toHaveCount(0);
});

test("admin validation retains values, blocks review, allows correction, and returns focus after Escape", async ({
  page,
}) => {
  await openAdmin(page);
  await page
    .getByLabel("Short public summary")
    .fill("This retained summary must survive validation.");
  await page.getByLabel("Public package name").fill("");
  await page.getByLabel("Return date").fill("2026-10-09");
  await page.getByLabel("Total capacity").fill("0");
  await page.getByLabel("Total starting price per person").fill("0");
  await page.getByRole("button", { name: "Review and publish" }).click();
  const errors = page.locator(".error-summary");
  await expect(errors).toContainText("Review 4 highlighted field(s)");
  await expect(errors).toBeFocused();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(page.getByLabel("Short public summary")).toHaveValue(
    "This retained summary must survive validation.",
  );
  await page.getByLabel("Public package name").fill("Corrected Noor journey");
  await page.getByLabel("Return date").fill("2026-10-22");
  await page.getByLabel("Total capacity").fill("24");
  await page.getByLabel("Total starting price per person").fill("94500");
  const review = page.getByRole("button", { name: "Review and publish" });
  await review.click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(review).toBeFocused();
});
