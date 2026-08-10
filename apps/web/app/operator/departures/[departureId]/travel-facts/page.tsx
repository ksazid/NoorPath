import OperatorWorkspaceShell from "../../../OperatorWorkspaceShell";
import TravelFactsEditor from "../../TravelFactsEditor";

export default async function TravelFactsPage({
  params,
}: {
  params: Promise<{ departureId: string }>;
}) {
  const { departureId } = await params;

  return (
    <OperatorWorkspaceShell
      title="Airline & airport facts"
      summary="Record supported airline, flight-leg and airport facts for this package without inventing supplier confirmation."
    >
      <TravelFactsEditor departureId={departureId} />
    </OperatorWorkspaceShell>
  );
}
