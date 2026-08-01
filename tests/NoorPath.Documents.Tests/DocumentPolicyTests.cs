using NoorPath.Documents;
using Xunit;

namespace NoorPath.Documents.Tests;

public sealed class DocumentPolicyTests
{
    [Theory]
    [InlineData("application/pdf", "%PDF-1.7")]
    [InlineData("image/png", "\u0089PNG\r\n\u001a\n")]
    public void Recognizes_allowed_signatures(string type, string value) =>
        Assert.True(
            DocumentPolicy.SignatureMatches(
                type,
                System.Text.Encoding.Latin1.GetBytes(value)));

    [Fact]
    public void Unsafe_submission_cannot_be_approved() =>
        Assert.Throws<InvalidOperationException>(
            () => DocumentPolicy.Review(
                SubmissionState.UnderReview,
                MalwareStatus.Indeterminate,
                ReviewDecision.Approve));

    [Theory]
    [InlineData(ReviewDecision.Approve, SubmissionState.Approved)]
    [InlineData(ReviewDecision.RequestCorrection, SubmissionState.CorrectionRequired)]
    [InlineData(ReviewDecision.Reject, SubmissionState.Rejected)]
    public void Safe_review_transitions_are_explicit(
        ReviewDecision decision,
        SubmissionState expected) =>
        Assert.Equal(
            expected,
            DocumentPolicy.Review(
                SubmissionState.UnderReview,
                MalwareStatus.Safe,
                decision));
}
