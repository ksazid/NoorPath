import { afterEach, describe, expect, it } from "vitest";
import { NextRequest } from "next/server";
import { GET } from "./route";

const originalEndpoint = process.env.NOORPATH_AUTH_SIGN_IN_URL;

afterEach(() => {
  if (originalEndpoint === undefined)
    delete process.env.NOORPATH_AUTH_SIGN_IN_URL;
  else process.env.NOORPATH_AUTH_SIGN_IN_URL = originalEndpoint;
});

describe("provider-neutral sign in", () => {
  it("rejects unknown authentication methods", async () => {
    const response = GET(
      new NextRequest("https://noorpath.test/api/auth/sign-in?method=password"),
    );

    expect(response.status).toBe(400);
    await expect(response.json()).resolves.toMatchObject({
      code: "invalid_sign_in_method",
    });
  });

  it("redirects phone OTP through the configured identity service", () => {
    process.env.NOORPATH_AUTH_SIGN_IN_URL =
      "https://identity.example.test/authorize";

    const response = GET(
      new NextRequest(
        "https://noorpath.test/api/auth/sign-in?method=phone&returnUrl=%2Faccount",
      ),
    );

    expect(response.status).toBe(303);
    const destination = new URL(response.headers.get("location")!);
    expect(destination.origin).toBe("https://identity.example.test");
    expect(destination.searchParams.get("method")).toBe("phone");
    expect(destination.searchParams.get("returnUrl")).toBe(
      "https://noorpath.test/account",
    );
  });

  it("does not accept an external return URL", () => {
    process.env.NOORPATH_AUTH_SIGN_IN_URL =
      "https://identity.example.test/authorize";

    const response = GET(
      new NextRequest(
        "https://noorpath.test/api/auth/sign-in?method=google&returnUrl=https%3A%2F%2Fevil.test",
      ),
    );

    const destination = new URL(response.headers.get("location")!);
    expect(destination.searchParams.get("returnUrl")).toBe(
      "https://noorpath.test/account",
    );
  });
});
