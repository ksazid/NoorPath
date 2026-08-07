import OperatorBookingAmendment from "../../../OperatorBookingAmendment";

export default async function OperatorBookingAmendmentPage({
  params,
}: {
  params: Promise<{ bookingId: string }>;
}) {
  const { bookingId } = await params;
  return <OperatorBookingAmendment bookingId={bookingId} />;
}
