import OperatorWorkspaceShell from "../../OperatorWorkspaceShell";
import DepartureComposer from "../DepartureComposer";

export default function NewDeparturePage() {
  return (
    <OperatorWorkspaceShell
      title="Departure authoring"
      summary="Create a departure using the same operator navigation and account chrome as the rest of NoorPath operations."
      contentOwnsLandmark
      showPageHeader={false}
      contentClassName="np-operator-legacy-embed"
    >
      <DepartureComposer />
    </OperatorWorkspaceShell>
  );
}
