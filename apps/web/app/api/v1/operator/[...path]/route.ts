import { NextRequest, NextResponse } from "next/server";
import { getAuth0Client } from "../../../../../lib/auth0";

type RouteContext = {
  params: Promise<{ path: string[] }>;
};

const forwardedRequestHeaders = [
  "accept",
  "content-type",
  "idempotency-key",
  "if-match",
] as const;

const forwardedResponseHeaders = [
  "content-type",
  "location",
  "etag",
  "retry-after",
] as const;

async function forward(request: NextRequest, context: RouteContext) {
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

    const { path } = await context.params;
    const target = new URL(
      `${apiOrigin.replace(/\/$/, "")}/api/v1/operator/${path
        .map((segment) => encodeURIComponent(segment))
        .join("/")}`,
    );
    target.search = request.nextUrl.search;

    const headers = new Headers({ Authorization: `Bearer ${accessToken}` });
    for (const name of forwardedRequestHeaders) {
      const value = request.headers.get(name);
      if (value) headers.set(name, value);
    }

    const body =
      request.method === "GET" || request.method === "HEAD"
        ? undefined
        : await request.arrayBuffer();

    const response = await fetch(target, {
      method: request.method,
      headers,
      body,
      cache: "no-store",
      redirect: "manual",
    });

    const responseHeaders = new Headers({
      "cache-control": "no-store",
      vary: "Cookie",
    });
    for (const name of forwardedResponseHeaders) {
      const value = response.headers.get(name);
      if (value) responseHeaders.set(name, value);
    }

    return new NextResponse(response.body, {
      status: response.status,
      headers: responseHeaders,
    });
  } catch {
    return NextResponse.json(
      { code: "not_authenticated", message: "Sign in required." },
      { status: 401 },
    );
  }
}

export const dynamic = "force-dynamic";

export function GET(request: NextRequest, context: RouteContext) {
  return forward(request, context);
}

export function POST(request: NextRequest, context: RouteContext) {
  return forward(request, context);
}

export function PUT(request: NextRequest, context: RouteContext) {
  return forward(request, context);
}

export function PATCH(request: NextRequest, context: RouteContext) {
  return forward(request, context);
}

export function DELETE(request: NextRequest, context: RouteContext) {
  return forward(request, context);
}
