import DepartureComposer from "../DepartureComposer";

export default async function EditDeparturePage({
  params,
}: {
  params: Promise<{ departureId: string }>;
}) {
  const { departureId } = await params;
  return <DepartureComposer initialDepartureId={departureId} />;
}
