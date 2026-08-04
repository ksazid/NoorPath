import { NextResponse } from "next/server";
import { getAuth0Client } from "./lib/auth0";

export async function proxy(request: Request) {
  const auth0 = getAuth0Client();
  return auth0 ? auth0.middleware(request) : NextResponse.next();
}

export const config = {
  matcher: [
    "/((?!_next/static|_next/image|favicon.ico|sitemap.xml|robots.txt).*)",
  ],
};
