import { readFileSync } from "node:fs";
import { createRequire } from "node:module";
import { expect, type Page } from "@playwright/test";

const require = createRequire(import.meta.url);
const axeSource = readFileSync(require.resolve("axe-core/axe.min.js"), "utf8");

export async function expectNoA11yViolations(page: Page) {
  await page.addScriptTag({ content: axeSource });
  const violations = await page.evaluate(async () => {
    const result = await window.axe.run(document, {
      runOnly: {
        type: "tag",
        values: ["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"],
      },
    });
    return result.violations.map(({ id, impact, nodes }) => ({
      id,
      impact,
      targets: nodes.map((node) => node.target),
    }));
  });
  expect(violations).toEqual([]);
}

export async function expectMinimumTargets(page: Page) {
  const undersized = await page
    .locator("button:visible, a:visible")
    .evaluateAll((elements) =>
      elements
        .filter((element) => {
          const box = element.getBoundingClientRect();
          return box.width < 44 || box.height < 44;
        })
        .map(
          (element) =>
            element.textContent?.trim() || element.getAttribute("aria-label"),
        ),
    );
  expect(undersized).toEqual([]);
}

export async function expectNoHorizontalOverflow(page: Page) {
  expect(
    await page.evaluate(
      () =>
        document.documentElement.scrollWidth <=
        document.documentElement.clientWidth,
    ),
  ).toBe(true);
}

declare global {
  interface Window {
    axe: {
      run(
        root: Document,
        options: object,
      ): Promise<{
        violations: Array<{
          id: string;
          impact: string | null;
          nodes: Array<{ target: unknown }>;
        }>;
      }>;
    };
  }
}
