import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { test } from "node:test";

const tokenJson = JSON.parse(readFileSync("tokens.json", "utf8"));
const tokenCss = readFileSync("tokens.css", "utf8");

const requiredCssTokens = [
  "--color-brand-haram",
  "--color-brand-kaaba",
  "--color-brand-kiswah",
  "--color-brand-madinah",
  "--color-surface-canvas",
  "--color-surface-raised",
  "--color-text-primary",
  "--color-text-secondary",
  "--color-border-default",
  "--color-focus-ring",
  "--color-action-primary",
  "--color-status-success",
  "--color-status-warning",
  "--color-status-danger",
  "--color-disabled-surface",
  "--color-skeleton-base",
  "--font-family-display",
  "--font-family-sans",
  "--space-4",
  "--radius-md",
  "--shadow-sm",
  "--motion-standard",
  "--layer-modal",
];

const foundationStyles = [
  "../../apps/web/app/design-system.css",
  "../../apps/web/app/primitives.css",
  "../../apps/web/app/interaction-primitives.css",
  "../../apps/web/app/shells.css",
  "../../apps/web/app/shell-slots.css",
];

test("semantic token JSON and CSS exports remain available", () => {
  assert.equal(tokenJson.color.brand.madinahGreen, "#176B50");
  assert.equal(tokenJson.color.surface.canvas, "#FBF9F5");
  assert.equal(tokenJson.space[4], "1rem");

  for (const token of requiredCssTokens) {
    assert.match(tokenCss, new RegExp(`${token.replaceAll("-", "\\-")}:`));
  }
});

test("shared foundation styles use semantic colours", () => {
  const rawColour = /#[0-9a-f]{3,8}\b|rgba?\(/i;

  for (const path of foundationStyles) {
    const css = readFileSync(path, "utf8");
    assert.doesNotMatch(css, rawColour, `${path} contains a raw colour value`);
  }
});

test("reduced-motion protection remains exported", () => {
  assert.match(tokenCss, /prefers-reduced-motion:\s*reduce/);
});
