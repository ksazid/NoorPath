import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import {
  findPublicPackagePreview,
  publicPackagePreviews,
  type PublicPackagePreview,
} from "../../public-package-preview";
import { ConfirmationBadge, PublicFooter, PublicHeader } from "../../public-ui";

type PackagePageProps = {
  params: Promise<{ departureId: string }>;
};

const itinerary = [
  ["Day 1", "Arrival in Jeddah", "Airport assistance and transfer to Makkah."],
  ["Days 1–5", "Makkah stay", "Time for Umrah, worship, and rest."],
  ["Day 6", "Guided Ziyarah", "Planned local visits with journey support."],
  ["Day 7", "Umrah orientation", "Guidance and practical journey briefing."],
  ["Day 8", "Makkah to Madinah", "Intercity transfer and hotel check-in."],
  ["Days 8–12", "Madinah stay", "Time for worship and reflection."],
  ["Final day", "Departure", "Checkout and return-airport transfer."],
] as const;

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

  return (
    <div className="public-page package-page">
      <PublicHeader />

      <main className="package-main" id="main-content">
        <nav className="package-breadcrumbs" aria-label="Breadcrumb">
          <Link href="/">Home</Link>
          <span aria-hidden="true">›</span>
          <Link href="/#packages">Umrah packages</Link>
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
                sizes="(max-width: 960px) 100vw, 32vw"
              />
            </div>
            <div className="package-gallery-secondary">
              <Image
                src="/assets/madinah-reference.svg"
                alt="Al-Masjid an-Nabawi and the green dome in Madinah"
                fill
                sizes="(max-width: 960px) 100vw, 24vw"
              />
            </div>
            <span className="package-gallery-status">Journey preview</span>
          </div>

          <div className="package-operator-summary">
            <p className="verified-operator">
              <CheckIcon /> Verified operator
            </p>
            <h1 id="package-title">{packagePreview.operatorName}</h1>
            <p className="package-name">{packagePreview.packageName}</p>
            <div className="operator-credentials" aria-label="Operator details">
              <span>Accountable operator</span>
              <span>Journey facts tracked</span>
            </div>
            <StaySummary city="Makkah" stay={packagePreview.makkah} />
            <StaySummary city="Madinah" stay={packagePreview.madinah} />
          </div>

          <aside className="journey-summary-card" aria-label="Journey summary">
            <h2>Journey summary</h2>
            <dl className="journey-summary-highlight">
              <div>
                <dt>Total journey</dt>
                <dd>{packagePreview.durationNights} nights</dd>
              </div>
              <div>
                <dt>Departure</dt>
                <dd>{packagePreview.departureDate}</dd>
              </div>
              <div>
                <dt>From</dt>
                <dd>{packagePreview.origin}</dd>
              </div>
            </dl>
            <h3>Confirmation progress</h3>
            <ol className="confirmation-timeline">
              <TimelineItem
                label="Accommodation details"
                state={
                  packagePreview.makkah.confirmationState === "confirmed" &&
                  packagePreview.madinah.confirmationState === "confirmed"
                    ? "confirmed"
                    : "pending"
                }
              />
              <TimelineItem
                label="Journey route"
                state={packagePreview.travel.confirmationState}
              />
              <TimelineItem label="Final travel details" state="pending" />
            </ol>
            <a className="summary-policy-link" href="#journey-facts">
              View all journey facts <span aria-hidden="true">→</span>
            </a>
          </aside>
        </section>

        <div className="package-content-grid" id="journey-facts">
          <section className="package-panel itinerary-panel">
            <h2>Your itinerary</h2>
            <ol className="itinerary-list">
              {itinerary.map(([day, title, copy]) => (
                <li key={`${day}-${title}`}>
                  <span>{day}</span>
                  <div>
                    <strong>{title}</strong>
                    <p>{copy}</p>
                  </div>
                </li>
              ))}
            </ol>
          </section>

          <div className="package-feature-column">
            <FeaturePanel
              title="Package includes"
              items={packagePreview.inclusions}
            />
            <FeaturePanel
              title="Travel details"
              items={[
                packagePreview.travel.routeSummary,
                packagePreview.travel.details,
              ]}
            />
            <FeaturePanel
              title="Not included"
              items={packagePreview.exclusions}
            />
          </div>

          <div className="package-status-column">
            <section className="package-panel fact-status-panel">
              <div className="status-tabs" aria-label="Fact status legend">
                <span>Confirmed</span>
                <span>Pending</span>
              </div>
              <StatusRow
                label="Makkah hotel"
                state={packagePreview.makkah.confirmationState}
              />
              <StatusRow
                label="Madinah hotel"
                state={packagePreview.madinah.confirmationState}
              />
              <StatusRow
                label="Journey route"
                state={packagePreview.travel.confirmationState}
              />
              <StatusRow label="Final flight details" state="pending" />
            </section>
            <section className="package-panel support-card">
              <h2>Need clarity?</h2>
              <p>
                Speak to a real person before you make any journey decision.
              </p>
              <a href="mailto:support@noorpath.example">
                Contact human support <span aria-hidden="true">→</span>
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
        <div>
          <span>Journey</span>
          <strong>{packagePreview.durationNights} nights</strong>
        </div>
        <div>
          <span>Departure</span>
          <strong>{packagePreview.departureDate}</strong>
        </div>
        <div>
          <span>From</span>
          <strong>{packagePreview.origin}</strong>
        </div>
        <a href="mailto:support@noorpath.example">
          Request journey details <span aria-hidden="true">→</span>
        </a>
      </div>

      <PublicFooter />
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
      <span>{city} hotel</span>
      <div>
        <strong>{stay.hotelName}</strong>
        <em>{stay.distanceDisclosure}</em>
      </div>
      <p>
        {stay.nights} nights <span aria-hidden="true">·</span>{" "}
        {stay.classification}
      </p>
    </div>
  );
}

function TimelineItem({
  label,
  state,
}: {
  label: string;
  state: "confirmed" | "pending";
}) {
  return (
    <li className={`timeline-${state}`}>
      <span aria-hidden="true">
        {state === "confirmed" ? <CheckIcon /> : null}
      </span>
      <div>
        <strong>{label}</strong>
        <small>
          {state === "confirmed"
            ? "Operator information received"
            : "Awaiting operator confirmation"}
        </small>
      </div>
    </li>
  );
}

function FeaturePanel({
  title,
  items,
}: {
  title: string;
  items: readonly string[];
}) {
  return (
    <section className="package-panel feature-panel">
      <h2>{title}</h2>
      <ul>
        {items.map((item) => (
          <li key={item}>
            <span aria-hidden="true">
              <CheckIcon />
            </span>
            <span>{item}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}

function StatusRow({
  label,
  state,
}: {
  label: string;
  state: "confirmed" | "pending";
}) {
  return (
    <div className="fact-status-row">
      <ConfirmationBadge state={state} />
      <span>{label}</span>
    </div>
  );
}

function CheckIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24">
      <path d="m6.5 12 3.2 3.2 7.8-7.8" />
    </svg>
  );
}
