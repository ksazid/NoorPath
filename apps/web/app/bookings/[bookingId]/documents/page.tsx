"use client";
import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useState } from "react";
import { useDeferredInitialLoad } from "../../../../lib/use-deferred-initial-load";
import { PublicFooter, PublicHeader } from "../../../public-ui";
type Requirement = {
  id: string;
  kind: string;
  submission: null | {
    id: string;
    state: string;
    malwareStatus: string;
    reviewReason?: string;
  };
};
type Traveller = {
  travellerId: string;
  fullName: string;
  requirements: Requirement[];
};
type Model = { policyVersion: string; ready: boolean; travellers: Traveller[] };
const headers = (json = false): HeadersInit => ({
  ...(json ? { "Content-Type": "application/json" } : {}),
  ...(process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY
    ? {
        "X-NoorPath-Test-Identity":
          process.env.NEXT_PUBLIC_NOORPATH_TEST_IDENTITY,
      }
    : {}),
});
export default function DocumentsPage() {
  const { bookingId } = useParams<{ bookingId: string }>();
  const [state, setState] = useState<
    { kind: "loading" } | { kind: "error" } | { kind: "ready"; data: Model }
  >({ kind: "loading" });
  const [message, setMessage] = useState("");
  const load = useCallback(async () => {
    setState({ kind: "loading" });
    try {
      const r = await fetch(`/api/v1/bookings/${bookingId}/documents`, {
        credentials: "include",
        cache: "no-store",
        headers: headers(),
      });
      if (!r.ok) throw new Error();
      setState({ kind: "ready", data: (await r.json()) as Model });
    } catch {
      setState({ kind: "error" });
    }
  }, [bookingId]);
  useDeferredInitialLoad(load);
  async function upload(req: Requirement, file: File) {
    setMessage(`Preparing ${file.name}…`);
    try {
      const start = await fetch(
        `/api/v1/bookings/${bookingId}/documents/${req.id}/uploads`,
        {
          method: "POST",
          credentials: "include",
          headers: headers(true),
          body: JSON.stringify({ contentType: file.type, size: file.size }),
        },
      );
      if (!start.ok) throw new Error("Choose a PDF, JPEG or PNG up to 10 MB.");
      const body = (await start.json()) as {
        submissionId: string;
        uploadUrl: string;
      };
      const put = await fetch(body.uploadUrl, {
        method: "PUT",
        headers: {
          "Content-Type": file.type,
          "x-amz-server-side-encryption": "AES256",
        },
        body: file,
      });
      if (!put.ok) throw new Error("Upload was interrupted. Please retry.");
      setMessage("Upload complete. Running safety checks…");
      const complete = await fetch(
        `/api/v1/bookings/${bookingId}/documents/submissions/${body.submissionId}/complete`,
        { method: "POST", credentials: "include", headers: headers() },
      );
      if (!complete.ok)
        throw new Error(
          "The file is safely quarantined. Please retry with a valid file.",
        );
      setMessage("Safety checks passed. Your document is under review.");
      await load();
    } catch (e) {
      setMessage(
        e instanceof Error ? e.message : "Upload unavailable. Please retry.",
      );
    }
  }
  return (
    <div className="journey-page">
      <PublicHeader mode="detail" />
      <main id="main-content" className="journey-main">
        <nav className="package-breadcrumbs" aria-label="Breadcrumb">
          <Link href={`/bookings/${bookingId}/journey`}>My Journey</Link>
          <span>/</span>
          <span aria-current="page">Documents</span>
        </nav>
        <p className="public-eyebrow">Private document checklist</p>
        <h1>Traveller documents</h1>
        <p className="journey-intro">
          PDF, JPEG or PNG, up to 10 MB. Files stay private and unavailable
          during safety checks.
        </p>
        <div aria-live="polite">{message}</div>
        {state.kind === "loading" ? (
          <section className="journey-state">
            <h2>Loading requirements</h2>
            <p>Checking the latest document status.</p>
          </section>
        ) : null}
        {state.kind === "error" ? (
          <section className="journey-state">
            <h2>Documents temporarily unavailable</h2>
            <p>Your existing documents are unchanged.</p>
            <button onClick={() => void load()}>Retry</button>
          </section>
        ) : null}
        {state.kind === "ready" ? (
          <>
            {state.data.ready ? (
              <p className="document-help" role="status">
                All required documents are approved.
              </p>
            ) : null}
            <div className="documents-list">
              {state.data.travellers.map((t) => (
                <section className="documents-card" key={t.travellerId}>
                  <h2>{t.fullName}</h2>
                  {t.requirements.map((r) => (
                    <div className="document-row" key={r.id}>
                      <div>
                        <strong>
                          {r.kind === "PassportBioPage"
                            ? "Passport bio page"
                            : "Passport photo"}
                        </strong>
                        <p className="document-status">
                          {r.submission?.state ?? "Not submitted"}
                        </p>
                        {r.submission?.reviewReason ? (
                          <p>Operator note: {r.submission.reviewReason}</p>
                        ) : null}
                      </div>
                      <label>
                        Choose file{" "}
                        <input
                          type="file"
                          accept="application/pdf,image/jpeg,image/png"
                          onChange={(e) => {
                            const f = e.target.files?.[0];
                            if (f) void upload(r, f);
                          }}
                        />
                      </label>
                    </div>
                  ))}
                </section>
              ))}
            </div>
          </>
        ) : null}
        <aside className="document-help">
          Do not email passport files. If a safety check cannot finish, the file
          remains quarantined and no operator can view it.
        </aside>
      </main>
      <PublicFooter />
    </div>
  );
}
