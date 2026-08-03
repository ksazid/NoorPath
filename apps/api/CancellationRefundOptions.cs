using Microsoft.Extensions.Options;
using NoorPath.Booking;
using NoorPath.Payments;

public sealed class CancellationPolicyOptions
{
    public const string SectionName = "CancellationPolicy";

    public bool Enabled { get; set; }
    public string Version { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = string.Empty;
    public string DepartureLocalTime { get; set; } = string.Empty;
    public int RefundProcessingBusinessDays { get; set; }
    public List<CancellationWindowOptions> Windows { get; set; } = [];
}

public sealed class CancellationWindowOptions
{
    public int MinimumDaysBeforeDeparture { get; set; }
    public int FeeBasisPoints { get; set; }
    public decimal NonRefundableAmount { get; set; }
}

public sealed class CancellationPolicyProvider(
    IOptions<CancellationPolicyOptions> configured)
{
    public bool TryGet(
        out CancellationPolicyDefinition? definition,
        out string code,
        out string message)
    {
        var options = configured.Value;
        definition = null;
        if (!options.Enabled)
        {
            code = "cancellation_policy_disabled";
            message = "Online cancellation is not enabled for this environment. Contact NoorPath support.";
            return false;
        }

        if (!TimeOnly.TryParse(options.DepartureLocalTime, out var departureLocalTime))
        {
            code = "cancellation_policy_invalid";
            message = "The cancellation policy is temporarily unavailable. Contact NoorPath support.";
            return false;
        }

        try
        {
            definition = new CancellationPolicyDefinition(
                options.Version.Trim(),
                options.TimeZoneId.Trim(),
                departureLocalTime,
                options.RefundProcessingBusinessDays,
                options.Windows.Select(item => new CancellationWindowPolicy(
                    item.MinimumDaysBeforeDeparture,
                    item.FeeBasisPoints,
                    item.NonRefundableAmount)).ToArray());
            CancellationPolicy.Validate(definition);
            code = "available";
            message = "The configured cancellation policy is available.";
            return true;
        }
        catch (ArgumentException)
        {
            definition = null;
            code = "cancellation_policy_invalid";
            message = "The cancellation policy is temporarily unavailable. Contact NoorPath support.";
            return false;
        }
        catch (TimeZoneNotFoundException)
        {
            definition = null;
            code = "cancellation_policy_invalid";
            message = "The cancellation policy is temporarily unavailable. Contact NoorPath support.";
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            definition = null;
            code = "cancellation_policy_invalid";
            message = "The cancellation policy is temporarily unavailable. Contact NoorPath support.";
            return false;
        }
    }
}

public sealed class RefundExecutionOptions
{
    public const string SectionName = "RefundExecution";

    public bool Enabled { get; set; }
}

public sealed class DisabledRefundProviderGateway(
    IOptions<RefundExecutionOptions> options) : IRefundProviderGateway
{
    public Task<RefundProviderResult> ExecuteAsync(
        RefundProviderRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var code = options.Value.Enabled
            ? "refund_provider_not_configured"
            : "refund_execution_disabled";
        return Task.FromResult(RefundProviderResult.Unavailable(code));
    }
}
