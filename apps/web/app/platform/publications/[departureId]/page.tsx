import PublicationReview from "../../../operator/departures/PublicationReview";

export default async function PlatformPublicationReviewPage({
  params,
}: {
  params: Promise<{ departureId: string }>;
}) {
  const { departureId } = await params;
  return <PublicationReview departureId={departureId} mode="platform" />;
}
