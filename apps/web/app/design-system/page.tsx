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
import {
  SkeletonBlock,
  SupportAction,
  TextField,
} from "../_components/FormPrimitives";
import {
  IconButton,
  MetricCard,
  StickyActionBar,
} from "../_components/InteractionPrimitives";
import {
  CheckboxField,
  RadioCard,
  SelectField,
  ToggleField,
} from "../_components/SelectionPrimitives";
import { requireDesignSystemShowcase } from "./requireShowcase";

export const metadata: Metadata = {
  title: "NoorPath Design System",
  robots: { index: false, follow: false },
};

export default function DesignSystemPage() {
  requireDesignSystemShowcase();

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
          <IconButton icon="search" label="Search packages" />
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

      <section className="np-showcase__section" aria-labelledby="metrics-title">
        <h2 id="metrics-title">Commercial metrics</h2>
        <div className="np-showcase__grid">
          <MetricCard
            label="Total package price"
            value="₹1,00,000"
            description="Per pilgrim for Triple Sharing"
          />
          <MetricCard
            label="Reserve today"
            value="₹20,000"
            description="Due to reserve the selected seats"
          />
          <MetricCard
            label="Remaining journey balance"
            value="₹80,000"
            description="Shown with the full payment schedule"
          />
        </div>
      </section>

      <section
        className="np-showcase__section"
        aria-labelledby="features-title"
      >
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

      <section
        className="np-showcase__section"
        aria-labelledby="occupancy-title"
      >
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

      <section className="np-showcase__section" aria-labelledby="forms-title">
        <h2 id="forms-title">Forms and support</h2>
        <div className="np-showcase__grid">
          <SurfaceCard>
            <TextField
              id="traveller-name"
              label="Traveller name"
              description="Enter the name exactly as shown on the passport."
              placeholder="Full name"
            />
          </SurfaceCard>
          <SupportAction
            href="/support"
            icon="whatsapp"
            title="WhatsApp Support"
            description="Chat with NoorPath for help with your journey."
          />
          <SupportAction
            href="/support"
            icon="phone"
            title="Request a Callback"
            description="Ask the support team to call you."
          />
        </div>
      </section>

      <section
        className="np-showcase__section"
        aria-labelledby="selection-title"
      >
        <h2 id="selection-title">Selection controls</h2>
        <div className="np-showcase__grid">
          <SurfaceCard>
            <SelectField
              id="payment-choice"
              label="Journey payment option"
              description="The full schedule is shown before reservation."
              defaultValue="milestones"
              options={[
                { label: "Full Payment at Booking", value: "full" },
                {
                  label: "Reservation + One Remaining Payment",
                  value: "remaining",
                },
                {
                  label: "Reservation + Payment Milestones",
                  value: "milestones",
                },
              ]}
            />
          </SurfaceCard>
          <SurfaceCard>
            <fieldset className="np-selection-group">
              <legend>Room preference</legend>
              <RadioCard name="room-preference" value="double">
                Double Sharing
              </RadioCard>
              <RadioCard defaultChecked name="room-preference" value="triple">
                Triple Sharing
              </RadioCard>
            </fieldset>
          </SurfaceCard>
          <SurfaceCard>
            <div className="np-selection-group">
              <CheckboxField
                defaultChecked
                label="Send journey updates"
                description="Receive important booking and departure updates."
              />
              <ToggleField
                label="Show completed milestones"
                description="Keep completed stages visible in My Journey."
              />
            </div>
          </SurfaceCard>
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

      <section className="np-showcase__section" aria-labelledby="loading-title">
        <h2 id="loading-title">Loading structure</h2>
        <SurfaceCard aria-label="Package card loading example">
          <div className="np-showcase__skeleton">
            <SkeletonBlock height="11rem" />
            <SkeletonBlock width="38%" />
            <SkeletonBlock height="2rem" width="72%" />
            <SkeletonBlock width="100%" />
            <SkeletonBlock width="84%" />
          </div>
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

      <StickyActionBar
        summary={
          <>
            <strong>₹20,000 due now</strong>
            <span>Remaining journey balance ₹80,000</span>
          </>
        }
        support={<IconButton icon="phone" label="Request a callback" />}
        action={<ActionButton>Reserve Your Seats</ActionButton>}
      />
    </main>
  );
}
