import OperatorWorkspaceShell from "../../OperatorWorkspaceShell";
import ClonePackageDraft from "./ClonePackageDraft";
import PackageQuickStart from "./PackageQuickStart";

export default async function NewPackageDraftPage({
  searchParams,
}: {
  searchParams: Promise<{ cloneFrom?: string }>;
}) {
  const { cloneFrom } = await searchParams;

  return (
    <OperatorWorkspaceShell
      title="Package authoring"
      summary="Create or clone a package draft inside the same operator workspace used for catalogue and departure operations."
      contentOwnsLandmark
      showPageHeader={false}
      contentClassName="np-operator-legacy-embed"
    >
      {cloneFrom ? (
        <ClonePackageDraft sourceDepartureId={cloneFrom} />
      ) : (
        <PackageQuickStart />
      )}
    </OperatorWorkspaceShell>
  );
}
