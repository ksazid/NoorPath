import Image from "next/image";
import { CustomerDiscovery } from "./CustomerDiscovery";
import { Icon, PublicFooter, PublicHeader } from "./public-ui";

const trustPoints = [
  {
    icon: "shield-check",
    title: "Verified Operator",
    copy: "Approved before public sale",
  },
  {
    icon: "calendar-blank",
    title: "Plan Ahead",
    copy: "Choose future departures early",
  },
  {
    icon: "receipt",
    title: "Clear Payment Plan",
    copy: "Know what is due before you commit",
  },
] as const;

const planningSteps = [
  {
    number: "01",
    title: "Choose when you want to travel",
    copy: "Explore published departures months ahead, from the city that works for you.",
  },
  {
    number: "02",
    title: "Understand the full journey",
    copy: "Review operator, stay, travel, pricing and availability before making a decision.",
  },
  {
    number: "03",
    title: "Secure it with a clear plan",
    copy: "Before commitment, your quote shows the total, due-now amount and payment schedule.",
  },
  {
    number: "04",
    title: "Prepare until departure",
    copy: "Follow payments, documents, visa readiness and next steps in one Umrah plan.",
  },
] as const;

const servicePoints = [
  {
    icon: "shield-check",
    title: "Verified Operators",
    copy: "Only eligible operators can publish journeys for public sale",
  },
  {
    icon: "calendar-blank",
    title: "Plan With Time",
    copy: "Browse future departures instead of waiting until the last minute",
  },
  {
    icon: "receipt",
    title: "Transparent Payments",
    copy: "Total, due-now and remaining balance are clear before commitment",
  },
  {
    icon: "headset",
    title: "Journey Support",
    copy: "Stay supported from first plan through travel readiness",
  },
] as const;

const planPreviewItems = [
  {
    label: "Payments",
    copy: "See paid amount, next due date and remaining balance",
  },
  {
    label: "Documents",
    copy: "Know what each traveller still needs to complete",
  },
  {
    label: "Visa & readiness",
    copy: "Follow customer-safe status and required next actions",
  },
  {
    label: "Departure",
    copy: "Keep your journey facts and preparation in one place",
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
                <p className="landing-hero-eyebrow">Plan months ahead</p>
                <h1 id="landing-title">
                  Your Umrah,
                  <br />
                  planned with time
                </h1>
                <p>
                  Choose a trusted future departure, understand the full cost,
                  <br />
                  and prepare step by step until you travel.
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
                  <span>When do you want to go?</span>
                  <strong>August 2027</strong>
                  <Icon name="caret-down" className="search-caret" />
                  <select aria-label="Travel month" defaultValue="august-2027">
                    <option value="june-2027">June 2027</option>
                    <option value="july-2027">July 2027</option>
                    <option value="august-2027">August 2027</option>
                    <option value="september-2027">September 2027</option>
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

          <section className="plan-ahead-story" id="plan-ahead" aria-labelledby="plan-ahead-title">
            <div className="plan-ahead-intro">
              <p className="plan-ahead-kicker">Start before the rush</p>
              <h2 id="plan-ahead-title">Your Umrah journey can begin months before departure.</h2>
              <p>
                NoorPath is designed for pilgrims who want time to choose carefully,
                spread the financial commitment clearly, and prepare with confidence.
              </p>
            </div>
            <ol className="plan-ahead-steps">
              {planningSteps.map((step) => (
                <li key={step.number}>
                  <span>{step.number}</span>
                  <div>
                    <h3>{step.title}</h3>
                    <p>{step.copy}</p>
                  </div>
                </li>
              ))}
            </ol>
          </section>

          <section className="landing-packages" id="packages">
            <div className="landing-section-heading">
              <div>
                <p className="plan-ahead-kicker">Future departures</p>
                <h2>Handpicked Umrah Packages</h2>
                <p>
                  Explore published journeys early, compare the facts, and give yourself
                  time to plan.
                </p>
              </div>
              <a href="#packages">
                View all packages <span aria-hidden="true">→</span>
              </a>
            </div>

            <CustomerDiscovery />
          </section>

          <section className="umrah-plan-preview" aria-labelledby="umrah-plan-title">
            <div className="umrah-plan-preview-copy">
              <p className="plan-ahead-kicker">After you book</p>
              <h2 id="umrah-plan-title">One plan from booking to departure.</h2>
              <p>
                NoorPath stays useful after the first payment. Your journey view brings
                together what is paid, what is next, and what still needs your attention.
              </p>
              <a href="#packages">Start with a future departure →</a>
            </div>
            <div className="umrah-plan-card" aria-label="My Umrah Plan preview">
              <div className="umrah-plan-card-heading">
                <span>My Umrah Plan</span>
                <strong>From booking to travel</strong>
              </div>
              <div className="umrah-plan-card-items">
                {planPreviewItems.map((item) => (
                  <div key={item.label}>
                    <span aria-hidden="true" />
                    <div>
                      <strong>{item.label}</strong>
                      <p>{item.copy}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
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
