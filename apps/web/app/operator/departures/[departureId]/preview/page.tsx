import OperatorWorkspaceShell from "../../../OperatorWorkspaceShell";
import PackageDraftPreview from "../../PackageDraftPreview";

export default async function PackageDraftPreviewPage({
  params,
}: {
  params: Promise<{ departureId: string }>;
}) {
  const { departureId } = await params;
  return (
    <OperatorWorkspaceShell
      title="Customer preview"
      summary="Inspect the saved customer-facing package projection without leaving the operator workspace."
      contentOwnsLandmark
      showPageHeader={false}
      contentClassName="np-operator-legacy-embed"
    >
      <PackageDraftPreview departureId={departureId} />
    </OperatorWorkspaceShell>
  );
}
