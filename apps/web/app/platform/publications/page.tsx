import PlatformAdminWorkspaceShell from "../../admin/PlatformAdminWorkspaceShell";
import PublicationQueue from "./PublicationQueue";

export default function PlatformPublicationsPage() {
  return (
    <PlatformAdminWorkspaceShell
      title="Publication reviews"
      summary="Review operator submissions independently and publish only when every saved readiness check passes."
      contentOwnsLandmark
      contentClassName="np-platform-admin-legacy-embed"
    >
      <PublicationQueue />
    </PlatformAdminWorkspaceShell>
  );
}
