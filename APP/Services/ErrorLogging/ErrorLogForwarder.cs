using System.Threading.Channels;
using DOMAIN.Entities.ErrorLogs;
using INFRASTRUCTURE.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace APP.Services.ErrorLogging;

/// <summary>Lets <see cref="ErrorLogWriter"/> wake the forwarder as soon as new entries are saved.</summary>
public sealed class ErrorLogForwardSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public void Notify() => _channel.Writer.TryWrite(true);

    /// <summary>Returns when notified or after <paramref name="timeout"/>, whichever comes first.</summary>
    public async Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timer = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timer.CancelAfter(timeout);
        try
        {
            await _channel.Reader.WaitToReadAsync(timer.Token);
            _channel.Reader.TryRead(out _);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { }
    }
}

/// <summary>
/// Sends stored error entries (ForwardedAt is null) to the central Oryx Monitor and marks them
/// once accepted, so nothing is lost while the monitor or the network is down. Runs only when
/// ErrorLogging:CollectorUrl and CollectorApiKey are set and entries are stored in the database.
/// </summary>
public sealed class ErrorLogForwarder(
    IServiceScopeFactory scopeFactory, CollectorClient client, ErrorLogForwardSignal signal,
    IOptions<ErrorLogOptions> options, ILogger<ErrorLogForwarder> logger) : BackgroundService
{
    public const int BatchSize = 200;
    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(5);

    private TimeSpan _backoff = TimeSpan.Zero;
    private bool _unauthorizedReported;

    public bool Enabled => options.Value.Enabled && options.Value.StoreInDatabase && client.Enabled;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Enabled) return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = await ForwardPendingAsync(stoppingToken);
                if (delay is { } wait) await Task.Delay(wait, stoppingToken);
                else await signal.WaitAsync(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Usually the local database being unavailable.
                using (ErrorLogSuppression.Begin())
                    logger.LogWarning(exception, "Forwarding error logs to the monitor failed; retrying shortly.");
                await Task.Delay(NextBackoff(null), stoppingToken).ContinueWith(_ => { });
            }
        }
    }

    /// <summary>
    /// Sends every waiting entry, oldest first. Returns null when all were sent (wait for new ones),
    /// or how long to wait before trying again when the monitor could not take them.
    /// </summary>
    public async Task<TimeSpan?> ForwardPendingAsync(CancellationToken cancellationToken)
    {
        using var _ = ErrorLogSuppression.Begin();
        while (true)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var batch = await db.ErrorLogs
                .Where(e => e.ForwardedAt == null)
                .OrderBy(e => e.OccurredAt)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
            if (batch.Count == 0)
            {
                _backoff = TimeSpan.Zero;
                return null;
            }

            var failure = await SendAsync(db, batch, cancellationToken);
            if (failure is { } response) return HandleFailure(response, batch.Count);

            _backoff = TimeSpan.Zero;
            _unauthorizedReported = false;
        }
    }

    /// <summary>Sends the entries, halving the batch when the monitor rejects it. Returns the failure that stopped it, if any.</summary>
    private async Task<CollectorResponse?> SendAsync(ApplicationDbContext db, List<ErrorLog> entries, CancellationToken cancellationToken)
    {
        var response = await client.SendErrorsAsync(entries, cancellationToken);
        switch (response.Outcome)
        {
            case CollectorOutcome.Accepted:
                await MarkForwardedAsync(db, entries, cancellationToken);
                return null;

            case CollectorOutcome.Rejected when entries.Count == 1:
                // Can never be accepted; skip it so it doesn't block everything behind it.
                logger.LogWarning("The monitor rejected error log entry {Id} ({Status}); it will not be sent again.",
                    entries[0].Id, response.Describe());
                await MarkForwardedAsync(db, entries, cancellationToken);
                return null;

            case CollectorOutcome.Rejected:
                var half = entries.Count / 2;
                return await SendAsync(db, entries.GetRange(0, half), cancellationToken)
                       ?? await SendAsync(db, entries.GetRange(half, entries.Count - half), cancellationToken);

            default:
                return response;
        }
    }

    private static async Task MarkForwardedAsync(ApplicationDbContext db, List<ErrorLog> entries, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in entries) entry.ForwardedAt = now;
        await db.SaveChangesAsync(cancellationToken);
    }

    private TimeSpan HandleFailure(CollectorResponse response, int pending)
    {
        if (response.Outcome == CollectorOutcome.Unauthorized)
        {
            if (!_unauthorizedReported)
                logger.LogWarning("The monitor refused this server's API key ({Status}); check ErrorLogging:CollectorApiKey. Retrying every 5 minutes.",
                    response.Describe());
            _unauthorizedReported = true;
            return MaxBackoff;
        }

        var delay = NextBackoff(response.RetryAfter);
        logger.LogWarning(response.Error, "The monitor could not take {Count} error log entries ({Status}); retrying in {Delay}.",
            pending, response.Describe(), delay);
        return delay;
    }

    private TimeSpan NextBackoff(TimeSpan? retryAfter)
    {
        _backoff = _backoff == TimeSpan.Zero ? MinBackoff : TimeSpan.FromTicks(Math.Min(_backoff.Ticks * 2, MaxBackoff.Ticks));
        return retryAfter is { } requested && requested > _backoff
            ? TimeSpan.FromTicks(Math.Min(requested.Ticks, MaxBackoff.Ticks))
            : _backoff;
    }
}

/// <summary>Tells the monitor this server is alive every ErrorLogging:HeartbeatIntervalMinutes.</summary>
public sealed class CollectorHeartbeat(CollectorClient client, IOptions<ErrorLogOptions> options, ILogger<CollectorHeartbeat> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var minutes = options.Value.HeartbeatIntervalMinutes;
        if (!client.Enabled || minutes <= 0) return;

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));
        do
        {
            using var _ = ErrorLogSuppression.Begin();
            try
            {
                var response = await client.SendHeartbeatAsync(stoppingToken);
                if (response.Outcome != CollectorOutcome.Accepted)
                    logger.LogDebug("Monitor heartbeat failed ({Status}).", response.Describe());
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Monitor heartbeat failed.");
            }
        } while (await WaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try { return await timer.WaitForNextTickAsync(stoppingToken); }
        catch (OperationCanceledException) { return false; }
    }
}
