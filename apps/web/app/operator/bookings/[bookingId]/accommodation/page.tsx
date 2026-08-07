import OperatorAccommodation from "../../../../OperatorAccommodation";

export default async function OperatorAccommodationPage({
  params,
}: {
  params: Promise<{ bookingId: string }>;
}) {
  const { bookingId } = await params;
  return <OperatorAccommodation bookingId={bookingId} />;
}
