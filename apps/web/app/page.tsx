import Image from "next/image";
import { CustomerDiscovery } from "./CustomerDiscovery";
import { Icon, PublicFooter, PublicHeader } from "./public-ui";

const trustPoints = [
  {
    icon: "shield-check",
    title: "Verified Operator",
    copy: "CBI Protected",
  },
  {
    icon: "headset",
    title: "24/7 Support",
    copy: "Before, during & after",
  },
  {
    icon: "receipt",
    title: "Transparent Pricing",
    copy: "No hidden charges",
  },
] as const;

const servicePoints = [
  {
    icon: "shield-check",
    title: "Verified Operators",
    copy: "Only government-approved and trusted partners",
  },
  {
    icon: "bed",
    title: "Quality Stays",
    copy: "Handpicked hotels near Haram & Masjid an-Nabawi",
  },
  {
    icon: "airplane-tilt",
    title: "Smooth Journey",
    copy: "Visa, flights, and transport handled with care",
  },
  {
    icon: "headset",
    title: "Dedicated Support",
    copy: "Real people, here for you every step of the way",
  },
] as const;

export default function HomePage() {
  return (
    <div className="public-page landing-page">
      <div className="landing-shell">
        <PublicHeader mode="landing" />

        <main id="main-content">
          <section className="landing-hero" aria-labelledby="landing-title">
            <Image
              className="landing-hero-art"
              src="/assets/kaaba-reference.svg"
              alt="Masjid al-Haram and the Kaaba in Makkah"
              fill
              priority
              sizes="(max-width: 760px) 100vw, 1220px"
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
                  Trusted packages, verified operators,
                  <br />
                  and peaceful journeys.
                </p>
              </div>

              <form className="journey-search" action="#packages">
                <label>
                  <Icon name="map-pin" />
                  <span>Departure city</span>
                  <strong>Lucknow, IN</strong>
                  <Icon name="caret-down" className="search-caret" />
                  <select aria-label="Departure city" defaultValue="lucknow">
                    <option value="lucknow">Lucknow, IN</option>
                    <option value="delhi">Delhi, IN</option>
                    <option value="mumbai">Mumbai, IN</option>
                  </select>
                </label>
                <label>
                  <Icon name="calendar-blank" />
                  <span>Departure</span>
                  <strong>25 Jun 2025</strong>
                  <Icon name="caret-down" className="search-caret" />
                  <select aria-label="Departure date" defaultValue="june">
                    <option value="june">25 Jun 2025</option>
                    <option value="july">10 Jul 2025</option>
                    <option value="august">05 Aug 2025</option>
                  </select>
                </label>
                <label>
                  <Icon name="user-circle" />
                  <span>Travellers</span>
                  <strong>2 Adults</strong>
                  <Icon name="caret-down" className="search-caret" />
                  <select aria-label="Number of travellers" defaultValue="2">
                    <option value="1">1 Adult</option>
                    <option value="2">2 Adults</option>
                    <option value="3">3 Adults</option>
                    <option value="4">4 Adults</option>
                  </select>
                </label>
                <button type="submit" aria-label="Find Umrah packages">
                  <Icon name="magnifying-glass" />
                  <span>Find packages</span>
                </button>
              </form>

              <div
                className="landing-trust-row"
                aria-label="Why choose NoorPath"
              >
                {trustPoints.map((item) => (
                  <div key={item.title}>
                    <span className="trust-symbol" aria-hidden="true">
                      <Icon name={item.icon} />
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
                <p>
                  Carefully selected for a comfortable and meaningful journey.
                </p>
              </div>
              <a href="#packages">
                View all packages <span aria-hidden="true">→</span>
              </a>
            </div>

            <CustomerDiscovery />
          </section>

          <section
            className="landing-service-strip"
            id="trust"
            aria-label="NoorPath services"
          >
            {servicePoints.map((item) => (
              <div key={item.title}>
                <span className="service-symbol" aria-hidden="true">
                  <Icon name={item.icon} />
                </span>
                <span>
                  <strong>{item.title}</strong>
                  <small>{item.copy}</small>
                </span>
              </div>
            ))}
          </section>
        </main>
      </div>

      <PublicFooter />
    </div>
  );
}
