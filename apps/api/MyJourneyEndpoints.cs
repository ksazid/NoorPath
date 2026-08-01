using Microsoft.EntityFrameworkCore;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Payments;
using NoorPath.Payments.Infrastructure;

public static class MyJourneyEndpoints
{
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
        TimeProvider timeProvider,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetTimestamp();
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return Results.Unauthorized();

        var booking = await bookings.Bookings.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == bookingId && item.AccountId == principal.AccountId.Value && item.State == BookingState.Confirmed,
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
            .Select(item => new { item.FullName })
            .ToArrayAsync(cancellationToken);
        var instalments = await bookings.Instalments.AsNoTracking()
            .Where(item => item.BookingId == booking.Id)
            .OrderBy(item => item.Sequence)
            .Select(item => new { item.Sequence, item.DueDate, item.Amount })
            .ToArrayAsync(cancellationToken);
        var payment = await payments.PaymentAttempts.AsNoTracking()
            .Where(item => item.BookingId == booking.Id && item.AccountId == principal.AccountId.Value)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => new { item.State, item.SettledAtUtc })
            .FirstOrDefaultAsync(cancellationToken);

        var durationMs = timeProvider.GetElapsedTime(startedAt).TotalMilliseconds;
        log.LogInformation(
            "MyJourney detail outcome=success bookingId={BookingId} paymentState={PaymentState} durationMs={DurationMs} correlationId={CorrelationId}",
            booking.Id, payment?.State, durationMs, http.TraceIdentifier);

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
}
