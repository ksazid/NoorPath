"use client";

import { useEffect } from "react";

type PartySummary = {
  id: string;
  status: string;
  version: number;
};

type PartyDetail = {
  party: PartySummary;
  members: { travellerId: string }[];
};

type QuoteResponse = { quoteId?: string };
type QuoteRequest = { travellerIds?: string[] };

function requestUrl(input: RequestInfo | URL) {
  if (typeof input === "string") return input;
  if (input instanceof URL) return input.toString();
  return input.url;
}

function testHeaders(json = false): HeadersInit {
  const headers: Record<string, string> = {};
  if (json) headers["Content-Type"] = "application/json";
  const identity = process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY;
  if (identity) headers["X-NoorPath-Test-Identity"] = identity;
  return headers;
}

function problem(status: number, title: string, detail: string) {
  return new Response(JSON.stringify({ title, detail, code: "family_party_required" }), {
    status,
    headers: { "Content-Type": "application/problem+json" },
  });
}

function sameMembers(left: string[], right: string[]) {
  return left.length === right.length && left.every((id) => right.includes(id));
}

export default function FamilyQuoteBridge() {
  useEffect(() => {
    const originalFetch = window.fetch.bind(window);
    const bridgedFetch: typeof window.fetch = async (input, init) => {
      const url = requestUrl(input);
      const method = (init?.method ?? (input instanceof Request ? input.method : "GET")).toUpperCase();
      const isQuoteCreation =
        method === "POST" &&
        /\/api\/v1\/departures\/[^/]+\/quotes(?:\?|$)/.test(url);
      if (!isQuoteCreation) return originalFetch(input, init);

      const quoteResponse = await originalFetch(input, init);
      if (!quoteResponse.ok) return quoteResponse;

      let quoteRequest: QuoteRequest;
      try {
        quoteRequest = JSON.parse(typeof init?.body === "string" ? init.body : "{}") as QuoteRequest;
      } catch {
        return quoteResponse;
      }
      const travellerIds = quoteRequest.travellerIds ?? [];
      if (travellerIds.length === 0) return quoteResponse;

      try {
        const partiesResponse = await originalFetch("/api/v1/family-parties", {
          credentials: "include",
          cache: "no-store",
          headers: testHeaders(),
        });
        if (!partiesResponse.ok) {
          return problem(
            503,
            "Family validation temporarily unavailable",
            "Your quote was not attached to a family party. Retry before securing availability.",
          );
        }
        const partiesBody = (await partiesResponse.json()) as { parties?: PartySummary[] };
        const validated = (partiesBody.parties ?? []).filter(
          (party) => party.status === "Validated",
        );
        if (validated.length === 0) return quoteResponse;

        const details = await Promise.all(
          validated.map(async (party) => {
            const response = await originalFetch(`/api/v1/family-parties/${party.id}`, {
              credentials: "include",
              cache: "no-store",
              headers: testHeaders(),
            });
            return response.ok ? ((await response.json()) as PartyDetail) : null;
          }),
        );
        const matches = details.filter(
          (detail): detail is PartyDetail =>
            detail !== null &&
            sameMembers(
              detail.members.map((member) => member.travellerId),
              travellerIds,
            ),
        );
        if (matches.length === 0) {
          return problem(
            409,
            "Validate the selected family travellers",
            "The quote travellers do not match a validated family party. Review Family travellers and try again.",
          );
        }
        if (matches.length > 1) {
          return problem(
            409,
            "Choose one family party",
            "More than one validated family party matches these travellers. Keep one active party for this booking and retry.",
          );
        }

        const quote = (await quoteResponse.clone().json()) as QuoteResponse;
        if (!quote.quoteId) return quoteResponse;
        const match = matches[0];
        const bindResponse = await originalFetch(
          `/api/v1/family-parties/${match.party.id}/quotes/${quote.quoteId}/snapshot`,
          {
            method: "POST",
            credentials: "include",
            headers: testHeaders(true),
            body: JSON.stringify({ version: match.party.version }),
          },
        );
        if (!bindResponse.ok) {
          return new Response(await bindResponse.text(), {
            status: bindResponse.status,
            headers: {
              "Content-Type":
                bindResponse.headers.get("content-type") ?? "application/problem+json",
            },
          });
        }
        return quoteResponse;
      } catch {
        return problem(
          503,
          "Family validation temporarily unavailable",
          "Your quote was not attached to a family party. Retry before securing availability.",
        );
      }
    };

    window.fetch = bridgedFetch;
    return () => {
      if (window.fetch === bridgedFetch) window.fetch = originalFetch;
    };
  }, []);

  return null;
}
