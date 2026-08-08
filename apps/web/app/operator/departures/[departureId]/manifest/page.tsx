import OperatorDepartureManifest from "../../../OperatorDepartureManifest";

export default async function OperatorDepartureManifestPage({
  params,
}: {
  params: Promise<{ departureId: string }>;
}) {
  const { departureId } = await params;
  return <OperatorDepartureManifest departureId={departureId} />;
}
