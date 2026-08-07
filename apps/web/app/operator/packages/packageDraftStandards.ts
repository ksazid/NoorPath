export const STANDARD_PACKAGE_INCLUSIONS = [
  "Return flights",
  "Visa included",
  "Makkah accommodation",
  "Madinah accommodation",
  "Breakfast, lunch and dinner",
  "Intercity travel",
  "Ziyarat transport",
  "Umrah guidance",
] as const;

export const STANDARD_PACKAGE_EXCLUSIONS = [
  "Personal expenses",
  "Optional excursions",
  "Travel insurance unless stated",
] as const;

const LEGACY_TERMS: Record<string, string> = {
  "visa assistance": "Visa included",
  visa: "Visa included",
  meals: "Breakfast, lunch and dinner",
  "full board meals": "Breakfast, lunch and dinner",
  "intercity transport": "Intercity travel",
};

export function normalizePackageItems(items: string[] | undefined) {
  const normalized = (items ?? [])
    .map((item) => item.trim())
    .filter(Boolean)
    .map((item) => LEGACY_TERMS[item.toLowerCase()] ?? item);

  return [
    ...new Map(
      normalized.map((item) => [item.toLowerCase(), item]),
    ).values(),
  ];
}

export function calculateJourneyDuration(
  departureDate: string,
  returnDate: string,
) {
  if (!departureDate || !returnDate) return null;
  const start = Date.parse(`${departureDate}T00:00:00Z`);
  const end = Date.parse(`${returnDate}T00:00:00Z`);
  if (
    !Number.isFinite(start) ||
    !Number.isFinite(end) ||
    end <= start
  )
    return null;

  const nights = Math.round((end - start) / 86_400_000);
  return { days: nights + 1, nights };
}

export function suggestPackageTitle(
  origin: string,
  departureDate: string,
  returnDate: string,
) {
  const duration = calculateJourneyDuration(departureDate, returnDate);
  if (!duration) return "Umrah package";
  const place = origin.trim() || "India";
  return `${duration.days} Days / ${duration.nights} Nights Umrah from ${place}`;
}
