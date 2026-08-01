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
  const report = await page.evaluate(() => {
    const viewportWidth = document.documentElement.clientWidth;
    const documentWidth = document.documentElement.scrollWidth;
    const allElements = document.querySelectorAll<HTMLElement>("body *");

    const elements = Array.from(allElements)
      .filter((element) => {
        const style = getComputedStyle(element);
        if (style.display === "none" || style.visibility === "hidden") {
          return false;
        }

        const box = element.getBoundingClientRect();
        return box.right > viewportWidth + 1 || box.left < -1;
      })
      .slice(0, 20)
      .map((element) => {
        const box = element.getBoundingClientRect();
        const classes = Array.from(element.classList);
        const classNames = classes.map((name) => `.${name}`);
        const rawText = element.textContent ?? "";
        const text = rawText.trim().replace(/\s+/g, " ").slice(0, 120);
        const identity = [
          element.tagName.toLowerCase(),
          element.id ? `#${element.id}` : "",
          ...classNames,
        ].join("");

        return {
          element: identity,
          text,
          left: Math.round(box.left * 100) / 100,
          right: Math.round(box.right * 100) / 100,
          width: Math.round(box.width * 100) / 100,
          clientWidth: element.clientWidth,
          scrollWidth: element.scrollWidth,
        };
      });

    return { viewportWidth, documentWidth, elements };
  });

  if (report.documentWidth > report.viewportWidth) {
    const details = JSON.stringify(report, null, 2);
    throw new Error(`Horizontal overflow detected:\n${details}`);
  }
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
