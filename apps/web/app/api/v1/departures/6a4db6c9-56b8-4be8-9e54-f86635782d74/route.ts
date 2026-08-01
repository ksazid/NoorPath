import { NextResponse } from "next/server";
import { previewDepartures } from "../preview-fixture";

export function GET() {
  return NextResponse.json(previewDepartures[1]);
}
