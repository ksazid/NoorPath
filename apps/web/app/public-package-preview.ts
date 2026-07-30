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
  price: number;
  amountDueToday: number;
  seatsRemaining: number;
  capacity: number;
  badge: string;
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
      "A calm Umrah journey with clearly disclosed stays in Makkah and Madinah.",
    origin: "Delhi (DEL)",
    departureDate: "18 Sep 2026",
    returnDate: "30 Sep 2026",
    durationNights: 12,
    price: 185000,
    amountDueToday: 35000,
    seatsRemaining: 18,
    capacity: 24,
    badge: "Most popular",
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
        "Return air travel and intercity transfers are included. Final flight facts remain subject to operator confirmation.",
      confirmationState: "pending",
    },
    inclusions: [
      "Return air travel",
      "Makkah and Madinah accommodation",
      "Intercity ground transfers",
      "Visa support",
      "Human journey support",
    ],
    exclusions: ["Personal expenses", "Optional excursions"],
    image: "/assets/kaaba-reference.svg",
  },
  {
    departureId: "cf0c2d15-5fbb-45ee-b745-20bb3578f76a",
    operatorName: "Noor Tours",
    packageName: "Haramain Comfort · 10 Nights",
    summary: "A focused journey balancing time in both holy cities.",
    origin: "Mumbai (BOM)",
    departureDate: "09 Oct 2026",
    returnDate: "19 Oct 2026",
    durationNights: 10,
    price: 94500,
    amountDueToday: 18900,
    seatsRemaining: 18,
    capacity: 24,
    badge: "Best value",
    makkah: {
      hotelName: "Makkah stay",
      classification: "Comfort category",
      distanceDisclosure: "Exact hotel pending operator confirmation",
      nights: 6,
      confirmationState: "pending",
    },
    madinah: {
      hotelName: "Madinah stay",
      classification: "Comfort category",
      distanceDisclosure: "Exact hotel pending operator confirmation",
      nights: 4,
      confirmationState: "pending",
    },
    travel: {
      routeSummary: "Mumbai → Jeddah → Makkah → Madinah → Mumbai",
      details: "Carrier and flight facts will follow operator confirmation.",
      confirmationState: "pending",
    },
    inclusions: [
      "Return air travel",
      "Accommodation for 10 nights",
      "Ground transfers",
      "Visa support",
    ],
    exclusions: ["Meals unless confirmed", "Personal expenses"],
    image: "/assets/madinah-reference.svg",
  },
  {
    departureId: "d823cb8a-3afe-41f4-83b7-40ca793012b5",
    operatorName: "Noor Tours",
    packageName: "Serene Umrah · 14 Nights",
    summary: "A longer journey with more time in Makkah and Madinah.",
    origin: "Lucknow (LKO)",
    departureDate: "06 Nov 2026",
    returnDate: "20 Nov 2026",
    durationNights: 14,
    price: 128500,
    amountDueToday: 25700,
    seatsRemaining: 9,
    capacity: 24,
    badge: "Premium",
    makkah: {
      hotelName: "Makkah stay",
      classification: "To be confirmed",
      distanceDisclosure: "Distance pending operator confirmation",
      nights: 8,
      confirmationState: "pending",
    },
    madinah: {
      hotelName: "Madinah stay",
      classification: "To be confirmed",
      distanceDisclosure: "Distance pending operator confirmation",
      nights: 6,
      confirmationState: "pending",
    },
    travel: {
      routeSummary: "Lucknow → Jeddah → Makkah → Madinah → Lucknow",
      details: "Carrier and final transfer timings are not yet confirmed.",
      confirmationState: "pending",
    },
    inclusions: [
      "Return air travel",
      "Accommodation for 14 nights",
      "Intercity transfers",
      "Visa support",
    ],
    exclusions: ["Personal expenses", "Items not explicitly included"],
    image: "/assets/kaaba-reference.svg",
  },
] as const;

export function findPublicPackagePreview(departureId: string) {
  return publicPackagePreviews.find(
    (packagePreview) => packagePreview.departureId === departureId,
  );
}
