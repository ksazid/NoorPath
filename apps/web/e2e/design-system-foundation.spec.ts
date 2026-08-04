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
    page.getByRole("button", { name: "Reserve Your Seats" }).first(),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Double Sharing" }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Triple Sharing" }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Quad Sharing" }),
  ).toBeVisible();
  await expect(page.getByText("Action required").first()).toBeVisible();

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);

  const primaryAction = page
    .getByRole("button", { name: "Reserve Your Seats" })
    .first();
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
    page.getByRole("button", { name: "Reserve Your Seats" }).first(),
  ).toBeVisible();

  await testInfo.attach("design-system-mobile-200-percent", {
    body: await page.screenshot({ fullPage: true }),
    contentType: "image/png",
  });
});

test("canonical customer shell remains accessible across desktop and mobile", async ({
  page,
}, testInfo) => {
  await page.goto("/design-system/customer-shell");

  await expect(
    page.getByRole("heading", {
      name: "Your trusted Umrah journey starts here",
    }),
  ).toBeVisible();
  await expect(page.getByRole("contentinfo")).toBeVisible();
  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);

  await testInfo.attach("customer-shell-initial", {
    body: await page.screenshot({ fullPage: true }),
    contentType: "image/png",
  });

  await page.setViewportSize({ width: 390, height: 844 });
  await page.reload();
  await page.getByText("Menu", { exact: true }).click();
  await expect(
    page
      .locator(".np-customer-menu__panel")
      .getByRole("link", { name: "My Journey" }),
  ).toBeVisible();
  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);

  await testInfo.attach("customer-shell-mobile", {
    body: await page.screenshot({ fullPage: true }),
    contentType: "image/png",
  });
});

test("canonical staff shell groups navigation and reflows to a drawer", async ({
  page,
}, testInfo) => {
  await page.goto("/design-system/staff-shell");

  await expect(
    page.getByRole("heading", { name: "Operations dashboard" }),
  ).toBeVisible();

  const initialWidth = page.viewportSize()?.width ?? 1363;
  const activeNavigation =
    initialWidth <= 900
      ? page.locator(".np-staff-menu__panel")
      : page.locator(".np-staff-sidebar");

  if (initialWidth <= 900) {
    await page.getByText("Workspace navigation", { exact: true }).click();
  }

  await expect(
    activeNavigation.getByRole("navigation", { name: "Staff navigation" }),
  ).toBeVisible();
  await expect(
    activeNavigation.getByText("Administration", { exact: true }),
  ).toBeVisible();
  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);

  await testInfo.attach("staff-shell-initial", {
    body: await page.screenshot({ fullPage: true }),
    contentType: "image/png",
  });

  if (initialWidth > 900) {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.reload();
    await page.getByText("Workspace navigation", { exact: true }).click();
  }

  await expect(
    page
      .locator(".np-staff-menu__panel")
      .getByRole("link", { name: "Audit Log" }),
  ).toBeVisible();
  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);

  await testInfo.attach("staff-shell-mobile", {
    body: await page.screenshot({ fullPage: true }),
    contentType: "image/png",
  });
});
