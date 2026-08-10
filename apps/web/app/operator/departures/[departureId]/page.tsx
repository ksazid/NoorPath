import OperatorWorkspaceShell from "../../OperatorWorkspaceShell";
import DepartureComposer from "../DepartureComposer";
import { TravelFactsLaunchLink } from "../TravelFactsEditor";

export default async function EditDeparturePage({
  params,
}: {
  params: Promise<{ departureId: string }>;
}) {
  const { departureId } = await params;
  return (
    <OperatorWorkspaceShell
      title="Departure authoring"
      summary="Update the saved package and departure facts without leaving the shared operator workspace."
      contentOwnsLandmark
      showPageHeader={false}
      contentClassName="np-operator-legacy-embed"
    >
      <DepartureComposer initialDepartureId={departureId} />
      <TravelFactsLaunchLink departureId={departureId} />
    </OperatorWorkspaceShell>
  );
}
