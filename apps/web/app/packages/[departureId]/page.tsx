import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import {
  findPublicPackagePreview,
  publicPackagePreviews,
  type PublicPackagePreview,
} from "../../public-package-preview";
import { Icon, PublicHeader } from "../../public-ui";

type PackagePageProps = {
  params: Promise<{ departureId: string }>;
};

const itinerary = [
  [
    "Day 1",
    "mosque",
    "Arrival in Jeddah",
    "Airport assistance & transfer to Makkah hotel. Check-in.",
  ],
  [
    "Day 1–5",
    "building",
    "Makkah Stay",
    "5 nights in Makkah. Perform Umrah, worship & leisure.",
  ],
  ["Day 6", "bus", "Guided Ziyarah", "Makkah Ziyarah with certified guide."],
  [
    "Day 7",
    "users-three",
    "Umrah Orientation",
    "Umrah guidance session & rituals briefing.",
  ],
  [
    "Day 8",
    "train",
    "Makkah to Madinah",
    "Check-out & high-speed train transfer to Madinah.",
  ],
  [
    "Day 8–12",
    "mosque",
    "Madinah Stay",
    "5 nights in Madinah. Worship & leisure.",
  ],
  [
    "Day 13",
    "airplane-tilt",
    "Departure",
    "Check-out & transfer to airport for your return flight.",
  ],
] as const;

const included = [
  ["airplane-tilt", "Return flights (Economy)"],
  ["file-text", "Visa assistance"],
  ["building", "Makkah hotel"],
  ["mosque", "Madinah hotel"],
  ["fork-knife", "Meals as per plan"],
  ["bus", "Airport & intercity transport"],
  ["map-trifold", "Guided Ziyarah"],
  ["users-three", "Group leader & support"],
] as const;

const travelKit = [
  ["suitcase-rolling", "Luggage tag"],
  ["handbag", "Neck pouch / Document wallet"],
  ["identification-card", "ID card"],
  ["sim-card", "SIM / eSIM guidance"],
  ["notepad", "Emergency contact card"],
] as const;

const umrahKit = [
  ["shirt-folded", "Ihram for men / Prayer essentials"],
  ["handbag", "Drawstring bag"],
  ["shirt-folded", "Unscented toiletries"],
  ["book-open-text", "Pocket Dua guide"],
  ["drop", "Zamzam handling guidance"],
] as const;

const confirmed = [
  "Hotels (Makkah & Madinah)",
  "Return flights (Economy)",
  "Umrah visa with insurance",
  "Airport & intercity transfers",
  "Meals as per itinerary",
  "Guided Ziyarah",
  "Group leader & support",
] as const;

const pending = [
  "Flight schedule — To be confirmed",
  "Room allocation — 7 days before arrival",
  "Ziyarah timings — To be confirmed",
] as const;

const formatPrice = (price: number) =>
  new Intl.NumberFormat("en-IN", {
    style: "currency",
    currency: "INR",
    maximumFractionDigits: 0,
  }).format(price);

export function generateStaticParams() {
  return publicPackagePreviews.map((packagePreview) => ({
    departureId: packagePreview.departureId,
  }));
}

export async function generateMetadata({
  params,
}: PackagePageProps): Promise<Metadata> {
  const { departureId } = await params;
  const packagePreview = findPublicPackagePreview(departureId);

  if (!packagePreview) return { title: "Package not found · NoorPath" };

  return {
    title: `${packagePreview.packageName} · NoorPath`,
    description: packagePreview.summary,
  };
}

export default async function PackageDetailsPage({ params }: PackagePageProps) {
  const { departureId } = await params;
  const packagePreview = findPublicPackagePreview(departureId);

  if (!packagePreview) notFound();

  const remaining = packagePreview.price - packagePreview.amountDueToday;
  const secondPayment = Math.round(remaining / 2 / 500) * 500;
  const finalPayment = remaining - secondPayment;

  return (
    <div className="public-page package-page">
      <PublicHeader />

      <main className="package-main" id="main-content">
        <nav className="package-breadcrumbs" aria-label="Breadcrumb">
          <Link href="/">Home</Link>
          <span aria-hidden="true">›</span>
          <Link href="/#packages">Umrah Packages</Link>
          <span aria-hidden="true">›</span>
          <span>{packagePreview.packageName}</span>
        </nav>

        <section className="package-overview" aria-labelledby="package-title">
          <div className="package-gallery">
            <div className="package-gallery-primary">
              <Image
                src={packagePreview.image}
                alt="Masjid al-Haram and the Kaaba in Makkah"
                fill
                priority
                sizes="(max-width: 960px) 55vw, 25vw"
              />
            </div>
            <div className="package-gallery-secondary">
              <Image
                src="/assets/madinah-reference.svg"
                alt="Al-Masjid an-Nabawi and the green dome in Madinah"
                fill
                sizes="(max-width: 960px) 45vw, 20vw"
              />
            </div>
            <span className="package-gallery-status">
              Available seats
              <strong>
                {packagePreview.seatsRemaining} / {packagePreview.capacity}
              </strong>
            </span>
          </div>

          <div className="package-operator-summary">
            <p className="verified-operator">
              <Icon name="seal-check" /> Verified operator
            </p>
            <h1 id="package-title">Noor International Tours &amp; Travels</h1>
            <div className="operator-credentials" aria-label="Operator details">
              <span>
                <Icon name="airplane-tilt" /> IATA Accredited
              </span>
              <span>
                <Icon name="certificate" /> ISO 9001:2015
              </span>
              <span>
                <Icon name="clock" /> 15+ Years Experience
              </span>
            </div>
            <StaySummary city="Makkah" stay={packagePreview.makkah} />
            <StaySummary city="Madinah" stay={packagePreview.madinah} />
          </div>

          <PaymentSummary
            price={packagePreview.price}
            dueToday={packagePreview.amountDueToday}
            remaining={remaining}
            secondPayment={secondPayment}
            finalPayment={finalPayment}
          />
        </section>

        <div className="package-content-grid" id="journey-facts">
          <section className="package-panel itinerary-panel">
            <h2>Your itinerary</h2>
            <ol className="itinerary-list">
              {itinerary.map(([day, icon, title, copy]) => (
                <li key={`${day}-${title}`}>
                  <span className="itinerary-day">{day}</span>
                  <span className="itinerary-icon">
                    <Icon name={icon} />
                  </span>
                  <div>
                    <strong>{title}</strong>
                    <p>{copy}</p>
                  </div>
                </li>
              ))}
            </ol>
          </section>

          <div className="package-feature-column">
            <IconGrid title="Package includes" items={included} columns={4} />
            <IconGrid
              title="Travel kit included"
              items={travelKit}
              columns={5}
            />
            <IconGrid title="Umrah kit included" items={umrahKit} columns={5} />
          </div>

          <div className="package-status-column">
            <section className="package-panel fact-status-panel">
              <div className="status-tabs" aria-label="Fact status legend">
                <span>Confirmed</span>
                <span>Pending</span>
              </div>
              <div className="status-columns">
                <ul>
                  {confirmed.map((item) => (
                    <li key={item}>
                      <Icon name="seal-check" /> {item}
                    </li>
                  ))}
                </ul>
                <ul className="pending-list">
                  {pending.map((item) => (
                    <li key={item}>
                      <Icon name="clock" /> {item}
                    </li>
                  ))}
                </ul>
              </div>
            </section>

            <section className="package-panel cancellation-panel">
              <h2>Cancellation summary</h2>
              <dl>
                <div>
                  <dt>Before 30 days of departure</dt>
                  <dd>₹10,000 per person</dd>
                </div>
                <div>
                  <dt>15–30 days of departure</dt>
                  <dd>25% of package</dd>
                </div>
                <div>
                  <dt>7–14 days of departure</dt>
                  <dd>50% of package</dd>
                </div>
                <div>
                  <dt>Within 7 days of departure</dt>
                  <dd>No refund</dd>
                </div>
              </dl>
              <a href="#payment-plan">
                View payment &amp; refund policy{" "}
                <span aria-hidden="true">›</span>
              </a>
            </section>
          </div>
        </div>
      </main>

      <div
        className="package-sticky-action"
        role="region"
        aria-label="Package actions"
      >
        <PriceCell label="Total package" value={packagePreview.price} />
        <PriceCell
          label="Pay today"
          value={packagePreview.amountDueToday}
          tone="green"
        />
        <PriceCell label="Remaining" value={remaining} tone="gold" />
        <a href="mailto:support@noorpath.example">
          Book now <span aria-hidden="true">›</span>
        </a>
      </div>
    </div>
  );
}

function StaySummary({
  city,
  stay,
}: {
  city: "Makkah" | "Madinah";
  stay: PublicPackagePreview["makkah"];
}) {
  return (
    <div className="stay-summary">
      <span>{city} Hotel</span>
      <div>
        <strong>{stay.hotelName}</strong>
        <em>{stay.distanceDisclosure}</em>
      </div>
      <p>
        {stay.nights} Nights <span aria-hidden="true">·</span> Quad Sharing
      </p>
    </div>
  );
}

function PaymentSummary({
  price,
  dueToday,
  remaining,
  secondPayment,
  finalPayment,
}: {
  price: number;
  dueToday: number;
  remaining: number;
  secondPayment: number;
  finalPayment: number;
}) {
  return (
    <aside className="payment-summary-card" aria-label="Payment summary">
      <h2>Payment summary</h2>
      <div className="payment-totals">
        <PriceCell label="Total package" value={price} />
        <PriceCell
          label={`Pay ${formatPrice(dueToday)} today`}
          value={dueToday}
          tone="green"
        />
        <PriceCell label="Remaining" value={remaining} tone="gold" />
      </div>
      <h3 id="payment-plan">Instalment plan</h3>
      <ol className="instalment-plan">
        <PaymentStep
          amount={dueToday}
          label="Reserve seat"
          note="At the time of booking"
          date="25 May 2025"
        />
        <PaymentStep
          amount={secondPayment}
          label="After documents verified"
          note="Within 10–15 days"
          date="10 Jun 2025"
        />
        <PaymentStep
          amount={finalPayment}
          label="Before ticketing"
          note="30–35 days before departure"
          date="15 Jul 2025"
        />
      </ol>
      <a href="#payment-plan">
        View payment &amp; refund policy <span aria-hidden="true">›</span>
      </a>
    </aside>
  );
}

function PaymentStep({
  amount,
  label,
  note,
  date,
}: {
  amount: number;
  label: string;
  note: string;
  date: string;
}) {
  return (
    <li>
      <span className="payment-dot" aria-hidden="true" />
      <div>
        <strong>
          {formatPrice(amount)} <span aria-hidden="true">—</span> {label}
        </strong>
        <small>{note}</small>
      </div>
      <time>{date}</time>
    </li>
  );
}

function IconGrid({
  title,
  items,
  columns,
}: {
  title: string;
  items: readonly (readonly [string, string])[];
  columns: 4 | 5;
}) {
  return (
    <section className="package-panel icon-grid-panel">
      <h2>{title}</h2>
      <ul style={{ "--icon-columns": columns } as React.CSSProperties}>
        {items.map(([icon, label]) => (
          <li key={label}>
            <Icon name={icon} />
            <span>{label}</span>
          </li>
        ))}
      </ul>
      {title !== "Package includes" ? (
        <small>
          Kit contents may differ based on traveller type and itinerary.
        </small>
      ) : null}
    </section>
  );
}

function PriceCell({
  label,
  value,
  tone,
}: {
  label: string;
  value: number;
  tone?: "green" | "gold";
}) {
  return (
    <div className={`price-cell${tone ? ` price-cell-${tone}` : ""}`}>
      <span>{label}</span>
      <strong>{formatPrice(value)}</strong>
    </div>
  );
}
