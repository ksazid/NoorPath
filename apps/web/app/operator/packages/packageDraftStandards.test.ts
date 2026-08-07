import { describe, expect, it } from "vitest";
import {
  calculateJourneyDuration,
  normalizePackageItems,
  suggestPackageTitle,
} from "./packageDraftStandards";

// prettier-ignore
describe("package draft standards", () => {
  it("uses customer-safe standard terminology", () => {
    expect(
      normalizePackageItems(["Visa assistance", "Meals", "visa assistance"]),
    ).toEqual(["Visa included", "Breakfast, lunch and dinner"]);
  });

  it("calculates inclusive days and hotel nights", () => {
    expect(calculateJourneyDuration("2026-09-01", "2026-09-12")).toEqual({
      days: 12,
      nights: 11,
    });
  });

  it("suggests a consistent package title", () => {
    expect(
      suggestPackageTitle("Delhi", "2026-09-01", "2026-09-12"),
    ).toBe("12 Days / 11 Nights Umrah from Delhi");
  });
});
