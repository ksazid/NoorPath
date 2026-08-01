import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { NextRequest } from "next/server";
import { GET } from "./route";

const names = [
  "AUTH0_DOMAIN",
  "AUTH0_CLIENT_ID",
  "AUTH0_CLIENT_SECRET",
  "AUTH0_SECRET",
] as const;
const original = Object.fromEntries(
  names.map((name) => [name, process.env[name]]),
);

beforeEach(() => {
  process.env.AUTH0_DOMAIN = "tenant.example.test";
  process.env.AUTH0_CLIENT_ID = "client";
  process.env.AUTH0_CLIENT_SECRET = "secret";
  process.env.AUTH0_SECRET = "0".repeat(64);
});

afterEach(() => {
  for (const name of names) {
    const value = original[name];
    if (value === undefined) delete process.env[name];
    else process.env[name] = value;
  }
});

describe("Auth0 sign in", () => {
  it("redirects Google through Universal Login", () => {
    const response = GET(
      new NextRequest(
        "https://noorpath.test/api/auth/sign-in?method=google&returnUrl=%2Foperator",
      ),
    );
    expect(response.status).toBe(303);
    const destination = new URL(response.headers.get("location")!);
    expect(destination.pathname).toBe("/auth/login");
    expect(destination.searchParams.get("connection")).toBe("google-oauth2");
    expect(destination.searchParams.get("returnTo")).toBe("/operator");
  });

  it("rejects an external return URL", () => {
    const response = GET(
      new NextRequest(
        "https://noorpath.test/api/auth/sign-in?method=google&returnUrl=https%3A%2F%2Fevil.test",
      ),
    );
    const destination = new URL(response.headers.get("location")!);
    expect(destination.searchParams.get("returnTo")).toBe("/account");
  });

  it("fails safely while phone OTP is not configured", async () => {
    const response = GET(
      new NextRequest(
        "https://noorpath.test/api/auth/sign-in?method=phone&returnUrl=%2Faccount",
      ),
    );
    expect(response.status).toBe(503);
    await expect(response.json()).resolves.toMatchObject({
      code: "authentication_method_unavailable",
    });
  });
});
