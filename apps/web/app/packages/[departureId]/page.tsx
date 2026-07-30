import type { Metadata } from "next";
import Image from "next/image";
import Link from "next/link";
import { notFound } from "next/navigation";
import {
  findPublicPackagePreview,
  publicPackagePreviews,
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

function StayCard({
  city,
  stay,
}: {
  city: "Makkah" | "Madinah";
  stay: StayPreview;
}) {
  return (
    <article className="public-stay-card">
      <div>
        <span className="public-eyebrow">{city} stay</span>
        <h3>{stay.hotelName}</h3>
      </div>
      <ConfirmationBadge state={stay.confirmationState} />
      <dl>
        <div>
          <dt>Classification</dt>
          <dd>{stay.classification}</dd>
        </div>
        <div>
          <dt>Distance disclosure</dt>
          <dd>{stay.distanceDisclosure}</dd>
        </div>
        <div>
          <dt>Nights</dt>
          <dd>{stay.nights}</dd>
        </div>
      </dl>
    </article>
  );
}

export default async function PackageDetailsPage({ params }: PackagePageProps) {
  const { departureId } = await params;
  const packagePreview = findPublicPackagePreview(departureId);

  if (!packagePreview) notFound();

  return (
    <div className="public-page">
      <PublicHeader />

      <main className="public-detail-main">
        <nav className="public-breadcrumbs" aria-label="Breadcrumb">
          <Link href="/#packages">Packages</Link>
          <span aria-hidden="true">/</span>
          <span>{packagePreview.packageName}</span>
        </nav>

        <section className="public-detail-hero">
          <div className="public-detail-image">
            <Image
              src={packagePreview.image}
              alt="Masjid al-Haram in Makkah"
              fill
              priority
              sizes="(max-width: 980px) 100vw, 55vw"
            />
          </div>
          <div className="public-detail-copy">
            <span className="public-eyebrow">
              Journey preview · {packagePreview.operatorName}
            </span>
            <h1>{packagePreview.packageName}</h1>
            <p className="public-detail-summary">{packagePreview.summary}</p>
            <dl className="public-detail-meta">
              <div>
                <dt>Departure from</dt>
                <dd>{packagePreview.origin}</dd>
              </div>
              <div>
                <dt>Journey length</dt>
                <dd>{packagePreview.durationNights} nights</dd>
              </div>
              <div>
                <dt>Departure</dt>
                <dd>{packagePreview.departureDate}</dd>
              </div>
              <div>
                <dt>Return</dt>
                <dd>{packagePreview.returnDate}</dd>
              </div>
            </dl>
          </div>
        </section>

        <div className="public-detail-grid">
          <div className="public-detail-content">
            <section className="public-detail-section">
              <span className="public-eyebrow">Accommodation</span>
              <h2>Your stay in the Haramain</h2>
              <div className="public-stay-grid">
                <StayCard city="Makkah" stay={packagePreview.makkah} />
                <StayCard city="Madinah" stay={packagePreview.madinah} />
              </div>
            </section>

            <section className="public-detail-section">
              <span className="public-eyebrow">Travel</span>
              <h2>Journey route and travel facts</h2>
              <div className="public-travel-facts">
                <ConfirmationBadge
                  state={packagePreview.travel.confirmationState}
                />
                <div>
                  <strong>{packagePreview.travel.routeSummary}</strong>
                  <p>{packagePreview.travel.details}</p>
                </div>
              </div>
            </section>

            <section className="public-detail-section">
              <span className="public-eyebrow">What is clear today</span>
              <h2>Included and not included</h2>
              <div className="public-list-grid">
                <div className="public-list-panel">
                  <h3>Included</h3>
                  <ul>
                    {packagePreview.inclusions.map((item) => (
                      <li key={item}>{item}</li>
                    ))}
                  </ul>
                </div>
                <div className="public-list-panel">
                  <h3>Not included</h3>
                  <ul>
                    {packagePreview.exclusions.map((item) => (
                      <li key={item}>{item}</li>
                    ))}
                  </ul>
                </div>
              </div>
            </section>
          </div>

          <aside className="public-support-panel" aria-label="Package support">
            <span className="public-eyebrow">Need clarity?</span>
            <h2>Speak to a human before you decide.</h2>
            <p>
              This is a V2 customer-experience preview. No booking, payment,
              price, seat, or availability claim is being made. Human support
              can help you understand the journey facts shown here.
            </p>
            <a
              className="public-support-primary"
              href="mailto:support@noorpath.example"
            >
              Contact human support
            </a>
            <Link className="public-support-secondary" href="/#packages">
              Back to packages
            </Link>
          </aside>
        </div>
      </main>

      <PublicFooter />
    </div>
  );
}
