import OperatorBookingDetail from "../../OperatorBookingDetail";

export default async function OperatorBookingDetailPage({
  params,
}: {
  params: Promise<{ bookingId: string }>;
}) {
  const { bookingId } = await params;
  return <OperatorBookingDetail bookingId={bookingId} />;
}
