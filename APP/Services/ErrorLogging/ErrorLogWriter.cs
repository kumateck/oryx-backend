using DOMAIN.Entities.ErrorLogs;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace APP.Services.ErrorLogging;

/// <summary>Saves queued errors in batches and deletes entries past the retention period.</summary>
public class ErrorLogWriter(
    ErrorLogQueue queue, IServiceScopeFactory scopeFactory, IOptions<ErrorLogOptions> options, ILogger<ErrorLogWriter> logger,
    CollectorClient collector, ErrorLogForwardSignal forwardSignal)
    : BackgroundService
{
    private const int BatchSize = 200;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;

        var nextCleanup = DateTime.UtcNow.AddMinutes(1);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await WaitForWorkAsync(nextCleanup - DateTime.UtcNow, stoppingToken);
                await FlushAsync(stoppingToken);

                if (DateTime.UtcNow >= nextCleanup)
                {
                    await DeleteExpiredAsync(stoppingToken);
                    nextCleanup = DateTime.UtcNow + CleanupInterval;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Error log writer failed; retrying shortly.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ContinueWith(_ => { });
            }
        }

        // Keep what was captured just before shutdown.
        using var shutdown = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await FlushAsync(shutdown.Token); } catch { /* best effort */ }
    }

    private async Task WaitForWorkAsync(TimeSpan maxWait, CancellationToken stoppingToken)
    {
        if (maxWait <= TimeSpan.Zero) return;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(maxWait);
        try { await queue.Reader.WaitToReadAsync(timeout.Token); }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested) { }
    }

    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var batch = new List<ErrorLog>(BatchSize);
            while (batch.Count < BatchSize && queue.Reader.TryRead(out var entry)) batch.Add(entry);
            if (batch.Count == 0) return;
            await SaveAsync(batch, cancellationToken);
        }
    }

    private async Task SaveAsync(List<ErrorLog> batch, CancellationToken cancellationToken)
    {
        if (options.Value.StoreInDatabase)
        {
            // ErrorLogForwarder sends stored entries on to the monitor, so they survive an outage there.
            await WithRetryAsync("database", batch.Count, async () =>
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.ErrorLogs.AddRange(batch);
                await db.SaveChangesAsync(cancellationToken);
            }, cancellationToken);
            forwardSignal.Notify();
        }
        else if (collector.Enabled)
        {
            // Nothing is kept locally, so this is best effort: a long monitor outage loses these entries.
            await WithRetryAsync("the monitor", batch.Count, async () =>
            {
                var response = await collector.SendErrorsAsync(batch, cancellationToken);
                if (response.Outcome == CollectorOutcome.Rejected)
                    logger.LogWarning("The monitor rejected {Count} error log entries ({Status}).", batch.Count, response.Describe());
                else if (response.Outcome != CollectorOutcome.Accepted)
                    throw new HttpRequestException($"Monitor returned {response.Describe()}.", response.Error);
            }, cancellationToken);
        }
    }

    private async Task WithRetryAsync(string store, int count, Func<Task> write, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var _ = ErrorLogSuppression.Begin();
            try
            {
                await write();
                return;
            }
            catch (Exception exception) when (attempt < 3 && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(exception, "Saving {Count} error log entries to {Store} failed (attempt {Attempt}).", count, store, attempt);
                await Task.Delay(TimeSpan.FromSeconds(2 * attempt), cancellationToken);
            }
            catch (Exception exception)
            {
                // They are still in the console output and Sentry; don't block newer errors behind these.
                logger.LogWarning(exception, "Dropped {Count} error log entries that could not be saved to {Store}.", count, store);
                return;
            }
        }
    }

    private async Task DeleteExpiredAsync(CancellationToken cancellationToken)
    {
        var days = options.Value.RetentionDays;
        if (days <= 0 || !options.Value.StoreInDatabase) return;

        using var _ = ErrorLogSuppression.Begin();
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var deleted = await db.ErrorLogs.Where(e => e.OccurredAt < cutoff).ExecuteDeleteAsync(cancellationToken);
        if (deleted > 0) logger.LogInformation("Deleted {Count} error log entries older than {Days} days.", deleted, days);
    }
}
