import type { Metadata } from "next";
import {
  ActionButton,
  FeatureTile,
  SurfaceCard,
} from "../../_components/DesignSystem";
import { CustomerShell } from "../../_components/Shells";
import { requireDesignSystemShowcase } from "../requireShowcase";

export const metadata: Metadata = {
  title: "NoorPath Customer Shell",
  robots: { index: false, follow: false },
};

export default function CustomerShellExamplePage() {
  requireDesignSystemShowcase();

  return (
    <CustomerShell activePath="/#packages">
      <header className="np-showcase__header">
        <span className="np-showcase__eyebrow">Customer shell example</span>
        <h1>Your trusted Umrah journey starts here</h1>
        <p>
          This synthetic example validates the canonical public header, content
          rhythm and full customer footer without changing a live customer
          journey.
        </p>
        <div className="np-showcase__row">
          <ActionButton>Browse Packages</ActionButton>
          <ActionButton variant="secondary">Talk to Us</ActionButton>
        </div>
      </header>
      <section className="np-showcase__section" aria-labelledby="trust-title">
        <h2 id="trust-title">Clear journey information</h2>
        <div className="np-showcase__grid">
          <FeatureTile
            icon="verified"
            title="Verified Operator"
            description="Operator identity and package responsibility remain explicit."
          />
          <FeatureTile
            icon="payment"
            title="Journey Payment Schedule"
            description="The total, amount due now and remaining balance are distinct."
          />
          <FeatureTile
            icon="support"
            title="Human Support"
            description="WhatsApp support and callback access remain visible."
          />
        </div>
      </section>
      <section className="np-showcase__section" aria-labelledby="next-title">
        <h2 id="next-title">One clear next action</h2>
        <SurfaceCard>
          <p>
            Each customer screen identifies the current stage, the next action
            and what happens after completion.
          </p>
        </SurfaceCard>
      </section>
    </CustomerShell>
  );
}
