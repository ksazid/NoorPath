import { randomUUID } from "node:crypto";
import { NextRequest, NextResponse } from "next/server";

const previewTravellers = [
  {
    travellerId: "10000000-0000-4000-8000-000000000001",
    fullName: "Amina Rahman",
    dateOfBirth: "1992-05-17",
  },
  {
    travellerId: "10000000-0000-4000-8000-000000000002",
    fullName: "Yusuf Rahman",
    dateOfBirth: "1989-11-02",
  },
  {
    travellerId: "10000000-0000-4000-8000-000000000003",
    fullName: "Sara Rahman",
    dateOfBirth: "1996-03-24",
  },
  {
    travellerId: "10000000-0000-4000-8000-000000000004",
    fullName: "Imran Rahman",
    dateOfBirth: "1991-08-09",
  },
] as const;

export function GET() {
  return NextResponse.json({ items: previewTravellers });
}

export async function POST(request: NextRequest) {
  const body = (await request.json()) as {
    fullName?: string;
    dateOfBirth?: string;
  };
  const fullName = body.fullName?.trim().replace(/\s+/g, " ") ?? "";
  const dateOfBirth = body.dateOfBirth ?? "";

  if (fullName.length < 2 || !/^\d{4}-\d{2}-\d{2}$/.test(dateOfBirth)) {
    return NextResponse.json(
      {
        title: "Review traveller details",
        errors: {
          ...(fullName.length < 2
            ? { fullName: ["Full name must be between 2 and 120 characters."] }
            : {}),
          ...(!/^\d{4}-\d{2}-\d{2}$/.test(dateOfBirth)
            ? { dateOfBirth: ["Enter a valid date of birth."] }
            : {}),
        },
      },
      { status: 422 },
    );
  }

  return NextResponse.json(
    { travellerId: randomUUID(), fullName, dateOfBirth },
    { status: 201 },
  );
}
