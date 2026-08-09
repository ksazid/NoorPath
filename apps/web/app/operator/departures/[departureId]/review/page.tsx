import Link from "next/link";
import OperatorWorkspaceShell from "../../../OperatorWorkspaceShell";
import PublicationReview from "../../PublicationReview";

export default async function OperatorPublicationReviewPage({
  params,
}: {
  params: Promise<{ departureId: string }>;
}) {
  const { departureId } = await params;

  return (
    <OperatorWorkspaceShell
      title="Publication review"
      summary="Review the saved customer facts and submit them from the same operator workspace used for package and departure operations."
      contentOwnsLandmark
      showPageHeader={false}
      contentClassName="np-operator-legacy-embed"
    >
      <div className="operator-preview-bar operator-review-guidance">
        <div>
          <strong>Publication review</strong>
          <span>
            Submit only after customer preview is accurate. Platform approval is
            not instant; allow at least 24 hours before the intended go-live
            time.
          </span>
        </div>
        <div>
          <Link
            className="secondary-button"
            href={`/operator/departures/${departureId}/preview`}
          >
            Open customer preview
          </Link>
          <Link
            className="secondary-button"
            href={`/operator/departures/${departureId}`}
          >
            Back to draft
          </Link>
        </div>
      </div>
      <PublicationReview departureId={departureId} mode="operator" />
    </OperatorWorkspaceShell>
  );
}
