export type FactConfirmationState = "confirmed" | "pending";

export type StayPreview = {
  hotelName: string;
  classification: string;
  distanceDisclosure: string;
  nights: number;
  confirmationState: FactConfirmationState;
};

export type TravelPreview = {
  routeSummary: string;
  details: string;
  confirmationState: FactConfirmationState;
};

export type PublicPackagePreview = {
  departureId: string;
  operatorName: string;
  packageName: string;
  summary: string;
  origin: string;
  departureDate: string;
  returnDate: string;
  durationNights: number;
  makkah: StayPreview;
  madinah: StayPreview;
  travel: TravelPreview;
  inclusions: readonly string[];
  exclusions: readonly string[];
  image: string;
};

export const publicPackagePreviews: readonly PublicPackagePreview[] = [
  {
    departureId: "3c9d522a-9481-4b79-9486-64cf997bfe31",
    operatorName: "Noor Tours",
    packageName: "Noor Harmony · 12 Nights",
    summary:
      "A calm Umrah journey with clearly disclosed stays in Makkah and Madinah, guided transfers, and travel facts kept explicit.",
    origin: "Delhi (DEL)",
    departureDate: "18 Sep 2026",
    returnDate: "30 Sep 2026",
    durationNights: 12,
    makkah: {
      hotelName: "Swissôtel Makkah",
      classification: "5 star",
      distanceDisclosure: "Approximately 300 m from Masjid al-Haram",
      nights: 7,
      confirmationState: "confirmed",
    },
    madinah: {
      hotelName: "Pullman Zamzam Madina",
      classification: "5 star",
      distanceDisclosure: "Approximately 250 m from Al-Masjid an-Nabawi",
      nights: 5,
      confirmationState: "confirmed",
    },
    travel: {
      routeSummary: "Delhi → Jeddah → Makkah → Madinah → Delhi",
      details:
        "Return air travel and intercity ground transfers are included in this preview. Final flight numbers remain subject to operator confirmation.",
      confirmationState: "pending",
    },
    inclusions: [
      "Return air travel",
      "Makkah and Madinah accommodation",
      "Intercity ground transfers",
      "Visa support",
      "Human journey support",
    ],
    exclusions: ["Personal expenses", "Optional excursions not listed above"],
    image: "/assets/kaaba-reference.svg",
  },
  {
    departureId: "cf0c2d15-5fbb-45ee-b745-20bb3578f76a",
    operatorName: "Noor Tours",
    packageName: "Haramain Comfort · 10 Nights",
    summary:
      "A focused ten-night journey balancing time in both holy cities with simple, explicit accommodation and transfer information.",
    origin: "Mumbai (BOM)",
    departureDate: "09 Oct 2026",
    returnDate: "19 Oct 2026",
    durationNights: 10,
    makkah: {
      hotelName: "Makkah stay",
      classification: "Comfort category",
      distanceDisclosure:
        "Exact hotel and walking distance pending operator confirmation",
      nights: 6,
      confirmationState: "pending",
    },
    madinah: {
      hotelName: "Madinah stay",
      classification: "Comfort category",
      distanceDisclosure:
        "Exact hotel and walking distance pending operator confirmation",
      nights: 4,
      confirmationState: "pending",
    },
    travel: {
      routeSummary: "Mumbai → Jeddah → Makkah → Madinah → Mumbai",
      details:
        "The journey route is prepared; carrier and flight-number facts will be shown only after operator confirmation.",
      confirmationState: "pending",
    },
    inclusions: [
      "Return air travel",
      "Accommodation for 10 nights",
      "Ground transfers between journey stages",
      "Visa support",
    ],
    exclusions: ["Meals unless explicitly confirmed", "Personal expenses"],
    image: "/assets/madinah-reference.svg",
  },
  {
    departureId: "d823cb8a-3afe-41f4-83b7-40ca793012b5",
    operatorName: "Noor Tours",
    packageName: "Serene Umrah · 14 Nights",
    summary:
      "A longer fourteen-night itinerary for travellers who value more time in Makkah and Madinah without hiding unconfirmed journey facts.",
    origin: "Lucknow (LKO)",
    departureDate: "06 Nov 2026",
    returnDate: "20 Nov 2026",
    durationNights: 14,
    makkah: {
      hotelName: "Makkah stay",
      classification: "To be confirmed",
      distanceDisclosure: "Distance disclosure pending operator confirmation",
      nights: 8,
      confirmationState: "pending",
    },
    madinah: {
      hotelName: "Madinah stay",
      classification: "To be confirmed",
      distanceDisclosure: "Distance disclosure pending operator confirmation",
      nights: 6,
      confirmationState: "pending",
    },
    travel: {
      routeSummary: "Lucknow → Jeddah → Makkah → Madinah → Lucknow",
      details:
        "Travel routing is shown as a planning preview. Carrier, flight number, and final transfer timings are not yet confirmed.",
      confirmationState: "pending",
    },
    inclusions: [
      "Return air travel",
      "Accommodation for 14 nights",
      "Intercity transfers",
      "Visa support",
    ],
    exclusions: [
      "Personal expenses",
      "Items not explicitly listed as included",
    ],
    image: "/assets/kaaba-reference.svg",
  },
] as const;

export function findPublicPackagePreview(departureId: string) {
  return publicPackagePreviews.find(
    (packagePreview) => packagePreview.departureId === departureId,
  );
}
