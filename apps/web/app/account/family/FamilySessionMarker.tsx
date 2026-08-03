"use client";

import { useEffect } from "react";

export default function FamilySessionMarker() {
  useEffect(() => {
    window.sessionStorage.setItem("noorpath:family-booking-session", "active");
  }, []);

  return null;
}
