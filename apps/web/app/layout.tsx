import type { Metadata } from "next";
import type { ReactNode } from "react";
import "@noorpath/design-tokens/tokens.css";
import CustomerRouteShell from "./CustomerRouteShell";
import FamilyQuoteBridge from "./FamilyQuoteBridge";
import "./styles.css";
import "./design-system.css";
import "./primitives.css";
import "./interaction-primitives.css";
import "./shells.css";
import "./account-identity-menu.css";
import "./shell-slots.css";
import "./customer-route-shell.css";
import "./composer.css";
import "./package-draft-refinement.css";
import "./commercial.css";
import "./commercial-refinement.css";
import "./payment-plan.css";
import "./payment-plan-refinement.css";
import "./publication.css";
import "./preview-publication-refinement.css";
import "./platform-publication-refinement.css";
import "./public.css";
import "./discovery.css";
import "./package-details.css";
import "./refinement.css";
import "./plan-early.css";
import "./vs07-entry.css";
import "./plan.css";
import "./plan-hardening.css";
import "./inventory-hold.css";
import "./booking-payment.css";
import "./account.css";
import "./operator.css";
import "./operator-package-management.css";
import "./operator-booking-management.css";
import "./operator-booking-detail.css";
import "./operator-booking-amendment.css";
import "./operator-accommodation.css";
import "./my-journey.css";
import "./documents.css";

export const metadata: Metadata = {
  title: "NoorPath",
  description: "A trusted path for your Umrah journey.",
};

export default function RootLayout({
  children,
}: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="en">
      <body>
        <FamilyQuoteBridge />
        <CustomerRouteShell>{children}</CustomerRouteShell>
      </body>
    </html>
  );
}
