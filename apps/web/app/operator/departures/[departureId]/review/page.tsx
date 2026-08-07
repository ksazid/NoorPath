import Link from "next/link";
import PublicationReview from "../../PublicationReview";

export default async function OperatorPublicationReviewPage({
  params,
}: {
  params: Promise<{ departureId: string }>;
}) {
  const { departureId } = await params;

  return (
    <>
      <div className="operator-preview-bar operator-review-guidance">
        <div>
          <strong>Publication review</strong>
          <span>
            Submit only after customer preview is accurate. Platform approval is
            not instant; allow at least 24 hours before the intended go-live time.
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
    </>
  );
}
