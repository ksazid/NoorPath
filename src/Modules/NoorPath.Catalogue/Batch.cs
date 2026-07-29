namespace NoorPath.Catalogue;

public enum BatchStatus { Draft, Published }
public enum AvailabilityMode { Exact, Limited, WaitlistOnly, Unavailable }

public sealed record CreateBatch(
    string OperatorId, string OperatorName, string PackageName, string Summary,
    string Tier, string DepartureCity, string Route, DateOnly DepartureDate,
    DateOnly ReturnDate, int Capacity, AvailabilityMode Availability,
    decimal TotalPriceInr, IReadOnlyList<string> Inclusions);

public sealed class Batch
{
    public Guid Id { get; } = Guid.NewGuid();
    public CreateBatch Details { get; }
    public BatchStatus Status { get; private set; } = BatchStatus.Draft;
    public int Version { get; private set; } = 1;
    public DateTimeOffset? PublishedAt { get; private set; }

    public Batch(CreateBatch details)
    {
        var errors = Validate(details);
        if (errors.Count != 0) throw new BatchValidationException(errors);
        Details = details with { Inclusions = details.Inclusions.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().ToArray() };
    }

    public PublicationAudit Publish(int expectedVersion, string actor, string correlationId, bool operatorApproved)
    {
        if (!operatorApproved) throw new InvalidOperationException("The operator is not approved for publication.");
        if (Status == BatchStatus.Published) throw new InvalidOperationException("The batch is already published.");
        if (Version != expectedVersion) throw new InvalidOperationException("The draft changed. Refresh and review it again.");
        var previous = Status;
        Status = BatchStatus.Published;
        Version++;
        PublishedAt = DateTimeOffset.UtcNow;
        return new(Id, actor, correlationId, previous, Status, PublishedAt.Value);
    }

    public static Dictionary<string, string[]> Validate(CreateBatch value)
    {
        var errors = new Dictionary<string, string[]>();
        void Required(string key, string text, int max) { if (string.IsNullOrWhiteSpace(text)) errors[key] = ["This field is required."]; else if (text.Length > max) errors[key] = [$"Must be {max} characters or fewer."]; }
        Required("operatorId", value.OperatorId, 80); Required("operatorName", value.OperatorName, 120);
        Required("packageName", value.PackageName, 120); Required("summary", value.Summary, 300);
        Required("tier", value.Tier, 40); Required("departureCity", value.DepartureCity, 80); Required("route", value.Route, 160);
        if (value.ReturnDate <= value.DepartureDate) errors["returnDate"] = ["Return date must be after departure."];
        if (value.Capacity <= 0) errors["capacity"] = ["Capacity must be greater than zero."];
        if (value.TotalPriceInr <= 0) errors["totalPriceInr"] = ["An effective INR total price is required."];
        if (value.Inclusions.Count > 20 || value.Inclusions.Any(x => x.Length > 80)) errors["inclusions"] = ["Use at most 20 inclusion highlights of 80 characters each."];
        return errors;
    }
}

public sealed class BatchValidationException(Dictionary<string, string[]> errors) : Exception("Batch validation failed.") { public Dictionary<string, string[]> Errors { get; } = errors; }
public sealed record PublicationAudit(Guid BatchId, string Actor, string CorrelationId, BatchStatus PreviousStatus, BatchStatus NewStatus, DateTimeOffset Timestamp);
public sealed record PublicBatch(Guid Id, string OperatorName, bool OperatorVerified, string PackageName, string Summary, string Tier, string DepartureCity, string Route, DateOnly DepartureDate, DateOnly ReturnDate, int DurationDays, int Capacity, decimal TotalStartingPriceInr, AvailabilityMode Availability, IReadOnlyList<string> Inclusions);

public interface IOperatorApproval
{
    bool IsApprovedTestOperator(string operatorId);
}
