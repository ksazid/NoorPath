using Microsoft.EntityFrameworkCore;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Documents;
using NoorPath.Documents.Infrastructure;
using NoorPath.Operators;
using NoorPath.Payments;
using NoorPath.Payments.Infrastructure;
using NoorPath.Visa;
using NoorPath.Visa.Infrastructure;

public static class OperatorBookingManagementEndpoints
{
    public static void MapOperatorBookingManagement(this WebApplication app)
    {
        app.MapGet("/api/v1/operator/bookings", ListAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> ListAsync(
        HttpContext http,
        IOperatorAccess operators,
        BookingDbContext bookings,
        CatalogueDbContext catalogue,
        PaymentsDbContext payments,
        DocumentsDbContext documents,
        VisaDbContext visa,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return Results.Unauthorized();

        var access = await operators.FindActiveMembershipAsync(
            principal.AccountId,
            cancellationToken);
        if (access is null || !access.IsAllowed(OperatorPermissions.AdminAccess))
            return Results.Forbid();

        var bookingRows = await bookings.Bookings.AsNoTracking()
            .Where(item => item.OperatorId == access.OperatorId)
            .OrderByDescending(item => item.UpdatedAtUtc)
            .ToArrayAsync(cancellationToken);

        if (bookingRows.Length == 0)
        {
            return Results.Ok(new
            {
                summary = new { total = 0, confirmed = 0, actionRequired = 0, travellers = 0 },
                items = Array.Empty<object>()
            });
        }

        var bookingIds = bookingRows.Select(item => item.Id).ToArray();
        var departureIds = bookingRows.Select(item => item.DepartureId).Distinct().ToArray();

        var travellerRows = await bookings.Travellers.AsNoTracking()
            .Where(item => bookingIds.Contains(item.BookingId))
            .OrderBy(item => item.Position)
            .ToArrayAsync(cancellationToken);

        var instalmentRows = await bookings.Instalments.AsNoTracking()
            .Where(item => bookingIds.Contains(item.BookingId))
            .OrderBy(item => item.Sequence)
            .ToArrayAsync(cancellationToken);

        var successfulPayments = await payments.PaymentAttempts.AsNoTracking()
            .Where(item =>
                bookingIds.Contains(item.BookingId)
                && item.State == PaymentAttemptState.Succeeded)
            .Select(item => new { item.BookingId, item.Amount })
            .ToArrayAsync(cancellationToken);

        var requirementRows = await documents.Requirements.AsNoTracking()
            .Where(item => bookingIds.Contains(item.BookingId))
            .ToArrayAsync(cancellationToken);
        var requirementIds = requirementRows.Select(item => item.Id).ToArray();
        var submissionRows = requirementIds.Length == 0
            ? Array.Empty<DocumentSubmissionRecord>()
            : await documents.Submissions.AsNoTracking()
                .Where(item => requirementIds.Contains(item.RequirementId))
                .OrderByDescending(item => item.CreatedAtUtc)
                .ToArrayAsync(cancellationToken);

        var visaRows = await visa.Cases.AsNoTracking()
            .Where(item => bookingIds.Contains(item.BookingId))
            .ToArrayAsync(cancellationToken);

        var catalogueRows = await (
            from departure in catalogue.DepartureBatches.AsNoTracking()
            join packageVersion in catalogue.PackageVersions.AsNoTracking()
                on departure.PackageVersionId equals packageVersion.Id
            where departureIds.Contains(departure.Id)
            select new
            {
                departure.Id,
                packageVersion.Name,
                departure.Origin,
                departure.DepartureDate,
                departure.ReturnDate
            }).ToArrayAsync(cancellationToken);
        var catalogueByDeparture = catalogueRows.ToDictionary(item => item.Id);

        var items = bookingRows.Select(booking =>
        {
            var bookingTravellers = travellerRows
                .Where(item => item.BookingId == booking.Id)
                .ToArray();
            var paid = successfulPayments
                .Where(item => item.BookingId == booking.Id)
                .Sum(item => item.Amount);
            paid = Math.Min(paid, booking.Total);
            var outstanding = Math.Max(booking.Total - paid, 0m);
            var paymentStatus = paid >= booking.Total
                ? "paid"
                : paid > 0
                    ? "partiallyPaid"
                    : "awaitingPayment";

            var bookingInstalments = instalmentRows
                .Where(item => item.BookingId == booking.Id)
                .OrderBy(item => item.Sequence)
                .ToArray();
            decimal cumulative = 0;
            BookingInstalmentRecord? nextInstalment = null;
            foreach (var instalment in bookingInstalments)
            {
                cumulative += instalment.Amount;
                if (paid < cumulative)
                {
                    nextInstalment = instalment;
                    break;
                }
            }

            var bookingRequirements = requirementRows
                .Where(item => item.BookingId == booking.Id)
                .ToArray();
            var approvedDocuments = bookingRequirements.Count(requirement =>
                submissionRows.FirstOrDefault(item => item.RequirementId == requirement.Id)?.State
                    == SubmissionState.Approved);
            var documentStatus = bookingRequirements.Length == 0
                ? "notStarted"
                : approvedDocuments == bookingRequirements.Length
                    ? "ready"
                    : approvedDocuments > 0
                        ? "inProgress"
                        : "actionRequired";

            var bookingVisa = visaRows
                .Where(item => item.BookingId == booking.Id)
                .ToArray();
            var approvedVisa = bookingVisa.Count(item => item.Status == VisaStatus.Approved);
            var visaStatus = bookingVisa.Length == 0
                ? "notStarted"
                : approvedVisa == booking.TravellerCount
                    ? "approved"
                    : bookingVisa.Any(item => item.Status == VisaStatus.ActionRequired)
                        ? "actionRequired"
                        : bookingVisa.Any(item => item.Status == VisaStatus.Rejected)
                            ? "rejected"
                            : "inProgress";

            catalogueByDeparture.TryGetValue(booking.DepartureId, out var departure);

            return new
            {
                bookingId = booking.Id,
                reference = booking.Reference,
                accountId = booking.AccountId,
                departureId = booking.DepartureId,
                packageName = departure?.Name ?? "Umrah package",
                origin = departure?.Origin ?? "Origin unavailable",
                departureDate = departure?.DepartureDate,
                returnDate = departure?.ReturnDate,
                state = BookingStateKey(booking.State),
                occupancy = booking.Occupancy.ToString().ToLowerInvariant(),
                travellerCount = booking.TravellerCount,
                travellers = bookingTravellers.Select(item => new
                {
                    travellerId = item.TravellerId,
                    fullName = item.FullName,
                    item.DateOfBirth
                }),
                payment = new
                {
                    booking.Currency,
                    total = booking.Total,
                    paid,
                    outstanding,
                    status = paymentStatus,
                    nextInstalment = nextInstalment is null
                        ? null
                        : new { nextInstalment.Sequence, nextInstalment.DueDate, nextInstalment.Amount }
                },
                documents = new
                {
                    status = documentStatus,
                    required = bookingRequirements.Length,
                    approved = approvedDocuments
                },
                visa = new
                {
                    status = visaStatus,
                    total = booking.TravellerCount,
                    approved = approvedVisa
                },
                booking.CreatedAtUtc,
                booking.UpdatedAtUtc
            };
        }).ToArray();

        var actionRequired = items.Count(item =>
            item.documents.status == "actionRequired"
            || item.visa.status is "actionRequired" or "rejected"
            || item.payment.status != "paid"
            || item.state is "confirmationException" or "paymentFailed");

        return Results.Ok(new
        {
            summary = new
            {
                total = items.Length,
                confirmed = bookingRows.Count(item => item.State == BookingState.Confirmed),
                actionRequired,
                travellers = bookingRows.Sum(item => item.TravellerCount)
            },
            items
        });
    }

    private static string BookingStateKey(BookingState state) => state switch
    {
        BookingState.PendingPayment => "pendingPayment",
        BookingState.PaymentInProgress => "paymentInProgress",
        BookingState.PaymentSucceeded => "paymentSucceeded",
        BookingState.PaymentFailed => "paymentFailed",
        BookingState.PaymentCancelled => "paymentCancelled",
        BookingState.PendingConfirmation => "pendingConfirmation",
        BookingState.Confirming => "confirming",
        BookingState.Confirmed => "confirmed",
        BookingState.ConfirmationException => "confirmationException",
        BookingState.Cancelled => "cancelled",
        _ => state.ToString().ToLowerInvariant()
    };
}
