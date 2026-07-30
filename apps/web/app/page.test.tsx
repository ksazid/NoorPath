import { describe, expect, it } from "vitest";
import HomePage from "./page";

describe("V2 public customer preview", () => {
  it("exports the customer landing page", () => {
    expect(HomePage).toBeTypeOf("function");
    expect(HomePage.name).toBe("HomePage");
  });
});
