namespace NoorPath.Booking;

public sealed record BookingAmendmentTraveller(
    Guid TravellerId,
    int Position,
    string FullName,
    DateOnly DateOfBirth);

public sealed record BookingAmendmentProposal(
    BookingOccupancy Occupancy,
    IReadOnlyList<BookingAmendmentTraveller> Travellers,
    string Reason);

public sealed record BookingAmendmentCommercialSnapshot(
    Guid PriceVersionId,
    BookingFinancialSnapshot Financials);

public sealed record BookingAmendmentPreview(
    Guid BookingId,
    int BookingVersion,
    BookingOccupancy CurrentOccupancy,
    IReadOnlyList<BookingAmendmentTraveller> CurrentTravellers,
    BookingFinancialSnapshot CurrentFinancials,
    BookingAmendmentProposal Proposal,
    BookingAmendmentCommercialSnapshot ProposedCommercials,
    decimal PriceDelta,
    string PreviewFingerprint,
    DateTimeOffset ExpiresAtUtc);

public static class BookingAmendmentPolicy
{
    public const int MaximumReasonLength = 500;

    public static bool CanAmend(BookingState state) => state == BookingState.Confirmed;

    public static void ValidateProposal(
        BookingState state,
        BookingAmendmentProposal proposal,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(proposal);

        if (!CanAmend(state))
            throw new InvalidOperationException($"Bookings in {state} state cannot be amended.");

        var reason = proposal.Reason?.Trim() ?? string.Empty;
        if (reason.Length is 0 or > MaximumReasonLength)
        {
            throw new ArgumentException(
                $"Amendment reason must be between 1 and {MaximumReasonLength} characters.",
                nameof(proposal));
        }

        var requiredCount = BookingPolicy.RequiredTravellerCount(proposal.Occupancy);
        if (proposal.Travellers.Count != requiredCount)
        {
            throw new ArgumentException(
                "Traveller count must match the selected occupancy.",
                nameof(proposal));
        }

        var expectedPosition = 1;
        var travellerIds = new HashSet<Guid>();
        foreach (var traveller in proposal.Travellers.OrderBy(x => x.Position))
        {
            if (traveller.TravellerId == Guid.Empty)
                throw new ArgumentException("Traveller id is required.", nameof(proposal));

            if (!travellerIds.Add(traveller.TravellerId))
                throw new ArgumentException("Traveller ids must be unique within a booking.", nameof(proposal));

            if (traveller.Position != expectedPosition++)
                throw new ArgumentException("Traveller positions must be contiguous from one.", nameof(proposal));

            var name = traveller.FullName?.Trim() ?? string.Empty;
            if (name.Length is 0 or > 120)
                throw new ArgumentException("Traveller name must be between 1 and 120 characters.", nameof(proposal));

            if (traveller.DateOfBirth >= today)
                throw new ArgumentException("Traveller date of birth must be in the past.", nameof(proposal));
        }
    }

    public static decimal CalculatePriceDelta(
        BookingFinancialSnapshot current,
        BookingFinancialSnapshot proposed,
        int proposedTravellerCount)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(proposed);

        BookingPolicy.ValidateSnapshot(proposed, proposedTravellerCount);

        if (!string.Equals(current.Currency, proposed.Currency, StringComparison.Ordinal))
            throw new InvalidOperationException("Booking amendment cannot change booking currency.");

        return proposed.Total - current.Total;
    }

    public static void ValidatePreviewForConfirmation(
        BookingAmendmentPreview preview,
        int currentBookingVersion,
        string previewFingerprint,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(preview);

        if (preview.BookingVersion != currentBookingVersion)
            throw new InvalidOperationException("Booking changed after the amendment preview was created.");

        if (!string.Equals(preview.PreviewFingerprint, previewFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("Amendment preview no longer matches the proposed change.");

        if (preview.ExpiresAtUtc <= now)
            throw new InvalidOperationException("Amendment preview has expired.");
    }
}
