using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NoorPath.Documents;
namespace NoorPath.Documents.Infrastructure;

public sealed class DocumentRetentionWorker(IServiceScopeFactory scopes, TimeProvider clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(6), clock);
        do { await DeleteDue(stoppingToken); } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
    private async Task DeleteDue(CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<DocumentsDbContext>(); var storage = scope.ServiceProvider.GetRequiredService<IPrivateDocumentStorage>(); var now = clock.GetUtcNow(); var due = await db.Submissions.Where(x => x.DeleteAfterUtc <= now && x.HoldAtUtc == null && x.State != SubmissionState.Deleted).ToArrayAsync(ct);
        foreach (var item in due) { await storage.DeleteAsync(item.ObjectKey, ct); item.State = SubmissionState.Deleted; item.ObjectKey = $"deleted:{item.Id:N}"; db.Audit.Add(new() { Id = Guid.NewGuid(), SubmissionId = item.Id, Action = "object_deleted", ActorId = "system", Purpose = "retention schedule", OccurredAtUtc = now }); }
        await db.SaveChangesAsync(ct);
        await db.Audit.Where(x => x.OccurredAtUtc < now.AddYears(-1)).ExecuteDeleteAsync(ct);
    }
}
