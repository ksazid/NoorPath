import type { Metadata } from "next";
import {
  ActionButton,
  StatusBadge,
  SurfaceCard,
} from "../../_components/DesignSystem";
import { StaffShell } from "../../_components/Shells";

export const metadata: Metadata = {
  title: "NoorPath Staff Shell",
  robots: { index: false, follow: false },
};

const navigation = [
  {
    label: "Overview",
    items: [{ href: "/operator", label: "Dashboard" }],
  },
  {
    label: "Content",
    items: [{ href: "/operator/packages", label: "Packages" }],
  },
  {
    label: "Operations",
    items: [
      { href: "/operator/bookings", label: "Bookings" },
      { href: "/operator/departures", label: "Departures" },
      { href: "/operator/documents", label: "Documents" },
      { href: "/operator/visa", label: "Visa Cases" },
      { href: "/operator/support", label: "Support" },
    ],
  },
  {
    label: "Administration",
    items: [
      { href: "/operator/team", label: "Team" },
      { href: "/operator/audit", label: "Audit Log" },
      { href: "/operator/settings", label: "Settings" },
    ],
  },
];

export default function StaffShellExamplePage() {
  return (
    <StaffShell
      activePath="/operator"
      headerActions={<ActionButton variant="secondary">Profile</ActionButton>}
      navigation={navigation}
      operatorName="NoorPath Test Operator"
      search={
        <input
          aria-label="Search staff workspace"
          placeholder="Search bookings or customers"
          type="search"
        />
      }
      title="Operations dashboard"
    >
      <div className="np-showcase__grid">
        <SurfaceCard>
          <StatusBadge tone="warning">4 actions required</StatusBadge>
          <h2>Documents awaiting review</h2>
          <p>Review the oldest customer submissions first.</p>
        </SurfaceCard>
        <SurfaceCard>
          <StatusBadge tone="info">12 upcoming</StatusBadge>
          <h2>Departures</h2>
          <p>Capacity, payment and document readiness remain visible together.</p>
        </SurfaceCard>
        <SurfaceCard>
          <StatusBadge tone="success">Healthy</StatusBadge>
          <h2>Published packages</h2>
          <p>Only approved and truthful customer projections are published.</p>
        </SurfaceCard>
      </div>
    </StaffShell>
  );
}
