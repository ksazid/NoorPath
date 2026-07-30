import { describe, expect, it } from "vitest";
import OperatorPage from "./page";

describe("VS-01 operator access", () => {
  it("exports the protected operator surface", () => {
    expect(OperatorPage).toBeTypeOf("function");
    expect(OperatorPage.name).toBe("OperatorPage");
  });
});
