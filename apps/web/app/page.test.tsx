import { describe, expect, it } from "vitest";
import HomePage from "./page";

describe("NoorPath foundation page", () => {
  it("renders the approved pilot identity", () => {
    const page = HomePage();

    expect(page.type).toBe("main");
    expect(JSON.stringify(page.props.children)).toContain("NoorPath pilot");
  });
});
