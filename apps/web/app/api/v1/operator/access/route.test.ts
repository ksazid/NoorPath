import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getAuth0Client } from "../../../../../lib/auth0";
import { GET } from "./route";

vi.mock("../../../../../lib/auth0", () => ({
  getAuth0Client: vi.fn(),
}));

const mockedGetAuth0Client = vi.mocked(getAuth0Client);
const originalApiOrigin = process.env.NOORPATH_API_URL;

beforeEach(() => {
  process.env.NOORPATH_API_URL = "https://api.noorpath.test";
  vi.clearAllMocks();
});

afterEach(() => {
  if (originalApiOrigin === undefined) delete process.env.NOORPATH_API_URL;
  else process.env.NOORPATH_API_URL = originalApiOrigin;
  vi.unstubAllGlobals();
});

describe("operator access proxy", () => {
  it("forwards the Auth0 access token to the operator API", async () => {
    mockedGetAuth0Client.mockReturnValue({
      getAccessToken: vi
        .fn()
        .mockResolvedValue({ token: "operator-access-token" }),
    } as never);
    const apiFetch = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ code: "forbidden" }), {
        status: 403,
        headers: { "content-type": "application/problem+json" },
      }),
    );
    vi.stubGlobal("fetch", apiFetch);

    const response = await GET();

    expect(apiFetch).toHaveBeenCalledWith(
      "https://api.noorpath.test/api/v1/operator/access",
      {
        headers: { Authorization: "Bearer operator-access-token" },
        cache: "no-store",
      },
    );
    expect(response.status).toBe(403);
    expect(response.headers.get("cache-control")).toBe("no-store");
    await expect(response.json()).resolves.toMatchObject({
      code: "forbidden",
    });
  });

  it(
    "returns a safe unauthenticated response when the session is unavailable",
    async () => {
      mockedGetAuth0Client.mockReturnValue({
        getAccessToken: vi
          .fn()
          .mockRejectedValue(new Error("session unavailable")),
      } as never);

      const response = await GET();

      expect(response.status).toBe(401);
      await expect(response.json()).resolves.toMatchObject({
        code: "not_authenticated",
      });
    },
  );
});
