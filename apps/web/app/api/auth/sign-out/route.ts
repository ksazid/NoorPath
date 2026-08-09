import { NextRequest, NextResponse } from "next/server";
import { isAuth0Configured } from "../../../../lib/auth0";

export function GET(request: NextRequest) {
  if (!isAuth0Configured()) return NextResponse.redirect(new URL("/", request.url), 303);

  const destination = new URL("/auth/logout", request.nextUrl.origin);
  destination.searchParams.set("returnTo", new URL("/", request.nextUrl.origin).toString());
  return NextResponse.redirect(destination, 303);
}
