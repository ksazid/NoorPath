import { NextRequest, NextResponse } from "next/server";
import { getAuth0Client } from "../../../../lib/auth0";

type Context = { params: Promise<{ path: string[] }> };

const forwardedRequestHeaders = [
  "accept",
  "content-type",
  "if-match",
  "if-none-match",
  "idempotency-key",
] as const;

const forwardedResponseHeaders = [
  "cache-control",
  "content-type",
  "etag",
  "location",
  "x-correlation-id",
] as const;

function apiOrigin() {
  const configured = process.env.NOORPATH_API_URL ?? "http://localhost:5000";
  const url = new URL(configured);
  if (
    !["http:", "https:"].includes(url.protocol) ||
    url.username ||
    url.password
  )
    throw new Error("NOORPATH_API_URL must be an HTTP(S) origin.");
  return url.origin;
}

async function forward(request: NextRequest, context: Context) {
  const { path } = await context.params;
  if (
    path.length === 0 ||
    path.some(
      (segment) =>
        !segment ||
        segment === "." ||
        segment === ".." ||
        !/^[A-Za-z0-9._~-]+$/.test(segment),
    )
  )
    return NextResponse.json({ code: "invalid_api_path" }, { status: 400 });

  const destination = new URL(
    `/api/v1/${path.map(encodeURIComponent).join("/")}`,
    apiOrigin(),
  );
  destination.search = request.nextUrl.search;

  const headers = new Headers();
  for (const name of forwardedRequestHeaders) {
    const value = request.headers.get(name);
    if (value) headers.set(name, value);
  }

  const auth0 = getAuth0Client();
  if (auth0 && (await auth0.getSession())) {
    try {
      const { token } = await auth0.getAccessToken({
        audience: process.env.AUTH0_AUDIENCE,
      });
      headers.set("authorization", `Bearer ${token}`);
    } catch {
      return NextResponse.json(
        { code: "authentication_required" },
        { status: 401 },
      );
    }
  }

  const upstream = await fetch(destination, {
    method: request.method,
    headers,
    body:
      request.method === "GET" || request.method === "HEAD"
        ? undefined
        : await request.arrayBuffer(),
    cache: "no-store",
    redirect: "manual",
  });

  const responseHeaders = new Headers();
  for (const name of forwardedResponseHeaders) {
    const value = upstream.headers.get(name);
    if (value) responseHeaders.set(name, value);
  }
  return new NextResponse(upstream.body, {
    status: upstream.status,
    headers: responseHeaders,
  });
}

export const GET = forward;
export const POST = forward;
export const PUT = forward;
export const PATCH = forward;
export const DELETE = forward;
