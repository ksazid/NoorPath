using NoorPath.Visa;
using Xunit;

namespace NoorPath.Visa.Tests;

public sealed class VisaPolicyTests
{
    [Fact] public void Lifecycle_is_explicit() { Assert.Contains(VisaStatus.AwaitingDocuments, VisaPolicy.AllowedNext(VisaStatus.NotStarted)); Assert.Empty(VisaPolicy.AllowedNext(VisaStatus.Approved)); Assert.Empty(VisaPolicy.AllowedNext(VisaStatus.Rejected)); }
    [Fact] public void Action_required_needs_a_reason() => Assert.Throws<ArgumentException>(() => VisaPolicy.Validate(VisaStatus.Processing, VisaStatus.ActionRequired, null));
    [Fact] public void Arbitrary_transition_is_rejected() => Assert.Throws<InvalidOperationException>(() => VisaPolicy.Validate(VisaStatus.NotStarted, VisaStatus.Approved, null));
    [Fact] public void Corrections_are_governed() { VisaPolicy.Validate(VisaStatus.ActionRequired, VisaStatus.AwaitingDocuments, "New passport needed"); }
}
