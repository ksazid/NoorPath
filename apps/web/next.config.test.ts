import { describe, expect, it } from "vitest";
import nextConfig from "./next.config";

describe("API routing", () => {
  it("does not shadow the authenticated App Router API proxy", async () => {
    const configured = await nextConfig.rewrites?.();
    const rewrites = Array.isArray(configured)
      ? configured
      : [
          ...(configured?.beforeFiles ?? []),
          ...(configured?.afterFiles ?? []),
          ...(configured?.fallback ?? []),
        ];

    expect(rewrites.some(({ source }) => source.startsWith("/api/v1"))).toBe(
      false,
    );
  });
});
