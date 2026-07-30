import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import {
  findPublicPackagePreview,
  publicPackagePreviews,
  type FactConfirmationState,
  type StayPreview,
} from "../../public-package-preview";
import { ConfirmationBadge, PublicFooter, PublicHeader } from "../../public-ui";

type PackagePageProps = {
  params: Promise<{ departureId: string }>;
};

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

function HeroFactPill({
  label,
  state,
}: {
  label: string;
  state: FactConfirmationState;
}) {
  return (
    <span
      className={
        state === "confirmed"
          ? "public-hero-fact-state confirmed"
          : "public-hero-fact-state pending"
      }
    >
      {label} {state === "confirmed" ? "confirmed" : "pending"}
    </span>
  );
}

function StayCard({
  city,
  stay,
}: {
  city: "Makkah" | "Madinah";
  stay: StayPreview;
}) {
  return (
    <article className="public-stay-card public-stay-card-refined">
      <div className="public-stay-card-heading">
        <h3>{city}</h3>
        <ConfirmationBadge state={stay.confirmationState} />
      </div>
      <strong className="public-stay-primary">
        {stay.hotelName} · {stay.nights} nights
      </strong>
      <p className="public-stay-secondary">{stay.distanceDisclosure}</p>
      <p className="public-stay-classification">{stay.classification}</p>
      <p className="public-stay-context">
        Hotel, nights, classification, and distance remain separate journey facts.
      </p>
    </article>
  );
}

export default async function PackageDetailsPage({ params }: PackagePageProps) {
  const { departureId } = await params;
  const packagePreview = findPublicPackagePreview(departureId);

  if (!packagePreview) notFound();

  return (
    <div className="public-page public-page-refined">
      <PublicHeader />

      <main className="public-detail-main public-detail-main-refined">
        <nav className="public-breadcrumbs" aria-label="Breadcrumb">
          <Link href="/#packages">Packages</Link>
          <span aria-hidden="true">/</span>
          <span>{packagePreview.packageName}</span>
        </nav>

        <section className="public-detail-hero public-detail-hero-refined">
          <div className="public-detail-image public-detail-image-refined">
            <Image
              src={packagePreview.image}
              alt="Masjid al-Haram in Makkah"
              fill
              priority
              sizes="(max-width: 980px) 100vw, 50vw"
            />
          </div>
          <div className="public-detail-copy public-detail-copy-refined">
            <span className="public-eyebrow">
              {packagePreview.operatorName}
            </span>
            <h1>{packagePreview.packageName}</h1>
            <p className="public-detail-route">
              {packagePreview.travel.routeSummary}
            </p>
            <div
              className="public-hero-fact-states"
              aria-label="Journey confirmation states"
            >
              <HeroFactPill
                label="Makkah"
                state={packagePreview.makkah.confirmationState}
              />
              <HeroFactPill
                label="Madinah"
                state={packagePreview.madinah.confirmationState}
              />
              <HeroFactPill
                label="Travel"
                state={packagePreview.travel.confirmationState}
              />
            </div>
            <p className="public-detail-summary">{packagePreview.summary}</p>
            <div className="public-detail-truth-note" role="note">
              <strong>Truth before transaction</strong>
              <span>
                Commercial details stay absent until they are authoritative.
              </span>
            </div>
            <a className="public-detail-hero-action" href="#journey-facts">
              Review journey facts
            </a>
          </div>
        </section>

        <div
          className="public-detail-content public-detail-content-refined"
          id="journey-facts"
        >
          <section
            className="public-detail-section public-detail-section-refined"
            aria-label="Accommodation facts"
          >
            <div className="public-stay-grid">
              <StayCard city="Makkah" stay={packagePreview.makkah} />
              <StayCard city="Madinah" stay={packagePreview.madinah} />
            </div>
          </section>

          <section className="public-detail-section public-detail-section-refined">
            <div className="public-travel-heading">
              <h2>Travel</h2>
              <ConfirmationBadge state={packagePreview.travel.confirmationState} />
            </div>
            <div className="public-travel-facts">
              <div>
                <strong>{packagePreview.travel.routeSummary}</strong>
                <p>{packagePreview.travel.details}</p>
              </div>
            </div>
          </section>

          <section className="public-detail-section public-detail-section-refined">
            <div className="public-list-grid">
              <div className="public-list-panel public-list-panel-refined included">
                <h3>Included</h3>
                <ul>
                  {packagePreview.inclusions.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ul>
              </div>
              <div className="public-list-panel public-list-panel-refined excluded">
                <h3>Not included</h3>
                <ul>
                  {packagePreview.exclusions.map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ul>
              </div>
            </div>
          </section>

          <div
            className="public-detail-legend"
            aria-label="How NoorPath presents package facts"
          >
            <span className="confirmed">What’s confirmed</span>
            <span className="pending">What’s still pending</span>
            <span>What is not yet commercial</span>
          </div>
        </div>
      </main>

      <PublicFooter />
    </div>
  );
}
