using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using NoorPath.Booking;
using NoorPath.Booking.Infrastructure;
using NoorPath.Catalogue.Infrastructure;
using NoorPath.Operators;
using NoorPath.Pricing;
using NoorPath.Pricing.Infrastructure;

public static class BookingAmendmentEndpoints
{
    private const string ProtectorPurpose = "NoorPath.BookingAmendmentPreview.v1";
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public static void MapBookingAmendments(this WebApplication app)
    {
        app.MapPost(
                "/api/v1/operator/bookings/{bookingId:guid}/amendments/preview",
                PreviewAsync)
            .RequireAuthorization();
        app.MapPost(
                "/api/v1/operator/bookings/{bookingId:guid}/amendments/confirm",
                ConfirmAsync)
            .RequireAuthorization();
        app.MapGet(
                "/api/v1/operator/bookings/{bookingId:guid}/amendments",
                HistoryAsync)
            .RequireAuthorization();
    }

    private static async Task<IResult> PreviewAsync(
        Guid bookingId,
        BookingAmendmentProposalRequest request,
        HttpContext http,
        IOperatorAccess operators,
        BookingDbContext bookings,
        PricingDbContext pricing,
        CatalogueDbContext catalogue,
        IDataProtectionProvider protectionProvider,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var accessResult = await ResolveOperatorAsync(http, operators, cancellationToken);
        if (accessResult.Result is not null)
            return accessResult.Result;

        var access = accessResult.Access!;
        var booking = await bookings.Bookings.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == bookingId && item.OperatorId == access.OperatorId,
                cancellationToken);
        if (booking is null)
            return Results.NotFound();

        BookingAmendmentProposal proposal;
        try
        {
            proposal = ToProposal(request);
            BookingAmendmentPolicy.ValidateProposal(
                booking.State,
                proposal,
                DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new
            {
                code = "booking_not_amendable",
                message = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["amendment"] = [exception.Message]
            });
        }

        var currentTravellers = await bookings.Travellers.AsNoTracking()
            .Where(item => item.BookingId == booking.Id)
            .OrderBy(item => item.Position)
            .Select(item => new BookingAmendmentTraveller(
                item.TravellerId,
                item.Position,
                item.FullName,
                item.DateOfBirth))
            .ToArrayAsync(cancellationToken);
        var currentInstalments = await bookings.Instalments.AsNoTracking()
            .Where(item => item.BookingId == booking.Id)
            .OrderBy(item => item.Sequence)
            .Select(item => new BookingInstalment(
                item.Sequence,
                item.DueDate,
                item.Amount))
            .ToArrayAsync(cancellationToken);

        var currentFinancials = new BookingFinancialSnapshot(
            booking.Currency,
            booking.UnitPrice,
            booking.Total,
            booking.DueNow,
            booking.Remaining,
            currentInstalments);

        BookingAmendmentCommercialSnapshot? proposedCommercials;
        try
        {
            proposedCommercials = await BuildCommercialSnapshotAsync(
                booking,
                proposal,
                pricing,
                catalogue,
                cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["pricing"] = [exception.Message]
            });
        }

        if (proposedCommercials is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["occupancy"] = [
                    "The selected occupancy does not have an active published price for this booking."
                ]
            });
        }

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(10);
        var fingerprint = Fingerprint(
            booking.Id,
            booking.Version,
            proposal,
            proposedCommercials,
            expiresAt);
        var envelope = new BookingAmendmentPreviewEnvelope(
            booking.Id,
            access.OperatorId,
            accessResult.AccountId!,
            booking.Version,
            proposal,
            proposedCommercials,
            fingerprint,
            expiresAt);
        var protector = protectionProvider.CreateProtector(ProtectorPurpose);
        var token = protector.Protect(JsonSerializer.Serialize(envelope, JsonOptions));
        var priceDelta = BookingAmendmentPolicy.CalculatePriceDelta(
            currentFinancials,
            proposedCommercials.Financials,
            proposal.Travellers.Count);

        return Results.Ok(new
        {
            bookingId = booking.Id,
            reference = booking.Reference,
            bookingVersion = booking.Version,
            current = new
            {
                occupancy = booking.Occupancy.ToString().ToLowerInvariant(),
                travellers = currentTravellers,
                financials = currentFinancials
            },
            proposed = new
            {
                occupancy = proposal.Occupancy.ToString().ToLowerInvariant(),
                travellers = proposal.Travellers,
                financials = proposedCommercials.Financials,
                priceVersionId = proposedCommercials.PriceVersionId
            },
            priceDelta,
            changesMoney = priceDelta != 0m,
            previewToken = token,
            previewFingerprint = fingerprint,
            expiresAtUtc = expiresAt
        });
    }

    private static async Task<IResult> ConfirmAsync(
        Guid bookingId,
        BookingAmendmentConfirmRequest request,
        HttpContext http,
        IOperatorAccess operators,
        BookingDbContext bookings,
        PricingDbContext pricing,
        CatalogueDbContext catalogue,
        IDataProtectionProvider protectionProvider,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!request.Confirmed)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["confirmed"] = [
                    "Explicit confirmation is required before applying a booking amendment."
                ]
            });
        }

        if (string.IsNullOrWhiteSpace(request.PreviewToken))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["previewToken"] = ["Create a fresh amendment preview before confirming."]
            });
        }

        var accessResult = await ResolveOperatorAsync(http, operators, cancellationToken);
        if (accessResult.Result is not null)
            return accessResult.Result;

        BookingAmendmentPreviewEnvelope envelope;
        try
        {
            var protector = protectionProvider.CreateProtector(ProtectorPurpose);
            var json = protector.Unprotect(request.PreviewToken);
            envelope = JsonSerializer.Deserialize<BookingAmendmentPreviewEnvelope>(
                    json,
                    JsonOptions)
                ?? throw new CryptographicException("Preview token payload is invalid.");
        }
        catch (Exception exception) when (
            exception is CryptographicException
            or JsonException
            or ArgumentException)
        {
            return Results.Conflict(new
            {
                code = "preview_invalid",
                message = "The amendment preview is invalid or no longer usable. Create a fresh preview."
            });
        }

        if (envelope.BookingId != bookingId
            || !string.Equals(
                envelope.OperatorId,
                accessResult.Access!.OperatorId,
                StringComparison.Ordinal)
            || !string.Equals(
                envelope.PrincipalAccountId,
                accessResult.AccountId,
                StringComparison.Ordinal))
        {
            return Results.NotFound();
        }

        var now = timeProvider.GetUtcNow();
        if (envelope.ExpiresAtUtc <= now)
        {
            return Results.Conflict(new
            {
                code = "preview_expired",
                message = "The amendment preview has expired. Create a fresh preview before confirming."
            });
        }

        await using var transaction = await bookings.Database.BeginTransactionAsync(cancellationToken);
        var booking = await bookings.Bookings
            .SingleOrDefaultAsync(
                item => item.Id == bookingId
                    && item.OperatorId == accessResult.Access.OperatorId,
                cancellationToken);
        if (booking is null)
            return Results.NotFound();

        if (booking.Version != envelope.BookingVersion)
        {
            return Results.Conflict(new
            {
                code = "booking_stale",
                message = "The booking changed after this preview was created. Refresh and preview again.",
                bookingVersion = booking.Version
            });
        }

        try
        {
            BookingAmendmentPolicy.ValidateProposal(
                booking.State,
                envelope.Proposal,
                DateOnly.FromDateTime(now.UtcDateTime));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Conflict(new
            {
                code = "booking_not_amendable",
                message = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["amendment"] = [exception.Message]
            });
        }

        BookingAmendmentCommercialSnapshot? recalculated;
        try
        {
            recalculated = await BuildCommercialSnapshotAsync(
                booking,
                envelope.Proposal,
                pricing,
                catalogue,
                cancellationToken);
        }
        catch (ArgumentException)
        {
            recalculated = null;
        }

        if (recalculated is null)
        {
            return Results.Conflict(new
            {
                code = "pricing_changed",
                message = "Published pricing changed after preview. Create a fresh preview."
            });
        }

        var expectedFingerprint = Fingerprint(
            booking.Id,
            booking.Version,
            envelope.Proposal,
            recalculated,
            envelope.ExpiresAtUtc);
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedFingerprint),
                Encoding.UTF8.GetBytes(envelope.Fingerprint)))
        {
            return Results.Conflict(new
            {
                code = "preview_stale",
                message = "The amendment preview no longer matches current authoritative pricing. Create a fresh preview."
            });
        }

        var beforeTravellers = await bookings.Travellers
            .Where(item => item.BookingId == booking.Id)
            .OrderBy(item => item.Position)
            .ToArrayAsync(cancellationToken);
        var beforeInstalments = await bookings.Instalments
            .Where(item => item.BookingId == booking.Id)
            .OrderBy(item => item.Sequence)
            .ToArrayAsync(cancellationToken);
        var beforeSnapshot = new BookingAmendmentAuditSnapshot(
            booking.Occupancy,
            beforeTravellers.Select(ToTraveller).ToArray(),
            new BookingFinancialSnapshot(
                booking.Currency,
                booking.UnitPrice,
                booking.Total,
                booking.DueNow,
                booking.Remaining,
                beforeInstalments.Select(item => new BookingInstalment(
                    item.Sequence,
                    item.DueDate,
                    item.Amount)).ToArray()),
            booking.PriceVersionId);

        var priceDelta = recalculated.Financials.Total - booking.Total;
        bookings.Travellers.RemoveRange(beforeTravellers);
        bookings.Instalments.RemoveRange(beforeInstalments);

        foreach (var traveller in envelope.Proposal.Travellers.OrderBy(item => item.Position))
        {
            bookings.Travellers.Add(new BookingTravellerRecord
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                TravellerId = traveller.TravellerId,
                Position = traveller.Position,
                FullName = traveller.FullName.Trim(),
                DateOfBirth = traveller.DateOfBirth
            });
        }

        foreach (var instalment in recalculated.Financials.Instalments)
        {
            bookings.Instalments.Add(new BookingInstalmentRecord
            {
                Id = Guid.NewGuid(),
                BookingId = booking.Id,
                Sequence = instalment.Sequence,
                DueDate = instalment.DueDate,
                Amount = instalment.Amount
            });
        }

        var previousVersion = booking.Version;
        booking.Occupancy = envelope.Proposal.Occupancy;
        booking.TravellerCount = envelope.Proposal.Travellers.Count;
        booking.PriceVersionId = recalculated.PriceVersionId;
        booking.Currency = recalculated.Financials.Currency;
        booking.UnitPrice = recalculated.Financials.UnitPrice;
        booking.Total = recalculated.Financials.Total;
        booking.DueNow = recalculated.Financials.DueNow;
        booking.Remaining = recalculated.Financials.Remaining;
        booking.Version++;
        booking.UpdatedAtUtc = now;

        var afterSnapshot = new BookingAmendmentAuditSnapshot(
            booking.Occupancy,
            envelope.Proposal.Travellers.OrderBy(item => item.Position).ToArray(),
            recalculated.Financials,
            recalculated.PriceVersionId);
        var amendmentId = Guid.NewGuid();
        bookings.Amendments.Add(new BookingAmendmentRecord
        {
            Id = amendmentId,
            BookingId = booking.Id,
            OperatorId = accessResult.Access.OperatorId,
            ActorAccountId = accessResult.AccountId!,
            Reason = envelope.Proposal.Reason.Trim(),
            PreviousBookingVersion = previousVersion,
            ResultingBookingVersion = booking.Version,
            PriceDelta = priceDelta,
            Currency = booking.Currency,
            PreviewFingerprint = envelope.Fingerprint,
            BeforeSnapshotJson = JsonSerializer.Serialize(beforeSnapshot, JsonOptions),
            AfterSnapshotJson = JsonSerializer.Serialize(afterSnapshot, JsonOptions),
            CorrelationId = http.TraceIdentifier,
            OccurredAtUtc = now
        });

        try
        {
            await bookings.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.Conflict(new
            {
                code = "booking_stale",
                message = "The booking changed while the amendment was being applied. Refresh and preview again."
            });
        }

        return Results.Ok(new
        {
            amendmentId,
            bookingId = booking.Id,
            bookingVersion = booking.Version,
            priceDelta,
            currency = booking.Currency,
            followUp = priceDelta switch
            {
                > 0m => "Additional collection must be handled in the Payments workflow.",
                < 0m => "Any credit or refund consequence must be handled in the governed refund workflow.",
                _ => "No payment follow-up is required from this amendment."
            }
        });
    }

    private static async Task<IResult> HistoryAsync(
        Guid bookingId,
        HttpContext http,
        IOperatorAccess operators,
        BookingDbContext bookings,
        CancellationToken cancellationToken)
    {
        var accessResult = await ResolveOperatorAsync(http, operators, cancellationToken);
        if (accessResult.Result is not null)
            return accessResult.Result;

        var owned = await bookings.Bookings.AsNoTracking()
            .AnyAsync(
                item => item.Id == bookingId
                    && item.OperatorId == accessResult.Access!.OperatorId,
                cancellationToken);
        if (!owned)
            return Results.NotFound();

        var history = await bookings.Amendments.AsNoTracking()
            .Where(item => item.BookingId == bookingId)
            .OrderByDescending(item => item.OccurredAtUtc)
            .Select(item => new
            {
                amendmentId = item.Id,
                item.Reason,
                item.PreviousBookingVersion,
                item.ResultingBookingVersion,
                item.Currency,
                item.PriceDelta,
                item.OccurredAtUtc,
                item.CorrelationId
            })
            .ToArrayAsync(cancellationToken);

        return Results.Ok(history);
    }

    private static async Task<BookingAmendmentCommercialSnapshot?> BuildCommercialSnapshotAsync(
        BookingRecord booking,
        BookingAmendmentProposal proposal,
        PricingDbContext pricing,
        CatalogueDbContext catalogue,
        CancellationToken cancellationToken)
    {
        var occupancy = proposal.Occupancy switch
        {
            BookingOccupancy.Double => PricingOccupancy.Double,
            BookingOccupancy.Triple => PricingOccupancy.Triple,
            BookingOccupancy.Quad => PricingOccupancy.Quad,
            _ => throw new ArgumentOutOfRangeException(nameof(proposal))
        };

        var version = await pricing.PriceVersions.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == booking.PriceVersionId
                    && item.OperatorId == booking.OperatorId
                    && item.DepartureId == booking.DepartureId,
                cancellationToken);
        if (version is null)
            return null;

        var publishedPrice = await pricing.PublishedOccupancyPrices.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.PriceVersionId == version.Id
                    && item.Occupancy == occupancy,
                cancellationToken);
        if (publishedPrice is null)
            return null;

        var departureDate = await catalogue.DepartureBatches.AsNoTracking()
            .Where(item => item.Id == booking.DepartureId)
            .Select(item => (DateOnly?)item.DepartureDate)
            .SingleOrDefaultAsync(cancellationToken);
        if (departureDate is null)
            return null;

        var travellerCount = proposal.Travellers.Count;
        var total = decimal.Round(
            publishedPrice.Amount * travellerCount,
            2,
            MidpointRounding.ToEven);
        var schedule = QuoteScheduleCalculator.Calculate(
            total,
            departureDate.Value,
            booking.CreatedAtUtc,
            version.PaymentPlan);
        var financials = new BookingFinancialSnapshot(
            version.Currency,
            publishedPrice.Amount,
            schedule.Total,
            schedule.DueNow,
            schedule.Remaining,
            schedule.Instalments
                .Select(item => new BookingInstalment(
                    item.Sequence,
                    item.DueDate,
                    item.Amount))
                .ToArray());

        return new BookingAmendmentCommercialSnapshot(version.Id, financials);
    }

    private static BookingAmendmentProposal ToProposal(
        BookingAmendmentProposalRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!Enum.TryParse<BookingOccupancy>(request.Occupancy, true, out var occupancy))
        {
            throw new ArgumentException(
                "Occupancy must be Double, Triple or Quad.",
                nameof(request));
        }

        if (request.Travellers is null)
            throw new ArgumentException("Travellers are required.", nameof(request));

        return new BookingAmendmentProposal(
            occupancy,
            request.Travellers.Select((item, index) => new BookingAmendmentTraveller(
                item.TravellerId == Guid.Empty ? Guid.NewGuid() : item.TravellerId,
                index + 1,
                item.FullName,
                item.DateOfBirth)).ToArray(),
            request.Reason);
    }

    private static BookingAmendmentTraveller ToTraveller(
        BookingTravellerRecord item) =>
        new(item.TravellerId, item.Position, item.FullName, item.DateOfBirth);

    private static string Fingerprint(
        Guid bookingId,
        int bookingVersion,
        BookingAmendmentProposal proposal,
        BookingAmendmentCommercialSnapshot commercials,
        DateTimeOffset expiresAtUtc)
    {
        var payload = JsonSerializer.Serialize(new
        {
            bookingId,
            bookingVersion,
            proposal,
            commercials,
            expiresAtUtc
        }, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))
            .ToLowerInvariant();
    }

    private static async Task<OperatorResolution> ResolveOperatorAsync(
        HttpContext http,
        IOperatorAccess operators,
        CancellationToken cancellationToken)
    {
        var principal = http.User.GetCurrentPrincipal();
        if (principal is null)
            return new(null, null, Results.Unauthorized());

        var access = await operators.FindActiveMembershipAsync(
            principal.AccountId,
            cancellationToken);
        if (access is null || !access.IsAllowed(OperatorPermissions.AdminAccess))
            return new(principal.AccountId.Value, null, Results.Forbid());

        return new(principal.AccountId.Value, access, null);
    }

    private sealed record OperatorResolution(
        string? AccountId,
        OperatorAccess? Access,
        IResult? Result);

    private sealed record BookingAmendmentPreviewEnvelope(
        Guid BookingId,
        string OperatorId,
        string PrincipalAccountId,
        int BookingVersion,
        BookingAmendmentProposal Proposal,
        BookingAmendmentCommercialSnapshot Commercials,
        string Fingerprint,
        DateTimeOffset ExpiresAtUtc);

    private sealed record BookingAmendmentAuditSnapshot(
        BookingOccupancy Occupancy,
        IReadOnlyList<BookingAmendmentTraveller> Travellers,
        BookingFinancialSnapshot Financials,
        Guid PriceVersionId);
}

public sealed record BookingAmendmentProposalRequest(
    string Occupancy,
    IReadOnlyList<BookingAmendmentTravellerRequest> Travellers,
    string Reason);

public sealed record BookingAmendmentTravellerRequest(
    Guid TravellerId,
    string FullName,
    DateOnly DateOfBirth);

public sealed record BookingAmendmentConfirmRequest(
    string PreviewToken,
    bool Confirmed);
