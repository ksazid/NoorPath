import ClonePackageDraft from "./ClonePackageDraft";
import PackageQuickStart from "./PackageQuickStart";

export default async function NewPackageDraftPage({
  searchParams,
}: {
  searchParams: Promise<{ cloneFrom?: string }>;
}) {
  const { cloneFrom } = await searchParams;

  if (cloneFrom) {
    return <ClonePackageDraft sourceDepartureId={cloneFrom} />;
  }

  return <PackageQuickStart />;
}
