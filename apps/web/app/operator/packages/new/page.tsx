import DepartureComposer from "../../departures/DepartureComposer";
import ClonePackageDraft from "./ClonePackageDraft";

export default async function NewPackageDraftPage({
  searchParams,
}: {
  searchParams: Promise<{ cloneFrom?: string }>;
}) {
  const { cloneFrom } = await searchParams;

  if (cloneFrom) {
    return <ClonePackageDraft sourceDepartureId={cloneFrom} />;
  }

  return <DepartureComposer />;
}
