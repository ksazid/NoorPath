import { NextResponse } from "next/server";
import { getAuth0Client } from "../../../lib/auth0";

async function handleAuth(request: Request) {
  const auth0 = getAuth0Client();
  if (!auth0) {
    return NextResponse.json(
      {
        code: "authentication_unavailable",
        message: "Secure sign-in is not configured for this environment.",
      },
      { status: 503 },
    );
  }

  return auth0.middleware(request);
}

export const GET = handleAuth;
export const POST = handleAuth;
