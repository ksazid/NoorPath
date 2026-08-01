using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Inventory;
using NoorPath.Inventory.Infrastructure;
using NoorPath.Pricing;
using NoorPath.Pricing.Infrastructure;
using NoorPath.Traveller.Infrastructure;

public static class BookingEndpoints
{
    public static void MapBookings(this WebApplication app)
    {
        app.MapPost(
                "/api/v1/inventory-holds/{holdId:guid}/bookings",
                CreateBookingAsync)
            .RequireAuthorization();
        app.MapGet("/api/v1/bookings/{bookingId:guid}", GetBookingAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> CreateBookingAsync(
        Guid holdId,
        HttpContext http,
        BookingDbContext bookings,
        PricingDbContext pricing,
        InventoryDbContext inventory,
        TravellerDbContext travellerProfiles,
        TimeProvider timeProvider,
        ILogger<Program> log,
        CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return NotAuthenticated(http);

        if (!CheckoutIdempotency.TryRead(http, out var idempotencyKey, out var error))
            return error!;

        var accountId = principal.AccountId.Value;
        var idempotencyKeyHash = CheckoutIdempotency.Hash(idempotencyKey!);
        var requestFingerprint = CheckoutIdempotency.Hash($"{accountId}\n{holdId:D}");
        var now = timeProvider.GetUtcNow();

        var hold = await inventory.Holds.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == holdId &&
                item.AccountId == accountId,
                cancellationToken);
        if (hold is null)
            return Results.NotFound();

        if (hold.State != InventoryHoldState.Active || hold.ExpiresAtUtc <= now)
            return HoldNotActive(http);

        var quote = await pricing.Quotes.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == hold.QuoteId &&
                item.AccountId == accountId,
                cancellationToken);
        if (quote is null)
            return QuoteUnavailable(http);

        if (quote.ExpiresAtUtc <= now
            || quote.DepartureId != hold.DepartureId
            || !string.Equals(quote.OperatorId, hold.OperatorId, StringComparison.Ordinal)
            || ToBookingOccupancy(quote.Occupancy) != ToBookingOccupancy(hold.Occupancy))
        {
            return QuoteUnavailable(http);
        }

        var quoteTravellers = await pricing.QuoteTravellers.AsNoTracking()
            .Where(item => item.QuoteId == quote.Id)
            .OrderBy(item => item.Position)
            .Select(item => new
            {
                item.TravellerId,
                item.Position
            })
            .ToArrayAsync(cancellationToken);
        var travellerIds = quoteTravellers
            .Select(item => item.TravellerId)
            .ToArray();
        var travellerById = await travellerProfiles.Travellers.AsNoTracking()
            .Where(item =>
                item.OwnerAccountId == accountId
                && travellerIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var instalments = await pricing.QuoteInstalments.AsNoTracking()
            .Where(item => item.QuoteId == quote.Id)
            .OrderBy(item => item.Sequence)
            .Select(item => new BookingInstalment(
                item.Sequence,
                item.DueDate,
                item.Amount))
            .ToArrayAsync(cancellationToken);

        var occupancy = ToBookingOccupancy(quote.Occupancy);
        if (quoteTravellers.Length != quote.TravellerCount
            || quoteTravellers.Length != BookingPolicy.RequiredTravellerCount(occupancy)
            || travellerById.Count != quoteTravellers.Length
            || quoteTravellers.Any(item => !travellerById.ContainsKey(item.TravellerId)))
        {
            return QuoteUnavailable(http);
        }

        var snapshot = new BookingFinancialSnapshot(
            quote.Currency,
            quote.UnitPrice,
            quote.Total,
            quote.DueNow,
            quote.Remaining,
            instalments);
        try
        {
            BookingPolicy.ValidateSnapshot(snapshot, quoteTravellers.Length);
        }
        catch (ArgumentException)
        {
            return QuoteUnavailable(http);
        }

        await using var transaction = await bookings.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        var existingByKey = await bookings.Bookings
            .SingleOrDefaultAsync(item =>
                item.AccountId == accountId &&
                item.IdempotencyKeyHash == idempotencyKeyHash,
                cancellationToken);
        if (existingByKey is not null)
        {
            if (!string.Equals(
                    existingByKey.RequestFingerprint,
                    requestFingerprint,
                    StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                return IdempotencyConflict(http);
            }

            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(await ToResponseAsync(
                existingByKey,
                bookings,
                cancellationToken));
        }

        var existingForHold = await bookings.Bookings.AsNoTracking()
            .SingleOrDefaultAsync(item => item.InventoryHoldId == hold.Id, cancellationToken);
        if (existingForHold is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ExistingBooking(http, existingForHold.Id);
        }

        var booking = new BookingRecord
        {
            Id = Guid.NewGuid(),
            Reference = CreateReference(now),
            AccountId = accountId,
            OperatorId = quote.OperatorId,
            DepartureId = quote.DepartureId,
            QuoteId = quote.Id,
            PriceVersionId = quote.PriceVersionId,
            InventoryHoldId = hold.Id,
            Occupancy = occupancy,
            TravellerCount = quoteTravellers.Length,
            Currency = quote.Currency,
            UnitPrice = quote.UnitPrice,
            Total = quote.Total,
            DueNow = quote.DueNow,
            Remaining = quote.Remaining,
            State = BookingState.PendingPayment,
            IdempotencyKeyHash = idempotencyKeyHash,
            RequestFingerprint = requestFingerprint,
            CorrelationId = http.TraceIdentifier,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        bookings.Bookings.Add(booking);
        bookings.Travellers.AddRange(quoteTravellers.Select(quoteTraveller =>
        {
            var traveller = travellerById[quoteTraveller.TravellerId];
            return new BookingTravellerRecord
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                TravellerId = traveller.Id,
                Position = quoteTraveller.Position,
                FullName = traveller.FullName,
                DateOfBirth = traveller.DateOfBirth
            };
        }));
        bookings.Instalments.AddRange(instalments.Select(instalment =>
            new BookingInstalmentRecord
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                Sequence = instalment.Sequence,
                DueDate = instalment.DueDate,
                Amount = instalment.Amount
            }));
        bookings.OutboxMessages.Add(new BookingOutboxRecord
        {
            EventId = Guid.NewGuid(),
            EventType = "BookingCreated",
            EventVersion = 1,
            OccurredAtUtc = now,
            AggregateType = "Booking",
            AggregateId = booking.Id,
            AggregateVersion = 1,
            CorrelationId = http.TraceIdentifier,
            CausationId = hold.Id.ToString("D"),
            Payload = JsonSerializer.Serialize(new
            {
                bookingId = booking.Id,
                bookingReference = booking.Reference,
                booking.AccountId,
                booking.DepartureId,
                booking.QuoteId,
                booking.PriceVersionId,
                booking.InventoryHoldId,
                occupancy = booking.Occupancy.ToString().ToLowerInvariant(),
                booking.Currency,
                booking.Total,
                booking.DueNow,
                travellerIds
            }),
            State = "Pending",
            CreatedAtUtc = now
        });

        try
        {
            await bookings.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            bookings.ChangeTracker.Clear();

            var racedByKey = await bookings.Bookings.AsNoTracking()
                .SingleOrDefaultAsync(item =>
                    item.AccountId == accountId &&
                    item.IdempotencyKeyHash == idempotencyKeyHash,
                    cancellationToken);
            if (racedByKey is not null)
            {
                return string.Equals(
                        racedByKey.RequestFingerprint,
                        requestFingerprint,
                        StringComparison.Ordinal)
                    ? Results.Ok(await ToResponseAsync(
                        racedByKey,
                        bookings,
                        cancellationToken))
                    : IdempotencyConflict(http);
            }

            var racedForHold = await bookings.Bookings.AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.InventoryHoldId == hold.Id,
                    cancellationToken);
            return racedForHold is not null
                ? ExistingBooking(http, racedForHold.Id)
                : BookingUnavailable(http);
        }

        var duration = (timeProvider.GetUtcNow() - startedAt).TotalMilliseconds;
        log.LogInformation(
            "Booking create outcome={Outcome} bookingId={BookingId} bookingReference={BookingReference} holdId={HoldId} quoteId={QuoteId} departureId={DepartureId} dueNow={DueNow} currency={Currency} durationMs={DurationMs} correlationId={CorrelationId}",
            "created",
            booking.Id,
            booking.Reference,
            hold.Id,
            quote.Id,
            quote.DepartureId,
            quote.DueNow,
            quote.Currency,
            duration,
            http.TraceIdentifier);

        return Results.Created(
            $"/api/v1/bookings/{booking.Id:D}",
            await ToResponseAsync(booking, bookings, cancellationToken));
    }

    private static async Task<IResult> GetBookingAsync(
        Guid bookingId,
        HttpContext http,
        BookingDbContext bookings,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return NotAuthenticated(http);

        var booking = await bookings.Bookings.AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Id == bookingId &&
                item.AccountId == principal.AccountId.Value,
                cancellationToken);
        return booking is null
            ? Results.NotFound()
            : Results.Ok(await ToResponseAsync(booking, bookings, cancellationToken));
    }

    private static async Task<object> ToResponseAsync(
        BookingRecord booking,
        BookingDbContext bookings,
        CancellationToken cancellationToken)
    {
        var travellers = await bookings.Travellers.AsNoTracking()
            .Where(item => item.BookingId == booking.Id)
            .OrderBy(item => item.Position)
            .Select(item => new
            {
                item.TravellerId,
                item.Position,
                item.FullName,
                item.DateOfBirth
            })
            .ToArrayAsync(cancellationToken);
        var instalments = await bookings.Instalments.AsNoTracking()
            .Where(item => item.BookingId == booking.Id)
            .OrderBy(item => item.Sequence)
            .Select(item => new
            {
                item.Sequence,
                item.DueDate,
                item.Amount
            })
            .ToArrayAsync(cancellationToken);

        return new
        {
            bookingId = booking.Id,
            bookingReference = booking.Reference,
            booking.DepartureId,
            booking.QuoteId,
            booking.PriceVersionId,
            inventoryHoldId = booking.InventoryHoldId,
            occupancy = booking.Occupancy.ToString().ToLowerInvariant(),
            booking.TravellerCount,
            booking.Currency,
            booking.UnitPrice,
            booking.Total,
            booking.DueNow,
            booking.Remaining,
            state = booking.State.ToString(),
            travellers,
            instalments,
            booking.CreatedAtUtc,
            booking.UpdatedAtUtc
        };
    }

    private static BookingOccupancy ToBookingOccupancy(PricingOccupancy occupancy) =>
        occupancy switch
        {
            PricingOccupancy.Double => BookingOccupancy.Double,
            PricingOccupancy.Triple => BookingOccupancy.Triple,
            PricingOccupancy.Quad => BookingOccupancy.Quad,
            _ => throw new ArgumentOutOfRangeException(nameof(occupancy))
        };

    private static BookingOccupancy ToBookingOccupancy(InventoryOccupancy occupancy) =>
        occupancy switch
        {
            InventoryOccupancy.Double => BookingOccupancy.Double,
            InventoryOccupancy.Triple => BookingOccupancy.Triple,
            InventoryOccupancy.Quad => BookingOccupancy.Quad,
            _ => throw new ArgumentOutOfRangeException(nameof(occupancy))
        };

    private static string CreateReference(DateTimeOffset now) =>
        $"NP-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

    private static IResult NotAuthenticated(HttpContext http) => Results.Problem(
        statusCode: StatusCodes.Status401Unauthorized,
        title: "Authentication required",
        detail: "Sign in before creating or viewing a booking.",
        extensions: CheckoutIdempotency.ProblemExtensions(http, "authentication_required"));

    private static IResult HoldNotActive(HttpContext http) => Results.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Availability is no longer secured",
        detail: "Secure availability again before creating the booking.",
        extensions: CheckoutIdempotency.ProblemExtensions(http, "inventory_hold_not_active"));

    private static IResult QuoteUnavailable(HttpContext http) => Results.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "The quote is no longer available",
        detail: "Create a fresh quote and secure availability again.",
        extensions: CheckoutIdempotency.ProblemExtensions(http, "quote_unavailable"));

    private static IResult IdempotencyConflict(HttpContext http) => Results.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Idempotency key conflict",
        detail: "Use a new idempotency key for a different booking request.",
        extensions: CheckoutIdempotency.ProblemExtensions(http, "idempotency_conflict"));

    private static IResult ExistingBooking(HttpContext http, Guid bookingId) =>
        Results.Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "A booking already exists for this hold",
            detail: "Continue with the existing booking instead of creating another one.",
            extensions: new Dictionary<string, object?>(
                CheckoutIdempotency.ProblemExtensions(http, "booking_exists_for_hold"))
            {
                ["bookingId"] = bookingId
            });

    private static IResult BookingUnavailable(HttpContext http) => Results.Problem(
        statusCode: StatusCodes.Status409Conflict,
        title: "Booking could not be created",
        detail: "Review the booking state before trying again.",
        extensions: CheckoutIdempotency.ProblemExtensions(http, "booking_unavailable"));
}
