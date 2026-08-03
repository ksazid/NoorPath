import { NextRequest, NextResponse } from "next/server";

const apiBase = process.env.NOORPATH_API_URL ?? "http://localhost:5000";

function forwardedHeaders(request: NextRequest, json = false) {
  const headers = new Headers();
  for (const name of [
    "authorization",
    "cookie",
    "x-noorpath-test-identity",
    "x-correlation-id",
  ]) {
    const value = request.headers.get(name);
    if (value) headers.set(name, value);
  }
  if (json) headers.set("content-type", "application/json");
  return headers;
}

function withIdAlias(body: unknown) {
  if (!body || typeof body !== "object") return body;
  const value = body as { items?: unknown[]; travellerId?: string };
  if (Array.isArray(value.items)) {
    return {
      ...value,
      items: value.items.map((item) => {
        if (!item || typeof item !== "object") return item;
        const traveller = item as { travellerId?: string };
        return { ...traveller, id: traveller.travellerId };
      }),
    };
  }
  return { ...value, id: value.travellerId };
}

async function proxy(request: NextRequest, method: "GET" | "POST") {
  const response = await fetch(`${apiBase}/api/v1/travellers`, {
    method,
    cache: "no-store",
    headers: forwardedHeaders(request, method === "POST"),
    body: method === "POST" ? await request.text() : undefined,
  });
  const contentType = response.headers.get("content-type") ?? "";
  if (!contentType.includes("application/json")) {
    return new NextResponse(await response.text(), { status: response.status });
  }
  const body = await response.json();
  return NextResponse.json(response.ok ? withIdAlias(body) : body, {
    status: response.status,
  });
}

export async function GET(request: NextRequest) {
  return proxy(request, "GET");
}

export async function POST(request: NextRequest) {
  return proxy(request, "POST");
}
