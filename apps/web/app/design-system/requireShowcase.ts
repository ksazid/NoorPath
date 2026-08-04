import { notFound } from "next/navigation";

export function requireDesignSystemShowcase() {
  if (
    process.env.NODE_ENV === "production" &&
    process.env.NOORPATH_ENABLE_DESIGN_SYSTEM_SHOWCASE !== "true"
  ) {
    notFound();
  }
}
