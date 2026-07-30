import type { Metadata } from "next";
import type { ReactNode } from "react";
import "./styles.css";
import "./composer.css";
import "./public.css";

export const metadata: Metadata = {
  title: "NoorPath",
  description: "A trusted path for your Umrah journey.",
};

export default function RootLayout({
  children,
}: Readonly<{ children: ReactNode }>) {
  return (
    <html lang="en">
      <body>{children}</body>
    </html>
  );
}
