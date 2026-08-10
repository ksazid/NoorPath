import PlatformAdminWorkspaceShell from "../../../admin/PlatformAdminWorkspaceShell";
import PublicationReview from "../../../operator/departures/PublicationReview";

export default async function PlatformPublicationReviewPage({
  params,
}: {
  params: Promise<{ departureId: string }>;
}) {
  const { departureId } = await params;
  return (
    <PlatformAdminWorkspaceShell
      title="Review publication"
      summary="Verify the exact saved catalogue, pricing, inventory, and readiness facts before independent approval."
      contentOwnsLandmark
      contentClassName="np-platform-admin-legacy-embed"
    >
      <PublicationReview departureId={departureId} mode="platform" />
    </PlatformAdminWorkspaceShell>
  );
}
