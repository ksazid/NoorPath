import Image from "next/image";
import Link from "next/link";
import { publicPackagePreviews } from "./public-package-preview";
import { PublicFooter, PublicHeader } from "./public-ui";

export default function HomePage() {
  return (
    <div className="public-page">
      <PublicHeader />

      <main>
        <section className="public-hero">
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
              Explore calm, factual journey previews with Makkah, Madinah, travel,
              and inclusion details kept explicit instead of hidden behind sales
              language.
            </p>
            <div className="public-preview-note" role="note">
              <strong>Preview catalogue</strong>
              <span>
                These example journeys let you review the NoorPath customer
                experience while the V2 public catalogue API is built. No booking,
                price, seat, or availability claim is being made here.
              </span>
            </div>
            <div className="public-trust-strip" aria-label="NoorPath trust principles">
              <div className="public-trust-item">
                <strong>Operator accountability</strong>
                <span>Journey facts stay tied to the operator.</span>
              </div>
              <div className="public-trust-item">
                <strong>Facts stay explicit</strong>
                <span>Pending details are shown as pending.</span>
              </div>
              <div className="public-trust-item">
                <strong>Human support</strong>
                <span>Help remains available around the journey.</span>
              </div>
            </div>
          </div>
        </section>

        <section className="public-catalogue" id="packages">
          <div className="public-section-heading">
            <div>
              <span className="public-eyebrow">Package previews</span>
              <h2>Find a journey that feels clear</h2>
              <p>
                Open any package to review the stay, route, inclusions, exclusions,
                and confirmation state of important journey facts.
              </p>
            </div>
            <p className="public-catalogue-note">
              Commercial details remain intentionally absent until their V2 slices
              own them.
            </p>
          </div>

          <div className="public-package-grid">
            {publicPackagePreviews.map((packagePreview) => (
              <article className="public-package-card" key={packagePreview.departureId}>
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
                    {packagePreview.durationNights} nights · {packagePreview.travel.routeSummary}
                  </p>
                  <dl className="public-card-facts">
                    <div>
                      <dt>Departure</dt>
                      <dd>{packagePreview.departureDate}</dd>
                    </div>
                    <div>
                      <dt>From</dt>
                      <dd>{packagePreview.origin}</dd>
                    </div>
                  </dl>
                  <div className="public-card-inclusions" aria-label="Inclusion highlights">
                    {packagePreview.inclusions.slice(0, 3).map((inclusion) => (
                      <span key={inclusion}>{inclusion}</span>
                    ))}
                  </div>
                  <Link
                    className="public-card-action"
                    href={`/packages/${packagePreview.departureId}`}
                  >
                    View package details
                  </Link>
                </div>
              </article>
            ))}
          </div>
        </section>

        <section className="public-trust-section" id="trust">
          <div className="public-trust-inner">
            <div>
              <span className="public-eyebrow">Trust through clarity</span>
              <h2>NoorPath should make uncertainty visible.</h2>
              <p>
                A pending hotel, route, or travel fact should look pending. Confirmed
                information should be easy to find. The customer experience should
                never manufacture urgency or imply certainty that the operator has
                not supplied.
              </p>
            </div>
            <div className="public-trust-list">
              <article>
                <h3>Makkah and Madinah stay facts stay independent</h3>
                <p>
                  Hotel, classification, distance disclosure, nights, and confirmation
                  state are shown separately for each city.
                </p>
              </article>
              <article>
                <h3>Travel facts have their own confirmation state</h3>
                <p>
                  Route information can be useful before every flight fact is final,
                  without presenting planning information as confirmed travel.
                </p>
              </article>
              <article>
                <h3>Commercial pressure is deliberately absent</h3>
                <p>
                  Pricing, inventory, scarcity, booking, and payment belong to later
                  slices and are not simulated in this public preview.
                </p>
              </article>
            </div>
          </div>
        </section>
      </main>

      <PublicFooter />
    </div>
  );
}
