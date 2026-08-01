namespace NoorPath.Documents;

public enum DocumentKind { PassportBioPage, PassportPhoto }
public enum SubmissionState { Quarantined, UnderReview, Approved, CorrectionRequired, Rejected, Superseded, Deleted }
public enum MalwareStatus { Pending, Safe, Unsafe, Indeterminate }
public enum ReviewDecision { Approve, RequestCorrection, Reject }

public static class DocumentPolicy
{
    public const string Version = "v1";
    public const long MaximumBytes = 10 * 1024 * 1024;
    public static readonly TimeSpan SignedUrlLifetime = TimeSpan.FromMinutes(5);
    public static readonly IReadOnlySet<string> AllowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "application/pdf", "image/jpeg", "image/png" };
    public static readonly DocumentKind[] RequiredKinds = [DocumentKind.PassportBioPage, DocumentKind.PassportPhoto];

    public static bool SignatureMatches(string contentType, ReadOnlySpan<byte> bytes) => contentType.ToLowerInvariant() switch
    {
        "application/pdf" => bytes.StartsWith("%PDF-"u8),
        "image/jpeg" => bytes.StartsWith([0xff, 0xd8, 0xff]),
        "image/png" => bytes.StartsWith([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
        _ => false
    };

    public static SubmissionState Review(SubmissionState current, MalwareStatus malware, ReviewDecision decision)
    {
        if (current != SubmissionState.UnderReview || malware != MalwareStatus.Safe)
            throw new InvalidOperationException("Only a safe submission under review can be reviewed.");
        return decision switch
        {
            ReviewDecision.Approve => SubmissionState.Approved,
            ReviewDecision.RequestCorrection => SubmissionState.CorrectionRequired,
            ReviewDecision.Reject => SubmissionState.Rejected,
            _ => throw new ArgumentOutOfRangeException(nameof(decision))
        };
    }
}

public sealed record StoredObjectInfo(long Size, string ContentType);
public interface IPrivateDocumentStorage
{
    string CreateOpaqueKey();
    Uri PresignUpload(string objectKey, string contentType, long contentLength, DateTimeOffset expiresAt);
    Uri PresignAccess(string objectKey, DateTimeOffset expiresAt);
    Task<StoredObjectInfo?> GetInfoAsync(string objectKey, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}

public interface IMalwareScanner
{
    Task<MalwareStatus> ScanAsync(Stream content, CancellationToken cancellationToken);
}
