import { describe, expect, it } from "vitest";
import { createEmptyDraft, validateDraft } from "./DepartureComposer";

describe("departure composer validation", () => {
  it("rejects an empty draft", () => {
    const errors = validateDraft(createEmptyDraft());

    expect(errors.packageName).toBeTruthy();
    expect(errors.summary).toBeTruthy();
    expect(errors["makkah.hotelName"]).toBeTruthy();
    expect(errors["madinah.hotelName"]).toBeTruthy();
    expect(errors["travel.routeSummary"]).toBeTruthy();
    expect(errors.origin).toBeTruthy();
    expect(errors.departureDate).toBeTruthy();
    expect(errors.returnDate).toBeTruthy();
    expect(errors.stays).toBeTruthy();
  });

  it("accepts a valid factual draft without pricing or inventory", () => {
    const draft = createEmptyDraft();
    draft.packageName = "Noor Harmony 12 Nights";
    draft.summary = "A factual Makkah and Madinah journey draft.";
    draft.makkah.hotelName = "Makkah Hotel";
    draft.makkah.nights = "6";
    draft.madinah.hotelName = "Madinah Hotel";
    draft.madinah.nights = "5";
    draft.travel.routeSummary = "Delhi → Jeddah → Makkah → Madinah";
    draft.origin = "Delhi (DEL)";
    draft.departureDate = "2026-10-10";
    draft.returnDate = "2026-10-22";

    expect(validateDraft(draft)).toEqual({});
    expect(draft).not.toHaveProperty("price");
    expect(draft).not.toHaveProperty("capacity");
    expect(draft).not.toHaveProperty("availability");
  });
});
