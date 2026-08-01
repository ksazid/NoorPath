import { NextRequest } from "next/server";
import { releasePreviewHold } from "../../../preview-hold";

export function POST(request: NextRequest) {
  const segments = new URL(request.url).pathname.split("/").filter(Boolean);
  const holdId = decodeURIComponent(segments.at(-2) ?? "");
  return releasePreviewHold(request, holdId);
}
