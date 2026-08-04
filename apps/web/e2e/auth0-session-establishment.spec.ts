import { expect, test } from "@playwright/test";
import {
  expectMinimumTargets,
  expectNoA11yViolations,
  expectNoHorizontalOverflow,
} from "./helpers";

test("protected customer entry preserves a safe account return destination", async ({
  page,
}) => {
  await page.goto("/auth/sign-in?returnUrl=/account");

  await expect(page.getByRole("heading", { level: 1 })).toHaveText(
    "Sign in to NoorPath",
  );
  await expect(
    page.getByRole("link", { name: "Continue with Google" }),
  ).toHaveAttribute("href", /returnTo=%2Faccount/);

  await expectNoA11yViolations(page);
  await expectMinimumTargets(page);
  await expectNoHorizontalOverflow(page);
});

test("operator and platform deep links remain relative and do not become open redirects", async ({
  page,
}) => {
  await page.goto(
    "/auth/sign-in?returnUrl=https%3A%2F%2Fevil.example%2Foperator",
  );

  await expect(
    page.getByRole("link", { name: "Continue with Google" }),
  ).toHaveAttribute("href", /returnTo=%2Faccount/);
});
