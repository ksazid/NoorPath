import type { Metadata } from "next";
import type { ReactNode } from "react";
import "@noorpath/design-tokens/tokens.css";
import FamilyQuoteBridge from "./FamilyQuoteBridge";
import "./styles.css";
import "./design-system.css";
import "./primitives.css";
import "./shells.css";
import "./composer.css";
import "./commercial.css";
import "./payment-plan.css";
import "./publication.css";
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
        {children}
      </body>
    </html>
  );
}
