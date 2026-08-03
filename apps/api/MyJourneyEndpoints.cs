using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.FamilyBooking.Infrastructure;
using NoorPath.Payments;
using NoorPath.Payments.Infrastructure;

public static class MyJourneyEndpoints
{
    private static readonly JsonSerializerOptions SnapshotJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static void MapMyJourney(this WebApplication app)
    {
        app.MapGet("/api/v1/journeys", ListAsync).RequireAuthorization();
        app.MapGet("/api/v1/journeys/{bookingId:guid}", GetAsync).RequireAuthorization();
    }

    private static async Task<IResult> ListAsync(
        HttpContext http,
        BookingDbContext bookings,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return Results.Unauthorized();

        var items = await bookings.Bookings.AsNoTracking()
            .Where(item => item.AccountId == principal.AccountId.Value && item.State == BookingState.Confirmed)
            .OrderByDescending(item => item.ConfirmedAtUtc)
            .Select(item => new
            {
                bookingId = item.Id,
                bookingReference = item.Reference,
                departureId = item.DepartureId,
                state = item.State.ToString(),
                travellerCount = item.TravellerCount,
                currency = item.Currency,
                total = item.Total,
                confirmedAtUtc = item.ConfirmedAtUtc
            })
            .ToArrayAsync(cancellationToken);

        log.LogInformation("MyJourney list outcome=success count={Count} correlationId={CorrelationId}", items.Length, http.TraceIdentifier);
        return Results.Ok(new { items, correlationId = http.TraceIdentifier });
    }

    private static async Task<IResult> GetAsync(
        Guid bookingId,
        HttpContext http,
        BookingDbContext bookings,
        PaymentsDbContext payments,
        CatalogueDbContext catalogue,
        FamilyBookingDbContext family,
        TimeProvider timeProvider,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetTimestamp();
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return Results.Unauthorized();

        var accountId = principal.AccountId.Value;
        var booking = await bookings.Bookings.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == bookingId && item.AccountId == accountId && item.State == BookingState.Confirmed,
            cancellationToken);
        if (booking is null)
            return Results.NotFound();

        var departure = await catalogue.DepartureBatches.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == booking.DepartureId, cancellationToken);
        var package = departure is null ? null : await catalogue.PackageVersions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == departure.PackageVersionId, cancellationToken);
        if (departure is null || package is null)
        {
            log.LogWarning("MyJourney detail outcome=projection_delayed bookingId={BookingId} correlationId={CorrelationId}", booking.Id, http.TraceIdentifier);
            return Results.Problem(
                statusCode: 503,
                title: "Journey details are still being prepared",
                extensions: new Dictionary<string, object?> { ["code"] = "projection_delayed", ["correlationId"] = http.TraceIdentifier });
        }

        var travellers = await bookings.Travellers.AsNoTracking()
            .Where(item => item.BookingId == booking.Id)
            .OrderBy(item => item.Position)
            .Select(item => new { item.TravellerId, item.FullName })
            .ToArrayAsync(cancellationToken);
        var instalments = await bookings.Instalments.AsNoTracking()
            .Where(item => item.BookingId == booking.Id)
            .OrderBy(item => item.Sequence)
            .Select(item => new { item.Sequence, item.DueDate, item.Amount })
            .ToArrayAsync(cancellationToken);
        var payment = await payments.PaymentAttempts.AsNoTracking()
            .Where(item => item.BookingId == booking.Id && item.AccountId == accountId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new { item.State, item.SettledAtUtc })
            .FirstOrDefaultAsync(cancellationToken);
        var familySummary = await GetFamilySummary(
            booking,
            accountId,
            travellers.ToDictionary(item => item.TravellerId, item => item.FullName),
            family,
            timeProvider,
            log,
            http.TraceIdentifier,
            cancellationToken);

        var durationMs = timeProvider.GetElapsedTime(startedAt).TotalMilliseconds;
        log.LogInformation(
            "MyJourney detail outcome=success bookingId={BookingId} paymentState={PaymentState} familySnapshot={FamilySnapshot} durationMs={DurationMs} correlationId={CorrelationId}",
            booking.Id,
            payment?.State,
            familySummary is null ? "absent" : "available",
            durationMs,
            http.TraceIdentifier);

        return Results.Ok(new
        {
            bookingId = booking.Id,
            bookingReference = booking.Reference,
            state = booking.State.ToString(),
            booking.ConfirmedAtUtc,
            journey = new
            {
                packageName = package.Name,
                departure.Origin,
                departureDate = departure.DepartureDate,
                returnDate = departure.ReturnDate,
                package.MakkahHotelName,
                package.MakkahNights,
                package.MadinahHotelName,
                package.MadinahNights,
                package.TravelRouteSummary
            },
            occupancy = booking.Occupancy.ToString(),
            travellers,
            family = familySummary,
            commercial = new { booking.Currency, booking.Total, paid = booking.DueNow, booking.Remaining },
            payment = new
            {
                state = payment?.State.ToString() ?? "Unavailable",
                payment?.SettledAtUtc,
                instalments = instalments.Select(item => new { item.Sequence, item.DueDate, item.Amount, status = "Scheduled" })
            },
            readiness = new { documents = "ComingNext", visa = "ComingNext" },
            support = new { bookingReference = booking.Reference, correlationId = http.TraceIdentifier },
            projection = new { generatedAtUtc = timeProvider.GetUtcNow(), durationMs }
        });
    }

    private static async Task<object?> GetFamilySummary(
        BookingRecord booking,
        string accountId,
        IReadOnlyDictionary<Guid, string> travellerNames,
        FamilyBookingDbContext family,
        TimeProvider timeProvider,
        ILogger<Program> log,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var snapshot = await family.BookingSnapshots.AsNoTracking().SingleOrDefaultAsync(
            item => item.BookingId == booking.Id && item.AccountId == accountId,
            cancellationToken);
        if (snapshot is null)
        {
            var quoteSnapshot = await family.QuoteSnapshots.AsNoTracking().SingleOrDefaultAsync(
                item => item.QuoteId == booking.QuoteId && item.AccountId == accountId,
                cancellationToken);
            if (quoteSnapshot is not null)
            {
                var projected = new FamilyBookingSnapshotRecord
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    FamilyPartyId = quoteSnapshot.FamilyPartyId,
                    AccountId = quoteSnapshot.AccountId,
                    PolicyVersion = quoteSnapshot.PolicyVersion,
                    PartyVersion = quoteSnapshot.PartyVersion,
                    PayloadJson = quoteSnapshot.PayloadJson,
                    CreatedAtUtc = timeProvider.GetUtcNow()
                };
                family.BookingSnapshots.Add(projected);
                family.Audit.Add(new FamilyBookingAuditRecord
                {
                    Id = Guid.NewGuid(),
                    AccountId = accountId,
                    ActorId = "system",
                    Action = "family_booking_snapshotted",
                    SubjectType = "booking",
                    SubjectId = booking.Id,
                    DetailJson = JsonSerializer.Serialize(new
                    {
                        quoteId = booking.QuoteId,
                        familyPartyId = quoteSnapshot.FamilyPartyId,
                        quoteSnapshot.PartyVersion,
                        quoteSnapshot.PolicyVersion
                    }),
                    OccurredAtUtc = projected.CreatedAtUtc
                });
                try
                {
                    await family.SaveChangesAsync(cancellationToken);
                    snapshot = projected;
                }
                catch (DbUpdateException)
                {
                    family.ChangeTracker.Clear();
                    snapshot = await family.BookingSnapshots.AsNoTracking().SingleOrDefaultAsync(
                        item => item.BookingId == booking.Id && item.AccountId == accountId,
                        cancellationToken);
                }
            }
        }

        if (snapshot is null)
            return null;

        FamilySnapshotPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<FamilySnapshotPayload>(snapshot.PayloadJson, SnapshotJson);
        }
        catch (JsonException)
        {
            log.LogWarning(
                "MyJourney family projection outcome=invalid_snapshot bookingId={BookingId} correlationId={CorrelationId}",
                booking.Id,
                correlationId);
            return null;
        }
        if (payload is null)
            return null;

        return new
        {
            familyPartyId = payload.FamilyPartyId,
            partyName = payload.PartyName,
            policyVersion = snapshot.PolicyVersion,
            partyVersion = snapshot.PartyVersion,
            travellers = payload.TravellerIds.Select(id => new
            {
                travellerId = id,
                fullName = travellerNames.GetValueOrDefault(id, "Traveller")
            }),
            mahramLinks = payload.MahramLinks.Select(link => new
            {
                protectedTravellerId = link.ProtectedTravellerId,
                protectedTravellerName = travellerNames.GetValueOrDefault(link.ProtectedTravellerId, "Traveller"),
                mahramTravellerId = link.MahramTravellerId,
                mahramTravellerName = travellerNames.GetValueOrDefault(link.MahramTravellerId, "Mahram"),
                link.RelationshipType
            }),
            disclaimer = "NoorPath records the customer-declared relationship snapshot used for this booking; it is not a religious or legal ruling."
        };
    }

    private sealed record FamilySnapshotPayload(
        Guid FamilyPartyId,
        string PartyName,
        string PolicyVersion,
        int PartyVersion,
        IReadOnlyList<Guid> TravellerIds,
        IReadOnlyList<FamilySnapshotLink> MahramLinks);

    private sealed record FamilySnapshotLink(
        Guid ProtectedTravellerId,
        Guid MahramTravellerId,
        string RelationshipType);
}
