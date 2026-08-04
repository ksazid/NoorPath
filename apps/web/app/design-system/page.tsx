import type { Metadata } from "next";
import {
  ActionButton,
  FeatureTile,
  OccupancyCard,
  StatePanel,
  StatusBadge,
  SurfaceCard,
  TimelineItem,
} from "../_components/DesignSystem";

export const metadata: Metadata = {
  title: "NoorPath Design System",
  robots: { index: false, follow: false },
};

export default function DesignSystemPage() {
  return (
    <main className="np-showcase" id="main-content">
      <header className="np-showcase__header">
        <span className="np-showcase__eyebrow">VS-18 foundation</span>
        <h1>NoorPath component foundation</h1>
        <p>
          A non-production showcase of the shared tokens, icons, controls,
          occupancy language, status treatment and journey-state patterns used
          across customer and staff surfaces.
        </p>
      </header>

      <section className="np-showcase__section" aria-labelledby="actions-title">
        <h2 id="actions-title">Actions</h2>
        <div className="np-showcase__row">
          <ActionButton>Reserve Your Seats</ActionButton>
          <ActionButton variant="secondary">Request a Callback</ActionButton>
          <ActionButton variant="tertiary">View details</ActionButton>
          <ActionButton variant="destructive">Cancel request</ActionButton>
          <ActionButton pending>Saving</ActionButton>
          <ActionButton disabled>Unavailable</ActionButton>
        </div>
      </section>

      <section className="np-showcase__section" aria-labelledby="status-title">
        <h2 id="status-title">Statuses</h2>
        <div className="np-showcase__row">
          <StatusBadge tone="success">Completed</StatusBadge>
          <StatusBadge tone="warning">Action required</StatusBadge>
          <StatusBadge tone="danger">Rejected</StatusBadge>
          <StatusBadge tone="info">Waiting for operator</StatusBadge>
          <StatusBadge>Not started</StatusBadge>
        </div>
      </section>

      <section className="np-showcase__section" aria-labelledby="features-title">
        <h2 id="features-title">Package features</h2>
        <div className="np-showcase__grid">
          <FeatureTile
            icon="plane"
            title="Return Flights"
            description="Flight details remain explicit as confirmed or pending."
          />
          <FeatureTile
            icon="passport"
            title="Umrah Visa Included"
            description="The package inclusion is stated without ambiguous assistance wording."
          />
          <FeatureTile
            icon="meal"
            title="Daily Meals Included"
            description="Breakfast, lunch and dinner are shown with applicable days."
          />
        </div>
      </section>

      <section className="np-showcase__section" aria-labelledby="occupancy-title">
        <h2 id="occupancy-title">Room occupancy</h2>
        <div className="np-showcase__grid">
          <OccupancyCard
            count={2}
            title="Double Sharing"
            description="2 pilgrims sharing one room"
          />
          <OccupancyCard
            count={3}
            title="Triple Sharing"
            description="3 pilgrims sharing one room"
            selected
          />
          <OccupancyCard
            count={4}
            title="Quad Sharing"
            description="4 pilgrims sharing one room"
          />
        </div>
      </section>

      <section className="np-showcase__section" aria-labelledby="journey-title">
        <h2 id="journey-title">Journey progress</h2>
        <SurfaceCard>
          <ol className="np-timeline">
            <TimelineItem label="Seats reserved" status="completed">
              Reservation reference NPT-SAMPLE
            </TimelineItem>
            <TimelineItem label="Traveller details" status="current">
              Add the remaining traveller information.
            </TimelineItem>
            <TimelineItem label="Passport review" status="action-required">
              One passport image must be replaced.
            </TimelineItem>
            <TimelineItem label="Visa processing" status="upcoming">
              Starts after document approval.
            </TimelineItem>
          </ol>
        </SurfaceCard>
      </section>

      <section className="np-showcase__section" aria-labelledby="states-title">
        <h2 id="states-title">System states</h2>
        <div className="np-showcase__grid">
          <StatePanel
            kind="loading"
            title="Loading your journey"
            description="Please wait while NoorPath retrieves the latest status."
          />
          <StatePanel
            kind="empty"
            title="No journey yet"
            description="Your confirmed Umrah journey will appear here after reservation."
            action={<ActionButton>Browse packages</ActionButton>}
          />
          <StatePanel
            kind="error"
            title="We could not load this"
            description="Your information is safe. Check your connection and try again."
            action={<ActionButton>Try again</ActionButton>}
          />
        </div>
      </section>
    </main>
  );
}
