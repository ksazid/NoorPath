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

public static class DepartureHandoverEndpoints
{
    public static void MapDepartureHandover(this WebApplication app)
    {
        app.MapGet("/api/v1/operator/departures/{departureId:guid}/handover", GetAsync)
            .RequireAuthorization();
        app.MapPost("/api/v1/operator/departures/{departureId:guid}/handover/complete", CompleteAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> GetAsync(
        Guid departureId,
        HttpContext http,
        IOperatorAccess operators,
        BookingDbContext bookings,
        CatalogueDbContext catalogue,
        PaymentsDbContext payments,
        DocumentsDbContext documents,
        VisaDbContext visa,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(http, operators, cancellationToken);
        if (access.Result is not null) return access.Result;

        var snapshot = await BuildSnapshotAsync(
            departureId, access.OperatorId, bookings, catalogue, payments, documents, visa, cancellationToken);
        if (snapshot is null) return Results.NotFound();

        var handover = await bookings.Set<DepartureHandoverRecord>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.DepartureId == departureId && x.OperatorId == access.OperatorId, cancellationToken);
        var audits = await bookings.Set<DepartureHandoverAuditRecord>().AsNoTracking()
            .Where(x => x.DepartureId == departureId && x.OperatorId == access.OperatorId)
            .OrderByDescending(x => x.OccurredAtUtc)
            .Take(20)
            .ToArrayAsync(cancellationToken);

        var handoverResponse = handover is null
            ? new HandoverStateResponse(false, null, null, null, null, 0)
            : new HandoverStateResponse(
                handover.IsCompleted,
                handover.GroupLeaderName,
                handover.FinalNote,
                handover.CompletedByAccountId,
                handover.CompletedAtUtc,
                handover.Version);

        return Results.Ok(new
        {
            snapshot.Departure,
            snapshot.Summary,
            canComplete = DepartureHandoverPolicy.CanComplete(snapshot.Summary.Travellers, snapshot.Summary.Blocked),
            handover = handoverResponse,
            audits = audits.Select(x => new
            {
                x.Action,
                x.Note,
                x.ActorAccountId,
                x.PreviousVersion,
                x.ResultingVersion,
                x.TravellerCount,
                x.BlockedCount,
                x.OccurredAtUtc
            })
        });
    }

    private static async Task<IResult> CompleteAsync(
        Guid departureId,
        CompleteDepartureHandoverRequest request,
        HttpContext http,
        IOperatorAccess operators,
        BookingDbContext bookings,
        CatalogueDbContext catalogue,
        PaymentsDbContext payments,
        DocumentsDbContext documents,
        VisaDbContext visa,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(http, operators, cancellationToken);
        if (access.Result is not null) return access.Result;

        var snapshot = await BuildSnapshotAsync(
            departureId, access.OperatorId, bookings, catalogue, payments, documents, visa, cancellationToken);
        if (snapshot is null) return Results.NotFound();

        var record = await bookings.Set<DepartureHandoverRecord>()
            .SingleOrDefaultAsync(x => x.DepartureId == departureId && x.OperatorId == access.OperatorId, cancellationToken);
        var currentVersion = record?.Version ?? 0;

        if (record?.IsCompleted == true)
        {
            return Results.Ok(new
            {
                record.IsCompleted,
                record.FinalNote,
                record.CompletedByAccountId,
                record.CompletedAtUtc,
                record.Version,
                idempotent = true
            });
        }

        var validation = DepartureHandoverPolicy.ValidateCompletion(
            snapshot.Summary.Travellers,
            snapshot.Summary.Blocked,
            request.FinalNote,
            request.ExpectedVersion,
            currentVersion,
            false);
        if (validation == "handover_stale")
            return Results.Conflict(new { code = validation, currentVersion });
        if (validation == "handover_blocked")
            return Results.Conflict(new { code = validation, blockers = snapshot.Summary });
        if (validation == "handover_empty")
            return Results.Conflict(new { code = validation });
        if (validation == "handover_note_invalid")
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["finalNote"] = ["Final handover note must be between 1 and 500 characters."]
            });

        var principal = http.User.GetCurrentPrincipal()!;
        var actor = principal.AccountId.Value;
        var now = timeProvider.GetUtcNow();
        var note = request.FinalNote!.Trim();

        if (record is null)
        {
            record = new DepartureHandoverRecord
            {
                Id = Guid.NewGuid(),
                DepartureId = departureId,
                OperatorId = access.OperatorId,
                IsCompleted = true,
                FinalNote = note,
                CompletedByAccountId = actor,
                CompletedAtUtc = now,
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            bookings.Add(record);
        }
        else
        {
            record.IsCompleted = true;
            record.FinalNote = note;
            record.CompletedByAccountId = actor;
            record.CompletedAtUtc = now;
            record.Version += 1;
            record.UpdatedAtUtc = now;
        }

        bookings.Add(new DepartureHandoverAuditRecord
        {
            Id = Guid.NewGuid(),
            DepartureId = departureId,
            OperatorId = access.OperatorId,
            ActorAccountId = actor,
            Action = "completed",
            Note = note,
            PreviousVersion = currentVersion,
            ResultingVersion = record.Version,
            TravellerCount = snapshot.Summary.Travellers,
            BlockedCount = snapshot.Summary.Blocked,
            CorrelationId = http.TraceIdentifier,
            OccurredAtUtc = now
        });

        try
        {
            await bookings.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { code = "handover_stale" });
        }

        return Results.Ok(new
        {
            record.IsCompleted,
            record.FinalNote,
            record.CompletedByAccountId,
            record.CompletedAtUtc,
            record.Version,
            idempotent = false
        });
    }

    private static async Task<HandoverSnapshot?> BuildSnapshotAsync(
        Guid departureId,
        string operatorId,
        BookingDbContext bookings,
        CatalogueDbContext catalogue,
        PaymentsDbContext payments,
        DocumentsDbContext documents,
        VisaDbContext visa,
        CancellationToken cancellationToken)
    {
        var departure = await (
            from batch in catalogue.DepartureBatches.AsNoTracking()
            join packageVersion in catalogue.PackageVersions.AsNoTracking()
                on batch.PackageVersionId equals packageVersion.Id
            where batch.Id == departureId && batch.OperatorId == operatorId
            select new HandoverDeparture(batch.Id, packageVersion.Name, batch.Origin, batch.DepartureDate, batch.ReturnDate)
        ).SingleOrDefaultAsync(cancellationToken);
        if (departure is null) return null;

        var bookingRows = await bookings.Bookings.AsNoTracking()
            .Where(x => x.OperatorId == operatorId && x.DepartureId == departureId && x.State == BookingState.Confirmed)
            .ToArrayAsync(cancellationToken);
        var bookingIds = bookingRows.Select(x => x.Id).ToArray();
        var travellers = bookingIds.Length == 0
            ? Array.Empty<BookingTravellerRecord>()
            : await bookings.Travellers.AsNoTracking().Where(x => bookingIds.Contains(x.BookingId)).ToArrayAsync(cancellationToken);
        var successfulPayments = bookingIds.Length == 0
            ? Array.Empty<PaymentAttemptRecord>()
            : await payments.PaymentAttempts.AsNoTracking()
                .Where(x => bookingIds.Contains(x.BookingId) && x.State == PaymentAttemptState.Succeeded)
                .ToArrayAsync(cancellationToken);
        var requirements = bookingIds.Length == 0
            ? Array.Empty<DocumentRequirementRecord>()
            : await documents.Requirements.AsNoTracking().Where(x => bookingIds.Contains(x.BookingId)).ToArrayAsync(cancellationToken);
        var requirementIds = requirements.Select(x => x.Id).ToArray();
        var submissions = requirementIds.Length == 0
            ? Array.Empty<DocumentSubmissionRecord>()
            : await documents.Submissions.AsNoTracking()
                .Where(x => requirementIds.Contains(x.RequirementId))
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToArrayAsync(cancellationToken);
        var visaCases = bookingIds.Length == 0
            ? Array.Empty<VisaCaseRecord>()
            : await visa.Cases.AsNoTracking().Where(x => bookingIds.Contains(x.BookingId) && x.OperatorId == operatorId).ToArrayAsync(cancellationToken);
        var assignments = bookingIds.Length == 0
            ? Array.Empty<AccommodationAssignmentRecord>()
            : await bookings.Set<AccommodationAssignmentRecord>().AsNoTracking()
                .Where(x => bookingIds.Contains(x.BookingId) && x.OperatorId == operatorId)
                .ToArrayAsync(cancellationToken);

        var paymentBlocked = 0;
        var documentBlocked = 0;
        var visaBlocked = 0;
        var accommodationBlocked = 0;
        var blocked = 0;

        foreach (var traveller in travellers)
        {
            var booking = bookingRows.Single(x => x.Id == traveller.BookingId);
            var paymentReady = successfulPayments.Where(x => x.BookingId == booking.Id).Sum(x => x.Amount) >= booking.Total;
            var travellerRequirements = requirements.Where(x => x.BookingId == booking.Id && x.TravellerId == traveller.TravellerId).ToArray();
            var documentsReady = travellerRequirements.Length > 0 && travellerRequirements.All(r =>
                submissions.FirstOrDefault(x => x.RequirementId == r.Id)?.State == SubmissionState.Approved);
            var visaReady = visaCases.FirstOrDefault(x => x.BookingId == booking.Id && x.TravellerId == traveller.TravellerId)?.Status == VisaStatus.Approved;
            var accommodationReady = assignments.Any(x => x.BookingId == booking.Id && x.TravellerId == traveller.TravellerId && x.Stay == AccommodationStay.Makkah)
                && assignments.Any(x => x.BookingId == booking.Id && x.TravellerId == traveller.TravellerId && x.Stay == AccommodationStay.Madinah);

            var itemBlocked = false;
            if (!paymentReady) { paymentBlocked++; itemBlocked = true; }
            if (!documentsReady) { documentBlocked++; itemBlocked = true; }
            if (!visaReady) { visaBlocked++; itemBlocked = true; }
            if (!accommodationReady) { accommodationBlocked++; itemBlocked = true; }
            if (itemBlocked) blocked++;
        }

        return new HandoverSnapshot(
            departure,
            new HandoverSummary(
                travellers.Length,
                travellers.Length - blocked,
                blocked,
                paymentBlocked,
                documentBlocked,
                visaBlocked,
                accommodationBlocked));
    }

    private static async Task<(IResult? Result, string OperatorId)> ResolveAccessAsync(
        HttpContext http,
        IOperatorAccess operators,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null) return (Results.Unauthorized(), string.Empty);
        var access = await operators.FindActiveMembershipAsync(principal.AccountId, cancellationToken);
        if (access is null || !access.IsAllowed(OperatorPermissions.AdminAccess))
            return (Results.Forbid(), string.Empty);
        return (null, access.OperatorId);
    }

    private sealed record HandoverSnapshot(HandoverDeparture Departure, HandoverSummary Summary);
    private sealed record HandoverDeparture(Guid Id, string PackageName, string Origin, DateOnly DepartureDate, DateOnly ReturnDate);
    private sealed record HandoverSummary(
        int Travellers,
        int Ready,
        int Blocked,
        int PaymentBlocked,
        int DocumentBlocked,
        int VisaBlocked,
        int AccommodationBlocked);
    private sealed record HandoverStateResponse(
        bool IsCompleted,
        string? GroupLeaderName,
        string? FinalNote,
        string? CompletedByAccountId,
        DateTimeOffset? CompletedAtUtc,
        int Version);
}

public sealed record CompleteDepartureHandoverRequest(string? FinalNote, int ExpectedVersion);
