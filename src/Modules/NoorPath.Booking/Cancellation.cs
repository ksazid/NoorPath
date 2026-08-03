namespace NoorPath.Booking;

public enum BookingCancellationState
{
    Requested,
    Approved,
    Rejected,
    Applying,
    Applied,
    Exception
}

public enum CancellationReasonCategory
{
    PlansChanged,
    Health,
    TravelDocuments,
    Financial,
    Other
}

public sealed record CancellationWindowPolicy(
    int MinimumDaysBeforeDeparture,
    int FeeBasisPoints,
    decimal NonRefundableAmount);

public sealed record CancellationPolicyDefinition(
    string Version,
    string TimeZoneId,
    TimeOnly DepartureLocalTime,
    int RefundProcessingBusinessDays,
    IReadOnlyList<CancellationWindowPolicy> Windows);

public sealed record CancellationFeeComponent(
    string Code,
    string Label,
    decimal Amount);

public sealed record CancellationEntitlement(
    string PolicyVersion,
    string PolicyTimeZoneId,
    DateTimeOffset DepartureAtUtc,
    int DaysBeforeDeparture,
    int WindowMinimumDaysBeforeDeparture,
    int FeeBasisPoints,
    string Currency,
    decimal SettledAmount,
    decimal PercentageFee,
    decimal NonRefundableAmount,
    decimal RefundableAmount,
    int RefundProcessingBusinessDays,
    IReadOnlyList<CancellationFeeComponent> FeeComponents);

public sealed record CancellationEvaluation(
    bool IsEligible,
    string Code,
    string Message,
    CancellationEntitlement? Entitlement);

public static class CancellationPolicy
{
    public static void Validate(CancellationPolicyDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (string.IsNullOrWhiteSpace(definition.Version)
            || definition.Version.Length > 80)
        {
            throw new ArgumentException("A policy version is required.", nameof(definition));
        }

        if (string.IsNullOrWhiteSpace(definition.TimeZoneId)
            || definition.TimeZoneId.Length > 80)
        {
            throw new ArgumentException("A policy timezone is required.", nameof(definition));
        }

        if (definition.RefundProcessingBusinessDays is < 1 or > 60)
        {
            throw new ArgumentOutOfRangeException(
                nameof(definition),
                "Refund processing days must be between 1 and 60.");
        }

        if (definition.Windows.Count == 0)
            throw new ArgumentException("At least one cancellation window is required.", nameof(definition));

        if (definition.Windows.Select(item => item.MinimumDaysBeforeDeparture).Distinct().Count()
            != definition.Windows.Count)
        {
            throw new ArgumentException("Cancellation window thresholds must be unique.", nameof(definition));
        }

        foreach (var window in definition.Windows)
        {
            if (window.MinimumDaysBeforeDeparture < 0)
                throw new ArgumentOutOfRangeException(nameof(definition), "Cancellation window days cannot be negative.");
            if (window.FeeBasisPoints is < 0 or > 10_000)
                throw new ArgumentOutOfRangeException(nameof(definition), "Cancellation fee basis points must be between 0 and 10000.");
            if (window.NonRefundableAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(definition), "Non-refundable amounts cannot be negative.");
        }

        _ = TimeZoneInfo.FindSystemTimeZoneById(definition.TimeZoneId);
    }

    public static CancellationEvaluation Evaluate(
        CancellationPolicyDefinition definition,
        DateTimeOffset nowUtc,
        DateOnly departureDate,
        string currency,
        decimal settledAmount)
    {
        Validate(definition);
        ValidateMoney(currency, settledAmount);

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(definition.TimeZoneId);
        var departureLocal = departureDate.ToDateTime(
            definition.DepartureLocalTime,
            DateTimeKind.Unspecified);
        var departureAtUtc = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(departureLocal, timeZone),
            TimeSpan.Zero);

        if (nowUtc >= departureAtUtc)
        {
            return new CancellationEvaluation(
                false,
                "departure_started",
                "Online cancellation is no longer available because the configured departure cutoff has passed.",
                null);
        }

        var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeZone);
        var daysBeforeDeparture = departureDate.DayNumber
            - DateOnly.FromDateTime(localNow.DateTime).DayNumber;
        var window = definition.Windows
            .OrderByDescending(item => item.MinimumDaysBeforeDeparture)
            .FirstOrDefault(item => daysBeforeDeparture >= item.MinimumDaysBeforeDeparture);
        if (window is null)
        {
            return new CancellationEvaluation(
                false,
                "cancellation_window_closed",
                "This booking is outside the configured online cancellation windows and requires human support.",
                null);
        }

        var percentageFee = decimal.Round(
            settledAmount * window.FeeBasisPoints / 10_000m,
            2,
            MidpointRounding.AwayFromZero);
        percentageFee = Math.Min(settledAmount, percentageFee);
        var nonRefundable = Math.Min(
            Math.Max(0m, settledAmount - percentageFee),
            window.NonRefundableAmount);
        var refundable = Math.Max(0m, settledAmount - percentageFee - nonRefundable);

        var components = new List<CancellationFeeComponent>();
        if (percentageFee > 0)
        {
            components.Add(new CancellationFeeComponent(
                "policy_percentage_fee",
                $"Policy fee ({window.FeeBasisPoints / 100m:0.##}%)",
                percentageFee));
        }
        if (nonRefundable > 0)
        {
            components.Add(new CancellationFeeComponent(
                "non_refundable_component",
                "Configured non-refundable component",
                nonRefundable));
        }

        return new CancellationEvaluation(
            true,
            "eligible_for_review",
            "This estimate is eligible for operator review. The immutable policy snapshot will be retained with the request.",
            new CancellationEntitlement(
                definition.Version,
                definition.TimeZoneId,
                departureAtUtc,
                daysBeforeDeparture,
                window.MinimumDaysBeforeDeparture,
                window.FeeBasisPoints,
                currency,
                settledAmount,
                percentageFee,
                nonRefundable,
                refundable,
                definition.RefundProcessingBusinessDays,
                components));
    }

    private static void ValidateMoney(string currency, decimal amount)
    {
        if (currency.Length != 3 || !currency.All(char.IsAsciiLetterUpper))
            throw new ArgumentException("Currency must be a three-letter uppercase code.", nameof(currency));
        if (amount < 0)
            throw new ArgumentOutOfRangeException(nameof(amount));
    }
}
