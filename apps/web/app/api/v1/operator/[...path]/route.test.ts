import { NextRequest } from "next/server";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getAuth0Client } from "../../../../../lib/auth0";
import { GET, POST } from "./route";

vi.mock("../../../../../lib/auth0", () => ({
  getAuth0Client: vi.fn(),
}));

const mockedGetAuth0Client = vi.mocked(getAuth0Client);
const originalApiOrigin = process.env.NOORPATH_API_URL;

beforeEach(() => {
  process.env.NOORPATH_API_URL = "https://api.noorpath.test";
  mockedGetAuth0Client.mockReturnValue({
    getAccessToken: vi.fn().mockResolvedValue({ token: "operator-token" }),
  } as never);
});

afterEach(() => {
  if (originalApiOrigin === undefined) delete process.env.NOORPATH_API_URL;
  else process.env.NOORPATH_API_URL = originalApiOrigin;
  vi.clearAllMocks();
  vi.unstubAllGlobals();
});

describe("operator API proxy", () => {
  it("forwards nested paths, query parameters, and the Auth0 bearer token", async () => {
    const apiFetch = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ items: [] }), {
        status: 200,
        headers: { "content-type": "application/json" },
      }),
    );
    vi.stubGlobal("fetch", apiFetch);

    const response = await GET(
      new NextRequest(
        "https://noorpath.test/api/v1/operator/visa/case-1?include=history",
      ),
      { params: Promise.resolve({ path: ["visa", "case-1"] }) },
    );

    expect(response.status).toBe(200);
    const [target, init] = apiFetch.mock.calls[0];
    expect(String(target)).toBe(
      "https://api.noorpath.test/api/v1/operator/visa/case-1?include=history",
    );
    expect(init).toMatchObject({ method: "GET", cache: "no-store" });
    expect((init.headers as Headers).get("authorization")).toBe(
      "Bearer operator-token",
    );
  });

  it("forwards JSON request bodies for operator actions", async () => {
    const apiFetch = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ status: "Submitted" }), {
        status: 200,
        headers: { "content-type": "application/json" },
      }),
    );
    vi.stubGlobal("fetch", apiFetch);

    const request = new NextRequest(
      "https://noorpath.test/api/v1/operator/visa/case-1/transitions",
      {
        method: "POST",
        headers: { "content-type": "application/json" },
        body: JSON.stringify({ status: "Submitted", version: 1 }),
      },
    );
    const response = await POST(request, {
      params: Promise.resolve({
        path: ["visa", "case-1", "transitions"],
      }),
    });

    expect(response.status).toBe(200);
    const [, init] = apiFetch.mock.calls[0];
    expect(init).toMatchObject({ method: "POST" });
    expect((init.headers as Headers).get("content-type")).toBe(
      "application/json",
    );
    expect(new TextDecoder().decode(init.body as ArrayBuffer)).toContain(
      '"status":"Submitted"',
    );
  });
});
