using System.Net.Sockets;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using NoorPath.Documents;

namespace NoorPath.Documents.Infrastructure;

public sealed class DocumentStorageOptions
{
    public const string SectionName = "Documents";
    public bool ProductionEnabled { get; set; } = false;
    public string Bucket { get; set; } = "";
    public string ClamAvHost { get; set; } = "localhost";
    public int ClamAvPort { get; set; } = 3310;
}

public sealed class DisabledDocumentStorage : IPrivateDocumentStorage
{
    private static InvalidOperationException Disabled() => new("Production document storage is disabled.");

    public string CreateOpaqueKey() => throw Disabled();
    public Uri PresignUpload(string key, string type, long length, DateTimeOffset expires) => throw Disabled();
    public Uri PresignAccess(string key, DateTimeOffset expires) => throw Disabled();
    public Task<StoredObjectInfo?> GetInfoAsync(string key, CancellationToken cancellationToken) => throw Disabled();
    public Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) => throw Disabled();
    public Task DeleteAsync(string key, CancellationToken cancellationToken) => throw Disabled();
}

public sealed class S3DocumentStorage(IAmazonS3 s3, IOptions<DocumentStorageOptions> options) : IPrivateDocumentStorage
{
    private readonly DocumentStorageOptions config = options.Value;

    public string CreateOpaqueKey() => $"quarantine/{Guid.NewGuid():N}";

    public Uri PresignUpload(string key, string type, long length, DateTimeOffset expires) =>
        new(s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = config.Bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            Expires = expires.UtcDateTime,
            ContentType = type,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
        }));

    public Uri PresignAccess(string key, DateTimeOffset expires) =>
        new(s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = config.Bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = expires.UtcDateTime
        }));

    public async Task<StoredObjectInfo?> GetInfoAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await s3.GetObjectMetadataAsync(config.Bucket, key, cancellationToken);
            return new(metadata.ContentLength, metadata.Headers.ContentType);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken) =>
        (await s3.GetObjectAsync(config.Bucket, key, cancellationToken)).ResponseStream;

    public async Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        await s3.DeleteObjectAsync(config.Bucket, key, cancellationToken);
}

public sealed class ClamAvScanner(IOptions<DocumentStorageOptions> options) : IMalwareScanner
{
    public async Task<MalwareStatus> ScanAsync(Stream content, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            await client.ConnectAsync(
                options.Value.ClamAvHost,
                options.Value.ClamAvPort,
                cancellationToken);

            await using var network = client.GetStream();
            await network.WriteAsync("zINSTREAM\0"u8.ToArray(), cancellationToken);

            var buffer = new byte[81920];
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await network.WriteAsync(
                    BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(read)),
                    cancellationToken);
                await network.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            await network.WriteAsync(new byte[4], cancellationToken);
            var resultBuffer = new byte[512];
            read = await network.ReadAsync(resultBuffer, cancellationToken);
            var result = Encoding.UTF8.GetString(resultBuffer, 0, read);

            return result.Contains(" OK", StringComparison.Ordinal)
                ? MalwareStatus.Safe
                : result.Contains(" FOUND", StringComparison.Ordinal)
                    ? MalwareStatus.Unsafe
                    : MalwareStatus.Indeterminate;
        }
        catch
        {
            return MalwareStatus.Indeterminate;
        }
    }
}
