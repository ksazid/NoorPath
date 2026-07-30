import Image from "next/image";
import Link from "next/link";
import { publicPackagePreviews } from "./public-package-preview";
import { PublicFooter, PublicHeader } from "./public-ui";

export default function HomePage() {
  return (
    <div className="public-page public-page-refined">
      <PublicHeader />

      <main>
        <section className="public-hero public-hero-refined">
          <Image
            className="public-hero-art"
            src="/assets/kaaba-morning.png"
            alt="Masjid al-Haram in Makkah in the morning light"
            fill
            priority
            sizes="100vw"
          />
          <div className="public-hero-content">
            <span className="public-eyebrow">Journeys you can understand</span>
            <h1>Your Umrah, thoughtfully planned.</h1>
            <p className="public-hero-copy">
              Calm, factual journey previews with Makkah, Madinah, travel, and
              inclusion details kept explicit.
            </p>
            <div
              className="public-hero-principles"
              aria-label="NoorPath journey principles"
            >
              <span>Facts first</span>
              <span>No manufactured urgency</span>
            </div>
          </div>
        </section>

        <section className="public-catalogue public-catalogue-refined" id="packages">
          <div className="public-section-heading public-section-heading-refined">
            <div>
              <span className="public-eyebrow">Package previews</span>
              <h2>Choose with clarity, not pressure</h2>
              <p>
                Compare essential journey facts first. Commercial information
                only appears when it is authoritative.
              </p>
            </div>
          </div>

          <div className="public-package-grid">
            {publicPackagePreviews.map((packagePreview) => (
              <article
                className="public-package-card public-package-card-refined"
                key={packagePreview.departureId}
              >
                <div className="public-package-image">
                  <Image
                    src={packagePreview.image}
                    alt="Masjid al-Haram in Makkah"
                    fill
                    sizes="(max-width: 760px) 100vw, (max-width: 980px) 50vw, 33vw"
                  />
                  <span className="public-card-state">Journey preview</span>
                </div>
                <div className="public-package-body">
                  <p className="public-operator-label">
                    Operator · {packagePreview.operatorName}
                  </p>
                  <h3>{packagePreview.packageName}</h3>
                  <p className="public-card-route">
                    {packagePreview.travel.routeSummary}
                  </p>
                  <dl className="public-card-facts public-card-facts-refined">
                    <div>
                      <dt>Departure</dt>
                      <dd>{packagePreview.departureDate}</dd>
                    </div>
                    <div>
                      <dt>From</dt>
                      <dd>{packagePreview.origin}</dd>
                    </div>
                  </dl>
                  <div
                    className="public-card-inclusions"
                    aria-label="Inclusion highlights"
                  >
                    {packagePreview.inclusions.slice(0, 3).map((inclusion) => (
                      <span key={inclusion}>{inclusion}</span>
                    ))}
                  </div>
                  <Link
                    className="public-card-action public-card-action-refined"
                    href={`/packages/${packagePreview.departureId}`}
                  >
                    View journey details <span aria-hidden="true">→</span>
                  </Link>
                </div>
              </article>
            ))}
          </div>

          <div
            className="public-customer-trust-bar"
            id="trust"
            aria-label="NoorPath trust principles"
          >
            <span className="confirmed">✓ Confirmed facts stand out</span>
            <span>○ Pending details stay visible</span>
            <span>Human support remains close</span>
          </div>
        </section>
      </main>

      <PublicFooter />
    </div>
  );
}
