import { NextRequest } from "next/server";
import { getPreviewHold } from "../../preview-hold";

export function GET(request: NextRequest) {
  const segments = new URL(request.url).pathname.split("/").filter(Boolean);
  const holdId = decodeURIComponent(segments.at(-1) ?? "");
  return getPreviewHold(request, holdId);
}
