using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPark.Application.Options;
using NPark.Domain.Entities;

namespace NPark.Infrastructure.Services.Lpr;

internal sealed class LprCaptureArchiveService(IServiceScopeFactory scopes, IOptions<LprCaptureOptions> options,
    TimeProvider time, ILogger<LprCaptureArchiveService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        var settings = options.Value;
        if (!settings.Enabled)
            return;

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(settings.ArchiveCheckIntervalMinutes));
        do
        {
            try
            {
                var now = time.GetUtcNow().UtcDateTime;
                var cutoff = now.AddDays(-settings.RetentionDays);
                while (await ArchiveBatchAsync(now, cutoff, settings.ArchiveBatchSize, ct) == settings.ArchiveBatchSize)
                    ct.ThrowIfCancellationRequested();
            }
            catch (Exception error) when (!ct.IsCancellationRequested)
            {
                logger.LogWarning(error, "LPR archive batch failed; active captures will be reconsidered on the next pass");
            }
        } while (await timer.WaitForNextTickAsync(ct));
    }

    internal async Task<int> ArchiveBatchAsync(DateTime now, DateTime cutoff, int batchSize, CancellationToken ct)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<NParkDBContext>();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var captures = await db.LprCaptures.OrderBy(x => x.CapturedAtUtc).ThenBy(x => x.Id)
            .Where(x => x.CapturedAtUtc < cutoff).Take(batchSize).ToListAsync(ct);
        if (captures.Count == 0)
            return 0;

        db.LprCapturesArchive.AddRange(captures.Select(capture => LprCaptureArchive.FromCapture(capture, now)));
        db.LprCaptures.RemoveRange(captures);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        logger.LogInformation("Archived {CaptureCount} LPR captures older than {CaptureCutoffUtc}", captures.Count, cutoff);
        return captures.Count;
    }
}
