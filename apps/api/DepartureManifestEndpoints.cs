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

public static class DepartureManifestEndpoints
{
    public static void MapDepartureManifest(this WebApplication app)
    {
        app.MapGet("/api/v1/operator/departures/{departureId:guid}/manifest", GetAsync)
            .RequireAuthorization();
        app.MapPost("/api/v1/operator/departures/{departureId:guid}/manifest/travellers/{travellerId:guid}/operations", UpdateOperationAsync)
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
        if (access.Result is not null)
            return access.Result;

        var departure = await (
            from batch in catalogue.DepartureBatches.AsNoTracking()
            join packageVersion in catalogue.PackageVersions.AsNoTracking()
                on batch.PackageVersionId equals packageVersion.Id
            where batch.Id == departureId && batch.OperatorId == access.OperatorId
            select new
            {
                batch.Id,
                batch.Origin,
                batch.DepartureDate,
                batch.ReturnDate,
                packageVersion.Name
            }).SingleOrDefaultAsync(cancellationToken);
        if (departure is null)
            return Results.NotFound();

        var bookingRows = await bookings.Bookings.AsNoTracking()
            .Where(item =>
                item.OperatorId == access.OperatorId
                && item.DepartureId == departureId
                && item.State == BookingState.Confirmed)
            .OrderBy(item => item.Reference)
            .ToArrayAsync(cancellationToken);
        var bookingIds = bookingRows.Select(item => item.Id).ToArray();

        var travellerRows = bookingIds.Length == 0
            ? Array.Empty<BookingTravellerRecord>()
            : await bookings.Travellers.AsNoTracking()
                .Where(item => bookingIds.Contains(item.BookingId))
                .OrderBy(item => item.BookingId)
                .ThenBy(item => item.Position)
                .ToArrayAsync(cancellationToken);
        var travellerIds = travellerRows.Select(item => item.TravellerId).ToArray();

        var successfulPayments = bookingIds.Length == 0
            ? Array.Empty<PaymentAttemptRecord>()
            : await payments.PaymentAttempts.AsNoTracking()
                .Where(item =>
                    bookingIds.Contains(item.BookingId)
                    && item.State == PaymentAttemptState.Succeeded)
                .ToArrayAsync(cancellationToken);

        var requirements = bookingIds.Length == 0
            ? Array.Empty<DocumentRequirementRecord>()
            : await documents.Requirements.AsNoTracking()
                .Where(item => bookingIds.Contains(item.BookingId))
                .ToArrayAsync(cancellationToken);
        var requirementIds = requirements.Select(item => item.Id).ToArray();
        var submissions = requirementIds.Length == 0
            ? Array.Empty<DocumentSubmissionRecord>()
            : await documents.Submissions.AsNoTracking()
                .Where(item => requirementIds.Contains(item.RequirementId))
                .OrderByDescending(item => item.CreatedAtUtc)
                .ToArrayAsync(cancellationToken);

        var visaCases = bookingIds.Length == 0
            ? Array.Empty<VisaCaseRecord>()
            : await visa.Cases.AsNoTracking()
                .Where(item =>
                    bookingIds.Contains(item.BookingId)
                    && item.OperatorId == access.OperatorId)
                .ToArrayAsync(cancellationToken);

        var assignments = bookingIds.Length == 0
            ? Array.Empty<AccommodationAssignmentRecord>()
            : await bookings.Set<AccommodationAssignmentRecord>().AsNoTracking()
                .Where(item =>
                    bookingIds.Contains(item.BookingId)
                    && item.OperatorId == access.OperatorId)
                .ToArrayAsync(cancellationToken);

        var operations = travellerIds.Length == 0
            ? Array.Empty<DepartureManifestTravellerRecord>()
            : await bookings.Set<DepartureManifestTravellerRecord>().AsNoTracking()
                .Where(item =>
                    item.DepartureId == departureId
                    && item.OperatorId == access.OperatorId
                    && travellerIds.Contains(item.TravellerId))
                .ToArrayAsync(cancellationToken);

        var items = travellerRows.Select(traveller =>
        {
            var booking = bookingRows.Single(item => item.Id == traveller.BookingId);
            var paid = successfulPayments
                .Where(item => item.BookingId == booking.Id)
                .Sum(item => item.Amount);
            var paymentReady = paid >= booking.Total;

            var travellerRequirements = requirements
                .Where(item => item.BookingId == booking.Id && item.TravellerId == traveller.TravellerId)
                .ToArray();
            var documentsReady = travellerRequirements.Length > 0
                && travellerRequirements.All(requirement =>
                    submissions.FirstOrDefault(item => item.RequirementId == requirement.Id)?.State
                        == SubmissionState.Approved);

            var travellerVisa = visaCases.FirstOrDefault(item =>
                item.BookingId == booking.Id && item.TravellerId == traveller.TravellerId);
            var visaReady = travellerVisa?.Status == VisaStatus.Approved;

            var makkahAssigned = assignments.Any(item =>
                item.BookingId == booking.Id
                && item.TravellerId == traveller.TravellerId
                && item.Stay == AccommodationStay.Makkah);
            var madinahAssigned = assignments.Any(item =>
                item.BookingId == booking.Id
                && item.TravellerId == traveller.TravellerId
                && item.Stay == AccommodationStay.Madinah);
            var accommodationReady = makkahAssigned && madinahAssigned;

            var blockers = new List<string>(4);
            if (!paymentReady) blockers.Add("payment");
            if (!documentsReady) blockers.Add("documents");
            if (!visaReady) blockers.Add("visa");
            if (!accommodationReady) blockers.Add("accommodation");

            var operation = operations.FirstOrDefault(item => item.TravellerId == traveller.TravellerId);
            return new
            {
                bookingId = booking.Id,
                bookingReference = booking.Reference,
                travellerId = traveller.TravellerId,
                traveller.Position,
                traveller.FullName,
                traveller.DateOfBirth,
                readiness = blockers.Count == 0 ? "ready" : "blocked",
                blockers,
                payment = new { ready = paymentReady, paid, total = booking.Total, booking.Currency },
                documents = new { ready = documentsReady, required = travellerRequirements.Length },
                visa = new { ready = visaReady, status = travellerVisa?.Status.ToString() ?? "NotStarted" },
                accommodation = new { ready = accommodationReady, makkahAssigned, madinahAssigned },
                operation = operation is null
                    ? null
                    : new
                    {
                        operation.Note,
                        operation.IsAcknowledged,
                        operation.Version,
                        operation.ActorAccountId,
                        operation.UpdatedAtUtc
                    }
            };
        }).ToArray();

        return Results.Ok(new
        {
            departure = new
            {
                departure.Id,
                packageName = departure.Name,
                departure.Origin,
                departure.DepartureDate,
                departure.ReturnDate
            },
            summary = new
            {
                travellers = items.Length,
                ready = items.Count(item => item.readiness == "ready"),
                blocked = items.Count(item => item.readiness == "blocked"),
                paymentBlocked = items.Count(item => item.blockers.Contains("payment")),
                documentBlocked = items.Count(item => item.blockers.Contains("documents")),
                visaBlocked = items.Count(item => item.blockers.Contains("visa")),
                accommodationBlocked = items.Count(item => item.blockers.Contains("accommodation"))
            },
            items
        });
    }

    private static async Task<IResult> UpdateOperationAsync(
        Guid departureId,
        Guid travellerId,
        UpdateDepartureManifestOperationRequest request,
        HttpContext http,
        IOperatorAccess operators,
        BookingDbContext bookings,
        CatalogueDbContext catalogue,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var access = await ResolveAccessAsync(http, operators, cancellationToken);
        if (access.Result is not null)
            return access.Result;

        var departureExists = await catalogue.DepartureBatches.AsNoTracking().AnyAsync(
            item => item.Id == departureId && item.OperatorId == access.OperatorId,
            cancellationToken);
        if (!departureExists)
            return Results.NotFound();

        var traveller = await (
            from booking in bookings.Bookings.AsNoTracking()
            join item in bookings.Travellers.AsNoTracking() on booking.Id equals item.BookingId
            where booking.OperatorId == access.OperatorId
                && booking.DepartureId == departureId
                && booking.State == BookingState.Confirmed
                && item.TravellerId == travellerId
            select new { booking.Id, item.TravellerId })
            .SingleOrDefaultAsync(cancellationToken);
        if (traveller is null)
            return Results.NotFound();

        var note = request.Note?.Trim() ?? string.Empty;
        if (note.Length is 0 or > 500)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["note"] = ["Operational note must be between 1 and 500 characters."]
            });
        }

        var principal = http.User.GetCurrentPrincipal()!;
        var actorAccountId = principal.AccountId.Value;
        var now = timeProvider.GetUtcNow();
        var record = await bookings.Set<DepartureManifestTravellerRecord>()
            .SingleOrDefaultAsync(item =>
                item.DepartureId == departureId
                && item.OperatorId == access.OperatorId
                && item.TravellerId == travellerId,
                cancellationToken);

        var previousVersion = record?.Version ?? 0;
        var previousNote = record?.Note;
        var previousIsAcknowledged = record?.IsAcknowledged ?? false;

        if (record is null)
        {
            if (request.ExpectedVersion != 0)
                return Results.Conflict(new { code = "manifest_operation_stale" });

            record = new DepartureManifestTravellerRecord
            {
                Id = Guid.NewGuid(),
                DepartureId = departureId,
                BookingId = traveller.Id,
                TravellerId = traveller.TravellerId,
                OperatorId = access.OperatorId,
                ActorAccountId = actorAccountId,
                Note = note,
                IsAcknowledged = request.IsAcknowledged,
                Version = 1,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            bookings.Add(record);
        }
        else
        {
            if (record.Version != request.ExpectedVersion)
                return Results.Conflict(new { code = "manifest_operation_stale", currentVersion = record.Version });

            record.Note = note;
            record.IsAcknowledged = request.IsAcknowledged;
            record.ActorAccountId = actorAccountId;
            record.Version += 1;
            record.UpdatedAtUtc = now;
        }

        bookings.Add(new DepartureManifestAuditRecord
        {
            Id = Guid.NewGuid(),
            DepartureId = departureId,
            BookingId = traveller.Id,
            TravellerId = traveller.TravellerId,
            OperatorId = access.OperatorId,
            ActorAccountId = actorAccountId,
            Action = request.IsAcknowledged ? "acknowledged" : "updated",
            PreviousNote = previousNote,
            ResultingNote = note,
            PreviousIsAcknowledged = previousIsAcknowledged,
            ResultingIsAcknowledged = request.IsAcknowledged,
            PreviousVersion = previousVersion,
            ResultingVersion = record.Version,
            CorrelationId = http.TraceIdentifier,
            OccurredAtUtc = now
        });

        try
        {
            await bookings.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Conflict(new { code = "manifest_operation_stale" });
        }

        return Results.Ok(new
        {
            record.TravellerId,
            record.Note,
            record.IsAcknowledged,
            record.Version,
            record.UpdatedAtUtc
        });
    }

    private static async Task<(IResult? Result, string OperatorId)> ResolveAccessAsync(
        HttpContext http,
        IOperatorAccess operators,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return (Results.Unauthorized(), string.Empty);

        var access = await operators.FindActiveMembershipAsync(principal.AccountId, cancellationToken);
        if (access is null || !access.IsAllowed(OperatorPermissions.AdminAccess))
            return (Results.Forbid(), string.Empty);

        return (null, access.OperatorId);
    }
}

public sealed record UpdateDepartureManifestOperationRequest(
    string? Note,
    bool IsAcknowledged,
    int ExpectedVersion);
