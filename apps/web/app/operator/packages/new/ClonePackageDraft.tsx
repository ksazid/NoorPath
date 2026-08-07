"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { normalizePackageItems } from "../packageDraftStandards";

type DraftResponse = {
  departureId: string;
  packageName?: string;
  summary?: string;
  makkah?: unknown;
  madinah?: unknown;
  travel?: unknown;
  origin?: string;
  departureDate?: string;
  returnDate?: string;
  inclusions?: string[];
  exclusions?: string[];
};

type CloneState = "cloning" | "failed";

export default function ClonePackageDraft({ sourceDepartureId }: { sourceDepartureId: string }) {
  const router = useRouter();
  const started = useRef(false);
  const [state, setState] = useState<CloneState>("cloning");

  useEffect(() => {
    if (started.current) return;
    started.current = true;

    const clone = async () => {
      try {
        const sourceResponse = await fetch(
          `/api/v1/operator/departures/${encodeURIComponent(sourceDepartureId)}`,
          { credentials: "include", cache: "no-store" },
        );
        if (!sourceResponse.ok) throw new Error("source unavailable");
        const source = (await sourceResponse.json()) as DraftResponse;

        const createResponse = await fetch("/api/v1/operator/departures", {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            packageName: `${source.packageName ?? "Umrah package"} — Copy`,
            summary: source.summary ?? "",
            makkah: source.makkah,
            madinah: source.madinah,
            travel: source.travel,
            origin: source.origin ?? "",
            departureDate: source.departureDate ?? "",
            returnDate: source.returnDate ?? "",
            inclusions: normalizePackageItems(source.inclusions),
            exclusions: normalizePackageItems(source.exclusions),
          }),
        });
        if (!createResponse.ok) throw new Error("clone failed");
        const created = (await createResponse.json()) as DraftResponse;
        router.replace(`/operator/departures/${created.departureId}`);
      } catch {
        setState("failed");
      }
    };

    void clone();
  }, [router, sourceDepartureId]);

  return (
    <main className="composer-state-page">
      <section className="composer-state-card" role="status" aria-live="polite">
        <span className="auth-eyebrow">Operator catalogue</span>
        <h1>{state === "cloning" ? "Creating package copy" : "Package could not be copied"}</h1>
        <p>
          {state === "cloning"
            ? "We are creating a new private draft from the selected package. The original remains unchanged."
            : "The source package may be unavailable or outside your operator scope."}
        </p>
        {state === "failed" ? (
          <div>
            <button className="primary-button" type="button" onClick={() => window.location.reload()}>
              Retry
            </button>
            <Link className="secondary-button" href="/operator/packages">
              Back to packages
            </Link>
          </div>
        ) : null}
      </section>
    </main>
  );
}
