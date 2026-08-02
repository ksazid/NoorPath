namespace NoorPath.Visa;

public enum VisaStatus { NotStarted, AwaitingDocuments, ReadyToSubmit, Submitted, Processing, Approved, ActionRequired, Rejected }

public static class VisaPolicy
{
    public static IReadOnlyList<VisaStatus> AllowedNext(VisaStatus current) => current switch
    {
        VisaStatus.NotStarted => [VisaStatus.AwaitingDocuments],
        VisaStatus.AwaitingDocuments => [VisaStatus.ReadyToSubmit],
        VisaStatus.ReadyToSubmit => [VisaStatus.Submitted, VisaStatus.AwaitingDocuments],
        VisaStatus.Submitted => [VisaStatus.Processing, VisaStatus.ActionRequired],
        VisaStatus.Processing => [VisaStatus.Approved, VisaStatus.ActionRequired, VisaStatus.Rejected],
        VisaStatus.ActionRequired => [VisaStatus.AwaitingDocuments, VisaStatus.ReadyToSubmit, VisaStatus.Submitted],
        _ => []
    };

    public static bool RequiresReason(VisaStatus next) => next is VisaStatus.ActionRequired or VisaStatus.Rejected;

    public static void Validate(VisaStatus current, VisaStatus next, string? reason)
    {
        if (!AllowedNext(current).Contains(next)) throw new InvalidOperationException("The visa transition is not allowed.");
        if (RequiresReason(next) && string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A customer-safe reason is required.", nameof(reason));
    }

    public static string CustomerLabel(VisaStatus status) => status switch
    {
        VisaStatus.NotStarted => "Not started",
        VisaStatus.AwaitingDocuments => "Waiting for documents",
        VisaStatus.ReadyToSubmit => "Ready for operator submission",
        VisaStatus.Submitted or VisaStatus.Processing => "In progress",
        VisaStatus.Approved => "Approved",
        VisaStatus.ActionRequired => "Action required",
        VisaStatus.Rejected => "Not approved",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
