import { NextRequest } from "next/server";
import { createPreviewQuote } from "../../preview-quote";

export async function POST(request: NextRequest) {
  return createPreviewQuote(request, 1);
}
