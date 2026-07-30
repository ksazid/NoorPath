import { describe, expect, it } from "vitest";
import {
  emptyOccupancyValues,
  validateInventory,
  validatePricing,
} from "./CommercialEditor";

describe("VS-03 commercial editor validation", () => {
  it("requires explicit currency and at least one positive occupancy price", () => {
    const values = emptyOccupancyValues();

    expect(validatePricing("", values).currency).toBeTruthy();
    expect(validatePricing("INR", values).pricing).toBeTruthy();

    values.double = "110000";
    expect(validatePricing("INR", values)).toEqual({});
  });

  it("rejects invalid price precision and unsupported numeric shapes", () => {
    const values = emptyOccupancyValues();
    values.double = "10.001";
    values.triple = "-1";

    const errors = validatePricing("INR", values);
    expect(errors["price.double"]).toBeTruthy();
    expect(errors["price.triple"]).toBeTruthy();
  });

  it("allows zero capacity but requires a reason for an inventory write", () => {
    const values = emptyOccupancyValues();
    values.quad = "0";

    expect(validateInventory(values, "").reason).toBeTruthy();
    expect(validateInventory(values, "Pause Quad allocation")).toEqual({});
  });

  it("rejects fractional or negative capacity", () => {
    const values = emptyOccupancyValues();
    values.double = "1.5";
    values.triple = "-2";

    const errors = validateInventory(values, "Capacity review");
    expect(errors["capacity.double"]).toBeTruthy();
    expect(errors["capacity.triple"]).toBeTruthy();
  });
});
