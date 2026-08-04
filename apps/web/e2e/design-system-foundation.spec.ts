import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

test("design-system foundation renders shared contracts on desktop", async ({
  page,
}, testInfo) => {
  await page.goto("/design-system");

  await expect(
    page.getByRole("heading", { name: "NoorPath component foundation" }),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Reserve Your Seats" }),
  ).toBeVisible();
  await expect(page.getByText("Double Sharing")).toBeVisible();
  await expect(page.getByText("Triple Sharing")).toBeVisible();
  await expect(page.getByText("Quad Sharing")).toBeVisible();
  await expect(page.getByText("Action required").first()).toBeVisible();

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);

  const primaryAction = page.getByRole("button", {
    name: "Reserve Your Seats",
  });
  await primaryAction.focus();
  await expect(primaryAction).toBeFocused();
  expect(
    await primaryAction.evaluate(
      (element) => getComputedStyle(element).outlineStyle,
    ),
  ).not.toBe("none");

  await testInfo.attach("design-system-desktop", {
    body: await page.screenshot({ fullPage: true }),
    contentType: "image/png",
  });
});

test("design-system foundation reflows on mobile with reduced motion", async ({
  page,
}, testInfo) => {
  await page.emulateMedia({ reducedMotion: "reduce" });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto("/design-system");

  await expect(
    page.getByRole("heading", { name: "NoorPath component foundation" }),
  ).toBeVisible();
  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);

  await page.evaluate(() => {
    document.documentElement.style.fontSize = "200%";
  });
  await expectNoHorizontalOverflow(page);
  await expect(
    page.getByRole("button", { name: "Reserve Your Seats" }),
  ).toBeVisible();

  await testInfo.attach("design-system-mobile-200-percent", {
    body: await page.screenshot({ fullPage: true }),
    contentType: "image/png",
  });
});
