import { NextResponse } from "next/server";
import { getAuth0Client } from "../../../../../lib/auth0";

export async function GET() {
  const auth0 = getAuth0Client();
  const apiOrigin = process.env.NOORPATH_API_URL;

  if (!auth0 || !apiOrigin) {
    return NextResponse.json(
      {
        code: "authentication_unavailable",
        message: "Secure sign-in is not configured for this environment.",
      },
      { status: 503 },
    );
  }

  try {
    const { token: accessToken } = await auth0.getAccessToken();
    if (!accessToken) {
      return NextResponse.json(
        { code: "not_authenticated", message: "Sign in required." },
        { status: 401 },
      );
    }

    const response = await fetch(
      apiOrigin.replace(/\/$/, "") + "/api/v1/platform/access",
      {
        headers: { Authorization: "Bearer " + accessToken },
        cache: "no-store",
      },
    );

    return new NextResponse(response.body, {
      status: response.status,
      headers: {
        "content-type":
          response.headers.get("content-type") ?? "application/json",
        "cache-control": "no-store",
        vary: "Cookie",
      },
    });
  } catch {
    return NextResponse.json(
      { code: "not_authenticated", message: "Sign in required." },
      { status: 401 },
    );
  }
}
