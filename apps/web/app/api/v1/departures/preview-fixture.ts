const stay = (
  hotelName: string,
  nights: number,
  confirmationState: "confirmed" | "pending",
) => ({
  hotelName,
  classification: "4 star",
  distanceDisclosure: "Walking-distance disclosure for preview review",
  nights,
  confirmationState,
});

export const previewDepartures = [
  {
    departureId: "3c9d522a-9481-4b79-9486-64cf997bfe31",
    operator: {
      id: "demo-delhi",
      displayName: "NoorPath Preview Operator — Delhi (Demo)",
    },
    packageName: "August Umrah Journey — Delhi",
    summary:
      "A future departure for reviewing NoorPath's plan-ahead and authoritative quote experience.",
    origin: "Delhi (DEL)",
    departureDate: "2027-08-14",
    returnDate: "2027-08-26",
    durationNights: 12,
    makkah: stay("Makkah Preview Hotel", 7, "confirmed"),
    madinah: stay("Madinah Preview Hotel", 5, "confirmed"),
    travel: {
      routeSummary: "Delhi → Jeddah → Makkah → Madinah → Delhi",
      details: "Final carrier and timing remain pending for this preview journey.",
      confirmationState: "pending" as const,
    },
    inclusions: ["Return flights", "Breakfast", "Journey support"],
    exclusions: ["Personal expenses"],
    pricing: {
      currency: "INR",
      occupancies: [
        {
          occupancy: "double" as const,
          amount: 110000,
          availableQuantity: 10,
          status: "available" as const,
        },
        {
          occupancy: "triple" as const,
          amount: 100000,
          availableQuantity: 8,
          status: "available" as const,
        },
        {
          occupancy: "quad" as const,
          amount: 90000,
          availableQuantity: 6,
          status: "available" as const,
        },
      ],
    },
  },
  {
    departureId: "6a4db6c9-56b8-4be8-9e54-f86635782d74",
    operator: {
      id: "demo-lucknow",
      displayName: "NoorPath Preview Operator — Lucknow (Demo)",
    },
    packageName: "September Umrah Journey — Lucknow",
    summary:
      "A future departure with a different origin and confirmed journey facts.",
    origin: "Lucknow (LKO)",
    departureDate: "2027-09-04",
    returnDate: "2027-09-16",
    durationNights: 12,
    makkah: stay("Safwah Preview Suites", 7, "confirmed"),
    madinah: stay("Taiba Preview Residence", 5, "confirmed"),
    travel: {
      routeSummary: "Lucknow → Jeddah → Makkah → Madinah → Lucknow",
      details: "Preview routing is confirmed for customer-experience verification.",
      confirmationState: "confirmed" as const,
    },
    inclusions: ["Return flights", "Breakfast & dinner", "Journey support"],
    exclusions: ["Personal expenses"],
    pricing: {
      currency: "INR",
      occupancies: [
        {
          occupancy: "double" as const,
          amount: 124000,
          availableQuantity: 4,
          status: "available" as const,
        },
        {
          occupancy: "triple" as const,
          amount: 108000,
          availableQuantity: 7,
          status: "available" as const,
        },
        {
          occupancy: "quad" as const,
          amount: 96000,
          availableQuantity: 8,
          status: "available" as const,
        },
      ],
    },
  },
  {
    departureId: "a48ec65d-0234-4ae3-bcad-3be17115691e",
    operator: {
      id: "demo-mumbai",
      displayName: "NoorPath Preview Operator — Mumbai (Demo)",
    },
    packageName: "October Umrah Journey — Mumbai",
    summary:
      "A future departure with a mix of confirmed and pending journey facts.",
    origin: "Mumbai (BOM)",
    departureDate: "2027-10-09",
    returnDate: "2027-10-22",
    durationNights: 13,
    makkah: stay("Haram View Preview Hotel", 7, "confirmed"),
    madinah: stay("Madinah Central Preview Hotel", 6, "pending"),
    travel: {
      routeSummary: "Mumbai → Jeddah → Makkah → Madinah → Mumbai",
      details: "Return carrier timing remains pending for this preview journey.",
      confirmationState: "pending" as const,
    },
    inclusions: ["Return flights", "Breakfast", "Airport transfers"],
    exclusions: ["Personal expenses"],
    pricing: {
      currency: "INR",
      occupancies: [
        {
          occupancy: "double" as const,
          amount: 136000,
          availableQuantity: 2,
          status: "available" as const,
        },
        {
          occupancy: "triple" as const,
          amount: 118000,
          availableQuantity: 5,
          status: "available" as const,
        },
        {
          occupancy: "quad" as const,
          amount: 102000,
          availableQuantity: 3,
          status: "available" as const,
        },
      ],
    },
  },
] as const;

export const previewDiscoveryItems = previewDepartures.map((departure) => {
  const headline = departure.pricing.occupancies.reduce((lowest, item) =>
    item.amount < lowest.amount ? item : lowest,
  );

  return {
    departureId: departure.departureId,
    operator: departure.operator,
    packageName: departure.packageName,
    summary: departure.summary,
    origin: departure.origin,
    departureDate: departure.departureDate,
    returnDate: departure.returnDate,
    durationNights: departure.durationNights,
    makkah: departure.makkah,
    madinah: departure.madinah,
    travelConfirmationState: departure.travel.confirmationState,
    inclusionHighlights: departure.inclusions,
    headlinePrice: {
      amount: headline.amount,
      currency: departure.pricing.currency,
      occupancy: headline.occupancy,
    },
    availability: {
      status: "available" as const,
      occupancies: departure.pricing.occupancies.map((item) => ({
        occupancy: item.occupancy,
        availableQuantity: item.availableQuantity,
      })),
    },
  };
});
