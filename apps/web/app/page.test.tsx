import { describe, expect, it } from "vitest";
import HomePage from "./page";

describe("S02 operator batch publication", () => {
  it("exports the interactive discovery surface", () => {
    expect(HomePage).toBeTypeOf("function");
    expect(HomePage.name).toBe("App");
  });
});
