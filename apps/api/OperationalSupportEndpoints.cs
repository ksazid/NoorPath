using Microsoft.EntityFrameworkCore;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Documents;
using NoorPath.Documents.Infrastructure;
using NoorPath.Operators;
using NoorPath.Payments;
using NoorPath.Payments.Infrastructure;
using NoorPath.Visa;
using NoorPath.Visa.Infrastructure;

public static class OperationalSupportEndpoints
{
    private sealed record SupportItem(
        Guid BookingId,
        string BookingReference,
        string Category,
        string Title,
        string Code,
        DateTimeOffset UpdatedAtUtc,
        string ActionLabel,
        string? ActionTarget);

    public static void MapOperationalSupport(this WebApplication app)
    {
        var support = app.MapGroup("/api/v1/operator/support").RequireAuthorization();
        support.MapGet("", Queue);
        support.MapGet("/bookings/{bookingId:guid}", Detail);
    }

    private static bool CanSupport(OperatorAccess access) =>
        access.IsAllowed(OperatorPermissions.OperationalSupport)
        || access.IsAllowed(OperatorPermissions.AdminAccess);

    private static async Task<OperatorAccess?> GetAccess(
        HttpContext http,
        IOperatorAccess operatorAccess,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        return principal is null
            ? null
            : await operatorAccess.FindActiveMembershipAsync(principal.AccountId, cancellationToken);
    }

    private static async Task<IResult> Queue(
        string? search,
        string? category,
        HttpContext http,
        IOperatorAccess operatorAccess,
        BookingDbContext bookings,
        PaymentsDbContext payments,
        DocumentsDbContext documents,
        VisaDbContext visa,
        CancellationToken cancellationToken)
    {
        var access = await GetAccess(http, operatorAccess, cancellationToken);
        if (access is null || !CanSupport(access))
            return Results.Forbid();

        var query = bookings.Bookings.AsNoTracking()
            .Where(item => item.OperatorId == access.OperatorId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item => item.Reference.Contains(term));
        }

        var bookingRows = await query
            .OrderByDescending(item => item.UpdatedAtUtc)
            .Take(100)
            .Select(item => new
            {
                item.Id,
                item.Reference,
                item.State,
                item.ConfirmationExceptionCode,
                item.UpdatedAtUtc
            })
            .ToArrayAsync(cancellationToken);
        var bookingIds = bookingRows.Select(item => item.Id).ToArray();

        var paymentRows = await payments.PaymentAttempts.AsNoTracking()
            .Where(item => bookingIds.Contains(item.BookingId))
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
        var requirements = await documents.Requirements.AsNoTracking()
            .Where(item => bookingIds.Contains(item.BookingId))
            .ToArrayAsync(cancellationToken);
        var requirementIds = requirements.Select(item => item.Id).ToArray();
        var submissions = await documents.Submissions.AsNoTracking()
            .Where(item => requirementIds.Contains(item.RequirementId))
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
        var visaRows = await visa.Cases.AsNoTracking()
            .Where(item => bookingIds.Contains(item.BookingId))
            .ToArrayAsync(cancellationToken);

        var items = new List<SupportItem>();
        foreach (var booking in bookingRows)
        {
            if (booking.State == BookingState.ConfirmationException)
                items.Add(new(booking.Id, booking.Reference, "confirmation", "Confirmation needs recovery", booking.ConfirmationExceptionCode ?? "confirmation_exception", booking.UpdatedAtUtc, "Retry confirmation", $"/api/v1/operator/bookings/{booking.Id:D}/confirmation/retry"));

            var latestPayment = paymentRows.FirstOrDefault(item => item.BookingId == booking.Id);
            if (latestPayment?.State is PaymentAttemptState.Failed or PaymentAttemptState.RequiresAction)
                items.Add(new(booking.Id, booking.Reference, "payment", latestPayment.State == PaymentAttemptState.Failed ? "Payment failed" : "Payment requires action", latestPayment.FailureCode ?? latestPayment.State.ToString(), latestPayment.UpdatedAtUtc, "Review payment", null));

            foreach (var requirement in requirements.Where(item => item.BookingId == booking.Id))
            {
                var latest = submissions.FirstOrDefault(item => item.RequirementId == requirement.Id);
                if (latest is null)
                    continue;
                if (latest.State == SubmissionState.Rejected || latest.MalwareStatus != MalwareStatus.Clean)
                    items.Add(new(booking.Id, booking.Reference, "documents", latest.State == SubmissionState.Rejected ? "Document correction required" : "Document safety review required", latest.State == SubmissionState.Rejected ? "document_rejected" : "document_not_safe", latest.CreatedAtUtc, "Open document review", "/operator/documents"));
            }

            foreach (var visaCase in visaRows.Where(item => item.BookingId == booking.Id && item.Status is VisaStatus.ActionRequired or VisaStatus.Rejected))
                items.Add(new(booking.Id, booking.Reference, "visa", visaCase.Status == VisaStatus.ActionRequired ? "Visa action required" : "Visa rejected", visaCase.Status.ToString(), visaCase.UpdatedAtUtc, "Open visa case", "/operator/visa"));
        }

        if (!string.IsNullOrWhiteSpace(category))
            items = items.Where(item => string.Equals(item.Category, category.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();

        http.RequestServices.GetRequiredService<ILogger<Program>>()
            .LogInformation("Operational support queue outcome=success operatorId={OperatorId} count={Count} correlationId={CorrelationId}", access.OperatorId, items.Count, http.TraceIdentifier);
        return Results.Ok(new { items = items.OrderBy(item => item.UpdatedAtUtc).ToArray() });
    }

    private static async Task<IResult> Detail(
        Guid bookingId,
        HttpContext http,
        IOperatorAccess operatorAccess,
        BookingDbContext bookings,
        PaymentsDbContext payments,
        DocumentsDbContext documents,
        VisaDbContext visa,
        CancellationToken cancellationToken)
    {
        var access = await GetAccess(http, operatorAccess, cancellationToken);
        if (access is null || !CanSupport(access))
            return Results.Forbid();

        var booking = await bookings.Bookings.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == bookingId && item.OperatorId == access.OperatorId, cancellationToken);
        if (booking is null)
            return Results.NotFound();

        var payment = await payments.PaymentAttempts.AsNoTracking()
            .Where(item => item.BookingId == bookingId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new { state = item.State.ToString(), item.FailureCode, item.UpdatedAtUtc })
            .FirstOrDefaultAsync(cancellationToken);
        var documentRequirements = await documents.Requirements.AsNoTracking()
            .Where(item => item.BookingId == bookingId)
            .ToArrayAsync(cancellationToken);
        var ids = documentRequirements.Select(item => item.Id).ToArray();
        var documentSubmissions = await documents.Submissions.AsNoTracking()
            .Where(item => ids.Contains(item.RequirementId))
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
        var documentSummary = documentRequirements.Select(requirement =>
        {
            var latest = documentSubmissions.FirstOrDefault(item => item.RequirementId == requirement.Id);
            return new { kind = requirement.Kind.ToString(), state = latest?.State.ToString() ?? "Missing", malwareStatus = latest?.MalwareStatus.ToString() ?? "Unknown", latest?.ReviewReason, latest?.Version };
        });
        var visaSummary = await visa.Cases.AsNoTracking()
            .Where(item => item.BookingId == bookingId)
            .Select(item => new { item.TravellerId, status = item.Status.ToString(), item.CustomerAction, item.Version, item.UpdatedAtUtc })
            .ToArrayAsync(cancellationToken);

        object[] allowedActions = booking.State == BookingState.ConfirmationException
            ? new object[] { new { code = "retry_confirmation", label = "Retry confirmation", target = $"/api/v1/operator/bookings/{booking.Id:D}/confirmation/retry" } }
            : Array.Empty<object>();
        return Results.Ok(new
        {
            booking = new { booking.Id, booking.Reference, state = booking.State.ToString(), booking.ConfirmationExceptionCode, booking.UpdatedAtUtc },
            payment,
            documents = documentSummary,
            visa = visaSummary,
            allowedActions
        });
    }
}
