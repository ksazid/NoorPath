import { createHash } from "node:crypto";
import { NextRequest, NextResponse } from "next/server";

export type PreviewOccupancy = "double" | "triple" | "quad";

type PreviewQuoteCookie = {
  quoteId: string;
  departureId: string;
  occupancy: PreviewOccupancy;
  createdAtUtc: string;
  expiresAtUtc: string;
};

type PreviewHoldToken = {
  quoteId: string;
  departureId: string;
  occupancy: PreviewOccupancy;
  quantity: 1;
  createdAtUtc: string;
  expiresAtUtc: string;
};

type PreviewHoldStatus = "active" | "released" | "expired";

type PreviewHold = PreviewHoldToken & {
  holdId: string;
  status: PreviewHoldStatus;
  terminalAtUtc: string | null;
  availabilityReserved: boolean;
};

const quoteCookie = "np-preview-quote";
const holdCookiePrefix = "np-preview-hold-";
const releaseCookiePrefix = "np-preview-released-";

export function writePreviewQuoteCookie(
  response: NextResponse,
  quote: PreviewQuoteCookie,
) {
  response.cookies.set({
    name: quoteCookie,
    value: encode(quote),
    httpOnly: true,
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production",
    path: "/",
    maxAge: 30 * 60,
  });
}

export function createPreviewHold(request: NextRequest, quoteId: string) {
  const idempotencyKey = request.headers.get("Idempotency-Key")?.trim() ?? "";
  if (
    idempotencyKey.length < 8 ||
    idempotencyKey.length > 100 ||
    !/^[\x20-\x7E]+$/.test(idempotencyKey)
  ) {
    return NextResponse.json(
      {
        title: "Review the hold request",
        detail: "Provide an Idempotency-Key containing 8 to 100 ASCII characters.",
        code: "invalid_idempotency_key",
      },
      { status: 400 },
    );
  }

  const quote = readCookie<PreviewQuoteCookie>(request, quoteCookie);
  if (!quote || quote.quoteId !== quoteId) {
    return NextResponse.json(
      {
        title: "Quote not found",
        detail: "Create a fresh preview quote before securing availability.",
        code: "quote_not_found",
      },
      { status: 404 },
    );
  }

  const now = new Date();
  const quoteExpiry = new Date(quote.expiresAtUtc);
  if (now.getTime() >= quoteExpiry.getTime()) {
    return NextResponse.json(
      {
        title: "Quote expired",
        detail: "Create a fresh quote before securing availability.",
        code: "quote_expired",
      },
      { status: 410 },
    );
  }

  const keyHash = hash(idempotencyKey);
  const existing = readCookie<PreviewHold>(
    request,
    `${holdCookiePrefix}${keyHash.slice(0, 24)}`,
  );
  if (existing) {
    if (existing.quoteId !== quoteId) {
      return NextResponse.json(
        {
          title: "Idempotency key conflict",
          detail: "Use a new idempotency key for a different quote.",
          code: "idempotency_conflict",
        },
        { status: 409 },
      );
    }

    return NextResponse.json(effectiveHold(request, existing));
  }

  const expiresAt = new Date(
    Math.min(now.getTime() + 15 * 60 * 1000, quoteExpiry.getTime()),
  );
  const token: PreviewHoldToken = {
    quoteId,
    departureId: quote.departureId,
    occupancy: quote.occupancy,
    quantity: 1,
    createdAtUtc: now.toISOString(),
    expiresAtUtc: expiresAt.toISOString(),
  };
  const hold: PreviewHold = {
    ...token,
    holdId: `ph_${encode(token)}`,
    status: "active",
    terminalAtUtc: null,
    availabilityReserved: true,
  };

  const response = NextResponse.json(hold, { status: 201 });
  response.cookies.set({
    name: `${holdCookiePrefix}${keyHash.slice(0, 24)}`,
    value: encode(hold),
    httpOnly: true,
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production",
    path: "/",
    maxAge: 60 * 60,
  });
  return response;
}

export function getPreviewHold(request: NextRequest, holdId: string) {
  const hold = decodeHoldId(holdId);
  if (!hold) return holdNotFound();
  return NextResponse.json(effectiveHold(request, hold));
}

export function releasePreviewHold(request: NextRequest, holdId: string) {
  const hold = decodeHoldId(holdId);
  if (!hold) return holdNotFound();

  const current = effectiveHold(request, hold);
  if (current.status !== "active") return NextResponse.json(current);

  const terminalAtUtc = new Date().toISOString();
  const released: PreviewHold = {
    ...current,
    status: "released",
    terminalAtUtc,
    availabilityReserved: false,
  };
  const response = NextResponse.json(released);
  response.cookies.set({
    name: releaseCookieName(holdId),
    value: terminalAtUtc,
    httpOnly: true,
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production",
    path: "/",
    maxAge: 60 * 60,
  });
  return response;
}

function effectiveHold(request: NextRequest, hold: PreviewHold): PreviewHold {
  const releasedAt = request.cookies.get(releaseCookieName(hold.holdId))?.value;
  if (releasedAt) {
    return {
      ...hold,
      status: "released",
      terminalAtUtc: releasedAt,
      availabilityReserved: false,
    };
  }

  if (Date.now() >= new Date(hold.expiresAtUtc).getTime()) {
    return {
      ...hold,
      status: "expired",
      terminalAtUtc: hold.expiresAtUtc,
      availabilityReserved: false,
    };
  }

  return {
    ...hold,
    status: "active",
    terminalAtUtc: null,
    availabilityReserved: true,
  };
}

function decodeHoldId(holdId: string): PreviewHold | null {
  if (!holdId.startsWith("ph_")) return null;
  const token = decode<PreviewHoldToken>(holdId.slice(3));
  if (!token) return null;
  return {
    ...token,
    holdId,
    status: "active",
    terminalAtUtc: null,
    availabilityReserved: true,
  };
}

function holdNotFound() {
  return NextResponse.json(
    {
      title: "Availability hold not found",
      detail: "This preview hold could not be recovered.",
      code: "hold_not_found",
    },
    { status: 404 },
  );
}

function readCookie<T>(request: NextRequest, name: string): T | null {
  const value = request.cookies.get(name)?.value;
  return value ? decode<T>(value) : null;
}

function releaseCookieName(holdId: string) {
  return `${releaseCookiePrefix}${hash(holdId).slice(0, 24)}`;
}

function hash(value: string) {
  return createHash("sha256").update(value).digest("hex");
}

function encode(value: unknown) {
  return Buffer.from(JSON.stringify(value), "utf8").toString("base64url");
}

function decode<T>(value: string): T | null {
  try {
    return JSON.parse(Buffer.from(value, "base64url").toString("utf8")) as T;
  } catch {
    return null;
  }
}
