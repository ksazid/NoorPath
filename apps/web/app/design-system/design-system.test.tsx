import { describe, expect, it } from "vitest";
import {
  ActionButton,
  OccupancyAvatarGroup,
  PACKAGE_DETAIL_SECTION_ORDER,
  StatusBadge,
} from "../_components/DesignSystem";
import { NoorPathIcon } from "../_components/NoorPathIcon";

describe("VS-18 design-system foundation", () => {
  it("keeps the approved Package Details section order fixed", () => {
    expect(PACKAGE_DETAIL_SECTION_ORDER).toEqual([
      "hero-gallery",
      "verified-operator",
      "package-summary",
      "hotels",
      "room-occupancy-and-pricing",
      "journey-payment-summary",
      "reserve-action",
      "itinerary",
      "package-inclusions",
      "travel-kit",
      "umrah-kit",
      "journey-payment-schedule",
      "service-confirmation",
      "cancellation-policy",
      "help-and-support",
      "sticky-reservation-action",
    ]);
  });

  it("exports the shared component and icon primitives", () => {
    expect(ActionButton).toBeTypeOf("function");
    expect(StatusBadge).toBeTypeOf("function");
    expect(OccupancyAvatarGroup).toBeTypeOf("function");
    expect(NoorPathIcon).toBeTypeOf("function");
  });
});
