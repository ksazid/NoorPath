import { NextRequest, NextResponse } from "next/server";
import { isAuth0Configured } from "../../../../lib/auth0";

function safeReturnPath(value: string | null) {
  return value?.startsWith("/") && !value.startsWith("//")
    ? value
    : "/account";
}

export function GET(request: NextRequest) {
  const method = request.nextUrl.searchParams.get("method");
  if (method === "phone")
    return NextResponse.json(
      {
        code: "authentication_method_unavailable",
        message: "Phone OTP is not configured yet. Continue with Google.",
      },
      { status: 503 },
    );
  if (method !== "google")
    return NextResponse.json(
      { code: "invalid_sign_in_method", message: "Continue with Google." },
      { status: 400 },
    );
  if (!isAuth0Configured())
    return NextResponse.json(
      {
        code: "authentication_unavailable",
        message: "Secure sign-in is not configured for this environment.",
      },
      { status: 503 },
    );

  const destination = new URL("/auth/login", request.nextUrl.origin);
  destination.searchParams.set("connection", "google-oauth2");
  destination.searchParams.set(
    "returnTo",
    safeReturnPath(request.nextUrl.searchParams.get("returnUrl")),
  );
  return NextResponse.redirect(destination, 303);
}
