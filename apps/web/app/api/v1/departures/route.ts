import { NextResponse } from "next/server";
import { previewDiscoveryItems } from "./preview-fixture";

export function GET() {
  return NextResponse.json({ items: previewDiscoveryItems });
}
