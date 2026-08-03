import type { ReactNode } from "react";
import FamilySessionMarker from "./FamilySessionMarker";

export default function FamilyLayout({ children }: { children: ReactNode }) {
  return (
    <>
      <FamilySessionMarker />
      {children}
    </>
  );
}
