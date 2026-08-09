import OperatorDepartureHandover from "../../../OperatorDepartureHandover";

export default async function OperatorDepartureHandoverPage({
  params,
}: {
  params: Promise<{ departureId: string }>;
}) {
  const { departureId } = await params;
  return <OperatorDepartureHandover departureId={departureId} />;
}
