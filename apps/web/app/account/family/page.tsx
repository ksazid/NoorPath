"use client";

import Link from "next/link";
import { FormEvent, useCallback, useMemo, useState } from "react";
import { useDeferredInitialLoad } from "../../../lib/use-deferred-initial-load";
import { PublicFooter, PublicHeader } from "../../public-ui";
import styles from "./page.module.css";

type Traveller = { id: string; fullName: string; dateOfBirth: string };
type PartySummary = {
  id: string;
  name: string;
  status: string;
  policyVersion: string;
  version: number;
  updatedAtUtc: string;
};
type PartyMember = { travellerId: string; version: number; addedAtUtc: string };
type MahramLink = {
  id: string;
  protectedTravellerId: string;
  mahramTravellerId: string;
  relationshipType: string;
  declaration: string;
  version: number;
  updatedAtUtc: string;
};
type PartyDetail = {
  party: PartySummary;
  members: PartyMember[];
  mahramLinks: MahramLink[];
};
type PageState =
  | { kind: "loading" }
  | { kind: "error" }
  | {
      kind: "ready";
      travellers: Traveller[];
      parties: PartySummary[];
      detail: PartyDetail | null;
    };

const relationshipTypes = [
  "Father",
  "Son",
  "Brother",
  "Husband",
  "PaternalUncle",
  "MaternalUncle",
  "Nephew",
  "Grandfather",
  "Grandson",
  "OtherDeclaredRelationship",
] as const;

const requestHeaders = (json = false): HeadersInit => ({
  ...(json ? { "Content-Type": "application/json" } : {}),
  ...(process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY
    ? {
        "X-NoorPath-Test-Identity":
          process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY,
      }
    : {}),
});

async function readProblem(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as {
      message?: string;
      detail?: string;
      title?: string;
    };
    return (
      body.message ??
      body.detail ??
      body.title ??
      "The request could not be completed."
    );
  } catch {
    return "The request could not be completed.";
  }
}

export default function FamilyTravellersPage() {
  const [state, setState] = useState<PageState>({ kind: "loading" });
  const [selectedPartyId, setSelectedPartyId] = useState<string | null>(null);
  const [partyName, setPartyName] = useState("My family");
  const [selectedTravellerId, setSelectedTravellerId] = useState("");
  const [protectedTravellerId, setProtectedTravellerId] = useState("");
  const [mahramTravellerId, setMahramTravellerId] = useState("");
  const [relationshipType, setRelationshipType] =
    useState<(typeof relationshipTypes)[number]>("Father");
  const [declaration, setDeclaration] = useState("");
  const [message, setMessage] = useState<{
    kind: "error" | "success";
    text: string;
  } | null>(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(
    async (preferredPartyId?: string) => {
      setState({ kind: "loading" });
      setMessage(null);
      try {
        const [travellersResponse, partiesResponse] = await Promise.all([
          fetch("/api/v1/travellers", {
            credentials: "include",
            cache: "no-store",
            headers: requestHeaders(),
          }),
          fetch("/api/v1/family-parties", {
            credentials: "include",
            cache: "no-store",
            headers: requestHeaders(),
          }),
        ]);
        if (!travellersResponse.ok || !partiesResponse.ok) throw new Error();
        const travellersBody = (await travellersResponse.json()) as {
          items: Traveller[];
        };
        const partiesBody = (await partiesResponse.json()) as {
          parties: PartySummary[];
        };
        const partyId =
          preferredPartyId ??
          selectedPartyId ??
          partiesBody.parties[0]?.id ??
          null;
        let detail: PartyDetail | null = null;
        if (partyId) {
          const detailResponse = await fetch(
            `/api/v1/family-parties/${partyId}`,
            {
              credentials: "include",
              cache: "no-store",
              headers: requestHeaders(),
            },
          );
          if (!detailResponse.ok) throw new Error();
          detail = (await detailResponse.json()) as PartyDetail;
        }
        setSelectedPartyId(partyId);
        setState({
          kind: "ready",
          travellers: travellersBody.items,
          parties: partiesBody.parties,
          detail,
        });
      } catch {
        setState({ kind: "error" });
      }
    },
    [selectedPartyId],
  );

  useDeferredInitialLoad(() => load());

  const current = state.kind === "ready" ? state.detail : null;
  const travellerById = useMemo(
    () =>
      new Map(
        state.kind === "ready"
          ? state.travellers.map((traveller) => [traveller.id, traveller])
          : [],
      ),
    [state],
  );
  const activeMemberIds = new Set(
    current?.members.map((member) => member.travellerId) ?? [],
  );
  const availableTravellers =
    state.kind === "ready"
      ? state.travellers.filter(
          (traveller) => !activeMemberIds.has(traveller.id),
        )
      : [];
  const memberTravellers =
    current?.members
      .map((member) => travellerById.get(member.travellerId))
      .filter((traveller): traveller is Traveller => Boolean(traveller)) ?? [];

  async function mutate(url: string, body: object, successMessage: string) {
    setBusy(true);
    setMessage(null);
    try {
      const response = await fetch(url, {
        method: "POST",
        credentials: "include",
        headers: requestHeaders(true),
        body: JSON.stringify(body),
      });
      if (!response.ok) throw new Error(await readProblem(response));
      setMessage({ kind: "success", text: successMessage });
      await load(selectedPartyId ?? undefined);
      return true;
    } catch (error) {
      setMessage({
        kind: "error",
        text:
          error instanceof Error
            ? error.message
            : "The request could not be completed.",
      });
      return false;
    } finally {
      setBusy(false);
    }
  }

  async function createParty(event: FormEvent) {
    event.preventDefault();
    setBusy(true);
    setMessage(null);
    try {
      const response = await fetch("/api/v1/family-parties", {
        method: "POST",
        credentials: "include",
        headers: requestHeaders(true),
        body: JSON.stringify({ name: partyName }),
      });
      if (!response.ok) throw new Error(await readProblem(response));
      const created = (await response.json()) as { id: string };
      setSelectedPartyId(created.id);
      setMessage({
        kind: "success",
        text: "Family party created. Add the travellers who will book together.",
      });
      await load(created.id);
    } catch (error) {
      setMessage({
        kind: "error",
        text:
          error instanceof Error
            ? error.message
            : "The family party could not be created.",
      });
    } finally {
      setBusy(false);
    }
  }

  async function addMember(event: FormEvent) {
    event.preventDefault();
    if (!current || !selectedTravellerId) return;
    const ok = await mutate(
      `/api/v1/family-parties/${current.party.id}/members`,
      { travellerId: selectedTravellerId, version: current.party.version },
      "Traveller added to the family party.",
    );
    if (ok) setSelectedTravellerId("");
  }

  async function addLink(event: FormEvent) {
    event.preventDefault();
    if (!current || !protectedTravellerId || !mahramTravellerId) return;
    const ok = await mutate(
      `/api/v1/family-parties/${current.party.id}/mahram-links`,
      {
        protectedTravellerId,
        mahramTravellerId,
        relationshipType,
        declaration,
        version: current.party.version,
      },
      "Mahram relationship recorded. Validate the party when all relationships are complete.",
    );
    if (ok) {
      setProtectedTravellerId("");
      setMahramTravellerId("");
      setDeclaration("");
    }
  }

  return (
    <div className="journey-page">
      <PublicHeader mode="detail" />
      <main id="main-content" className="journey-main">
        <nav className="package-breadcrumbs" aria-label="Breadcrumb">
          <Link href="/account">My NoorPath</Link>
          <span>/</span>
          <span aria-current="page">Family travellers</span>
        </nav>
        <p className="public-eyebrow">Private family booking</p>
        <h1>Family travellers</h1>
        <p className="journey-intro">
          Build the group travelling together and record declared Mahram
          relationships. NoorPath checks structure and ownership but does not
          issue a religious or legal ruling.
        </p>

        {message ? (
          <div
            className={message.kind === "error" ? styles.error : styles.success}
            role={message.kind === "error" ? "alert" : "status"}
          >
            {message.text}
          </div>
        ) : null}

        {state.kind === "loading" ? (
          <section className="journey-state" aria-busy="true">
            <h2>Loading family travellers</h2>
            <p>Checking your private traveller profiles and saved parties.</p>
          </section>
        ) : null}
        {state.kind === "error" ? (
          <section className="journey-state">
            <h2>Family travellers are temporarily unavailable</h2>
            <p>
              Your existing traveller and relationship records are unchanged.
            </p>
            <button onClick={() => void load()}>Retry</button>
          </section>
        ) : null}

        {state.kind === "ready" && !state.detail ? (
          <section className={styles.panel}>
            <h2>Create your family party</h2>
            <p className={styles.muted}>
              Start with a named travel group, then add existing account-owned
              travellers.
            </p>
            <form className={styles.form} onSubmit={createParty}>
              <label className={styles.field}>
                Party name
                <input
                  value={partyName}
                  maxLength={100}
                  onChange={(event) => setPartyName(event.target.value)}
                  required
                />
              </label>
              <button className={styles.button} disabled={busy}>
                Create family party
              </button>
            </form>
          </section>
        ) : null}

        {state.kind === "ready" && state.detail ? (
          <div className={styles.layout}>
            <div>
              {state.parties.length > 1 ? (
                <section className={styles.panel}>
                  <label className={styles.field}>
                    Family party
                    <select
                      value={state.detail.party.id}
                      onChange={(event) => {
                        setSelectedPartyId(event.target.value);
                        void load(event.target.value);
                      }}
                    >
                      {state.parties.map((party) => (
                        <option key={party.id} value={party.id}>
                          {party.name}
                        </option>
                      ))}
                    </select>
                  </label>
                </section>
              ) : null}

              <section className={styles.panel}>
                <h2>Travellers in this party</h2>
                <p className={styles.muted}>
                  Only traveller profiles owned by this account can be added.
                </p>
                {memberTravellers.length === 0 ? (
                  <p className={styles.notice}>No travellers added yet.</p>
                ) : (
                  <ul className={styles.list}>
                    {memberTravellers.map((traveller) => (
                      <li className={styles.item} key={traveller.id}>
                        <div>
                          <strong>{traveller.fullName}</strong>
                          <span>Date of birth: {traveller.dateOfBirth}</span>
                        </div>
                        <button
                          className={styles.danger}
                          disabled={busy}
                          onClick={() =>
                            void mutate(
                              `/api/v1/family-parties/${state.detail!.party.id}/members/${traveller.id}/remove`,
                              { version: state.detail!.party.version },
                              `${traveller.fullName} removed from the party.`,
                            )
                          }
                        >
                          Remove
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
                {availableTravellers.length > 0 ? (
                  <form className={styles.form} onSubmit={addMember}>
                    <label className={styles.field}>
                      Add traveller
                      <select
                        value={selectedTravellerId}
                        onChange={(event) =>
                          setSelectedTravellerId(event.target.value)
                        }
                        required
                      >
                        <option value="">Choose a traveller</option>
                        {availableTravellers.map((traveller) => (
                          <option key={traveller.id} value={traveller.id}>
                            {traveller.fullName}
                          </option>
                        ))}
                      </select>
                    </label>
                    <button
                      className={styles.button}
                      disabled={busy || !selectedTravellerId}
                    >
                      Add traveller
                    </button>
                  </form>
                ) : state.travellers.length === 0 ? (
                  <p className={styles.notice}>
                    Create traveller profiles from your account before building
                    a family party.
                  </p>
                ) : null}
              </section>

              <section className={styles.panel}>
                <h2>Mahram relationships</h2>
                <p className={styles.muted}>
                  Record a directed relationship between two travellers in this
                  same party. Supporting document verification is outside this
                  slice.
                </p>
                {state.detail.mahramLinks.length === 0 ? (
                  <p className={styles.notice}>
                    No Mahram relationships recorded.
                  </p>
                ) : (
                  <ul className={styles.list}>
                    {state.detail.mahramLinks.map((link) => (
                      <li className={styles.item} key={link.id}>
                        <div>
                          <strong>
                            {travellerById.get(link.protectedTravellerId)
                              ?.fullName ?? "Traveller"}{" "}
                            →{" "}
                            {travellerById.get(link.mahramTravellerId)
                              ?.fullName ?? "Mahram"}
                          </strong>
                          <span>
                            {link.relationshipType
                              .replaceAll(/([A-Z])/g, " $1")
                              .trim()}{" "}
                            · {link.declaration}
                          </span>
                        </div>
                        <button
                          className={styles.danger}
                          disabled={busy}
                          onClick={() =>
                            void mutate(
                              `/api/v1/family-parties/${state.detail!.party.id}/mahram-links/${link.id}/remove`,
                              { version: state.detail!.party.version },
                              "Mahram relationship removed.",
                            )
                          }
                        >
                          Remove
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
                {memberTravellers.length >= 2 ? (
                  <form className={styles.form} onSubmit={addLink}>
                    <div className={styles.row}>
                      <label className={styles.field}>
                        Traveller
                        <select
                          value={protectedTravellerId}
                          onChange={(event) =>
                            setProtectedTravellerId(event.target.value)
                          }
                          required
                        >
                          <option value="">Choose traveller</option>
                          {memberTravellers.map((traveller) => (
                            <option key={traveller.id} value={traveller.id}>
                              {traveller.fullName}
                            </option>
                          ))}
                        </select>
                      </label>
                      <label className={styles.field}>
                        Mahram
                        <select
                          value={mahramTravellerId}
                          onChange={(event) =>
                            setMahramTravellerId(event.target.value)
                          }
                          required
                        >
                          <option value="">Choose Mahram</option>
                          {memberTravellers.map((traveller) => (
                            <option key={traveller.id} value={traveller.id}>
                              {traveller.fullName}
                            </option>
                          ))}
                        </select>
                      </label>
                    </div>
                    <label className={styles.field}>
                      Relationship
                      <select
                        value={relationshipType}
                        onChange={(event) =>
                          setRelationshipType(
                            event.target
                              .value as (typeof relationshipTypes)[number],
                          )
                        }
                      >
                        {relationshipTypes.map((type) => (
                          <option key={type} value={type}>
                            {type.replaceAll(/([A-Z])/g, " $1").trim()}
                          </option>
                        ))}
                      </select>
                    </label>
                    <label className={styles.field}>
                      Customer declaration
                      <textarea
                        value={declaration}
                        maxLength={500}
                        onChange={(event) => setDeclaration(event.target.value)}
                        placeholder="I confirm that the relationship entered above is accurate."
                        required
                      />
                    </label>
                    <button
                      className={styles.button}
                      disabled={
                        busy || protectedTravellerId === mahramTravellerId
                      }
                    >
                      Record relationship
                    </button>
                  </form>
                ) : null}
              </section>
            </div>

            <aside className={styles.summary} aria-label="Family party summary">
              <span className={styles.badge}>{state.detail.party.status}</span>
              <h2>{state.detail.party.name}</h2>
              <p className={styles.muted}>
                Validation is reset whenever a traveller or relationship
                changes.
              </p>
              <dl>
                <div>
                  <dt>Travellers</dt>
                  <dd>{state.detail.members.length}</dd>
                </div>
                <div>
                  <dt>Mahram links</dt>
                  <dd>{state.detail.mahramLinks.length}</dd>
                </div>
                <div>
                  <dt>Policy version</dt>
                  <dd>{state.detail.party.policyVersion}</dd>
                </div>
              </dl>
              <div className={styles.form}>
                <button
                  className={styles.button}
                  disabled={busy || state.detail.members.length === 0}
                  onClick={() =>
                    void mutate(
                      `/api/v1/family-parties/${state.detail!.party.id}/validate`,
                      { version: state.detail!.party.version },
                      "Family party validated and ready to use in quote and booking.",
                    )
                  }
                >
                  Validate party
                </button>
                <Link className={styles.secondary} href="/">
                  Browse packages
                </Link>
              </div>
            </aside>
          </div>
        ) : null}
      </main>
      <PublicFooter />
    </div>
  );
}
