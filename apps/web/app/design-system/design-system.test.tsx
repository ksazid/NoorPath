import { describe, expect, it } from "vitest";
import {
  ActionButton,
  OccupancyAvatarGroup,
  PACKAGE_DETAIL_SECTION_ORDER,
  StatusBadge,
} from "../_components/DesignSystem";
import {
  SkeletonBlock,
  SupportAction,
  TextField,
} from "../_components/FormPrimitives";
import { NoorPathIcon } from "../_components/NoorPathIcon";
import { CustomerShell, StaffShell } from "../_components/Shells";

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

  it("exports the shared component, icon, form and shell primitives", () => {
    expect(ActionButton).toBeTypeOf("function");
    expect(StatusBadge).toBeTypeOf("function");
    expect(OccupancyAvatarGroup).toBeTypeOf("function");
    expect(TextField).toBeTypeOf("function");
    expect(SupportAction).toBeTypeOf("function");
    expect(SkeletonBlock).toBeTypeOf("function");
    expect(NoorPathIcon).toBeTypeOf("function");
    expect(CustomerShell).toBeTypeOf("function");
    expect(StaffShell).toBeTypeOf("function");
  });
});
