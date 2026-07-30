import PublicationReview from "../../PublicationReview";

export default async function OperatorPublicationReviewPage({
  params,
}: {
  params: Promise<{ departureId: string }>;
}) {
  const { departureId } = await params;
  return <PublicationReview departureId={departureId} mode="operator" />;
}
