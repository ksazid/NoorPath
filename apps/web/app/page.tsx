import Image from "next/image";
import Link from "next/link";
import { publicPackagePreviews } from "./public-package-preview";
import { PublicFooter, PublicHeader } from "./public-ui";

const trustPoints = [
  {
    title: "Verified operators",
    copy: "Operator accountability stays visible.",
  },
  {
    title: "24/7 support",
    copy: "Human help before, during, and after.",
  },
  {
    title: "Transparent details",
    copy: "Pending facts are clearly identified.",
  },
] as const;

const servicePoints = [
  {
    title: "Verified operators",
    copy: "Journey facts remain tied to accountable operators.",
  },
  {
    title: "Quality stays",
    copy: "Makkah and Madinah details remain separate and clear.",
  },
  {
    title: "Smooth journey",
    copy: "Travel, transfers, and inclusions are easy to review.",
  },
  {
    title: "Dedicated support",
    copy: "A real person is available when you need clarity.",
  },
] as const;

export default function HomePage() {
  return (
    <div className="public-page landing-page">
      <PublicHeader mode="landing" />

      <main id="main-content">
        <section className="landing-hero" aria-labelledby="landing-title">
          <Image
            className="landing-hero-art"
            src="/assets/kaaba-reference.svg"
            alt="Masjid al-Haram and the Kaaba in Makkah"
            fill
            priority
            sizes="100vw"
          />
          <div className="landing-hero-wash" aria-hidden="true" />
          <div className="landing-hero-inner">
            <div className="landing-hero-copy">
              <h1 id="landing-title">
                Your Umrah,
                <br />
                Our Responsibility
              </h1>
              <p>
                Trusted journeys, accountable operators, and peaceful support
                from departure to return.
              </p>
            </div>

            <form className="journey-search" action="#packages">
              <label>
                <span>Departure city</span>
                <strong>Lucknow, India</strong>
                <select aria-label="Departure city" defaultValue="lucknow">
                  <option value="lucknow">Lucknow, India</option>
                  <option value="delhi">Delhi, India</option>
                  <option value="mumbai">Mumbai, India</option>
                </select>
              </label>
              <label>
                <span>Departure</span>
                <strong>September 2026</strong>
                <select aria-label="Departure month" defaultValue="september">
                  <option value="september">September 2026</option>
                  <option value="october">October 2026</option>
                  <option value="november">November 2026</option>
                </select>
              </label>
              <label>
                <span>Travellers</span>
                <strong>2 adults</strong>
                <select aria-label="Number of travellers" defaultValue="2">
                  <option value="1">1 adult</option>
                  <option value="2">2 adults</option>
                  <option value="3">3 adults</option>
                  <option value="4">4 adults</option>
                </select>
              </label>
              <button type="submit" aria-label="Find Umrah packages">
                <SearchIcon />
                <span>Find packages</span>
              </button>
            </form>

            <div className="landing-trust-row" aria-label="Why choose NoorPath">
              {trustPoints.map((item) => (
                <div key={item.title}>
                  <span className="trust-symbol" aria-hidden="true">
                    <CheckIcon />
                  </span>
                  <span>
                    <strong>{item.title}</strong>
                    <small>{item.copy}</small>
                  </span>
                </div>
              ))}
            </div>
          </div>
        </section>

        <section className="landing-packages" id="packages">
          <div className="landing-section-heading">
            <div>
              <h2>Handpicked Umrah Packages</h2>
              <p>Carefully selected for a clear and meaningful journey.</p>
            </div>
            <a href="#packages">
              View all packages <span aria-hidden="true">→</span>
            </a>
          </div>

          <div className="landing-package-grid">
            {publicPackagePreviews.map((packagePreview, index) => (
              <article
                className="landing-package-card"
                key={packagePreview.departureId}
              >
                <div className="landing-package-image">
                  <Image
                    src={packagePreview.image}
                    alt="Masjid al-Haram in Makkah"
                    fill
                    sizes="(max-width: 760px) 100vw, (max-width: 980px) 50vw, 33vw"
                  />
                  <span className="package-tier">
                    {index === 0
                      ? "Most popular"
                      : index === 1
                        ? "Best value"
                        : "Premium"}
                  </span>
                </div>
                <div className="landing-package-body">
                  <p className="verified-operator">
                    <CheckIcon /> Verified operator
                  </p>
                  <h3>{packagePreview.packageName}</h3>
                  <p className="package-stay">
                    Makkah {packagePreview.makkah.nights} nights
                    <span aria-hidden="true">·</span>
                    Madinah {packagePreview.madinah.nights} nights
                  </p>
                  <div
                    className="package-inclusions"
                    aria-label="Inclusion highlights"
                  >
                    {packagePreview.inclusions.slice(0, 3).map((inclusion) => (
                      <span key={inclusion}>{inclusion}</span>
                    ))}
                  </div>
                  <dl className="package-departure">
                    <div>
                      <dt>Departure</dt>
                      <dd>{packagePreview.departureDate}</dd>
                    </div>
                    <div>
                      <dt>From</dt>
                      <dd>{packagePreview.origin}</dd>
                    </div>
                  </dl>
                  <div className="package-card-footer">
                    <span>
                      <strong>{packagePreview.durationNights} nights</strong>
                      <small>journey preview</small>
                    </span>
                    <Link href={`/packages/${packagePreview.departureId}`}>
                      View package <span aria-hidden="true">→</span>
                    </Link>
                  </div>
                </div>
              </article>
            ))}
          </div>
        </section>

        <section
          className="landing-service-strip"
          aria-label="NoorPath services"
        >
          {servicePoints.map((item) => (
            <div key={item.title}>
              <span className="service-symbol" aria-hidden="true">
                <CheckIcon />
              </span>
              <span>
                <strong>{item.title}</strong>
                <small>{item.copy}</small>
              </span>
            </div>
          ))}
        </section>
      </main>

      <PublicFooter />
    </div>
  );
}

function SearchIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24">
      <circle cx="11" cy="11" r="6.5" />
      <path d="m16 16 4 4" />
    </svg>
  );
}

function CheckIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24">
      <path d="m6.5 12 3.2 3.2 7.8-7.8" />
    </svg>
  );
}
