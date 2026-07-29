using NoorPath.Catalogue;

namespace NoorPath.Catalogue.Tests;

public sealed class BatchTests
{
    private static CreateBatch Valid() => new("test-approved-noor", "Noor Tours", "Noor Comfort", "A supported journey", "Comfort", "Delhi", "Jeddah to Makkah", new(2026, 10, 10), new(2026, 10, 22), 24, AvailabilityMode.Exact, 94500, ["Flights", "", "Flights", "Breakfast"]);

    [Fact] public void Draft_normalizes_dynamic_inclusions() { var batch = new Batch(Valid()); Assert.Equal(["Flights", "Breakfast"], batch.Details.Inclusions); }
    [Fact] public void Invalid_dates_and_capacity_are_rejected() { var command = Valid() with { ReturnDate = new(2026, 10, 9), Capacity = 0 }; var error = Assert.Throws<BatchValidationException>(() => new Batch(command)); Assert.Contains("returnDate", error.Errors.Keys); Assert.Contains("capacity", error.Errors.Keys); }
    [Fact] public void Publishing_requires_approval_and_current_version() { var batch = new Batch(Valid()); Assert.Throws<InvalidOperationException>(() => batch.Publish(1, "admin", "trace", false)); Assert.Throws<InvalidOperationException>(() => batch.Publish(2, "admin", "trace", true)); var audit = batch.Publish(1, "admin", "trace", true); Assert.Equal(BatchStatus.Published, batch.Status); Assert.Equal(BatchStatus.Draft, audit.PreviousStatus); Assert.Throws<InvalidOperationException>(() => batch.Publish(2, "admin", "trace", true)); }
    [Theory] [InlineData(AvailabilityMode.Exact)] [InlineData(AvailabilityMode.Limited)] [InlineData(AvailabilityMode.WaitlistOnly)] [InlineData(AvailabilityMode.Unavailable)] public void Availability_modes_are_allowlisted(AvailabilityMode mode) { var batch = new Batch(Valid() with { Availability = mode }); Assert.Equal(mode, batch.Details.Availability); }
}
