import PackageDraftPreview from "../../PackageDraftPreview";

export default async function PackageDraftPreviewPage({
  params,
}: {
  params: Promise<{ departureId: string }>;
}) {
  const { departureId } = await params;
  return <PackageDraftPreview departureId={departureId} />;
}
