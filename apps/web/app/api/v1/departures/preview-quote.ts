import { randomUUID } from "node:crypto";
import { NextRequest, NextResponse } from "next/server";
import { writePreviewQuoteCookie } from "../preview-hold";
import { previewDepartures } from "./preview-fixture";

type PreviewOccupancy = "double" | "triple" | "quad";

const requiredTravellers: Record<PreviewOccupancy, number> = {
  double: 2,
  triple: 3,
  quad: 4,
};

export async function createPreviewQuote(
  request: NextRequest,
  departureIndex: number,
) {
  const departure = previewDepartures[departureIndex];
  const body = (await request.json()) as {
    occupancy?: string;
    travellerIds?: string[];
  };
  const occupancy = body.occupancy as PreviewOccupancy | undefined;
  const travellerIds = body.travellerIds ?? [];

  if (!occupancy || !(occupancy in requiredTravellers)) {
    return NextResponse.json(
      {
        title: "Review your Umrah plan",
        errors: { occupancy: ["Choose double, triple or quad sharing."] },
      },
      { status: 422 },
    );
  }

  const required = requiredTravellers[occupancy];
  if (travellerIds.length !== required || new Set(travellerIds).size !== required) {
    return NextResponse.json(
      {
        title: "Review your Umrah plan",
        errors: {
          travellerIds: [
            `${occupancy} sharing requires exactly ${required} different travellers.`,
          ],
        },
      },
      { status: 422 },
    );
  }

  const price = departure.pricing.occupancies.find(
    (item) => item.occupancy === occupancy,
  );
  if (!price || price.status !== "available") {
    return NextResponse.json(
      {
        title: "Quote unavailable",
        detail: "This room-sharing option is not currently available.",
        code: "quote_unavailable",
      },
      { status: 409 },
    );
  }

  const quoteId = randomUUID();
  const createdAt = new Date();
  const expiresAt = new Date(createdAt.getTime() + 30 * 60 * 1000);
  const total = price.amount * required;
  const departureDate = new Date(`${departure.departureDate}T00:00:00Z`);
  const finalDueDate = new Date(departureDate.getTime() - 30 * 24 * 60 * 60 * 1000);

  let dueNow = roundMoney(total * 0.2);
  let remaining = roundMoney(total - dueNow);
  let instalments: Array<{ sequence: number; dueDate: string; amount: number }> = [];

  if (finalDueDate <= createdAt) {
    dueNow = total;
    remaining = 0;
  } else {
    const dueDates = buildDueDates(createdAt, finalDueDate, 5);
    const remainingCents = Math.round(remaining * 100);
    const regularCents = Math.floor(remainingCents / dueDates.length);
    let allocatedCents = 0;
    instalments = dueDates.map((dueDate, index) => {
      const cents =
        index === dueDates.length - 1
          ? remainingCents - allocatedCents
          : regularCents;
      allocatedCents += cents;
      return {
        sequence: index + 1,
        dueDate: dueDate.toISOString().slice(0, 10),
        amount: cents / 100,
      };
    });
  }

  const response = NextResponse.json(
    {
      quoteId,
      departureId: departure.departureId,
      priceVersionId: `20000000-0000-4000-8000-00000000000${departureIndex + 1}`,
      occupancy,
      travellerCount: required,
      currency: departure.pricing.currency,
      unitPrice: price.amount,
      total,
      dueNow,
      remaining,
      instalments,
      createdAtUtc: createdAt.toISOString(),
      expiresAtUtc: expiresAt.toISOString(),
      expired: false,
      availabilityReserved: false,
    },
    { status: 201 },
  );

  writePreviewQuoteCookie(response, {
    quoteId,
    departureId: departure.departureId,
    occupancy,
    createdAtUtc: createdAt.toISOString(),
    expiresAtUtc: expiresAt.toISOString(),
  });

  return response;
}

function buildDueDates(createdAt: Date, finalDueDate: Date, dayOfMonth: number) {
  const dates: Date[] = [];
  let year = createdAt.getUTCFullYear();
  let month = createdAt.getUTCMonth();

  while (true) {
    const candidate = new Date(Date.UTC(year, month, dayOfMonth));
    if (candidate > createdAt && candidate <= finalDueDate) dates.push(candidate);
    if (candidate >= finalDueDate) break;

    month += 1;
    if (month === 12) {
      month = 0;
      year += 1;
    }
  }

  if (
    dates.length === 0 ||
    dates[dates.length - 1].getTime() !== finalDueDate.getTime()
  ) {
    dates.push(finalDueDate);
  }

  return dates;
}

function roundMoney(value: number) {
  return Math.round((value + Number.EPSILON) * 100) / 100;
}
