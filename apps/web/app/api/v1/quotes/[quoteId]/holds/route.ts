import { NextRequest } from "next/server";
import { createPreviewHold } from "../../../preview-hold";

export function POST(request: NextRequest) {
  const segments = new URL(request.url).pathname.split("/").filter(Boolean);
  const quoteId = decodeURIComponent(segments.at(-2) ?? "");
  return createPreviewHold(request, quoteId);
}
