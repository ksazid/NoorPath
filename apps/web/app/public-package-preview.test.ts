import { describe, expect, it } from "vitest";
import {
  findPublicPackagePreview,
  publicPackagePreviews,
} from "./public-package-preview";

describe("public package previews", () => {
  it("resolves every preview by departure id", () => {
    for (const packagePreview of publicPackagePreviews) {
      expect(findPublicPackagePreview(packagePreview.departureId)).toEqual(
        packagePreview,
      );
    }
  });

  it("keeps later commercial slice concepts out of the preview contract", () => {
    const contract = JSON.stringify(publicPackagePreviews);

    expect(contract).not.toMatch(/price/i);
    expect(contract).not.toMatch(/capacity/i);
    expect(contract).not.toMatch(/availability/i);
    expect(contract).not.toMatch(/publish/i);
  });

  it("keeps Makkah, Madinah, and travel confirmation states explicit", () => {
    for (const packagePreview of publicPackagePreviews) {
      expect(packagePreview.makkah.confirmationState).toMatch(
        /^(confirmed|pending)$/,
      );
      expect(packagePreview.madinah.confirmationState).toMatch(
        /^(confirmed|pending)$/,
      );
      expect(packagePreview.travel.confirmationState).toMatch(
        /^(confirmed|pending)$/,
      );
    }
  });
});
