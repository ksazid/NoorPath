import PlatformAdminWorkspace from "./PlatformAdminWorkspace";
import PlatformAdminWorkspaceShell from "./PlatformAdminWorkspaceShell";

export default function AdminPage() {
  return (
    <PlatformAdminWorkspaceShell
      title="Platform operations"
      summary="Review operator access, lifecycle state, and governed administration from one consistent NoorPath workspace."
      contentOwnsLandmark
      contentClassName="np-platform-admin-legacy-embed"
    >
      <PlatformAdminWorkspace />
    </PlatformAdminWorkspaceShell>
  );
}
