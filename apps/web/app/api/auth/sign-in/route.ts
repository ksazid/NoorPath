import { NextRequest, NextResponse } from "next/server";

const methods = new Set(["phone", "google"]);

export function GET(request: NextRequest) {
  const method = request.nextUrl.searchParams.get("method");
  const requestedReturnUrl = request.nextUrl.searchParams.get("returnUrl");
  const returnUrl =
    requestedReturnUrl?.startsWith("/") && !requestedReturnUrl.startsWith("//")
      ? requestedReturnUrl
      : "/account";

  if (!method || !methods.has(method)) {
    return NextResponse.json(
      {
        code: "invalid_sign_in_method",
        message: "Choose a supported sign-in method.",
      },
      { status: 400 },
    );
  }

  const endpoint = process.env.NOORPATH_AUTH_SIGN_IN_URL;
  if (!endpoint) {
    return NextResponse.json(
      {
        code: "authentication_unavailable",
        message: "Secure sign-in is not configured for this environment.",
      },
      { status: 503 },
    );
  }

  const destination = new URL(endpoint);
  destination.searchParams.set("method", method);
  destination.searchParams.set(
    "returnUrl",
    new URL(returnUrl, request.nextUrl.origin).toString(),
  );
  return NextResponse.redirect(destination, 303);
}
