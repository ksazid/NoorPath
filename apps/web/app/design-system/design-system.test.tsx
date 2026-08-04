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
import {
  IconButton,
  MetricCard,
  StickyActionBar,
} from "../_components/InteractionPrimitives";
import { NoorPathIcon } from "../_components/NoorPathIcon";
import {
  CheckboxField,
  RadioCard,
  SelectField,
  ToggleField,
} from "../_components/SelectionPrimitives";
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

  it("exports the shared design-system primitives", () => {
    for (const component of [
      ActionButton,
      StatusBadge,
      OccupancyAvatarGroup,
      TextField,
      SelectField,
      CheckboxField,
      RadioCard,
      ToggleField,
      SupportAction,
      SkeletonBlock,
      IconButton,
      MetricCard,
      StickyActionBar,
      NoorPathIcon,
      CustomerShell,
      StaffShell,
    ]) {
      expect(component).toBeTypeOf("function");
    }
  });
});
