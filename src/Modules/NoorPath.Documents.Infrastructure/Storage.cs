using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using NoorPath.Documents;
using System.Net.Sockets;
using System.Text;

namespace NoorPath.Documents.Infrastructure;
public sealed class DocumentStorageOptions { public const string SectionName = "Documents"; public bool ProductionEnabled { get; set; } = false; public string Bucket { get; set; } = ""; public string ClamAvHost { get; set; } = "localhost"; public int ClamAvPort { get; set; } = 3310; }
public sealed class DisabledDocumentStorage : IPrivateDocumentStorage
{
    private static InvalidOperationException Disabled() => new("Production document storage is disabled.");
    public string CreateOpaqueKey() => throw Disabled(); public Uri PresignUpload(string k,string t,long l,DateTimeOffset e)=>throw Disabled(); public Uri PresignAccess(string k,DateTimeOffset e)=>throw Disabled(); public Task<StoredObjectInfo?> GetInfoAsync(string k,CancellationToken c)=>throw Disabled(); public Task<Stream> OpenReadAsync(string k,CancellationToken c)=>throw Disabled(); public Task DeleteAsync(string k,CancellationToken c)=>throw Disabled();
}
public sealed class S3DocumentStorage(IAmazonS3 s3, IOptions<DocumentStorageOptions> options) : IPrivateDocumentStorage
{
    private readonly DocumentStorageOptions config = options.Value;
    public string CreateOpaqueKey() => $"quarantine/{Guid.NewGuid():N}";
    public Uri PresignUpload(string key,string type,long length,DateTimeOffset expires) => new(s3.GetPreSignedURL(new GetPreSignedUrlRequest { BucketName=config.Bucket, Key=key, Verb=HttpVerb.PUT, Expires=expires.UtcDateTime, ContentType=type, ServerSideEncryptionMethod=ServerSideEncryptionMethod.AES256 }));
    public Uri PresignAccess(string key,DateTimeOffset expires) => new(s3.GetPreSignedURL(new GetPreSignedUrlRequest { BucketName=config.Bucket, Key=key, Verb=HttpVerb.GET, Expires=expires.UtcDateTime }));
    public async Task<StoredObjectInfo?> GetInfoAsync(string key,CancellationToken ct) { try { var x=await s3.GetObjectMetadataAsync(config.Bucket,key,ct); return new(x.ContentLength,x.Headers.ContentType); } catch(AmazonS3Exception e) when(e.StatusCode==System.Net.HttpStatusCode.NotFound){ return null; } }
    public async Task<Stream> OpenReadAsync(string key,CancellationToken ct) => (await s3.GetObjectAsync(config.Bucket,key,ct)).ResponseStream;
    public async Task DeleteAsync(string key,CancellationToken ct) => await s3.DeleteObjectAsync(config.Bucket,key,ct);
}
public sealed class ClamAvScanner(IOptions<DocumentStorageOptions> options) : IMalwareScanner
{
    public async Task<MalwareStatus> ScanAsync(Stream content,CancellationToken ct)
    {
        try { using var client=new TcpClient(); await client.ConnectAsync(options.Value.ClamAvHost,options.Value.ClamAvPort,ct); await using var net=client.GetStream(); await net.WriteAsync("zINSTREAM\0"u8.ToArray(),ct); var buffer=new byte[81920]; int read; while((read=await content.ReadAsync(buffer,ct))>0){ await net.WriteAsync(BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(read)),ct); await net.WriteAsync(buffer.AsMemory(0,read),ct); } await net.WriteAsync(new byte[4],ct); var resultBuffer=new byte[512]; read=await net.ReadAsync(resultBuffer,ct); var result=Encoding.UTF8.GetString(resultBuffer,0,read); return result.Contains(" OK",StringComparison.Ordinal)?MalwareStatus.Safe:result.Contains(" FOUND",StringComparison.Ordinal)?MalwareStatus.Unsafe:MalwareStatus.Indeterminate; } catch { return MalwareStatus.Indeterminate; }
    }
}
